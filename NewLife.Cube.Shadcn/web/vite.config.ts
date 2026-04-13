import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { resolve } from 'path';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
    },
  },
  server: {
    port: 5182,
    proxy: {
      '/Admin': 'http://localhost:5000',
      '/Cube': 'http://localhost:5000',
      '/Sso': 'http://localhost:5000',
      '/api': 'http://localhost:5000',
    },
  },
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
  },
});
