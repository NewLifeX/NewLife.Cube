import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import AutoImport from 'unplugin-auto-import/vite';
import { resolve } from 'path';

export default defineConfig({
  plugins: [
    vue(),
    AutoImport({
      imports: ['vue', 'vue-router', 'pinia'],
      dts: 'src/auto-imports.d.ts',
    }),
  ],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
    },
  },
  server: {
    port: 5183,
    proxy: {
      '/Admin': { target: 'http://localhost:5000', changeOrigin: true },
      '/Cube': { target: 'http://localhost:5000', changeOrigin: true },
      '/Sso': { target: 'http://localhost:5000', changeOrigin: true },
      '/api': { target: 'http://localhost:5000', changeOrigin: true },
    },
  },
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
  },
});
