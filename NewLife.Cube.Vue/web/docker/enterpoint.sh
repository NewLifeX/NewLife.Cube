#!/bin/bash
set +e
set -x

echo "🚀 Cube Frontend Container Starting..."

# 替换 BUILD_ 开头的占位符（vite 插件注入到 index.html 的内联脚本）
# configs\config.production.ts 中新增了需要替换的 BUILD_  变量，需要在这里添加对应的 sed 替换命令
# sed 找不到匹配时不会报错，直接执行即可
sed -i "s|BUILD_REQUEST_BASE_URL|${BUILD_REQUEST_BASE_URL:-/}|g" /usr/share/nginx/html/index.html

# 可以在这里添加更多 BUILD_ 变量替换...

echo "✅ replace complete!"

# 执行原始入口点命令
exec "$@"
