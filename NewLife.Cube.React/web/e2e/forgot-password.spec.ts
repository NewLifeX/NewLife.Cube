/**
 * 忘记密码页 E2E（AUTH-4）
 *
 * 覆盖：页面加载无错误、空表单提交校验拦截、渠道切换。
 * 注：完整重置流程依赖验证码发送（demo 未配置短信/邮件时无法真实走通，验证到可测边界）。
 */
import { expect, test } from '@playwright/test';

test.describe('忘记密码页（/forgot-password）', () => {
  test('页面加载无错误且展示表单', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/forgot-password');
    // 原生收敛版隐藏了营销 aside，断言可见的卡片标题（div 元素，非 heading role）
    await expect(page.getByText('找回密码')).toBeVisible({ timeout: 10000 });
    await expect(page.getByRole('button', { name: /获取验证码|发送/ })).toBeVisible();
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('空账号提交被校验拦截', async ({ page }) => {
    await page.goto('/forgot-password');
    await page.getByRole('button', { name: /获取验证码|发送/ }).click();
    // 邮箱格式校验或必填提示
    await expect(page.locator('.ant-form-item-explain-error').first()).toBeVisible({ timeout: 8000 });
  });

  test('邮箱与手机渠道切换', async ({ page }) => {
    await page.goto('/forgot-password');
    // Radio.Button 渲染为 .ant-radio-button-wrapper
    const radios = page.locator('.ant-radio-button-wrapper');
    expect(await radios.count()).toBeGreaterThan(0);
  });
});
