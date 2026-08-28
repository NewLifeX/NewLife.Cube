/**
 * 登录会话复用（global setup）
 *
 * 通过 React 登录页完成一次登录，保存 storageState（Cookie + localStorage），
 * 供后续 spec 复用，避免每个用例都走登录流程。
 *
 * 凭据从环境变量读取（ADMIN_USER / ADMIN_PASS），禁止硬编码。
 */
import { test as setup, expect } from '@playwright/test';

const ADMIN_USER = process.env.ADMIN_USER || 'admin';
const ADMIN_PASS = process.env.ADMIN_PASS || 'admin';

setup('登录并保存会话', async ({ page }) => {
  const errors: string[] = [];
  page.on('console', (m) => {
    if (m.type() === 'error') errors.push(m.text());
  });
  page.on('pageerror', (e) => errors.push(e.message));

  await page.goto('/login');
  await page.getByPlaceholder('用户名 / 邮箱 / 手机号').fill(ADMIN_USER);
  await page.getByPlaceholder('请输入密码').fill(ADMIN_PASS);
  await page.getByRole('button', { name: '登 录' }).click();

  // 登录成功后跳转首页（新版工作台：欢迎横幅 + 常用菜单卡片）
  await page.waitForURL('**/');
  await expect(page.locator('.cube-home-hero')).toBeVisible({ timeout: 10000 });
  await expect(page.getByText('常用菜单')).toBeVisible({ timeout: 10000 });

  // 断言无 JS 错误
  expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);

  await page.context().storageState({ path: '.auth/admin.json' });
});
