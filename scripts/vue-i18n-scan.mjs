#!/usr/bin/env node
/**
 * Vue 前端硬编码中文盘点（IMP-1 进度度量）
 *
 * 目的：把「页面文案还没接 t()」这件事变成可度量的数字，便于分批推进与验收。
 * 统计口径：源码字符串字面量与模板文本节点中的中文；**注释不计**（注释里的中文是文档，不是文案）。
 *
 * 用法：
 *   node scripts/vue-i18n-scan.mjs            # 汇总
 *   node scripts/vue-i18n-scan.mjs --detail   # 额外列出每个文件条数
 *   node scripts/vue-i18n-scan.mjs --max=0    # 门禁：超过阈值即以非零码退出（CI 用）
 *
 * 允许清单（`baseline`）：中文正则/后端数据匹配等「必须保留中文」的字面量，
 * 按「文件 + 内容」精确登记；命中清单的条目不计入总数，避免门禁被噪声卡住。
 */
import fs from 'node:fs';
import path from 'node:path';

/** 必须保留中文的条目（正则/后端文案匹配、语言自称名）：文件后缀 + 字面量内容 */
const baseline = [
  { file: 'core/components/CubeTable/cubeTable.logic.ts', text: /毫秒|小时|分|十分之一|百分之一|千分之一|万分之一/ },
  { file: 'core/engine/logic.ts', text: /共|成功/ },
  { file: 'core/components/SideMenu.vue', text: '工作台' },
  { file: 'core/i18n/index.ts', text: /中文|日本語/ },
];

const isAllowed = (file, text) =>
  baseline.some((rule) => file.endsWith(rule.file) && (rule.text instanceof RegExp ? rule.text.test(text) : rule.text === text));

const roots = ['NewLife.Cube.Vue/web/core', 'NewLife.Cube.Vue/web/apps', 'NewLife.Cube.Vue/web/ct'];
const skip = /node_modules|__tests__|__mocks__|\.spec\.|\.test\.|\.story\.|locales|\/i18n\/|\.d\.ts$/;
const cjk = /[\u4e00-\u9fa5]/;

function walk(dir, out = []) {
  if (!fs.existsSync(dir)) return out;
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (!skip.test(full)) walk(full, out);
    } else if (/\.(vue|ts)$/.test(entry.name) && !skip.test(full)) {
      out.push(full);
    }
  }
  return out;
}

/** 去掉注释，避免把注释里的中文算作文案 */
function stripComments(text) {
  return text
    .replace(/\/\*[\s\S]*?\*\//g, '')
    .replace(/<!--[\s\S]*?-->/g, '')
    .split('\n')
    .map((line) => line.replace(/(^|[^:])\/\/.*$/, '$1'))
    .join('\n');
}

function collect(file) {
  const text = stripComments(fs.readFileSync(file, 'utf8'));
  const hits = [];
  // 引号内中文
  for (const m of text.matchAll(/(['"`])((?:\\.|(?!\1)[^\\])*?)\1/g)) {
    if (cjk.test(m[2])) hits.push(m[2].trim());
  }
  // 模板文本节点
  for (const m of text.matchAll(/>([^<>{}]+)</g)) {
    if (cjk.test(m[1])) hits.push(m[1].trim());
  }
  return hits;
}

const rows = [];
for (const root of roots) {
  for (const file of walk(root)) {
    const rel = file.replace(/\\/g, '/').replace('NewLife.Cube.Vue/web/', '');
    const hits = collect(file).filter((text) => !isAllowed(rel, text));
    if (hits.length > 0) {
      rows.push({ file: rel, count: hits.length });
    }
  }
}

/** 按目录归组：core 与 apps/<应用> 分开看，便于按批推进 */
function groupOf(file) {
  if (file.startsWith('core/pages/')) return 'core/pages';
  if (file.startsWith('core/layouts/')) return 'core/layouts';
  if (file.startsWith('core/views/')) return 'core/views';
  if (file.startsWith('core/components/')) return 'core/components';
  if (file.startsWith('core/')) return 'core/（其他）';
  const m = file.match(/^apps\/([^/]+)\//);
  return m ? `apps/${m[1]}` : file;
}

const groups = new Map();
for (const row of rows) {
  const key = groupOf(row.file);
  const item = groups.get(key) || { files: 0, literals: 0 };
  item.files++;
  item.literals += row.count;
  groups.set(key, item);
}

const totalFiles = rows.length;
const totalLiterals = rows.reduce((sum, r) => sum + r.count, 0);

console.log(`硬编码中文文案：${totalFiles} 个文件 / ${totalLiterals} 条（不含注释）`);
console.log('');
console.log('分组统计：');
for (const [key, item] of [...groups.entries()].sort((a, b) => b[1].literals - a[1].literals)) {
  console.log(`  ${String(item.literals).padStart(5)}  ${String(item.files).padStart(3)} 文件  ${key}`);
}

if (process.argv.includes('--detail')) {
  console.log('');
  console.log('逐文件（前 40）：');
  for (const row of rows.sort((a, b) => b.count - a.count).slice(0, 40)) {
    console.log(`  ${String(row.count).padStart(5)}  ${row.file}`);
  }
}

// ── 门禁：--max=N 时超过阈值以非零码退出（CI 防回退） ──
const maxArg = process.argv.find((arg) => arg.startsWith('--max='));
if (maxArg) {
  const max = Number(maxArg.split('=')[1]);
  if (Number.isFinite(max) && totalLiterals > max) {
    console.error('');
    console.error(`✗ 硬编码中文文案 ${totalLiterals} 条，超出阀值 ${max} 条`);
    process.exit(1);
  }
  console.log('');
  console.log(`✓ 硬编码中文文案 ${totalLiterals} 条，未超阀值 ${max} 条`);
}
