import { sveltekit } from '@sveltejs/kit/vite';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [tailwindcss(), sveltekit()],
  server: {
    port: 5186,
    proxy: {
      '/Admin': { target: 'http://localhost:5000', changeOrigin: true },
      '/Cube': { target: 'http://localhost:5000', changeOrigin: true },
      '/Sso': { target: 'http://localhost:5000', changeOrigin: true },
      '/api': { target: 'http://localhost:5000', changeOrigin: true },
    },
  },
});
