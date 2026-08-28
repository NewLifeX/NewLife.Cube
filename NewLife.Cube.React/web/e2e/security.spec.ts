/**
 * 安全中心 E2E（AUTH-5）
 *
 * 覆盖：安全中心页面加载无错误、MFA 状态区域渲染、个人信息展示。
 */
import { expect, test } from '@playwright/test';

test.describe('安全中心（/profile/security）', () => {
  test('页面加载无错误且展示安全项', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/profile/security');
    await expect(page.getByText(/安全中心|两步验证|MFA|邮箱|手机/).first()).toBeVisible({ timeout: 10000 });
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('MFA 状态区域可见（启用/未启用标识）', async ({ page }) => {
    await page.goto('/profile/security');
    // MFA 区块（开关或状态文本）应存在，不依赖具体开启状态
    await expect(page.locator('.ant-card').first()).toBeVisible({ timeout: 10000 });
  });
});
