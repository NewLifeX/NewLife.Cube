/**
 * 从已安装的 element-plus 中提取全部合法的 --el-* CSS 变量名，
 * 生成 scripts/element-tokens.json 供 check-theme-tokens.mjs 相对路径导入使用。
 *
 * 这样 element token 列表不会硬编码在脚本里，升级 element-plus 后只需重跑本脚本即可同步：
 *   npm run check:theme:gen
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const webRoot = path.resolve(scriptDir, '..');

/**
 * 从起始目录开始向上搜索，找到第一个包含 node_modules/element-plus 的目录。
 * 适配 pnpm monorepo 场景：pnpm install 在根目录执行，依赖可能被 hoist 到上方。
 * @param {string} fromDir 起始搜索目录
 * @returns {string|null} element-plus 目录的绝对路径，或 null
 */
function findElementPlus(fromDir) {
  let current = path.resolve(fromDir);
  const root = path.parse(current).root;
  while (true) {
    const candidate = path.join(current, 'node_modules', 'element-plus');
    if (fs.existsSync(candidate)) return candidate;
    if (current === root) break;
    current = path.dirname(current);
  }
  return null;
}

// 先查 webRoot 本级，再逐级向上搜索（适配 pnpm hoist）
let epDir = path.join(webRoot, 'node_modules', 'element-plus');
if (!fs.existsSync(epDir)) {
  epDir = findElementPlus(webRoot);
}

if (!epDir) {
  console.log(`[gen-element-tokens] 未找到 element-plus（已从 ${webRoot} 向上搜索至根目录），跳过。`);
  process.exit(0);
}

console.log(`[gen-element-tokens] 使用 element-plus 路径: ${epDir}`);

const tokens = new Set();

function walk(dir) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      walk(full);
      continue;
    }
    if (!/\.css$/.test(entry.name)) continue;
    const text = fs.readFileSync(full, 'utf8');
    const re = /--el-[\w-]+/g;
    let m;
    while ((m = re.exec(text))) tokens.add(m[0]);
  }
}

// theme-chalk 内含各组件及 dark 主题，dist 内含打包后的全量 css
walk(path.join(epDir, 'theme-chalk'));
walk(path.join(epDir, 'dist'));

const outFile = path.join(scriptDir, 'element-tokens.json');
fs.writeFileSync(outFile, JSON.stringify([...tokens].sort(), null, 2) + '\n', 'utf8');
console.log(`[gen-element-tokens] 已生成 ${outFile}（共 ${tokens.size} 个合法 --el- token）`);
