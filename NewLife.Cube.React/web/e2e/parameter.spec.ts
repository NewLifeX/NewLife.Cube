/**
 * 字典参数页 E2E（长数值 LongValue 大文本）
 *
 * 验证：列表加载 / 编辑表单「长数值」渲染为大文本 textarea（对齐 MVC）。
 * 后端 ParameterController：LongValue 配 ItemType=textarea（实体无长度信息，前端 itemType 映射驱动）。
 */
import { expect, test } from '@playwright/test';

const ROW = '.ant-table-tbody tr.ant-table-row';
const TABLE_TIMEOUT = 15000;

test.describe('字典参数页（/Admin/Parameter）', () => {
  test('列表页加载无错误且展示数据', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/Admin/Parameter');
    await expect(page.locator(ROW).first()).toBeVisible({ timeout: TABLE_TIMEOUT });
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('编辑表单「长数值」为大文本 textarea（对齐 MVC rows=3）', async ({ page }) => {
    await page.goto('/Admin/Parameter');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });

    await page.locator('button', { hasText: '编辑' }).first().click();
    await expect(page.locator('.ant-modal').first()).toBeVisible({ timeout: 8000 });

    // 定位「长数值」表单项（兼容 vertical/inline 双表单风格，参考 menu.spec.ts）
    const formItemSel = '.ant-modal .ant-form-item, .ant-modal .cube-form-inline-cell, .ant-modal .cube-form-vertical-item';
    const longValueItem = page.locator(formItemSel, { hasText: '长数值' }).first();
    await expect(longValueItem).toBeVisible();

    // 长数值应为 textarea（大文本，不再是单行 input），rows=3 对齐 MVC
    const textarea = longValueItem.locator('textarea');
    await expect(textarea).toBeVisible();
    await expect(textarea).toHaveAttribute('rows', '3');

    // 普通文本字段（名称）仍为 input，不受影响
    const nameItem = page.locator(formItemSel, { hasText: /^名称/ }).first();
    await expect(nameItem.locator('input').first()).toBeVisible();

    // 关闭弹窗
    await page.locator('.ant-modal button', { hasText: /取\s*消/ }).first().click().catch(() => undefined);
  });

  test('双击行进入编辑弹窗（对齐 MVC 双击行任意地方编辑）', async ({ page }) => {
    await page.goto('/Admin/Parameter');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });

    // 双击第一行 → 编辑弹窗打开（无需点操作列按钮）
    await page.locator(ROW).first().dblclick();
    await expect(page.locator('.ant-modal').first()).toBeVisible({ timeout: 8000 });
    // 编辑弹窗含「长数值」textarea（说明进入的是编辑而非查看）
    const formItemSel = '.ant-modal .ant-form-item, .ant-modal .cube-form-inline-cell, .ant-modal .cube-form-vertical-item';
    const longValueItem = page.locator(formItemSel, { hasText: '长数值' }).first();
    await expect(longValueItem.locator('textarea')).toBeVisible({ timeout: 8000 });

    await page.locator('.ant-modal button', { hasText: /取\s*消/ }).first().click().catch(() => undefined);
  });
});
