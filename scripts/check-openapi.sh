#!/usr/bin/env bash
# OpenAPI 契约检测（纯 shell，不依赖 node/jq，只用 git-bash 自带的 awk/grep/sed/sort/comm）。
#
# 原理：漏写 [Http*] 动词的 action 会被 .NET 内建 OpenAPI 生成器【静默丢弃】→ 对应 path/方法从 openapi.json 消失，
#       dotnet build 本身 0 错误不报。故把"基线"与"生成结果"逐 (path, HTTP方法) 配对比对，任一对消失即判失败。
# 提取：按 .NET 内建生成器稳定的 2 空格缩进，用 awk 状态机在 "paths" 段内识别
#       path 键（以 / 开头）与其下一层缩进的 HTTP 方法键(get/put/post/...)，产出 "path|method" 集合。
# 覆盖：既抓"整条 path 消失"，也抓"path 还在但少了某个方法"。
#
# 用法:
#   bash check-openapi.sh                    # 比对(默认路径,需先编译出 obj/openapi)
#   bash check-openapi.sh --build            # 先 dotnet build CubeDemo 产 json 再比对
#   bash check-openapi.sh --update-baseline  # 用生成结果覆盖基线(确认无误后刷新)
#   bash check-openapi.sh <BASE_DIR> <GEN_DIR>   # 显式路径(供 CI / MSBuild target 调用)
# 覆盖路径 env: BASE_DIR / GEN_DIR
set -euo pipefail

SELF="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(git -C "$SELF" rev-parse --show-toplevel)"
CUBEDEMO="$ROOT/CubeDemo"
BASE="${BASE_DIR:-$CUBEDEMO/contracts/openapi}"
GEN="${GEN_DIR:-$CUBEDEMO/obj/openapi}"

MODE="check"
POS=()
for a in "$@"; do
  case "$a" in
    --build)           MODE="build" ;;
    --update-baseline) MODE="update" ;;
    *)                 POS+=("$a") ;;
  esac
done
if [ "${#POS[@]}" -ge 2 ]; then BASE="${POS[0]}"; GEN="${POS[1]}"; fi

if [ "$MODE" = "build" ]; then
  echo "==> dotnet build CubeDemo（编译期生成 openapi json）"
  dotnet build "$CUBEDEMO/CubeDemo.csproj" -c Debug -clp:ErrorsOnly
fi

if [ "$MODE" = "update" ]; then
  [ -d "$GEN" ] || { echo "生成目录不存在: $GEN（先 --build 或手动编译）" >&2; exit 2; }
  mkdir -p "$BASE"
  cp -f "$GEN"/*.json "$BASE"/
  echo "基线已更新: $BASE  <-  $GEN"
  exit 0
fi

[ -d "$BASE" ] || { echo "基线目录不存在: $BASE（先 --update-baseline 建立基线）" >&2; exit 2; }
[ -d "$GEN" ]  || { echo "生成目录不存在: $GEN（先编译 CubeDemo 或加 --build）" >&2; exit 2; }

# 提取一个 openapi.json 里所有 (path, HTTP方法) 配对，每行 "path|method"，排序去重。
# 字符级状态机：正确处理字符串/转义，用"键栈"K[depth] 判定结构层级，不依赖缩进/换行/是否单行。
# 规则：paths 键 → 其对象内的 / 开头键为 path → path 对象内的 get/put/... 直接子键为方法。
methodset_of() {
  awk '
    BEGIN{ RS="\0"; DM="^(get|put|post|delete|options|head|patch|trace)$" }
    {
      s = $0; n = length(s); depth = 0; inStr = 0; esc = 0; tok = ""; pending = ""
      for (i = 1; i <= n; i++) {
        c = substr(s, i, 1)
        if (inStr) {
          if (esc) { tok = tok c; esc = 0; continue }
          if (c == "\\") { tok = tok c; esc = 1; continue }
          if (c == "\"") {
            inStr = 0; j = i + 1
            while (j <= n) { k = substr(s, j, 1); if (k==" "||k=="\t"||k=="\r"||k=="\n") { j++; continue }; break }
            nx = substr(s, j, 1)
            if (nx == ":") {                 # 该字符串是对象的键
              if (K[depth] == "paths" && tok ~ /^\//)      pending = tok          # path 键
              else if (K[depth] ~ /^\// && tok ~ DM)       print K[depth] "|" tok # 方法键
              else                                          pending = tok
            } else {                          # 值字符串，不建立键上下文
              pending = ""
            }
            tok = ""; continue
          }
          tok = tok c; continue
        }
        if (c == "\"") { inStr = 1; continue }
        if (c == "{" || c == "[") { depth++; K[depth] = pending; pending = ""; continue }
        if (c == "}" || c == "]") { K[depth] = ""; depth--; continue }
      }
    }
  ' "$1" 2>/dev/null | sort -u || true
}

echo "baseline  = $BASE"
echo "generated = $GEN"

fmt(){ sed -E 's#\|([a-z]+)$#  [\1]#'; }   # "path|get" -> "path  [get]"

failed=0
for bf in "$BASE"/*.json; do
  [ -e "$bf" ] || { echo "基线目录无 json: $BASE" >&2; exit 2; }
  f="$(basename "$bf")"; gf="$GEN/$f"
  bset="$(methodset_of "$bf")"
  if [ -z "$bset" ]; then echo "[$f] ✗ 基线未解析出任何接口方法（提取器失效/格式异常，拒绝静默放过）"; failed=1; continue; fi
  if [ ! -e "$gf" ]; then echo "[$f] ✗ 生成结果缺整个文件（该 Area 未生成？）"; failed=1; continue; fi
  gset="$(methodset_of "$gf")"
  if [ -z "$gset" ]; then echo "[$f] ✗ 生成结果未解析出任何接口方法（Area 全空或提取失效）"; failed=1; continue; fi
  missing="$(comm -23 <(printf '%s\n' "$bset") <(printf '%s\n' "$gset"))"
  added="$(   comm -13 <(printf '%s\n' "$bset") <(printf '%s\n' "$gset"))"
  if [ -n "$missing" ]; then
    echo "[$f] ✗ 缺失接口方法:"; printf '%s\n' "$missing" | fmt | sed 's/^/    - /'; failed=1
  elif [ -n "$added" ]; then
    echo "[$f] ↑ 新增接口方法（提示，不失败）:"; printf '%s\n' "$added" | fmt | sed 's/^/    - /'
  else
    pp="$(printf '%s\n' "$bset" | cut -d'|' -f1 | sort -u | wc -l | tr -d ' ')"
    mp="$(printf '%s\n' "$bset" | grep -c '|')"
    echo "[$f] ✓ OK（$pp paths / $mp 方法 与基线一致）"
  fi
done

if [ "$failed" -eq 1 ]; then
  echo
  echo "❌ OpenAPI 契约校验失败：有接口或方法从文档消失。最常见原因——控制器 action 漏写/误删 [Http*] 动词，被内建生成器静默丢弃。"
  exit 1
fi
echo
echo "✅ OpenAPI 契约校验通过"
