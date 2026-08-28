/**
 * 注册页 E2E（AUTH-2）
 *
 * 覆盖：页面加载无错误、注册方式切换、空表单提交校验拦截。
 * 注：完整注册流程依赖后端验证码（短信/邮件），demo 未配置时仅验证到可测边界。
 */
import { expect, test } from '@playwright/test';

test.describe('注册页（/register）', () => {
  test('注册页加载无错误且展示表单', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/register');
    await expect(page.getByPlaceholder('请输入用户名')).toBeVisible({ timeout: 10000 });
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('注册方式切换按钮可见', async ({ page }) => {
    await page.goto('/register');
    await expect(page.getByRole('button', { name: '账号密码' }).first()).toBeVisible({ timeout: 10000 });
  });

  test('空表单提交被校验拦截', async ({ page }) => {
    await page.goto('/register');
    await page.getByRole('button', { name: '注 册' }).click();
    await expect(page.getByText('请输入用户名').first()).toBeVisible({ timeout: 8000 });
    await expect(page.getByText('请输入密码').first()).toBeVisible();
    await expect(page.getByText('请确认密码').first()).toBeVisible();
  });

  test('密码与确认密码不一致被拦截', async ({ page }) => {
    await page.goto('/register');
    await page.getByPlaceholder('请输入用户名').fill(`e2e-reg-${Date.now()}`);
    await page.getByPlaceholder('请输入密码').fill('password123');
    await page.getByPlaceholder('请再次输入密码').fill('different123');
    await page.getByRole('button', { name: '注 册' }).click();
    await expect(page.getByText('两次输入的密码不一致').first()).toBeVisible({ timeout: 8000 });
  });
});
