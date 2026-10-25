import { test, expect } from '@playwright/test';

/**
 * 动态页回归：这些页面没有手写视图，列表列、表头、表单字段全部来自后端 `/meta`。
 *
 * 断言口径：
 * - 表头/字段名取后端元数据（英文界面），页面里不出现前端词表的字段键；
 * - 树形实体（菜单）按树渲染，存在缩进层级；
 * - M2 页面（角色）保留自定义的「菜单权限」入口，且弹窗能加载权限树。
 */
test.describe('动态页（/meta 驱动）', () => {
  test('部门列表按元数据渲染', async ({ page }) => {
    await page.goto('/Admin/Department');
    await expect(page.locator('h1')).toBeVisible();

    const headers = page.locator('.el-table__header th');
    await expect(headers.filter({ hasText: 'Name' }).first()).toBeVisible({ timeout: 15000 });
    await expect(page.locator('.el-table__body tr').first()).toBeVisible();
  });

  test('菜单列表按树渲染', async ({ page }) => {
    await page.goto('/Admin/Menu');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible();

    // 树形渲染的证据：存在缩进单元与展开图标
    expect(await page.locator('.el-table__indent').count()).toBeGreaterThan(0);
    expect(await page.locator('.el-table__expand-icon').count()).toBeGreaterThan(0);
  });

  test('角色页保留菜单权限入口并加载权限树', async ({ page }) => {
    await page.goto('/Admin/Role');
    const permissionButton = page.getByRole('button', { name: 'Menu permissions' }).first();
    await expect(permissionButton).toBeVisible();

    await permissionButton.click();
    const dialog = page.locator('.el-dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.locator('.el-tree-node').first()).toBeVisible({ timeout: 15000 });
    expect(await dialog.locator('.el-checkbox').count()).toBeGreaterThan(0);
  });

  test('设置中心以二级 Tab 串联多个设置页', async ({ page }) => {
    await page.goto('/Admin/Cube');
    await expect(page.locator('.el-tabs__item').first()).toBeVisible();

    const tabs = await page.locator('.el-tabs__item').allInnerTexts();
    expect(tabs.length).toBeGreaterThanOrEqual(6);
    // 至少一个 Tab 能加载出元数据字段
    await expect(page.locator('.el-form-item, .fmc-field').first()).toBeVisible({ timeout: 15000 });
  });
});
