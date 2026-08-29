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
    // 用不存在的用户名验证失败提示：避免用 admin 触发登录风控（同用户失败计数达阈值会封禁，影响后续用例重登）
    await page.getByPlaceholder('用户名 / 邮箱 / 手机号').fill('no-such-user');
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

  test('保存密码勾选框可见，勾选后登录请求携带 remember=true', async ({ page }) => {
    await page.goto('/login');
    const remember = page.getByRole('checkbox', { name: '保存密码' });
    await expect(remember).toBeVisible();

    // 拦截 /Auth/Login 请求体，验证勾选框已接通 remember 参数（无需真实登录成功）
    let loginBody: Record<string, unknown> | undefined;
    page.on('request', (req) => {
      if (req.url().includes('/Auth/Login') && req.method() === 'POST') loginBody = req.postDataJSON();
    });

    await remember.check();
    await expect(remember).toBeChecked();

    // 用不存在的用户名触发登录请求（后端会拒绝，但请求体已可断言）
    await page.getByPlaceholder('用户名 / 邮箱 / 手机号').fill('no-such-user');
    await page.getByPlaceholder('请输入密码').fill('wrong-password');
    await page.getByRole('button', { name: '登 录' }).click();

    await expect.poll(() => loginBody, { timeout: 8000 }).toBeTruthy();
    expect(loginBody!.remember).toBe(true);
  });

  test('未勾选保存密码时登录请求不带 remember', async ({ page }) => {
    await page.goto('/login');

    let loginBody: Record<string, unknown> | undefined;
    page.on('request', (req) => {
      if (req.url().includes('/Auth/Login') && req.method() === 'POST') loginBody = req.postDataJSON();
    });

    await page.getByPlaceholder('用户名 / 邮箱 / 手机号').fill('no-such-user');
    await page.getByPlaceholder('请输入密码').fill('wrong-password');
    await page.getByRole('button', { name: '登 录' }).click();

    await expect.poll(() => loginBody, { timeout: 8000 }).toBeTruthy();
    expect(loginBody!.remember).toBeFalsy();
  });
});
