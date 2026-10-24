/**
 * 页面呈现架构全链路 E2E
 *
 * 覆盖 M1 动态页的工具栏/图标、地区级联、链接与 dataAction，M2 混合页的 preset + 单元格插槽，
 * 以及 M3 自定义页不经过 CubeTable 的整页接管。
 *
 * 运行前需启动 CubeDemo（https://localhost:7116）与 Vue 开发服务器（默认 5187）：
 *   pnpm test:e2e --grep "页面呈现"
 */
import { test, expect } from '@playwright/test';

const testFieldPath = '/Test/TestField';

test.describe('页面呈现架构全链路', () => {
  test('M1 动态页：工具栏按钮按后端声明渲染并带图标', async ({ page }) => {
    await page.goto(testFieldPath);
    await expect(page.locator('.el-table')).toBeVisible({ timeout: 15000 });

    const toolbar = page.getByRole('button', { name: '测试工具栏' });
    await expect(toolbar).toBeVisible();
    await expect(toolbar.locator('.el-icon')).toBeVisible();
  });

  test('M1 动态页：地区级联搜索控件可展开', async ({ page }) => {
    await page.goto(testFieldPath);
    await expect(page.locator('.el-table')).toBeVisible({ timeout: 15000 });

    await page.locator('.cts-toggle').click();
    await expect(page.locator('.area-select')).toBeVisible();
  });

  test('M1 动态页：链接列跳转与 dataAction 动作都可用', async ({ page }) => {
    const createdName = `E2E-PAGE-${Date.now()}`;
    const apiBase = process.env.PLAYWRIGHT_API_URL || 'https://localhost:7116';

    // 测试页对当前角色未开放新增按钮，用真实实体接口播种数据；列表/链接/动作仍走页面全链路
    const created = await page.request.post(`${apiBase}/api/Test/TestField`, {
      data: {
        ShortText: createdName,
        LongText: '',
        FileUrl: '',
        Avatar: '',
        JsonVal: '',
        HtmlVal: '',
        MarkdownVal: '',
        ColorVal: '',
        IconVal: '',
        MailVal: '',
        MobileVal: '',
        UrlVal: '',
        UserId: '',
        MultiVal: '',
        ListMVal: '',
      },
      ignoreHTTPSErrors: true,
    });
    expect(created.ok()).toBeTruthy();
    const createdBody = await created.json();
    expect(createdBody.code).toBe(0);
    const id = createdBody?.data?.id ?? createdBody?.data?.ID;
    expect(id).toBeTruthy();

    try {
      await page.goto(testFieldPath);
      await expect(page.locator('.el-table')).toBeVisible({ timeout: 15000 });
      await expect(page.getByText(createdName)).toBeVisible();

      const row = page.locator('tr', { hasText: createdName }).first();

      // dataAction：阻止跳转，请求后端动作后提示并刷新列表
      await row.locator('a', { hasText: '执行' }).click();
      await expect(page.locator('.el-message--success')).toBeVisible();
      await expect(row).toBeVisible();

      // 普通链接：按 {Id} 占位符生成目标地址
      await row.locator('a', { hasText: '查看' }).click();
      await expect(page).toHaveURL(/\/Test\/TestField\?Id=\d+/);
    } finally {
      if (id) await page.request.delete(`${apiBase}/api/Test/TestField?ids=${id}`, { ignoreHTTPSErrors: true });
    }
  });

  test('M2 混合页：preset 调整列序且单元格插槽生效', async ({ page }) => {
    await page.goto('/Admin/Parameter');
    await expect(page.locator('.cube-table')).toBeVisible({ timeout: 15000 });
    await expect(page.locator('.el-table')).toBeVisible({ timeout: 15000 });

    const headers = await page.locator('.el-table__header th').allTextContents();
    const kindIndex = headers.findIndex((text) => text.trim() === 'Kind');
    const idIndex = headers.findIndex((text) => text.trim() === 'ID');
    expect(kindIndex).toBeGreaterThanOrEqual(0);
    expect(idIndex).toBeGreaterThanOrEqual(0);
    // preset list.order 把 Kind 前置，验证真实页面消费了页面呈现配置
    expect(kindIndex).toBeLessThan(idIndex);

    // 单元格插槽把枚举渲染成标签
    await expect(page.locator('.el-table__row .el-tag').first()).toBeVisible({ timeout: 15000 });
  });

  test('M3 自定义页：整页接管且不渲染 CubeTable', async ({ page }) => {
    await page.goto('/Admin/Menu');
    await expect(page.getByRole('heading', { name: '菜单管理' })).toBeVisible({ timeout: 15000 });
    await expect(page.locator('.menu-container')).toBeVisible();
    await expect(page.locator('.cube-table')).toHaveCount(0);
  });
});
