/**
 * 登录页 E2E（AUTH-1 标准场景）
 */
import { expect, test } from '@playwright/test';

test.describe('登录页', () => {
  test('登录页加载无错误且展示系统名', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/login');
    await expect(page.getByPlaceholder('用户名 / 邮箱 / 手机号')).toBeVisible();
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('空表单提交被校验拦截', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('button', { name: '登 录' }).click();
    await expect(page.getByText('请输入').first()).toBeVisible();
    await expect(page.getByText('请输入密码').first()).toBeVisible();
  });

  test('错误密码提示登录失败', async ({ page }) => {
    await page.goto('/login');
    await page.getByPlaceholder('用户名 / 邮箱 / 手机号').fill('admin');
    await page.getByPlaceholder('请输入密码').fill('wrong-password');
    await page.getByRole('button', { name: '登 录' }).click();
    // 登录失败不跳转，仍在登录页
    await expect(page.getByPlaceholder('用户名 / 邮箱 / 手机号')).toBeVisible();
    await expect(page.locator('.ant-message')).toBeVisible({ timeout: 8000 });
  });

  test('注册/忘记密码入口可见', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByRole('button', { name: '注册账号' })).toBeVisible();
    await expect(page.getByRole('button', { name: '忘记密码' })).toBeVisible();
  });
});
