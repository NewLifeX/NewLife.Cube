import { test, expect } from '@playwright/test';

/**
 * 安全中心 · 修改密码（对齐 MVC `Admin/User/ChangePassword` 页）
 *
 * 只做「不改动密码」的负向验证：两次输入不一致走前端拦截，旧密码错误由后端拒绝。
 * 真正改密会让后续用例登录失效，留给人工验证。
 */
const apiBase = process.env.PLAYWRIGHT_API_URL || 'https://localhost:7116';

test.describe('安全中心修改密码', () => {
  test('两次输入不一致时前端拦截', async ({ page }) => {
    let requested = false;
    page.on('request', (r) => {
      if (r.url().includes('/ChangePassword')) requested = true;
    });

    await page.goto('/profile/security');
    await page.locator('[data-cy="password-toggle"]').click();
    await expect(page.locator('[data-cy="password-new"]')).toBeVisible({ timeout: 20000 });

    await page.locator('[data-cy="password-new"]').fill('NewPass#2026');
    await page.locator('[data-cy="password-confirm"]').fill('OtherPass#2026');
    await page.locator('[data-cy="password-submit"]').click();

    // 客户端校验提示（文案随语言变化，只断言出现提示且没有发出请求）
    await expect(page.locator('.el-message').first()).toBeVisible({ timeout: 10000 });
    await page.waitForTimeout(500);
    expect(requested).toBe(false);
  });

  test('旧密码错误时后端拒绝且密码不变', async ({ page }) => {
    await page.goto('/profile/security');
    await page.locator('[data-cy="password-toggle"]').click();
    await expect(page.locator('[data-cy="password-old"]')).toBeVisible({ timeout: 20000 });

    await page.locator('[data-cy="password-old"]').fill('definitely-wrong-old');
    await page.locator('[data-cy="password-new"]').fill('NewPass#2026');
    await page.locator('[data-cy="password-confirm"]').fill('NewPass#2026');
    await page.locator('[data-cy="password-submit"]').click();

    await expect(page.locator('.el-message--error').first()).toBeVisible({ timeout: 20000 });

    // 原密码仍可用
    const login = await page.request.post(`${apiBase}/Auth/Login`, {
      data: { username: 'admin', password: 'admin' },
      ignoreHTTPSErrors: true,
    });
    expect((await login.json()).code).toBe(0);
  });
});
