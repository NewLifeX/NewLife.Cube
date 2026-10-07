#!/usr/bin/env bash
# 一键启用 Cube 仓的 git hooks（把 core.hooksPath 指向仓库内 .githooks）。
# 新克隆的人执行一次即可；存量项目重复执行幂等、用于"确保生效"。
# git 不会在 clone 后自动启用 hook（安全设计），所以这一步必须有人/有脚本触发。
set -e
root="$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel)"
for h in "$root"/.githooks/*; do [ -f "$h" ] && chmod +x "$h"; done
git -C "$root" config core.hooksPath .githooks
echo "已启用 git hooks: core.hooksPath=.githooks  →  $root/.githooks"
echo "自检: git config core.hooksPath  应输出  .githooks"
