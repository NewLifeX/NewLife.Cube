import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E 配置（对齐 Vue 皮肤 web/e2e）
 *
 * 运行前需启动被测后端（CubeDemo + React 皮肤）：
 *   cd Bin/CubeDemo && ./CubeDemo.exe --urls http://*:5050
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 30000,
  fullyParallel: false,
  retries: 0,
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:5050',
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
