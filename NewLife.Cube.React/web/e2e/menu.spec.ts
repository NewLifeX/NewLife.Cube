/**
 * 菜单页 E2E（父编号下拉选择）
 *
 * 验证：菜单编辑表单的「父编号」为下拉选择（不再是数字输入），
 * 选项含菜单层级路径（如 系统管理/部门），对齐 MVC 与部门页父级。
 */
import { expect, test } from '@playwright/test';

const ROW = '.ant-table-tbody tr.ant-table-row';
const TABLE_TIMEOUT = 15000;

test.describe('菜单页（/Admin/Menu）', () => {
  test('列表加载且编辑表单父编号为下拉选择', async ({ page }) => {
    await page.goto('/Admin/Menu');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });

    // 打开第一行编辑
    await page.locator(ROW).first().locator('.cube-op-btn-op-edit').click();
    await expect(page.locator('.ant-modal')).toBeVisible({ timeout: 8000 });

    // 父编号：下拉选择（非数字输入）
    const parentRow = page.locator('.ant-modal .ant-form-item, .ant-modal .cube-form-inline-cell, .ant-modal .cube-form-vertical-item', {
      hasText: '父编号',
    });
    await parentRow.locator('.ant-select').click();
    // 选项含层级路径（系统管理/xxx）
    await expect(page.locator('.ant-select-item-option').first()).toBeVisible({ timeout: 8000 });
    await expect(page.locator('.ant-select-item-option', { hasText: '系统管理/' }).first()).toBeVisible();
    await expect(page.locator('.ant-select-item-option', { hasText: '系统管理' }).first()).toBeVisible();
    // 关闭下拉与弹窗
    await page.keyboard.press('Escape');
    await page.locator('.ant-modal button', { hasText: /取\s*消/ }).first().click();
  });
});
