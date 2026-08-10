/**
 * 主题 token 检查入口脚本（一次完成两步）
 *
 * 依次执行：
 *   1. 生成 Element token 列表（scripts/gen-element-tokens.mjs）
 *   2. 执行主题 token 检查（scripts/check-theme-tokens.mjs）
 *
 * 任一一步失败都会以相同退出码退出，方便作为 postinstall / CI 门禁使用。
 * 用法：node scripts/theme-check.mjs   （或 npm run check:theme:refresh）
 */
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDir = path.dirname(fileURLToPath(import.meta.url));

function run(step, file) {
  console.log(`\n[theme-check] === ${step} ===`);
  const res = spawnSync(process.execPath, [path.join(scriptDir, file)], {
    stdio: 'inherit',
  });
  if (res.status !== 0) {
    console.error(`[theme-check] 步骤失败，终止：${file} (exit ${res.status ?? 'unknown'})`);
    process.exit(res.status ?? 1);
  }
}

run('生成 Element token 列表', 'gen-element-tokens.mjs');
run('执行主题 token 检查', 'check-theme-tokens.mjs');

console.log('\n[theme-check] 全部完成 ✅');
