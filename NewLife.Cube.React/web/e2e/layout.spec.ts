/**
 * 布局与导航 E2E（FND-6 路由守卫 / FND-7 布局 / FND-8 多标签 / FND-14 移动端导航）
 *
 * 覆盖：多标签增删切、面包屑、未登录回跳、移动端 Drawer 导航。
 */
import { expect, test } from '@playwright/test';

test.describe('多标签页（FND-8）', () => {
  test('打开页面自动加标签，可点击切换回首页', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('.ant-tabs-tab', { hasText: '首页' })).toBeVisible({ timeout: 10000 });

    // 通过首页“常用菜单”项卡片 SPA 内导航到实体页（不可用整页 goto，会重置内存标签状态）
    // 注意用 URL 文本精确定位内层项卡片，避免误匹配外层“常用菜单”卡片（后代含相同文本、无 onClick）
    await page.locator('.cube-home-card .ant-list .ant-card', { hasText: '/Admin/User' }).first().click();
    await expect(page.locator('.ant-tabs-tab')).toHaveCount(2, { timeout: 10000 });

    // 标签标题取菜单名而非"默认页面"（设计规范 §6.2：标签名与菜单关联）
    await expect(page.locator('.ant-tabs-tab', { hasText: '用户' })).toBeVisible({ timeout: 10000 });
    await expect(page.locator('.ant-tabs-tab', { hasText: '默认页面' })).toHaveCount(0);

    // 点击首页标签切换回来
    await page.locator('.ant-tabs-tab', { hasText: '首页' }).click();
    await expect(page.locator('.cube-home-hero')).toBeVisible({ timeout: 8000 });
  });
});

test.describe('面包屑（FND-7）', () => {
  test('实体页显示当前菜单面包屑', async ({ page }) => {
    await page.goto('/Admin/User');
    await expect(page.locator('.cube-shell-header .ant-breadcrumb')).toContainText('用户', { timeout: 10000 });
  });
});

test.describe('登录守卫回跳（FND-6）', () => {
  test('未登录访问受保护页 → 跳登录并携带回跳参数', async ({ browser }) => {
    // 显式清空登录态，避免继承 project 级 storageState
    const ctx = await browser.newContext({ storageState: { cookies: [], origins: [] } });
    const page = await ctx.newPage();
    await page.goto('/Admin/User');
    await page.waitForURL(/\/login\?r=/, { timeout: 8000 });
    expect(page.url()).toContain(encodeURIComponent('/Admin/User'));
    await ctx.close();
  });
});

test.describe('移动端导航（FND-14）', () => {
  test('窄视口 Sider 隐藏，切换按钮打开 Drawer 导航', async ({ page }) => {
    await page.setViewportSize({ width: 800, height: 900 });
    await page.goto('/');
    // <lg 时桌面 Sider 隐藏
    await expect(page.locator('.cube-shell-sider')).toBeHidden({ timeout: 8000 });
    // 顶栏切换按钮 → 移动 Drawer（7081 一体站点 antd 按钮可能被覆盖层遮挡，用原生 click 触发）
    // 注意：AntD5 Drawer 的 className 加在 content 元素上，.cube-mobile-drawer 即 .ant-drawer-content
    await page.evaluate(() => {
      (document.querySelector('button[aria-label="切换导航"]') as HTMLButtonElement | null)?.click();
    });
    await expect(page.locator('.cube-mobile-drawer')).toBeVisible({ timeout: 8000 });
    // Drawer 内渲染菜单
    await expect(page.locator('.cube-mobile-drawer').getByText('系统管理')).toBeVisible();
    await page.keyboard.press('Escape');
  });
});
