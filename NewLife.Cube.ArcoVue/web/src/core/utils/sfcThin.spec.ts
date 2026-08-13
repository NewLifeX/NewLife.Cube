import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

/**
 * SFC 构薄机械门禁（OSC-260813c3e9）：
 * 1. 递归收集 src 下全部 .vue（含 apps/_demo）；
 * 2. 无 <script> 的 .vue 视为通过；
 * 3. ALLOWLIST 内的路径跳过断言（正在抽离中的文件）；
 * 4. 其余 script 禁止业务生命周期 / cubeApi 调用；
 * 5. ALLOWLIST 中的路径必须真实存在于磁盘（防止写错路径）。
 * 抽离收口（T44）后 ALLOWLIST 必须为 []。
 */

const srcRoot = resolve(fileURLToPath(import.meta.url), '..', '..', '..');

/** 抽离中暂存豁免的 .vue（posix 路径，相对 src/）；每完成一个 T-b 删除一行，终态 [] */
const ALLOWLIST: string[] = [];

/** 禁止 token：业务生命周期钩子与 cubeApi 调用 */
const FORBIDDEN = /\b(watch|onMounted|onBeforeUnmount|onUnmounted)\s*\(|cubeApi\./;

function collectVueFiles(dir: string): string[] {
  const out: string[] = [];
  for (const name of readdirSync(dir)) {
    const full = join(dir, name);
    if (statSync(full).isDirectory()) out.push(...collectVueFiles(full));
    else if (name.endsWith('.vue')) out.push(full);
  }
  return out;
}

function toPosix(p: string): string {
  return relative(srcRoot, p).split(sep).join('/');
}

const vueFiles = collectVueFiles(srcRoot).map(toPosix).sort();

describe('sfcThin：SFC 不内嵌业务 TS', () => {
  it('ALLOWLIST 中的路径必须存在于磁盘', () => {
    for (const p of ALLOWLIST) {
      expect(vueFiles, `ALLOWLIST 路径不存在：${p}`).toContain(p);
    }
  });

  for (const rel of vueFiles) {
    const inList = ALLOWLIST.includes(rel);
    it(`scan ${rel}`, () => {
      const raw = readFileSync(join(srcRoot, rel), 'utf8');
      const m = raw.match(/<script(?:\s[^>]*)?>([\s\S]*?)<\/script>/);
      if (!m) return; // 无 script 视为通过
      if (inList) return; // 抽离中的文件豁免
      expect(
        m[1],
        `${rel} 禁止在 .vue 内写 watch/onMounted/onBeforeUnmount/onUnmounted 或调用 cubeApi`,
      ).not.toMatch(FORBIDDEN);
    });
  }
});
