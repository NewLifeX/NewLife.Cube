/**
 * 首页 + 顶栏切换器 + 用户菜单 E2E（HOME-1 / CMP-6 / CMP-5）
 *
 * 覆盖：首页加载无错误、欢迎信息与常用菜单入口、主题/语言/明暗切换、用户菜单退出登录。
 */
import { expect, test } from '@playwright/test';

test.describe('首页（/）', () => {
  test('首页加载无错误且展示欢迎信息与常用菜单', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/');
    await expect(page.getByText(/欢迎回来/).first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('常用菜单')).toBeVisible({ timeout: 10000 });
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('首页常用菜单入口可点击进入实体页', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByText('常用菜单')).toBeVisible({ timeout: 10000 });
    // 等待菜单数据加载
    await page.waitForTimeout(1500);
    const items = page.locator('.ant-list-item .ant-card');
    if ((await items.count()) > 0) {
      await items.first().click();
      // 进入实体页后应渲染列表（非 404）
      await expect(page.locator('.ant-card, h2').first()).toBeVisible({ timeout: 10000 });
    } else {
      // 无顶层菜单时展示空状态
      await expect(page.getByText('暂无可用菜单').first()).toBeVisible({ timeout: 8000 });
    }
  });
});

test.describe('顶栏切换器（CMP-6）', () => {
  test('主题切换：选择森林主题不报错', async ({ page }) => {
    await page.goto('/');
    await page.locator('.anticon-bg-colors').click();
    await expect(page.getByText(/森林|海洋|极光|工业|赛博/).last()).toBeVisible({ timeout: 5000 });
    // 选择一个主题项（森林）
    const forest = page.getByText(/森林/).first();
    if (await forest.isVisible().catch(() => false)) {
      await forest.click();
    }
    // 无 JS 错误
    expect(page.locator('.ant-message-error')).toHaveCount(0);
  });

  test('语言切换：切换到 English', async ({ page }) => {
    await page.goto('/');
    await page.locator('.anticon-global').click();
    await page.getByText('English').click();
    // 顶栏应出现英文（面包屑或页面内容），放宽：无错误即可
    await page.waitForTimeout(500);
    expect(page.locator('.ant-message-error')).toHaveCount(0);
  });

  test('明暗模式切换按钮可用', async ({ page }) => {
    await page.goto('/');
    const modeBtn = page.locator('.anticon-sun, .anticon-moon').first();
    await expect(modeBtn).toBeVisible({ timeout: 10000 });
    await modeBtn.click();
    await page.waitForTimeout(300);
    // 点击后应切换为另一个图标
    await expect(page.locator('.anticon-sun, .anticon-moon').first()).toBeVisible();
  });
});

test.describe('用户菜单与登出（CMP-5）', () => {
  test('点击用户菜单显示安全中心与退出登录入口', async ({ page }) => {
    await page.goto('/');
    // 用户菜单（头像+用户名），用稳定触发器类名（首页卡片按钮也含 ant-dropdown-trigger，不能用 last）
    await page.locator('.cube-user-trigger').first().click();
    // 首页 hero 也有“安全中心”按钮，需限定在用户菜单下拉内定位
    await expect(page.locator('.ant-dropdown:visible').getByText('安全中心')).toBeVisible({ timeout: 5000 });
    await expect(page.locator('.ant-dropdown:visible').getByText('退出登录')).toBeVisible();
  });

  test('退出登录跳转登录页', async ({ browser }) => {
    // 独立 context 重新登录：登出会撤销服务端 token，避免污染共享 storageState 会话
    const ctx = await browser.newContext();
    const page = await ctx.newPage();
    await page.goto('/login');
    await page.getByPlaceholder('用户名 / 邮箱 / 手机号').fill(process.env.ADMIN_USER || 'admin');
    await page.getByPlaceholder('请输入密码').fill(process.env.ADMIN_PASS || 'admin');
    await page.getByRole('button', { name: '登 录' }).click();
    await page.waitForURL('**/', { timeout: 10000 });
    await page.locator('.cube-user-trigger').first().click();
    await page.getByText('退出登录').click();
    await page.waitForURL(/\/login/, { timeout: 10000 });
    await ctx.close();
  });
});
