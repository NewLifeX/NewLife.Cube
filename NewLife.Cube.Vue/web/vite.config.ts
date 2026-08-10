import { join } from 'path';
import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import vueJsx from '@vitejs/plugin-vue-jsx';
import vueDevTools from 'vite-plugin-vue-devtools';
import AutoImport from 'unplugin-auto-import/vite';
import Components from 'unplugin-vue-components/vite';
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers';
import cubeFront from './core/plugin/index'; // This is the plugin we want to build as a library
import { CodeInspectorPlugin } from 'code-inspector-plugin';

// https://vite.dev/config/
export default defineConfig(({ command, mode }) => {
  // Check an environment variable to determine the build target
  if (process.env.BUILD_TARGET === 'plugin') {
    return {
      // Configuration for building the plugin as a library
      build: {
        lib: {
          entry: fileURLToPath(new URL('./core/plugin/index.ts', import.meta.url)),
          name: 'CubeFrontPlugin', // Global variable name for UMD build (if UMD format is included)
          formats: ['es'], // Output ES Module format
          fileName: () => 'index.js', // Output to dist/plugin/index.js
        },
        rollupOptions: {
          // Externalize dependencies that should not be bundled into the library
          // e.g., external: ['vue'],
          external: [], // Adjust if your plugin has peer dependencies
          output: {
            // globals: { // Define globals for externalized UMD dependencies
            //   vue: 'Vue',
            // },
          },
        },
        outDir: 'dist/plugin', // Output directory for the plugin
        sourcemap: true,
        ssr: true, // Indicate that this library is intended for SSR/Node.js-like environments
      },
      // Plugins needed for building the library itself (if any)
      // Typically, fewer plugins are needed for a library compared to an app.
      // If core/plugin/index.ts is plain TS/JS, it might not need vue plugins.
      plugins: [
        // Add plugins required for your library build if necessary
        // For example, if it uses Vue features:
        // vue(),
        // vueJsx(),
      ],
    };
  } else {
    // Default configuration for building the main application
    return {
      plugins: [
        vue(),
        vueJsx(),
        vueDevTools(),
        CodeInspectorPlugin({
          bundler: 'vite',
        }),
        cubeFront(), // The plugin is used here in the main app build
        AutoImport({
          resolvers: [ElementPlusResolver()],
          // Exclude ElMessage from auto-import since we use request interceptor for unified error handling
          exclude: [/ElMessage/],
          imports: [
            'vue',
            'vue-router',
            // Add other imports you want to auto-import globally
          ],
        }),
        Components({
          resolvers: [ElementPlusResolver()],
        }),
      ],
      resolve: {
        alias: {
          '@newlifex/cube-vue': fileURLToPath(new URL('./', import.meta.url)),
        },
      },
      build: {
        sourcemap: true, // Generates source maps for better debugging
        outDir: '../wwwroot',
        emptyOutDir: true,
        commonjsOptions: {
          strictRequires: false,
          transformMixedEsModules: true,
        },
        rollupOptions: {
          external: (id) => id.includes('@lezer/php'),
        },
      },
      server: {
        port: 5187,
        // 前端 API 请求经 Vite 代理转发到本地 CubeDemo 后端（:5050，含本任务 LOV 特性）。
        // .env.development 中 VITE_API_URL=http://localhost:5187/api，故所有请求带 /api 前缀，
        // 这里剥离 /api 后转发到 :5050（:5050 自身无 /api 前缀路由）。
        proxy: {
          '/api': {
            target: 'http://localhost:5050',
            changeOrigin: true,
            rewrite: (p) => p.replace(/^\/api/, ''),
          },
        },
      },
    };
  }
});
