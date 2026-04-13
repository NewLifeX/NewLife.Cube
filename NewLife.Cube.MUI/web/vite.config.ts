import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
    },
  },
  server: {
    port: 5181,
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
