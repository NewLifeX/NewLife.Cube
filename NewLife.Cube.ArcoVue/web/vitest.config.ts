import { defineConfig } from 'vitest/config';

/** 轻量单测：不加载完整 Vite Vue 插件链 */
export default defineConfig({
  test: {
    environment: 'node',
    include: ['*.spec.ts', 'src/**/*.{spec,test}.ts'],
    exclude: ['**/node_modules/**', '**/dist/**', '../wwwroot/**'],
  },
});
