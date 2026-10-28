import { test, expect } from '@playwright/test';

/**
 * 找回密码（忘记密码）回归
 *
 * 只断言「页面可达 + 入口显示与站点通道开关一致 + 本地校验拦截」，不真的发码：
 * 发码依赖站点短信/邮件配置，CI 与本地开发库上并不稳定，服务端行为由后端单测
 * （`AuthEnhancedServiceTests`）覆盖。断言避开界面文案（语言随 navigator.language
 * 变化），只用 data-cy 与结构类名。
 */
const apiURL = process.env.PLAYWRIGHT_API_URL || 'https://localhost:7116';

/** 读取站点登录能力（匿名接口），用于与入口显示条件对照 */
async function fetchLoginAbility(page: import('@playwright/test').Page) {
  // 与 auth.setup.ts 一致：直连后端根路径，不加前端代理用的 /api 前缀
  const res = await page.request.get(`${apiURL}/Auth/LoginConfig`);
  const body = await res.json();
  return body?.data?.login ?? {};
}

test.describe('找回密码', () => {
  test('找回密码页可直接访问', async ({ page }) => {
    await page.goto('/forgot-password');

    await expect(page.locator('[data-cy="forgot-username"]')).toBeVisible({ timeout: 15000 });
  });

  test('登录页入口显示与站点短信/邮件通道开关一致', async ({ page }) => {
    const ability = await fetchLoginAbility(page);
    const expected = ability?.sms === true || ability?.mail === true;

    await page.goto('/login');
    await expect(page.locator('[data-cy="username"]')).toBeVisible({ timeout: 15000 });

    const entry = page.locator('[data-cy="forgot-entry"]');
    if (expected) {
      await expect(entry).toBeVisible();
      await entry.click();
      await expect(page).toHaveURL(/forgot-password/);
      await expect(page.locator('[data-cy="forgot-username"]')).toBeVisible();
    } else {
      await expect(entry).toHaveCount(0);
    }
  });

  test('账号为空时提交被拦截，不进入第二步', async ({ page }) => {
    await page.goto('/forgot-password');
    await expect(page.locator('[data-cy="forgot-username"]')).toBeVisible({ timeout: 15000 });

    await page.locator('.primary-btn').click();

    await expect(page.locator('.input-error')).toBeVisible();
    // 仍在第一步：验证码输入框不存在
    await expect(page.locator('[data-cy="forgot-code"]')).toHaveCount(0);
  });

  test('账号含 @ 时自动切到邮箱渠道', async ({ page }) => {
    await page.goto('/forgot-password');
    const input = page.locator('[data-cy="forgot-username"]');
    await expect(input).toBeVisible({ timeout: 15000 });

    await input.fill('dev@example.com');

    await expect(page.locator('[data-cy="forgot-channel-mail"]')).toHaveClass(/active/);
  });
});
