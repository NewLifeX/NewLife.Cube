/**
 * 最小化 Vitest 配置（仅用于单元 / 组件测试）
 *
 * 说明：项目根 vite.config.ts 加载了 cubeFront() / codeInspectorPlugin / vueDevTools /
 * AutoImport / Components 等重型插件，在 CI / 沙箱环境下会导致 Vite 开发服务器初始化
 * 卡死（worker RPC timeout）。本配置仅保留 @vitejs/plugin-vue + jsdom，足以驱动
 * fieldControl 映射单测与 FormContent 组件渲染矩阵测试，且不会拉起全量插件扫描。
 */
import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vitest/config';
import vue from '@vitejs/plugin-vue';

/**
 * 桩插件：为 cubeFront() 框架插件提供的虚拟模块 `virtual:@newlifex/cube-vue-config`
 * 提供最小实现。该虚拟模块在运行时由框架插件生成（来自 configs/config.*.ts），
 * 但这里只做单元测试，getConfig() 在 configData={} 时会回退到 defaultConfig，
 * 足以驱动 FormContent / 控件渲染，且不会触发任何网络请求。
 */
function cubeVirtualConfigStub() {
  const id = 'virtual:@newlifex/cube-vue-config';
  return {
    name: 'cube-virtual-config-stub',
    resolveId(source: string) {
      return source === id ? source : null;
    },
    load(loadId: string) {
      if (loadId === id) {
        return `export const configData = {}; export const currentEnv = 'development';`;
      }
      return null;
    },
  };
}

export default defineConfig({
  plugins: [cubeVirtualConfigStub(), vue()],
  resolve: {
    // 显式声明扩展名，确保 .vue 单文件组件能被解析（最小化配置下
    // @vitejs/plugin-vue 的扩展名自动注入可能不生效）
    extensions: ['.mjs', '.js', '.mts', '.ts', '.jsx', '.tsx', '.json', '.vue'],
    alias: {
      '@newlifex/cube-vue': fileURLToPath(new URL('./', import.meta.url)),
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    include: ['core/__tests__/**/*.{spec,test}.ts'],
    exclude: ['**/node_modules/**', '**/dist/**', '**/cypress/**', 'e2e/**', '**/.{idea,git,cache}/**'],
    testTimeout: 20000,
    hookTimeout: 20000,
  },
});
