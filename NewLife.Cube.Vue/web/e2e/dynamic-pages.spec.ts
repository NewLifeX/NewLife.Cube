import { test, expect } from '@playwright/test';

/**
 * 动态页回归：这些页面没有手写视图，列表列、表头、表单字段全部来自后端 `/meta`。
 *
 * 断言只用结构选择器（表格行、表头位置、缩进、Tab、表单控件），不依赖界面文案——
 * 界面语言随 `navigator.language` 变化，用文案断言会让用例在非中文环境随机失败。
 */
test.describe('动态页（/meta 驱动）', () => {
  test('部门列表按元数据渲染', async ({ page }) => {
    await page.goto('/Admin/Department');
    await expect(page.locator('h1')).toBeVisible();

    // 表头数量 >= 5 且存在数据行，说明列由后端元数据下发
    await expect(page.locator('.el-table__header th').nth(4)).toBeVisible({ timeout: 15000 });
    await expect(page.locator('.el-table__body tr').first()).toBeVisible();
  });

  test('菜单列表按树渲染', async ({ page }) => {
    await page.goto('/Admin/Menu');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 15000 });

    // 树形渲染的证据：存在缩进单元与展开图标
    expect(await page.locator('.el-table__indent').count()).toBeGreaterThan(0);
    expect(await page.locator('.el-table__expand-icon').count()).toBeGreaterThan(0);
  });

  test('角色页保留菜单权限入口并加载权限树', async ({ page }) => {
    await page.goto('/Admin/Role');
    const row = page.locator('.el-table__body tr').first();
    await expect(row).toBeVisible({ timeout: 15000 });

    // 行内除内置查看/编辑/删除外还有一个自定义动作，文案随语言变化，按位置取最后一个
    const actions = row.locator('.el-table__cell:last-child button');
    expect(await actions.count()).toBeGreaterThan(1);
    await actions.last().click();

    const dialog = page.locator('.el-dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.locator('.el-tree-node').first()).toBeVisible({ timeout: 15000 });
    expect(await dialog.locator('.el-checkbox').count()).toBeGreaterThan(0);
  });

  test('参数页与用户统计页同样由元数据渲染', async ({ page }) => {
    // 参数页：标准增删改查列表（枚举列按后端字典显示）
    await page.goto('/Admin/Parameter');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 15000 });
    await expect(page.locator('.el-table__header th').nth(4)).toBeVisible();

    // 用户统计页：只读列表，写入口由后端 setting.isReadOnly 收掉，图表走 charts 端点
    await page.goto('/Admin/UserStat');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 15000 });
    const toolbarTexts = await page.locator('.cube-table button').allInnerTexts();
    expect(toolbarTexts.some((text) => text.trim().length > 0)).toBe(true);
  });

  test('设置中心以二级 Tab 串联多个设置页', async ({ page }) => {
    await page.goto('/Admin/Cube');
    await expect(page.locator('.el-tabs__item').first()).toBeVisible({ timeout: 15000 });

    expect(await page.locator('.el-tabs__item').count()).toBeGreaterThanOrEqual(6);
    // 至少一个 Tab 能加载出元数据字段
    await expect(page.locator('.el-form-item, .fmc-field').first()).toBeVisible({ timeout: 15000 });
  });

  test('/Cube 区域列表同样按元数据渲染', async ({ page }) => {
    await page.goto('/Cube/CronJob');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 15000 });
    await expect(page.locator('.el-table__header th').nth(4)).toBeVisible();

    await page.goto('/Cube/Area');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 15000 });
    expect(await page.locator('.el-table__body tr').count()).toBeGreaterThan(5);
  });
});
