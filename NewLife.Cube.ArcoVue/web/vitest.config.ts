import { defineConfig } from 'vitest/config';
import { resolve } from 'path';

/** 轻量单测：解析 @ 别名；不加载完整 Vue 插件链 */
export default defineConfig({
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
    },
  },
  test: {
    environment: 'node',
    include: ['*.spec.ts', 'src/**/*.{spec,test}.ts'],
    exclude: ['**/node_modules/**', '**/dist/**', '../wwwroot/**'],
  },
});
