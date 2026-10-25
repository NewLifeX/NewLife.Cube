/**
 * 页面呈现架构全链路 E2E
 *
 * 覆盖 M1 动态页的工具栏/图标、地区级联、链接与 dataAction、枚举字典文案，
 * M2 混合页的自定义行操作（角色页，见 dynamic-pages.spec.ts），
 * 以及 M3 自定义页不经过 CubeTable 的整页接管。
 *
 * 运行前需启动 CubeDemo（https://localhost:7116）与 Vue 开发服务器（默认 5187）：
 *   pnpm test:e2e --grep "页面呈现"
 */
import { test, expect } from '@playwright/test';
import { readMeta } from './metaHelper';

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

  test('M1 动态页：枚举列按后端下发的字典显示文案', async ({ page }) => {
    const apiBase = process.env.PLAYWRIGHT_API_URL || 'https://localhost:7116';
    const paramName = `E2E-ENUM-${Date.now()}`;
    // Kind=21（列表）是枚举值，页面应显示后端字典里的文案而不是数字
    const created = await page.request.post(`${apiBase}/api/Admin/Parameter`, {
      data: { Name: paramName, Value: '1', Category: 'E2E', Kind: 21, Enable: true },
      ignoreHTTPSErrors: true,
    });
    expect(created.ok()).toBeTruthy();
    const createdBody = await created.json();
    expect(createdBody.code).toBe(0);
    const paramId = createdBody?.data?.id ?? createdBody?.data?.ID;

    try {
      await page.goto('/Admin/Parameter');
      await expect(page.locator('.cube-table')).toBeVisible({ timeout: 15000 });
      await expect(page.locator('.el-table')).toBeVisible({ timeout: 15000 });

      // 字典文案由后端下发（已按界面语言本地化），从 /meta 取，避免绑死中文/英文
      const meta = await readMeta(page, '/api/Admin/Parameter/meta');
      const kindLabel = (meta.list ?? []).find((field) => field.name === 'Kind')?.dataSource?.['21'];
      expect(kindLabel).toBeTruthy();

      const row = page.locator('tr', { hasText: paramName }).first();
      await expect(row).toBeVisible({ timeout: 15000 });
      await expect(row).toContainText(kindLabel!);
    } finally {
      if (paramId) await page.request.delete(`${apiBase}/api/Admin/Parameter?ids=${paramId}`, { ignoreHTTPSErrors: true });
    }
  });

  test('M3 自定义页：工作台部件管理走 Widget 接口', async ({ page }) => {
    // 部件管理没有实体元数据（后端是自定义 JSON 接口），仍为手写页，但必须能正常渲染
    await page.goto('/Cube/Widget');
    await expect(page.locator('.wa-table .el-table__body tr').first()).toBeVisible({ timeout: 15000 });

    // 部件行数、启用开关、分组顺序入口都来自真实接口
    expect(await page.locator('.wa-table .el-table__body tr').count()).toBeGreaterThan(3);
    expect(await page.locator('.wa-table .el-switch').count()).toBeGreaterThan(3);
    expect(await page.locator('.wa-group').count()).toBeGreaterThan(0);
  });

  test('M3 自定义页：整页接管且不渲染 CubeTable', async ({ page }) => {
    // 菜单页已改为 /meta 动态渲染（树形表格），这里改用仍为自定义实现的文件管理页
    await page.goto('/Admin/File');
    await expect(page.locator('.page-header')).toBeVisible({ timeout: 15000 });
    await expect(page.locator('.file-list')).toBeVisible();
    await expect(page.locator('.cube-table')).toHaveCount(0);
  });
});
