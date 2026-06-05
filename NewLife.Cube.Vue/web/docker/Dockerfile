# 构建阶段
FROM node:20-alpine AS builder

WORKDIR /app

# 安装 pnpm
RUN npm install -g pnpm

# 安装依赖
COPY package.json pnpm-lock.yaml* ./
RUN pnpm install --frozen-lockfile --prod

# 复制源代码
COPY . .

# 构建（支持环境变量）
ARG VITE_APP_BASE_API
ENV VITE_APP_BASE_API=$VITE_APP_BASE_API

RUN pnpm run build

# 生产阶段
FROM nginx:alpine

# 复制构建产物
COPY --from=builder /app/dist /usr/share/nginx/html

# 复制入口点脚本
COPY docker/enterpoint.sh /docker-entrypoint.d/enterpoint.sh
RUN chmod +x /docker-entrypoint.d/enterpoint.sh

# 暴露端口
EXPOSE 80

# 设置入口点
CMD ["nginx", "-g", "daemon off;"]