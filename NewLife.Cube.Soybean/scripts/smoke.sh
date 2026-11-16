#!/usr/bin/env bash
# NewLife.Cube.Soybean smoke test.
# Usage: bash smoke.sh [baseUrl] [user] [password]
# Default: http://localhost:6090 admin Admin@123
set -u

BASE="${1:-http://localhost:6090}"
USER_NAME="${2:-admin}"
PASSWORD="${3:-Admin@123}"
COOKIE="$(mktemp)"
TMP_BODY="$(mktemp)"
FAIL=0

cleanup() { rm -f "$COOKIE" "$TMP_BODY"; }
trap cleanup EXIT

pass() { printf 'PASS  %s\n' "$1"; }
fail() { printf 'FAIL  %s\n' "$1"; FAIL=$((FAIL + 1)); }

# check <name> <expected-code> <url> [curl-args...]
check() {
  local name="$1" expected="$2" url="$3"
  shift 3
  local code
  code="$(curl -s -o "$TMP_BODY" -w '%{http_code}' "$@" "$url")"
  if [ "$code" = "$expected" ]; then pass "$name ($code)"; else fail "$name (expect $expected, got $code)"; fi
}

# body_contains <name> <needle>
body_contains() {
  if grep -q -- "$2" "$TMP_BODY"; then pass "$1"; else fail "$1 (missing: $2)"; fi
}

printf '== Soybean smoke @ %s ==\n' "$BASE"

# 1. SPA shell served
check "spa index" 200 "$BASE/"
body_contains "spa index has app mount" 'id="app"'
ASSET="$(grep -o '/assets/index-[A-Za-z0-9_-]*\.js' "$TMP_BODY" | head -1)"
if [ -n "$ASSET" ]; then
  check "spa main bundle" 200 "$BASE$ASSET"
else
  fail "spa main bundle (asset not found)"
fi

# 2. Deep link falls back to SPA
check "spa deep link" 200 "$BASE/Admin/User"

# 3. Anonymous menu tree must not be 200
check "anonymous menutree blocked" 302 "$BASE/Cube/MenuTree"

# 4. Login
check "auth login" 200 "$BASE/Auth/Login" \
  -H 'Content-Type: application/json' \
  -d "{\"username\":\"${USER_NAME}\",\"password\":\"${PASSWORD}\",\"remember\":true}" \
  --cookie-jar "$COOKIE"
body_contains "login code=0" '"code":0'

TOKEN="$(grep -o '"access_token":"[^"]*"' "$TMP_BODY" | head -1 | cut -d'"' -f4)"
if [ -n "$TOKEN" ]; then pass "login token"; else fail "login token"; fi
AUTH=(-H "Authorization: Bearer $TOKEN")

# 5. Authenticated endpoints
check "auth info" 200 "$BASE/Auth/Info" -b "$COOKIE" "${AUTH[@]}"
body_contains "auth info has name" '"name"'

check "site" 200 "$BASE/Cube/Site" -b "$COOKIE" "${AUTH[@]}"

check "menu tree" 200 "$BASE/Cube/MenuTree" -b "$COOKIE" "${AUTH[@]}"
body_contains "menu tree has nodes" '"name"'

check "entity meta" 200 "$BASE/api/Admin/User/meta" -b "$COOKIE" "${AUTH[@]}"
body_contains "meta code=0" '"code":0'

check "entity list" 200 "$BASE/api/Admin/User?pageIndex=1&pageSize=2" -b "$COOKIE" "${AUTH[@]}"
body_contains "list has data" '"data"'

check "workbench dashboard" 200 "$BASE/api/Admin/Index/Dashboard" -b "$COOKIE" "${AUTH[@]}"
body_contains "dashboard has kpis" '"kpis"'

printf '== %s ==\n' "$([ "$FAIL" -eq 0 ] && echo 'ALL PASSED' || echo "$FAIL FAILED")"
exit "$([ "$FAIL" -eq 0 ] && echo 0 || echo 1)"
