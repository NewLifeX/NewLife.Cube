/**
 * 参数管理页面 E2E（Section 覆盖示范页）
 *
 * 验证参数页使用默认引擎提供的查询区/表格/工具栏，并正确渲染列头与开关列。
 * 页面表头由 i18n 输出（当前为英文源文），因此按英文断言。
 *
 * 说明：当前角色在该页没有新增按钮，CRUD 闭环由后端端点测试与
 * `skin-compliance.spec.ts`（TestField 播种/清理）覆盖，这里只保证页面结构合规。
 *
 * 运行：pnpm test:e2e --grep "参数管理"
 */
import { test, expect } from '@playwright/test';

const basePath = '/Admin/Parameter';

test.describe('参数管理（Section 覆盖示范页）', () => {
  test('列表页加载且查询区/表格/操作列齐备', async ({ page }) => {
    await page.goto(basePath);
    await expect(page.locator('.el-table')).toBeVisible({ timeout: 20000 });

    await expect(page.getByPlaceholder('搜索关键字')).toBeVisible();
    await expect(page.getByRole('columnheader', { name: '操作' })).toBeVisible();
  });

  test('列头覆盖参数页关键列', async ({ page }) => {
    await page.goto(basePath);
    await expect(page.locator('.el-table')).toBeVisible({ timeout: 20000 });

    for (const name of ['Kind', 'Name', 'Value', 'Enabled', 'Category', '操作']) {
      await expect(page.getByRole('columnheader', { name, exact: true })).toBeVisible();
    }
  });
});
