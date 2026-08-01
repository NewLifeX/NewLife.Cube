/**
 * 修补 @lezer/php@1.0.5 中 readonly 变量名与 Rollup 4.x 解析冲突的问题。
 * 
 * readonly 是 TypeScript 关键字，Rollup 4.46+ 的 AST 解析器在处理 CJS→ESM
 * 转换时会因该命名触发 "Identifier 'readonly' has already been declared" 错误。
 * 
 * 修复：将 readonly 重命名为 _readonly，并更新 keywordMap 中的引用。
 */
const fs = require('fs');
const path = require('path');

// 在 pnpm monorepo 中，包位于 node_modules/.pnpm/ 下
const rootDir = path.resolve(__dirname, '..');

function findPackageDir() {
  const pnpmDir = path.join(rootDir, 'node_modules', '.pnpm');
  if (!fs.existsSync(pnpmDir)) return null;
  
  const entries = fs.readdirSync(pnpmDir);
  const target = entries.find(e => e.startsWith('@lezer+php@'));
  if (!target) return null;
  
  return path.join(pnpmDir, target, 'node_modules', '@lezer', 'php', 'dist');
}

function patchFile(filePath) {
  if (!fs.existsSync(filePath)) {
    console.log(`  [SKIP] ${filePath} not found`);
    return false;
  }
  
  let content = fs.readFileSync(filePath, 'utf8');
  
  // 检查是否已修补
  if (content.includes('_readonly')) {
    console.log(`  [OK] Already patched: ${filePath}`);
    return true;
  }
  
  // 替换 const 声明
  const constPattern = /(  or = 50,\n  print = 51,\n)  readonly = 52,/;
  content = content.replace(constPattern, '$1  _readonly = 52,');
  
  // 替换 keywordMap 引用
  const mapPattern = /(  or,\n  print,\n)  readonly,/;
  content = content.replace(mapPattern, '$1  readonly: _readonly,');
  
  fs.writeFileSync(filePath, content, 'utf8');
  console.log(`  [PATCHED] ${filePath}`);
  return true;
}

const distDir = findPackageDir();
if (!distDir) {
  console.error('[ERROR] Cannot find @lezer/php in node_modules/.pnpm/');
  process.exit(1);
}

console.log(`Patching @lezer/php in: ${distDir}`);
patchFile(path.join(distDir, 'index.js'));
patchFile(path.join(distDir, 'index.cjs'));
console.log('Done.');
