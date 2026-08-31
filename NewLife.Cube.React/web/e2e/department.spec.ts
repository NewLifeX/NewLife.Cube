/**
 * 部门列表页 E2E（搜索字段控件驱动）
 *
 * 验证：页面加载 / 列表展示 / 父级下拉（层级路径 label，同名部门可区分）/ 父级搜索过滤 / 管理者搜索字段已移除。
 * 后端 DepartmentController.SearchFields：ParentID 配部门缓存 DataSource（层级路径 label），ManagerId 移除（对齐 MVC）。
 */
import { expect, test } from '@playwright/test';

const ROW = '.ant-table-tbody tr.ant-table-row';
const TABLE_TIMEOUT = 15000;

test.describe('部门列表页（/Admin/Department）', () => {
  test('列表页加载无错误且展示数据', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/Admin/Department');
    await expect(page.locator(ROW).first()).toBeVisible({ timeout: TABLE_TIMEOUT });
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('搜索区无「管理者」数字输入，父级为下拉且选项含层级路径', async ({ page }) => {
    await page.goto('/Admin/Department');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });

    // 管理者搜索字段已移除（后端不支持按管理者过滤，对齐 MVC）
    await expect(page.locator('.cube-search-item', { hasText: '管理者' })).toHaveCount(0);

    // 父级为下拉选择（不再是数字输入）
    const parentItem = page.locator('.cube-search-item', { hasText: '父级' });
    await expect(parentItem).toHaveCount(1);
    await expect(parentItem.locator('.ant-select')).toBeVisible();

    // 打开下拉：选项应含层级路径，同名「行政部」可区分（总公司/行政部 vs 上海分公司/行政部）
    await parentItem.locator('.ant-select').click();
    await expect(page.locator('.ant-select-item-option').first()).toBeVisible({ timeout: 8000 });
    await expect(page.getByTitle('总公司/行政部', { exact: true }).first()).toBeVisible();
    await expect(page.getByTitle('上海分公司/行政部', { exact: true }).first()).toBeVisible();
    await expect(page.getByTitle('上海分公司', { exact: true }).first()).toBeVisible();
    await page.keyboard.press('Escape');
  });

  test('选择父级后搜索过滤出对应子部门', async ({ page }) => {
    await page.goto('/Admin/Department');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    await page
      .waitForFunction(() => !document.querySelector('.ant-spin-spinning'), undefined, { timeout: 10000 })
      .catch(() => undefined);
    await page.waitForTimeout(300);
    const rowsBefore = await page.locator(ROW).count();
    expect(rowsBefore).toBeGreaterThan(0);

    // 监听列表请求，断言提交了 ParentID=1（总公司）
    let listReq: string | null = null;
    const onReq = (req: { url: () => string; method: () => string }) => {
      if (req.url().includes('/api/Admin/Department') && req.method() === 'GET') listReq = req.url();
    };
    page.on('request', onReq as never);

    await page.locator('.cube-search-item', { hasText: '父级' }).locator('.ant-select').click();
    await page.getByTitle('总公司', { exact: true }).first().click();
    await page.getByRole('button', { name: /搜\s*索/ }).click();
    await page.waitForTimeout(1200);

    const rows = await page.locator(ROW).count();
    expect(rows).toBeGreaterThan(0);
    expect(rows).toBeLessThan(rowsBefore);
    expect(listReq, '搜索请求应携带 ParentID=1').toContain('ParentID=1');
    page.off('request', onReq as never);
  });
});
