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

test.describe('验证码按 LoginConfig 开关请求', () => {
  // 拦截真实 LoginConfig 响应，仅改写 login.captcha，与后端配置解耦
  async function mockLoginCaptcha(page: import('@playwright/test').Page, captcha: boolean) {
    await page.route('**/Auth/LoginConfig', async (route) => {
      const res = await route.fetch();
      const json = (await res.json()) as { data: { login?: { captcha?: boolean } } };
      if (json.data?.login) json.data.login.captcha = captcha;
      await route.fulfill({ response: res, json });
    });
  }

  test('login.captcha=false 时不请求验证码接口', async ({ page }) => {
    await mockLoginCaptcha(page, false);
    let captchaRequests = 0;
    page.on('request', (req) => {
      if (req.url().includes('/Auth/Captcha')) captchaRequests++;
    });

    await page.goto('/login');
    await expect(page.getByPlaceholder('用户名 / 邮箱 / 手机号')).toBeVisible();
    // 等待潜在请求窗口，确认无验证码请求
    await page.waitForTimeout(800);
    expect(captchaRequests).toBe(0);
    await expect(page.getByText('图形验证码')).toHaveCount(0);
  });

  test('login.captcha=true 时请求验证码接口并展示图形验证码', async ({ page }) => {
    await mockLoginCaptcha(page, true);
    let captchaRequests = 0;
    page.on('request', (req) => {
      if (req.url().includes('/Auth/Captcha')) captchaRequests++;
    });

    await page.goto('/login');
    await expect(page.getByText('图形验证码')).toBeVisible({ timeout: 8000 });
    expect(captchaRequests).toBeGreaterThan(0);
  });
});

test.describe('保存密码跨会话恢复（干净上下文）', () => {
  // 不复用登录态，走完整登录流程验证"再次访问免登录"
  test.use({ storageState: { cookies: [], origins: [] } });

  test('勾选保存密码登录后，localStorage 丢失可从 Cookie 恢复免登录', async ({ page }) => {
    const ADMIN_USER = process.env.ADMIN_USER || 'admin';
    const ADMIN_PASS = process.env.ADMIN_PASS || 'admin';

    await page.goto('/login');
    await page.getByRole('checkbox', { name: '保存密码' }).check();
    await page.getByPlaceholder('用户名 / 邮箱 / 手机号').fill(ADMIN_USER);
    await page.getByPlaceholder('请输入密码').fill(ADMIN_PASS);
    await page.getByRole('button', { name: '登 录' }).click();
    await page.waitForURL('**/', { timeout: 15000 });

    // 双通道写入：localStorage + Cookie 都有 token
    expect(await page.evaluate(() => localStorage.getItem('token'))).toBeTruthy();
    expect(await page.evaluate(() => document.cookie.includes('token='))).toBe(true);

    // 模拟 localStorage 丢失（浏览器清理站点数据场景），Cookie 保留
    await page.evaluate(() => localStorage.removeItem('token'));

    // 再次访问受保护页：应从 Cookie 恢复 token，免登录进入
    await page.goto('/Admin/User');
    await page.waitForURL('**/Admin/User', { timeout: 8000 });
    expect(page.url()).toContain('/Admin/User');
  });
});
