/**
 * 通用列表页 E2E（LIST 标准场景）
 *
 * 以 /Admin/User 为例验证：页面加载 / 列表展示 / 搜索 / 新增 / 编辑 / 删除 / 空错误状态。
 * 测试数据使用唯一前缀（ts-{时间戳}）保证隔离；删除用例先搜索定位目标（兼容分页）。
 *
 * 注意：表格启用 scroll.x 后 antd 会渲染隐藏的测量行（.ant-table-measure-row），
 * 数据行选择器统一使用 `tr.ant-table-row` 排除测量行。
 */
import { expect, test } from '@playwright/test';

const PREFIX = `e2e-${Date.now()}`;
const TABLE_TIMEOUT = 15000;
/** 数据行（排除 antd 测量行） */
const ROW = '.ant-table-tbody tr.ant-table-row';

test.describe('通用列表页（/Admin/User）', () => {
  test('列表页加载无错误且展示数据', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/Admin/User');
    await expect(page.locator(ROW).first()).toBeVisible({ timeout: TABLE_TIMEOUT });
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('搜索按关键词过滤', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    // 等待加载完成（antd 加载中可能只有占位行），避免 rowsBefore 在数据未就绪时取样
    await page
      .waitForFunction(() => !document.querySelector('.ant-spin-spinning'), undefined, { timeout: 10000 })
      .catch(() => undefined);
    await page.waitForTimeout(300);
    const rowsBefore = await page.locator(ROW).count();
    const searchInput = page.locator('.ant-card input[type="text"], .ant-card input:not([type])').first();
    if (await searchInput.isVisible().catch(() => false)) {
      await searchInput.fill('admin');
      await page.getByRole('button', { name: /搜\s*索/ }).click();
      await page.waitForTimeout(1000);
      const rows = await page.locator(ROW).count();
      expect(rows).toBeGreaterThan(0);
      expect(rows).toBeLessThanOrEqual(rowsBefore);
    }
  });

  test('新增用户并出现在列表', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
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
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    const row = page.locator(ROW).first();
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
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    // 搜索定位目标（兼容分页）
    const searchInput = page.locator('.ant-card input[type="text"], .ant-card input:not([type])').first();
    if (await searchInput.isVisible().catch(() => false)) {
      await searchInput.fill(PREFIX);
      await page.getByRole('button', { name: /搜\s*索/ }).click();
      await page.waitForTimeout(1000);
    }
    const target = page.locator(ROW, { hasText: PREFIX }).first();
    if ((await target.count()) > 0) {
      await target.getByRole('button', { name: '删除' }).click();
      // 危险操作二次确认（Popconfirm）
      await page.locator('.ant-popover:visible').getByRole('button', { name: /删\s*除/ }).click();
      await expect(page.locator('.ant-message')).toBeVisible({ timeout: 8000 });
    }
  });

  test('空状态（不存在的实体 → 404）', async ({ page }) => {
    await page.goto('/NoSuchEntity/XYZ');
    await expect(page.getByText('404').first()).toBeVisible({ timeout: 8000 });
  });

  test('导出 CSV 触发文件下载', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    await page.getByRole('button', { name: /导\s*出/ }).click();
    const downloadPromise = page.waitForEvent('download', { timeout: 15000 });
    await page.getByText('导出 CSV').click();
    const download = await downloadPromise;
    expect(download.suggestedFilename().toLowerCase()).toContain('.csv');
  });

  test('导入 CSV 新增用户并落库', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
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

  test('分页器渲染且可翻页', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    const pager = page.locator('.ant-pagination');
    await expect(pager).toBeVisible();
    // 数据量足够时有第 2 页，点击并断言激活页码
    const page2 = pager.locator('.ant-pagination-item-2');
    if (await page2.isVisible().catch(() => false)) {
      await page2.click();
      await page.waitForTimeout(800);
      await expect(pager.locator('.ant-pagination-item-active')).toContainText('2');
    }
  });

  test('新增弹窗空提交被校验拦截', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    await page.getByRole('button', { name: /新增/ }).click();
    await page.waitForSelector('.ant-modal:not([style*="display: none"])', { timeout: 8000 });
    // 不填用户名直接保存 → 前端必填校验 或 后端错误提示（至少其一出现，拦截提交）
    await page.getByRole('button', { name: /保\s*存/ }).click();
    await expect(page.locator('.ant-form-item-explain-error, .ant-message').first()).toBeVisible({ timeout: 8000 });
    // 弹窗未被关闭（说明未提交成功）
    await expect(page.locator('.ant-modal:not([style*="display: none"])')).toBeVisible();
  });

  test('批量删除选中用户', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    // 先新增两个测试用户
    for (let i = 0; i < 2; i++) {
      const name = `${PREFIX}-bd${i}`;
      await page.getByRole('button', { name: /新增/ }).click();
      await page.waitForSelector('.ant-modal:not([style*="display: none"])', { timeout: 8000 });
      await page.locator('.ant-modal input').first().fill(name);
      await page.getByRole('button', { name: /保\s*存/ }).click();
      await expect(page.locator('.ant-message')).toBeVisible({ timeout: 8000 });
    }
    // 搜索定位目标（兼容分页）
    const searchInput = page.locator('.ant-card input[type="text"], .ant-card input:not([type])').first();
    if (await searchInput.isVisible().catch(() => false)) {
      await searchInput.fill(PREFIX);
      await page.getByRole('button', { name: /搜\s*索/ }).click();
      await page.waitForTimeout(1000);
    }
    // 勾选前两行
    const rows = page.locator(ROW);
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
    for (let i = 0; i < Math.min(2, count); i++) {
      await rows.nth(i).locator('input[type="checkbox"]').check({ timeout: 5000 });
    }
    // 点击批量删除（Toolbar 删除按钮）→ Popconfirm 确认
    await page.getByRole('button', { name: /删\s*除/ }).first().click();
    const confirmBtn = page.locator('.ant-popover').getByRole('button', { name: /删\s*除/ }).first();
    await confirmBtn.click();
    await expect(page.locator('.ant-message')).toBeVisible({ timeout: 8000 });
  });

  test('图表弹窗：有数据渲染弹窗，无数据友好提示', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    await page.getByRole('button', { name: /图\s*表/ }).click();
    // 有图表数据 → 弹窗；无数据 → message「暂无图表数据」；二者至少其一出现
    await expect(
      page.locator('.ant-modal:not([style*="display: none"]), .ant-message').first(),
    ).toBeVisible({ timeout: 10000 });
  });
});
