import { defineConfig } from 'vitest/config';
import { resolve } from 'path';

/** 轻量单测：解析 @ 别名；不加载完整 Vue 插件链 */
export default defineConfig({
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
      // 测试环境替换 Semi 材料为 stub（避免 node 解析 semi-ui/icons CJS 语法报错）
      '@flowgram.ai/fixed-semi-materials': resolve(__dirname, 'src/core/utils/__stubs__/fixedSemiMaterials.ts'),
    },
  },
  test: {
    environment: 'node',
    include: ['*.spec.ts', 'src/**/*.{spec,test}.ts'],
    exclude: ['**/node_modules/**', '**/dist/**', '../wwwroot/**'],
    // FlowGram 设计器链（React/semi 材料）为 ESM/CJS 混合，node 直读报语法错误 → 内联转译
    server: {
      deps: {
        inline: [/@flowgram\.ai\//, /@douyinfe\//, /styled-components/],
      },
    },
  },
});
