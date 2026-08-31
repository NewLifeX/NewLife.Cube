/**
 * 工作台部件管理页 E2E（/Cube/Widget）
 *
 * 验证：页面加载无错误、部件列表表格渲染（名称/标题/状态/操作）、
 * 分组显示顺序 / 组内默认顺序面板存在。后端 WidgetController 仅系统管理员可访问。
 */
import { expect, test } from '@playwright/test';

const ROW = '.ant-table-tbody tr.ant-table-row';
const TABLE_TIMEOUT = 15000;

test.describe('工作台部件管理页（/Cube/Widget）', () => {
  test('页面加载无错误且部件列表渲染', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/Cube/Widget');
    await expect(page.locator(ROW).first()).toBeVisible({ timeout: TABLE_TIMEOUT });

    // 分组显示顺序 / 组内默认顺序面板存在（对齐 MVC Index.cshtml）
    await expect(page.getByText('分组显示顺序', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('组内默认顺序', { exact: true }).first()).toBeVisible();

    // 部件列表列：名称/标题/状态/操作
    await expect(page.locator('th', { hasText: '名称' })).toBeVisible();
    await expect(page.locator('th', { hasText: '标题' })).toBeVisible();
    await expect(page.locator('th', { hasText: '状态' })).toBeVisible();
    await expect(page.locator('th', { hasText: '操作' })).toBeVisible();

    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('部件行含启用/禁用状态与操作按钮', async ({ page }) => {
    await page.goto('/Cube/Widget');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });

    // 第一行状态列（✅启用/⛔禁用，列序 6）+ 操作按钮（禁用/启用，列序 7；AntD 按钮汉字间自动加空格）
    const firstRow = page.locator(ROW).first();
    const statusText = await firstRow.locator('td').nth(6).innerText();
    expect(statusText).toMatch(/启用|禁用/);
    await expect(firstRow.getByRole('button', { name: /启\s*用|禁\s*用/ }).first()).toBeVisible();
  });
});
