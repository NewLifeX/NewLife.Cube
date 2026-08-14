import { expect, test as setup } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

/**
 * 登录并保存会话状态（OSC-2608139feb）。
 * 账号默认 admin/admin，可用环境变量 E2E_USER / E2E_PASSWORD 覆盖。
 */
const USER = process.env.E2E_USER || 'admin';
const PASSWORD = process.env.E2E_PASSWORD || 'admin';

setup('authenticate', async ({ page }) => {
  await page.goto('/login');
  // 登录页首个输入框是「租户 Code」，必须按 placeholder 定位
  await page.getByPlaceholder('请输入用户名').fill(USER);
  await page.getByPlaceholder('请输入密码').fill(PASSWORD);
  await page.getByRole('button', { name: '登录', exact: true }).first().click();

  // 登录成功进入布局（首页或菜单可见）
  await expect(page.locator('.arco-layout').first()).toBeVisible({ timeout: 30_000 });

  mkdirSync(join(dirname(fileURLToPath(import.meta.url)), '..', 'playwright', '.auth'), { recursive: true });
  await page.context().storageState({ path: 'playwright/.auth/user.json' });
});
