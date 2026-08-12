/**
 * 主题 token 检查（check:theme）
 *
 * 规则：
 *  1. 扫描 core 下所有 vue/ts/scss/css 文件，提取每一个 CSS 自定义属性（用法 var(--x) 与定义 --x:）。
 *  2. 以 --el- 开头的 token：必须是 element-plus 真实存在的 token（相对导入 scripts/element-tokens.json 校验），
 *     否则视为拼写错误或引用了不存在的变量 —— 报错。
 *  3. 非 --el- 开头的自定义 token：必须登记在 scripts/custom-tokens-allow.json 白名单中（命名空间或独立 token），
 *     否则视为未审批的随意 token —— 报错，从而能及时发现。
 *  4. 保留“硬编码颜色”补充检查：16 进制（3/4/6/8 位）、rgb(a)、以及 CSS 命名颜色（red/blue…）
 *     只要出现在代码里（包括 var(--x, #fff) / var(--x, red) 兜底色值、color-mix(..., #fff) 内）即报警。
 *     仅放行明确合法的来源：transparent / currentColor / <template（Vue 模板防误伤）。
 *
 * element token 列表不硬编码在脚本中，而是通过相对路径从 element-tokens.json 导入；
 * 升级 element-plus 后运行 `npm run check:theme:gen` 重新生成即可。
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const webRoot = path.resolve(scriptDir, '..');

// ---- 1. Element Plus token 列表（相对路径导入，不硬编码） ----
const ELEMENT_TOKENS_FILE = path.join(scriptDir, 'element-tokens.json');
let elementTokens;
try {
  elementTokens = new Set(JSON.parse(fs.readFileSync(ELEMENT_TOKENS_FILE, 'utf8')));
} catch (err) {
  console.error(`[check-theme-tokens] 无法读取 Element token 列表: ${ELEMENT_TOKENS_FILE}`);
  console.error(`请先运行: npm run check:theme:gen  (${err.message})`);
  process.exit(1);
}

// ---- 2. 项目自定义 token 白名单（相对路径导入） ----
const ALLOW_FILE = path.join(scriptDir, 'custom-tokens-allow.json');
let allowCfg;
try {
  allowCfg = JSON.parse(fs.readFileSync(ALLOW_FILE, 'utf8'));
} catch (err) {
  console.error(`[check-theme-tokens] 无法读取自定义 token 白名单: ${ALLOW_FILE} (${err.message})`);
  process.exit(1);
}
const allowedPrefixes = Array.isArray(allowCfg.prefixes) ? allowCfg.prefixes : [];
const allowedTokens = new Set(Array.isArray(allowCfg.tokens) ? allowCfg.tokens : []);
function isAllowedCustom(token) {
  if (allowedTokens.has(token)) return true;
  return allowedPrefixes.some((p) => token.startsWith(p));
}

// ---- 3. 扫描目标与忽略文件 ----
const root = process.cwd();
// 支持可选参数：node check-theme-tokens.mjs <path> 仅检查指定目录/文件（用于反模式演示）
const targets = process.argv[2]
  ? [path.resolve(process.argv[2])]
  : [path.join(root, 'core')];
const ignore = [
  /core[\\/]themes[\\/].+\.css$/,
  /core[\\/]__tests__[\\/].+\.(spec|test)\.ts$/,
  /core[\\/]pages[\\/]LoginPage\.vue$/,
  /core[\\/]configure[\\/]defaultConfig[\\/]index\.ts$/,
  /core[\\/]initApp\.ts$/,
];

// 解析 CSS 自定义属性：用法 var(--x) 与定义 --x:
const useRe = /var\(\s*(--[A-Za-z][\w-]*)\s*[,)]/g;
const defRe = /(?:^|[\s;{])(--[A-Za-z][\w-]*)\s*:/g;

// 硬编码颜色补充检查
// 1) 16 进制（3/4/6/8 位）与 rgb(a)
//    不以分号结尾也要能命中（color: #ff0000; 是最高频写法）；其后紧跟非 hex 字符即视为完整值。
const hexOrRgb = /(?<!&)#[0-9A-Fa-f]{3,8}(?![0-9A-Fa-f])/;
const rgbRe = /rgba?\(/;
// 2) CSS 命名颜色（red/blue…）。仅匹配小写、且前后不能是 单词字符或连字符，
//    以避免误伤 white-space、card-red、--el-color-red 等标识符。
//    transparent / currentColor 作为关键字单独放行，不列入此集合。
const NAMED_COLORS = [
  'aliceblue', 'antiquewhite', 'aquamarine', 'azure', 'beige', 'bisque',
  'blanchedalmond', 'blueviolet', 'burlywood', 'cadetblue', 'chartreuse', 'cornflowerblue',
  'cornsilk', 'darkgoldenrod', 'darkorchid', 'darkslategrey', 'deepskyblue', 'firebrick',
  'floralwhite', 'forestgreen', 'ghostwhite', 'goldenrod', 'greenyellow', 'indianred',
  'lavenderblush', 'lawngreen', 'lemonchiffon', 'lightgoldenrodyellow', 'lightslategrey',
  'mintcream', 'mistyrose', 'navajowhite', 'olivedrab', 'orangered', 'palegoldenrod',
  'palevioletred', 'papayawhip', 'peachpuff', 'powderblue', 'rebeccapurple', 'rosybrown',
  'royalblue', 'saddlebrown', 'sandybrown', 'seashell', 'slategrey', 'steelblue',
  'whitesmoke', 'yellowgreen',
  'aqua', 'black', 'blue', 'brown', 'chocolate', 'coral', 'crimson', 'cyan', 'gold',
  'gray', 'green', 'grey', 'indigo', 'ivory', 'khaki', 'linen', 'magenta', 'maroon',
  'mediumblue', 'mediumorchid', 'mediumpurple', 'mediumseagreen', 'mediumslateblue',
  'mediumspringgreen', 'mediumturquoise', 'mediumvioletred', 'midnightblue', 'oldlace',
  'olive', 'orange', 'orchid', 'peru', 'pink', 'plum', 'purple', 'red', 'salmon',
  'seagreen', 'sienna', 'silver', 'skyblue', 'slateblue', 'slategray', 'snow', 'tan',
  'teal', 'thistle', 'tomato', 'turquoise', 'violet', 'wheat', 'white', 'yellow',
  'darkblue', 'darkcyan', 'darkgray', 'darkgreen', 'darkgrey', 'darkkhaki', 'darkmagenta',
  'darkolivegreen', 'darkorange', 'darksalmon', 'darkseagreen', 'darkslateblue',
  'darkslategray', 'darkturquoise', 'darkviolet', 'deeppink', 'dimgray', 'dimgrey',
  'dodgerblue', 'fuchsia', 'gainsboro', 'honeydew', 'hotpink', 'lavender', 'lightblue',
  'lightcoral', 'lightcyan', 'lightgray', 'lightgreen', 'lightgrey', 'lightpink',
  'lightsalmon', 'lightseagreen', 'lightskyblue', 'lightsteelblue', 'lightyellow',
  'lime', 'limegreen', 'mediumaquamarine', 'moccasin', 'navy', 'springgreen',
];
// 按长度降序排列，避免短色名误匹配长色名前缀（冗余保护，主要靠下面的前后顾）
const namedColorRe = new RegExp('(?<![\\w-])(' + [...NAMED_COLORS].sort((a, b) => b.length - a.length).join('|') + ')(?![\\w-])');
// 放行清单：仅保留明确合法的来源。
// 不再整体放行 var(--el-...) / color-mix(...)，这样它们内部的硬编码兜底色值
// （var(--x, #fff)、var(--x, red)、color-mix(..., #fff)）也能被捕获。
const allowPatterns = [
  /transparent/,
  /currentColor/,
  /<template/,
];

const failures = [];

function checkToken(token, rel, lineNo, line) {
  if (token.startsWith('--el-')) {
    if (!elementTokens.has(token)) {
      failures.push(`${rel}:${lineNo}: 未知的 Element token -> ${token}  | ${line.trim()}`);
    }
  } else if (!isAllowedCustom(token)) {
    failures.push(`${rel}:${lineNo}: 未授权的自定义 token -> ${token}  | ${line.trim()}`);
  }
}

function processFile(full) {
  if (!/\.(vue|ts|scss|css)$/.test(full)) return;
  if (ignore.some((p) => p.test(full))) return;
  const text = fs.readFileSync(full, 'utf8');
  const rel = path.relative(root, full);
  const isVue = /\.vue$/.test(full);
  const isStyleFile = /\.(css|scss)$/.test(full);
  // .vue 仅在 <style> 块内检查硬编码颜色，避免误伤 <script> 里的注释 / 类型联合 / 字符串字面量（如 'blue'）
  let inStyle = false;
  const lines = text.split(/\r?\n/);
  lines.forEach((line, i) => {
    const lineNo = i + 1;
    if (isVue) {
      if (/<style[\s>]/.test(line)) inStyle = true;
      if (/<\/style>/.test(line)) inStyle = false;
    }
    const seen = new Set();
    let m;
    // 定义：--x:
    while ((m = defRe.exec(line))) {
      const tk = m[1];
      if (!seen.has(tk)) {
        seen.add(tk);
        checkToken(tk, rel, lineNo, line);
      }
    }
    // 用法：var(--x)
    while ((m = useRe.exec(line))) {
      const tk = m[1];
      if (!seen.has(tk)) {
        seen.add(tk);
        checkToken(tk, rel, lineNo, line);
      }
    }
    // 硬编码颜色（补充检查：16 进制 / rgb(a) / 命名颜色）
    // 仅 .css/.scss 全文 或 .vue 的 <style> 块内检查，降低误报
    const inColorContext = isStyleFile || (isVue && inStyle);
    if (inColorContext) {
      // 先剔除 // 与 /* */ 注释，避免误伤 SCSS 注释 / 块注释里的颜色词
      const checkLine = line.replace(/\/\/.*$/, '').replace(/\/\*[\s\S]*?\*\//g, '');
      if (
        (hexOrRgb.test(checkLine) || rgbRe.test(checkLine) || namedColorRe.test(checkLine)) &&
        !allowPatterns.some((p) => p.test(checkLine))
      ) {
        failures.push(`${rel}:${lineNo}: 硬编码颜色 -> ${line.trim()}`);
      }
    }
  });
}

function walk(dir) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      walk(full);
    } else {
      processFile(full);
    }
  }
}

for (const d of targets) {
  if (fs.existsSync(d)) {
    if (fs.statSync(d).isFile()) {
      processFile(d);
    } else {
      walk(d);
    }
  } else {
    console.warn(`[check-theme-tokens] 目标不存在，已跳过: ${d}`);
  }
}

if (failures.length) {
  console.error(`主题 token 检查未通过，发现 ${failures.length} 处问题：`);
  console.error(failures.join('\n'));
  process.exit(1);
}

console.log(
  `Theme token check: OK（已校验 ${elementTokens.size} 个 Element token；自定义 token 白名单 ${allowedPrefixes.length} 个命名空间 + ${allowedTokens.size} 个独立 token）`,
);
