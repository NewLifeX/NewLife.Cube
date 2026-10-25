/**
 * 参数管理页面 E2E（Section 覆盖示范页）
 *
 * 验证参数页使用默认引擎提供的查询区/表格/工具栏，并正确渲染列头。
 *
 * 断言不写死界面文案：页面表头来自后端 `/meta`（按请求语言本地化），
 * 因此先取元数据的 displayName/header，再逐列比对，界面语言切换不会让用例失效。
 *
 * 说明：当前角色在该页没有新增按钮，CRUD 闭环由后端端点测试与
 * `skin-compliance.spec.ts`（TestField 播种/清理）覆盖，这里只保证页面结构合规。
 *
 * 运行：pnpm test:e2e --grep "参数管理"
 */
import { test, expect } from '@playwright/test';
import { readListHeaders } from './metaHelper';

const basePath = '/Admin/Parameter';

test.describe('参数管理（Section 覆盖示范页）', () => {
  test('列表页加载且查询区/表格/操作列齐备', async ({ page }) => {
    await page.goto(basePath);
    await expect(page.locator('.el-table')).toBeVisible({ timeout: 20000 });

    // 查询区：搜索框 + 查询/重置按钮（工具栏里还有隐藏的导入 input，按可见性取第一个）
    await expect(page.locator('.cube-table input:not([type="hidden"]):visible').first()).toBeVisible();
    expect(await page.locator('.cube-table button').count()).toBeGreaterThan(1);
    // 数据区：最后一列是操作列
    await expect(page.locator('.el-table__header th').last()).toBeVisible();
  });

  test('列头与元数据一致', async ({ page }) => {
    await page.goto(basePath);
    await expect(page.locator('.el-table__header th').nth(2)).toBeVisible({ timeout: 20000 });

    const expected = await readListHeaders(page, '/api/Admin/Parameter/meta');
    expect(expected.length).toBeGreaterThan(5);

    const headers = (await page.locator('.el-table__header th').allInnerTexts()).join('|');
    for (const name of expected.slice(0, 5)) {
      expect(headers).toContain(name);
    }
  });
});
