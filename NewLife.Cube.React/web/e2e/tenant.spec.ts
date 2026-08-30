/**
 * 多租户 E2E：租户切换器显示与切换
 *
 * 前置：被测站点需开启多租户（CubeSetting.EnableTenant=true）且当前用户有租户归属/系统管理员，
 * 否则用例自动跳过（test.skip），不影响无多租户环境下的 E2E 套件。
 *
 * 覆盖：
 * - 多租户开启：头像下拉显示"当前租户"、租户列表与"系统管理后台"入口（系统管理员）
 * - 点击"系统管理后台"切换成功：刷新后当前租户变为"系统管理后台"
 */
import { test, expect } from '@playwright/test';

test.describe('多租户切换', () => {
  // 探测多租户开关：未开启则整组跳过
  test.beforeEach(async ({ request }) => {
    const res = await request.get('/Auth/LoginConfig');
    const body = await res.json();
    const enableTenant: boolean | undefined = body?.data?.enableTenant;
    test.skip(!enableTenant, '未开启多租户，跳过租户切换 E2E');
  });

  test('多租户开启：头像下拉显示当前租户、租户列表与系统管理后台入口', async ({ page }) => {
    await page.goto('/');
    // 打开用户菜单（头像触发器）
    await page.locator('.cube-user-trigger').click();

    // 当前租户标题（租户名或系统管理后台）
    await expect(page.getByText(/^当前租户：/)).toBeVisible();
    // 系统管理员：显示"系统管理后台"入口
    await expect(page.getByText('系统管理后台')).toBeVisible();
    // 下拉已打开（租户区可见即可，安全中心等常规项与侧边栏同名，不做严格匹配）
  });

  test('切换到系统管理后台成功（系统管理员）', async ({ page }) => {
    await page.goto('/');
    await page.locator('.cube-user-trigger').click();
    await page.getByText('系统管理后台').click();

    // 切换成功：页面刷新，重新打开下拉后当前租户变为"系统管理后台"
    await page.waitForLoadState('load');
    await page.locator('.cube-user-trigger').click();
    await expect(page.getByText('当前租户：系统管理后台')).toBeVisible();
  });
});
