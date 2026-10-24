import { test, expect } from '@playwright/test';

/**
 * Vue 工作台真实数据 E2E
 *
 * 验证登录后的首页不再显示占位「主页」，而是消费 `Admin/Index/Dashboard`
 * 渲染 KPI 与 Widget 内容卡，并可刷新重载。
 *
 * 登录使用 `page.request`（与 page 共享 Cookie），随后写入本地 token 以通过路由守卫；
 * 后端同时支持 Cookie 会话，因此无需依赖被脱敏的 accessToken 字段。
 */
test.describe('Vue 工作台（真实数据）', () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  const apiUrl = process.env.PLAYWRIGHT_API_URL || 'https://localhost:7116';

  test.beforeEach(async ({ page }) => {
    const response = await page.request.post(`${apiUrl}/Auth/Login`, {
      data: { username: 'admin', password: 'admin' },
    });
    expect(response.ok()).toBeTruthy();

    const body = await response.json();
    expect(body.code, body.message).toBe(0);

    // 路由守卫读 localStorage 里的 token；Vite 走 HTTP 时 API 签发的 Secure Cookie 不会回传，
    // 因此必须写入真实 accessToken，而不是仅依赖 Cookie 会话
    const token = body.data?.accessToken || body.data?.access_token || body.data?.Token;
    expect(token).toBeTruthy();

    // 与 auth.setup 一致：把 API 登录产生的 Cookie 复制到浏览器上下文，
    // 仅靠 page.request 的会话不足以让页面侧的 /site、/Auth/Current 请求带上凭证
    const apiCookies = (await page.request.storageState()).cookies;
    if (apiCookies.length > 0) await page.context().addCookies(apiCookies);

    await page.addInitScript((value) => {
      localStorage.setItem('token', value);
    }, token);
  });

  test('首页渲染 KPI 与内容卡，且不出现加载失败', async ({ page }) => {
    await page.goto('/');

    await expect(page.locator('[data-cy="workbench"]')).toBeVisible();
    await expect(page.locator('[data-cy="workbench-banner"]')).toBeVisible();
    await expect(page.locator('[data-cy="workbench-kpis"] button').first()).toBeVisible();
    await expect(page.locator('[data-cy="workbench-widget"]').first()).toBeVisible();
    await expect(page.getByText('工作台加载失败')).toHaveCount(0);
  });

  test('点击刷新后工作台仍正常渲染', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('[data-cy="workbench-banner"]')).toBeVisible();

    await page.getByRole('button', { name: '刷新' }).click();

    await expect(page.locator('[data-cy="workbench-banner"]')).toBeVisible();
    await expect(page.getByText('工作台加载失败')).toHaveCount(0);
  });
});
