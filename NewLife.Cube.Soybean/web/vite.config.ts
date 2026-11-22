import process from 'node:process';
import { URL, fileURLToPath } from 'node:url';
import { defineConfig, loadEnv } from 'vite';
import { setupVitePlugins } from './build/plugins';
import { getBuildTime } from './build/config';

/** 开发期后端地址（魔方 WebApi 宿主） */
const DEV_BACKEND = 'http://localhost:6080';

/** 需要转发到后端的服务接口前缀（实体接口统一走 /api） */
const SERVICE_PREFIXES = ['/Auth', '/Sso', '/Mfa', '/OAuth', '/Cube/MenuTree', '/Cube/Site', '/Cube/Setting', '/Cube/Avatar'];

export default defineConfig(configEnv => {
  const viteEnv = loadEnv(configEnv.mode, process.cwd()) as unknown as Env.ImportMeta;

  const buildTime = getBuildTime();

  const proxy = Object.fromEntries(
    ['/api', ...SERVICE_PREFIXES].map(prefix => [prefix, { target: DEV_BACKEND, changeOrigin: true }])
  );

  return {
    base: viteEnv.VITE_BASE_URL,
    resolve: {
      alias: {
        '~': fileURLToPath(new URL('./', import.meta.url)),
        '@': fileURLToPath(new URL('./src', import.meta.url))
      }
    },
    css: {
      preprocessorOptions: {
        scss: {
          api: 'modern-compiler',
          additionalData: `@use "@/styles/scss/global.scss" as *;`
        }
      }
    },
    plugins: setupVitePlugins(viteEnv, buildTime),
    define: {
      BUILD_TIME: JSON.stringify(buildTime)
    },
    server: {
      host: '0.0.0.0',
      port: 9527,
      open: true,
      proxy
    },
    preview: {
      port: 9725
    },
    build: {
      outDir: '../wwwroot',
      emptyOutDir: true,
      reportCompressedSize: false,
      sourcemap: viteEnv.VITE_SOURCE_MAP === 'Y',
      commonjsOptions: {
        ignoreTryCatch: false
      }
    }
  };
});
