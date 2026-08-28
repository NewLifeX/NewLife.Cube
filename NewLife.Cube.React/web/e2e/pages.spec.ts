/**
 * 页面级 E2E（AUTH-6 /unauthorized + HOME-2 /loading）
 *
 * 覆盖：未授权页加载无错误、加载页加载无错误。
 */
import { expect, test } from '@playwright/test';

test.describe('未授权页（/unauthorized）', () => {
  test('页面加载无错误且展示 403 提示', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/unauthorized');
    await expect(page.getByText('403').first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/没有权限访问/).first()).toBeVisible();
    await expect(page.getByRole('button', { name: /返回首页/ })).toBeVisible();
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });
});

test.describe('加载页（/loading）', () => {
  test('页面加载无错误且展示加载指示器', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/loading');
    // antd Spin 加载指示器
    await expect(page.locator('.ant-spin-spinning').first()).toBeVisible({ timeout: 10000 });
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });
});
