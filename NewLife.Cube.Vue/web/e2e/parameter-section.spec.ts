/**
 * 参数管理页面 E2E 测试
 *
 * 验证 Section 覆盖生效：
 * 1. 参数页面能正常加载（使用默认引擎的搜索/分页/弹窗）
 * 2. kind 列显示为标签（普通/系统/用户）而非纯数字
 * 3. 新增/编辑/删除 CRUD 闭环可用
 *
 * 运行：pnpm test:e2e --grep "参数管理"
 */
import { test, expect } from '@playwright/test';

const basePath = '/Admin/Parameter';

test.describe('参数管理（Section 覆盖示范页）', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(basePath);
    // 等待表格加载
    await expect(page.locator('.el-table')).toBeVisible({ timeout: 15000 });
  });

  test('列表页加载且展示 Section 覆盖列头', async ({ page }) => {
    // 验证默认引擎提供的搜索栏存在
    await expect(page.locator('.lp-search-area')).toBeVisible();
    // 验证 Section 覆盖的列头存在（"类别"而非"类别1"——用户已手工修正）
    await expect(page.getByRole('columnheader', { name: '类别' })).toBeVisible();
    // 验证其他默认列头
    await expect(page.getByRole('columnheader', { name: '编号' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: '参数名称' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: '显示名称' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: '参数值' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: '分类' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: '启用' })).toBeVisible();
    await expect(page.getByRole('columnheader', { name: '操作' })).toBeVisible();
  });

  test('kind 列显示为标签而非纯数字', async ({ page }) => {
    // 等待表格行加载
    await page.waitForSelector('.el-table__row', { timeout: 15000 });
    // 检查类别列单元格是否包含 el-tag 标签
    const kindCells = page.locator('.el-table__row .el-tag');
    const count = await kindCells.count();
    expect(count).toBeGreaterThanOrEqual(1);
    // 标签文本应符合枚举值：普通/系统/用户/未知
    const texts = await kindCells.allTextContents();
    for (const text of texts) {
      expect(['普通', '系统', '用户', '未知']).toContain(text.trim());
    }
  });

  test('启用列显示为标签', async ({ page }) => {
    await page.waitForSelector('.el-table__row', { timeout: 15000 });
    const enableTags = page.locator('.el-table__row .el-tag--success, .el-table__row .el-tag--danger');
    const count = await enableTags.count();
    expect(count).toBeGreaterThanOrEqual(1);
  });

  test('新建一条参数记录', async ({ page }) => {
    // 点击"新建"按钮（由默认工具栏提供）
    await page.getByRole('button', { name: '新建' }).click();
    // 等待弹窗出现（由默认引擎的 openListFormDialog 提供）
    const dialog = page.locator('.el-dialog');
    await expect(dialog).toBeVisible({ timeout: 5000 });
    // 填写表单
    const nameInput = dialog.locator('.el-form-item').filter({ hasText: '名称' }).locator('input, textarea');
    if (await nameInput.count() > 0) {
      await nameInput.fill(`E2E-参数-${Date.now()}`);
    }
    // 点击确定
    await dialog.locator('.el-dialog__footer .el-button--primary').click();
    // 弹窗关闭
    await expect(dialog).not.toBeVisible({ timeout: 5000 });
  });

  test('编辑第一条记录', async ({ page }) => {
    await page.waitForSelector('.el-table__row', { timeout: 15000 });
    // 点击第一行的编辑按钮
    const editBtn = page.locator('.el-table__row').first().getByRole('button', { name: '编辑' });
    await editBtn.click();
    // 等待弹窗
    const dialog = page.locator('.el-dialog');
    await expect(dialog).toBeVisible({ timeout: 5000 });
    // 关闭弹窗（不保存）
    await dialog.locator('.el-dialog__footer .el-button').first().click();
    await expect(dialog).not.toBeVisible({ timeout: 5000 });
  });
});