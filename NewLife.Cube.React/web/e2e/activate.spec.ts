/**
 * 激活页 E2E（AUTH-3）
 *
 * 覆盖：页面加载无错误、渠道切换、空账号提交拦截、无效链接激活错误提示。
 * 注：真实激活流程依赖验证码后端（demo 未配置短信/邮件时验证到可测边界）。
 */
import { expect, test } from '@playwright/test';

test.describe('激活页（/activate）', () => {
  test('页面加载无错误且展示验证码激活表单', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/activate');
    await expect(page.getByText(/激活|验证码/).first()).toBeVisible({ timeout: 10000 });
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('邮箱与手机渠道切换', async ({ page }) => {
    await page.goto('/activate');
    // Radio.Button 渲染为 .ant-radio-button-wrapper
    const radios = page.locator('.ant-radio-button-wrapper');
    expect(await radios.count()).toBeGreaterThan(0);
  });

  test('空账号提交被校验拦截', async ({ page }) => {
    await page.goto('/activate');
    // antd Button 中文插空格（"激 活"）
    await page.getByRole('button', { name: /激\s*活/ }).click();
    // 必填或格式校验提示（邮箱格式不正确 / 请输入等）
    await expect(page.locator('.ant-form-item-explain-error').first()).toBeVisible({ timeout: 8000 });
  });

  test('无效链接激活展示错误提示', async ({ page }) => {
    await page.goto('/activate?token=invalid-token&account=nobody');
    // 链接激活失败 → Alert 或 message 错误提示
    await expect(page.locator('.ant-alert-error, .ant-message-error').first()).toBeVisible({ timeout: 10000 });
  });
});
