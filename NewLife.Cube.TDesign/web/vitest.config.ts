import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vitest/config';

/**
 * TDesign 皮肤最小化单元测试配置。
 *
 * 只测试与 UI 框架无关的 store / 工具逻辑，不拉起组件渲染与样式，
 * 保证测试在 CI / 沙箱环境下快速稳定运行。
 */
export default defineConfig({
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
    // 共享包 @newlifex/cube/pinia 与皮肤各自的 node_modules 下会解析出两份 pinia 实例，
    // setActivePinia 只对其中一份生效，store 取不到活动 Pinia（报 “no active Pinia”），
    // 因此强制 vue / vue-router / pinia 去重到同一份。
    dedupe: ['vue', 'vue-router', 'pinia'],
  },
  test: {
    environment: 'node',
    include: ['src/**/__tests__/**/*.spec.ts'],
    exclude: ['**/node_modules/**', '**/dist/**', '**/wwwroot/**'],
  },
});
