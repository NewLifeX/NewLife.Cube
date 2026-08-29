import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';

// https://vite.dev/config/
export default defineConfig(() => {
  return {
    plugins: [react()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    build: {
      // 构建产物输出到上级 wwwroot，供 .NET 嵌入式资源打包
      sourcemap: true,
      outDir: '../wwwroot',
      emptyOutDir: true,
    },
    server: {
      port: 5188,
      // 前端 API 请求经 Vite 代理转发到本地 CubeDemo 后端（:5050）。
      // .env.development 中 VITE_API_URL=/api，所有请求带 /api 前缀，
      // 这里剥离 /api 后转发到 :5050（:5050 自身无 /api 前缀路由）。
      proxy: {
        '/api': {
          target: 'http://localhost:5050',
          changeOrigin: true,
          rewrite: (p) => p.replace(/^\/api/, ''),
        },
        // AI 服务接口（/Ai/AiChat）不带 /api 前缀
        '/Ai': {
          target: 'http://localhost:5050',
          changeOrigin: true,
        },
      },
    },
  };
});
