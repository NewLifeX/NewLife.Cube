import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vitest/config';

/**
 * MUI 皮肤最小化单元测试配置。
 *
 * 只测试与 UI 框架无关的注册表单校验/载荷构造逻辑，避免引入 React Testing Library
 * 与 jsdom 等组件测试依赖，保证测试在 CI / 沙箱环境下快速稳定运行。
 */
export default defineConfig({
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  test: {
    environment: 'node',
    include: ['src/**/__tests__/**/*.spec.ts'],
    exclude: ['**/node_modules/**', '**/dist/**', '**/wwwroot/**'],
  },
});
