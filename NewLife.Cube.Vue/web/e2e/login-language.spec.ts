import { test, expect } from '@playwright/test';

/**
 * 登录页语言与站点信息回归
 *
 * 历史问题：`.login-language` 与全屏 `.login-wrapper` 同层级且被后者覆盖，
 * 语言下拉点不开，表现为「登录页不支持多语言」。本用例按真实鼠标路径驱动：
 * 悬停语言切换器 → 选择 English → 断言 i18n 与本地偏好同步。
 *
 * 语言断言不依赖固定文案以外的文案；站点标题用 mock 站点名验证
 * `/Cube/Site` 的 name 能写入 document.title。
 */
test.describe('登录页语言切换', () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  test('语言下拉可打开并切换到 English', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('cube-language', 'zh-CN');
    });

    await page.goto('/login');
    await expect(page.locator('[data-cy="username"]')).toBeVisible({ timeout: 15000 });

    const trigger = page.locator('.login-language .el-dropdown-link');
    const box = await trigger.boundingBox();
    expect(box, '语言切换器应可见').not.toBeNull();

    // Element Plus 默认 hover 触发；这里用真实鼠标移动到切换器上，等价用户操作
    await page.mouse.move(box!.x + box!.width / 2, box!.y + box!.height / 2);

    const english = page.getByRole('menuitem', { name: 'English' });
    await expect(english).toBeVisible({ timeout: 5000 });
    await english.click();

    await expect(page.locator('html')).toHaveAttribute('lang', 'en-US');
    await expect(page.locator('.login-btn')).toContainText('Sign in');
    await expect.poll(() => page.evaluate(() => localStorage.getItem('cube-language'))).toBe('en-US');
  });

  test('浏览器标题跟随站点名称', async ({ page }) => {
    await page.route('**/Cube/Site*', async (route) => {
      const response = await route.fetch();
      const body = await response.json();
      body.data = { ...(body.data ?? {}), name: 'E2E 站点' };
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
    });

    await page.goto('/login');
    await expect(page.locator('[data-cy="username"]')).toBeVisible({ timeout: 15000 });

    await expect(page).toHaveTitle('E2E 站点');
  });
});
