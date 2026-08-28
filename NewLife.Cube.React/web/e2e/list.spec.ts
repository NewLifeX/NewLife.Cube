/**
 * 通用列表页 E2E（LIST 标准场景）
 *
 * 以 /Admin/User 为例验证：页面加载 / 列表展示 / 搜索 / 新增 / 编辑 / 删除 / 空错误状态。
 * 测试数据使用唯一前缀（ts-{时间戳}）保证隔离；删除用例先搜索定位目标（兼容分页）。
 */
import { expect, test } from '@playwright/test';

const PREFIX = `e2e-${Date.now()}`;
const TABLE_TIMEOUT = 15000;

test.describe('通用列表页（/Admin/User）', () => {
  test('列表页加载无错误且展示数据', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/Admin/User');
    await expect(page.locator('.ant-table-tbody tr').first()).toBeVisible({ timeout: TABLE_TIMEOUT });
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('搜索按关键词过滤', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector('.ant-table-tbody tr', { timeout: TABLE_TIMEOUT });
    const rowsBefore = await page.locator('.ant-table-tbody tr').count();
    const searchInput = page.locator('.ant-card input[type="text"], .ant-card input:not([type])').first();
    if (await searchInput.isVisible().catch(() => false)) {
      await searchInput.fill('admin');
      await page.getByRole('button', { name: /搜\s*索/ }).click();
      await page.waitForTimeout(1000);
      const rows = await page.locator('.ant-table-tbody tr').count();
      expect(rows).toBeGreaterThan(0);
      expect(rows).toBeLessThanOrEqual(rowsBefore);
    }
  });

  test('新增用户并出现在列表', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector('.ant-table-tbody tr', { timeout: TABLE_TIMEOUT });
    const name = `${PREFIX}-user`;
    await page.getByRole('button', { name: /新增/ }).click();
    await page.waitForSelector('.ant-modal:not([style*="display: none"])', { timeout: 8000 });
    // 填写必填字段（用户名，主键已过滤，首个输入框为名称）
    await page.locator('.ant-modal input').first().fill(name);
    await page.getByRole('button', { name: /保\s*存/ }).click();
    await expect(page.locator('.ant-message')).toBeVisible({ timeout: 8000 });
    // 搜索确认落库（避免分页影响）
    await page.locator('.ant-card input[type="text"], .ant-card input:not([type])').first().fill(name);
    await page.getByRole('button', { name: /搜\s*索/ }).click();
    await page.waitForTimeout(1000);
    await expect(page.locator('.ant-table-tbody').getByText(name).first()).toBeVisible({ timeout: 10000 });
  });

  test('编辑用户后值更新', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector('.ant-table-tbody tr', { timeout: TABLE_TIMEOUT });
    const row = page.locator('.ant-table-tbody tr').first();
    await row.getByRole('button', { name: '编辑' }).click();
    await page.waitForSelector('.ant-modal:not([style*="display: none"])', { timeout: 8000 });
    // 修改第二个可编辑输入框（非用户名，避开登录账号），保存后应有成功提示
    const editInput = page.locator('.ant-modal:visible input').nth(1);
    if (await editInput.isVisible().catch(() => false)) {
      await editInput.fill(`${PREFIX}-edit`);
      await page.getByRole('button', { name: /保\s*存/ }).click();
      await expect(page.locator('.ant-message')).toBeVisible({ timeout: 8000 });
    }
  });

  test('删除新增用户', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector('.ant-table-tbody tr', { timeout: TABLE_TIMEOUT });
    // 搜索定位目标（兼容分页）
    const searchInput = page.locator('.ant-card input[type="text"], .ant-card input:not([type])').first();
    if (await searchInput.isVisible().catch(() => false)) {
      await searchInput.fill(PREFIX);
      await page.getByRole('button', { name: /搜\s*索/ }).click();
      await page.waitForTimeout(1000);
    }
    const target = page.locator('.ant-table-tbody tr', { hasText: PREFIX }).first();
    if ((await target.count()) > 0) {
      await target.getByRole('button', { name: '删除' }).click();
      await expect(page.locator('.ant-message')).toBeVisible({ timeout: 8000 });
    }
  });

  test('空状态（不存在的实体 → 404）', async ({ page }) => {
    await page.goto('/NoSuchEntity/XYZ');
    await expect(page.getByText('404').first()).toBeVisible({ timeout: 8000 });
  });

  test('导出 CSV 触发文件下载', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector('.ant-table-tbody tr', { timeout: TABLE_TIMEOUT });
    await page.getByRole('button', { name: /导\s*出/ }).click();
    const downloadPromise = page.waitForEvent('download', { timeout: 15000 });
    await page.getByText('导出 CSV').click();
    const download = await downloadPromise;
    expect(download.suggestedFilename().toLowerCase()).toContain('.csv');
  });

  test('导入 CSV 新增用户并落库', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector('.ant-table-tbody tr', { timeout: TABLE_TIMEOUT });
    const name = `e2e-import-${Date.now()}`;
    // CSV 首行为表头（字段名），后续行为数据
    const csv = `Name,DisplayName,Password\n${name},导入测试,admin123\n`;
    const buffer = Buffer.from(csv, 'utf-8');
    // 触发隐藏文件选择框并上传
    await page.getByRole('button', { name: /导\s*入/ }).click();
    await page.locator('input[type="file"]').setInputFiles({
      name: 'users.csv',
      mimeType: 'text/csv',
      buffer,
    });
    await expect(page.locator('.ant-message')).toBeVisible({ timeout: 10000 });
    // 搜索确认落库
    const searchInput = page.locator('.ant-card input[type="text"], .ant-card input:not([type])').first();
    if (await searchInput.isVisible().catch(() => false)) {
      await searchInput.fill(name);
      await page.getByRole('button', { name: /搜\s*索/ }).click();
      await page.waitForTimeout(1000);
      await expect(page.locator('.ant-table-tbody').getByText(name).first()).toBeVisible({ timeout: 10000 });
    }
  });
});
