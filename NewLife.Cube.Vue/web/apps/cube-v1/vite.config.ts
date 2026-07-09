import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'path';

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
      '@newlifex/cube-vue': resolve(__dirname, '../../core'),
    },
  },
  server: {
    port: 5174,
  },
});
