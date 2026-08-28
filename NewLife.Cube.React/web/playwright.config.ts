import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E 配置（对齐 Vue 皮肤 web/e2e）
 *
 * 运行前需启动被测后端（CubeDemo + React 皮肤）：
 *   cd Bin/CubeDemo && ./CubeDemo.exe --urls http://*:5050
 *
 * 也可用环境变量 E2E_BASE_URL 指向其它站点（如 React 自托管一体化站点 7081）：
 *   $env:E2E_BASE_URL="http://localhost:7081"; pnpm test:e2e
 */
const baseURL = process.env.E2E_BASE_URL || 'http://localhost:5050';

export default defineConfig({
  testDir: './e2e',
  timeout: 30000,
  fullyParallel: false,
  retries: 0,
  reporter: [['list']],
  use: {
    baseURL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
    },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], storageState: '.auth/admin.json' },
      dependencies: ['setup'],
    },
  ],
});
