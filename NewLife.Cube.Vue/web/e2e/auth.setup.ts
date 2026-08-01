import { test as setup, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const authFile = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../playwright/.auth/user.json');
const apiUrl = process.env.PLAYWRIGHT_API_URL || 'https://localhost:7116';

setup('authenticate', async ({ page, request }) => {
  const response = await request.post(`${apiUrl}/Auth/Login`, {
    data: { username: 'admin', password: 'admin' },
  });

  expect(response.ok()).toBeTruthy();
  const body = await response.json();
  expect(body.code).toBe(0);

  const token = body.data?.accessToken || body.data?.access_token || body.data?.Token;
  expect(token).toBeTruthy();

  await page.goto('/');
  await page.evaluate(
    ([accessToken, refreshToken]) => {
      localStorage.setItem('token', accessToken);
      if (refreshToken) {
        localStorage.setItem('refresh_token', refreshToken);
      }
    },
    [token, body.data?.refreshToken || body.data?.refresh_token || body.data?.RefreshToken || ''],
  );

  fs.mkdirSync(path.dirname(authFile), { recursive: true });
  await page.context().storageState({ path: authFile });
});
