import { test, expect } from '@playwright/test';

/**
 * 消息通知前端冒烟：导航栏铃铛可见，消息中心路由可加载。
 * 已读/处理等交互依赖真实通知数据，由接口层单元测试覆盖。
 */
test.describe('消息通知', () => {
  test('导航栏铃铛与消息中心页面可加载', async ({ page }) => {
    await page.goto('/');

    const bell = page.locator('.notify-badge button').first();
    await expect(bell).toBeVisible();

    await page.goto('/notifications');
    await expect(page.locator('.notification-page')).toBeVisible();
    await expect(page.locator('.notification-page h2')).toBeVisible();
  });
});
