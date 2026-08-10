import { test, expect } from '@playwright/test';

const basePath = '/Admin/Test/TestField';

test.describe('字段类型全覆盖 E2E（TestField 真实 CRUD）', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(basePath);
  });

  test('列表页加载且展示多类型字段列', async ({ page }) => {
    await expect(page.locator('.el-table')).toBeVisible();
    await expect(page.getByRole('columnheader', { name: '短文本' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: '数值' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: '是否启用' })).toBeVisible();
  });

  test('新建：表单按字段类型渲染正确控件', async ({ page }) => {
    await page.getByRole('button', { name: '新建' }).click();
    const dialog = page.locator('.el-dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.locator('input[type="text"]').first()).toBeVisible();
    await expect(dialog.locator('.el-input-number').first()).toBeVisible();
    await expect(dialog.locator('.el-switch').first()).toBeVisible();
    await expect(dialog.locator('.el-date-editor').first()).toBeVisible();
    await expect(dialog.locator('.lov-select').first()).toBeVisible();
    await expect(dialog.locator('textarea').first()).toBeVisible();
  });

  test('新建、编辑、删除一条记录', async ({ page }) => {
    const createdName = `PW-${Date.now()}`;
    const editedName = `${createdName}-edited`;

    await page.getByRole('button', { name: '新建' }).click();
    const dialog = page.locator('.el-dialog');
    await expect(dialog).toBeVisible();
    await dialog.locator('input[type="text"]').first().fill(createdName);
    await dialog.locator('.el-input-number input').first().fill('123');
    await dialog.getByRole('button', { name: '保存' }).click();

    await expect(page.locator('.el-message--success')).toBeVisible();
    await expect(page.getByText(createdName)).toBeVisible();

    const row = page.locator('tr', { hasText: createdName }).first();
    await row.locator('.el-button--primary').click();
    await expect(dialog).toBeVisible();
    await dialog.locator('input[type="text"]').first().fill(editedName);
    await dialog.getByRole('button', { name: '保存' }).click();

    await expect(page.locator('.el-message--success')).toBeVisible();
    await expect(page.getByText(editedName)).toBeVisible();

    const editedRow = page.locator('tr', { hasText: editedName }).first();
    await editedRow.locator('.el-button--danger').click();
    await page.getByRole('button', { name: '确定' }).click();

    await expect(page.locator('.el-message--success')).toBeVisible();
    await expect(page.getByText(editedName)).not.toBeVisible();
  });
});
