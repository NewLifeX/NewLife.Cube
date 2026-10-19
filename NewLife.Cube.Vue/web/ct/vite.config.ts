import { fileURLToPath, URL } from 'node:url';
import { defineConfig, type Plugin } from 'vite';
import vue from '@vitejs/plugin-vue';
import vueJsx from '@vitejs/plugin-vue-jsx';

// 组件测试（CT）专用 Vite 配置：用一份共享 gallery 渲染任意组件，
// 并对 lov-api/request 做别名桩（避免依赖后端、避免重依赖链），
// 同时泛型 mock 项目自带的 virtual:@newlifex/cube-vue-* 虚拟模块
//（这些模块由 cubeFront 插件读取 configs/ 生成，CT 环境与项目根不同，直接桩为空对象）。
// 与 e2e 的 playwright.config.ts 完全解耦，独立端口 5190。
function mockCubeVueVirtual(): Plugin {
  return {
    name: 'mock-cube-vue-virtual',
    enforce: 'pre',
    resolveId(id) {
      if (id.startsWith('virtual:@newlifex/cube-vue-')) return '\0' + id;
      return null;
    },
    load(id) {
      if (!id.startsWith('\0virtual:@newlifex/cube-vue-')) return null;
      // 配置虚拟模块必须给具名导出：`core/configure/index.ts` 是
      // `import { configData, currentEnv } from 'virtual:@newlifex/cube-vue-config'`，
      // 只给 `export default {}` 会让浏览器在链接阶段就报
      // “does not provide an export named 'configData'”，表现为 gallery 整页白屏、
      // 任何用到 `core/utils/url`（它 import configure）的组件都拍不到。
      // 形状与 `core/client.d.ts` 的声明、`vitest.config.unit.ts` 里的同名桩保持一致。
      if (id === '\0virtual:@newlifex/cube-vue-config') {
        return 'export const configData = {}; export const currentEnv = "development"; export default { configData, currentEnv };';
      }
      return 'export default {}';
    },
  };
}

export default defineConfig({
  root: fileURLToPath(new URL('.', import.meta.url)),
  plugins: [
    mockCubeVueVirtual(),
    vue(),
    vueJsx(),
  ],
  resolve: {
    alias: [
      // 具体路径放前面，优先于父级别名
      { find: '@newlifex/cube-vue/core/utils/request', replacement: fileURLToPath(new URL('./mocks/request.ts', import.meta.url)) },
      { find: '@newlifex/cube-vue/core/utils/lov-api', replacement: fileURLToPath(new URL('./mocks/lov-api.ts', import.meta.url)) },
      // 项目源码别名（与 web/vite.config.ts 一致）
      { find: '@newlifex/cube-vue', replacement: fileURLToPath(new URL('../', import.meta.url)) },
    ],
  },
  server: {
    host: '127.0.0.1',
    port: 5190,
    strictPort: true,
  },
});
