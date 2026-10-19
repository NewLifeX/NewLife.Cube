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

    await page.addInitScript(() => {
      localStorage.setItem('token', 'e2e-cookie-session');
    });
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
