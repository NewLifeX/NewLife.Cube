// 发布脚本：为 packages/ 下所有 @newlifex/* 公共包设置 -beta.<时间戳> 预发布版本
// 用法：node scripts/set-beta-version.mjs
// 说明：5 个公共包锁步发布，统一去掉已有预发布后缀后追加 -beta.YYYYMMDDHHMMSS，
//       供 CI 在 master 分支触发时调用（publish-npm.yml）。
import { readFileSync, writeFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';

const dir = join(process.cwd(), 'packages');

const d = new Date();
const ts = [
  d.getFullYear(),
  String(d.getMonth() + 1).padStart(2, '0'),
  String(d.getDate()).padStart(2, '0'),
  String(d.getHours()).padStart(2, '0'),
  String(d.getMinutes()).padStart(2, '0'),
  String(d.getSeconds()).padStart(2, '0'),
].join('');

const changed = [];
for (const name of readdirSync(dir)) {
  const file = join(dir, name, 'package.json');
  let pkg;
  try {
    pkg = JSON.parse(readFileSync(file, 'utf8'));
  } catch {
    continue;
  }
  // 仅处理 @newlifex/* 公共库，跳过 private 包
  if (!pkg.name?.startsWith('@newlifex/') || pkg.private) continue;

  const base = pkg.version.split('-')[0]; // 去掉已有预发布后缀，保留基础版本
  const beta = `${base}-beta.${ts}`;
  pkg.version = beta;
  writeFileSync(file, JSON.stringify(pkg, null, 2) + '\n', 'utf8');
  changed.push(`${pkg.name}@${beta}`);
}

if (changed.length > 0) {
  console.log(changed.join('\n'));
} else {
  console.error('未找到需要设置 beta 版本的 @newlifex 包');
  process.exitCode = 1;
}
