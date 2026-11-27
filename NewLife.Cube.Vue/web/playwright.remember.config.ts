import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  testMatch: 'remember-token-refresh.spec.ts',
  workers: 1,
  reporter: 'list',
  outputDir: '../../.temp/remember-browser/results',
  use: { baseURL: 'http://127.0.0.1:5198', browserName: 'chromium' },
  webServer: {
    command: 'pnpm dev --host 127.0.0.1 --port 5198 --strictPort',
    url: 'http://127.0.0.1:5198',
    reuseExistingServer: false,
    timeout: 120000,
  },
});
