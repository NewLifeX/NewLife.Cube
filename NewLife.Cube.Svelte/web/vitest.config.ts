import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vitest/config';

/**
 * Svelte 皮肤最小化单元测试配置。
 *
 * 只测试 Svelte store（框架无关的认证状态与菜单权限），不编译 .svelte 组件，
 * 保证测试在 CI / 沙箱环境下快速稳定运行。
 */
export default defineConfig({
  resolve: {
    alias: {
      $lib: fileURLToPath(new URL('./src/lib', import.meta.url)),
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  test: {
    environment: 'node',
    include: ['src/**/__tests__/**/*.spec.ts'],
    exclude: ['**/node_modules/**', '**/dist/**', '**/wwwroot/**', '**/.svelte-kit/**'],
  },
});
