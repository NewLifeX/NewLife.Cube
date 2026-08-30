/**
 * 个人中心 E2E（/profile）
 *
 * 覆盖：页面加载无错误、用户卡与基本信息展示、安全中心入口跳转。
 */
import { expect, test } from '@playwright/test';

test.describe('个人中心（/profile）', () => {
  test('页面加载无错误且展示用户资料', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/profile');
    await expect(page.getByText('基本信息').first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('登录次数').first()).toBeVisible();
    await expect(page.getByText('账号操作').first()).toBeVisible();
    await expect(page.getByText('安全中心').first()).toBeVisible();
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('安全中心入口跳转到 /profile/security', async ({ page }) => {
    await page.goto('/profile');
    await expect(page.getByText('账号操作').first()).toBeVisible({ timeout: 10000 });
    await page.getByRole('button', { name: '安全中心' }).click();
    await page.waitForURL('**/profile/security', { timeout: 10000 });
    await expect(page.getByText(/安全中心|两步验证|MFA|邮箱|手机/).first()).toBeVisible({ timeout: 10000 });
  });
});
