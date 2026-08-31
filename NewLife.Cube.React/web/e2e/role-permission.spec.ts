/**
 * 角色权限配置 E2E（操作列「权限」弹窗，对齐 MVC 角色授权）
 *
 * 验证：操作列权限按钮（编辑左侧）/ 弹窗树形表格（菜单+查看/添加/修改/删除/快捷）/
 * 勾选保存持久化（后端落库）。测试角色 e2e-{ts}-perm 用完即删，保证数据隔离。
 */
import { expect, test } from '@playwright/test';

const PREFIX = `e2e-${Date.now()}`;
const ROW = '.ant-table-tbody tr.ant-table-row';
const TABLE_TIMEOUT = 15000;

test.describe('角色权限（/Admin/Role）', () => {
  test('操作列含「权限」按钮且位于编辑左侧', async ({ page }) => {
    await page.goto('/Admin/Role');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });

    const row = page.locator(ROW).first();
    const opBtns = row.locator('td:last-child button');
    await expect(opBtns.first()).toHaveText('权限');
    await expect(opBtns.nth(1)).toHaveText('编辑');
    await expect(opBtns.nth(2)).toHaveText('删除');
  });

  test('权限弹窗：树形表格加载，勾选保存后重新打开保持（后端落库）', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/Admin/Role');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });

    // 新增测试角色（工具栏按钮精确定位）
    const name = `${PREFIX}-perm`;
    await page.locator('.cube-toolbar').getByRole('button', { name: /新\s*增/ }).click();
    await expect(page.locator('.ant-modal')).toBeVisible({ timeout: 8000 });
    await page.locator('.ant-modal input').first().fill(name);
    await page.getByRole('button', { name: /保\s*存/ }).click();
    await expect(page.locator('.ant-message')).toBeVisible({ timeout: 8000 });
    await page.waitForTimeout(1200);
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);

    // 新角色（ID 最大）通常在第一行；按名称定位兜底
    const row = page.locator(ROW, { hasText: name }).first();
    await expect(row).toBeVisible({ timeout: TABLE_TIMEOUT });

    // 打开权限弹窗：表头 + 树形数据
    await row.locator('.cube-op-btn-op-perm').click();
    await expect(page.locator('.ant-modal')).toBeVisible({ timeout: 8000 });
    const modal = page.locator('.ant-modal');
    await expect(modal.locator('.ant-modal-title')).toContainText(name);
    const headers = await modal.locator('th').allInnerTexts();
    expect(headers.join(',')).toContain('查看');
    expect(headers.join(',')).toContain('添加');
    expect(headers.join(',')).toContain('修改');
    expect(headers.join(',')).toContain('删除');
    // 树形展开：应用日志（AppLog）行可见
    const appLogRow = modal.locator('tbody tr', { hasText: '应用日志' }).first();
    await expect(appLogRow).toBeVisible();

    // 勾选「查看」并保存（点击 antd 视觉 checkbox，触发 onChange）
    await appLogRow.locator('td').nth(1).locator('.ant-checkbox').click();
    await expect(appLogRow.locator('td').nth(1).locator('input[type=checkbox]')).toBeChecked({ timeout: 5000 });
    await modal.locator('button', { hasText: /保\s*存/ }).first().click();
    await expect(page.locator('.ant-message').getByText('权限保存成功')).toBeVisible({ timeout: 8000 });

    // 重新打开：勾选保持（后端落库，非前端内存态）
    await row.locator('.cube-op-btn-op-perm').click();
    await expect(page.locator('.ant-modal')).toBeVisible({ timeout: 8000 });
    const modal2 = page.locator('.ant-modal');
    const appLogRow2 = modal2.locator('tbody tr', { hasText: '应用日志' }).first();
    await expect(appLogRow2.locator('td').nth(1).locator('input[type=checkbox]')).toBeChecked();
    await modal2.locator('button', { hasText: /取\s*消/ }).first().click();

    // 清理：删除测试角色（Popconfirm 确认）
    await row.locator('.cube-op-btn-op-delete').click();
    await page.waitForSelector('.ant-popconfirm:not([style*="display: none"])', { timeout: 8000 });
    await page.locator('.ant-popconfirm button').filter({ hasText: /删\s*除/ }).first().click();
    await page.waitForTimeout(1200);
    await expect(page.locator(ROW, { hasText: name })).toHaveCount(0);
  });
});
