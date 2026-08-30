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
    // 导出收纳在「高级」菜单内（规范 §7.8）
    await page.getByRole('button', { name: /高\s*级/ }).click();
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
    // 导入收纳在「高级」菜单内（规范 §7.8），触发隐藏文件选择框并上传
    await page.getByRole('button', { name: /高\s*级/ }).click();
    await page.getByText('导入 Excel/Json/Zip').click();
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
    // 点击工具栏「删除选中」（选中行后出现）→ Popconfirm 确认
    await page.getByRole('button', { name: /删除选中/ }).click();
    const confirmBtn = page.locator('.ant-popover').getByRole('button', { name: /删\s*除/ }).first();
    await confirmBtn.click();
    await expect(page.locator('.ant-message')).toBeVisible({ timeout: 8000 });
  });

  test('无图表数据的页面不显示 表格/图表 视图切换', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    // /Admin/User 无图表数据（canChart=false），Segmented 视图切换不渲染，表格仍可见
    await expect(page.locator('.ant-segmented')).toHaveCount(0);
    await expect(page.locator(ROW).first()).toBeVisible({ timeout: TABLE_TIMEOUT });
  });

  test('列表页纵向与横向滚动可用（滚动回归）', async ({ page }) => {
    // 固定较小视口，确保列表内容超高、表格超宽，从而触发内部滚动
    await page.setViewportSize({ width: 1280, height: 700 });
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    // 等待加载完成（antd 加载中可能只有占位行），避免滚动度量在数据未就绪时取样
    await page
      .waitForFunction(() => !document.querySelector('.ant-spin-spinning'), undefined, { timeout: 10000 })
      .catch(() => undefined);
    await page.waitForTimeout(300);

    // 纵向：.cube-shell-body 是唯一滚动容器（overflow-y:auto），内容超高时可滚动到底露出分页
    const vScroll = await page.evaluate(() => {
      const body = document.querySelector<HTMLElement>('.cube-shell-body');
      if (!body) return { ok: false, reason: 'no-scroll-body' };
      const overflow = getComputedStyle(body).overflowY;
      if (body.scrollHeight <= body.clientHeight)
        return { ok: false, reason: 'content-not-taller', sh: body.scrollHeight, ch: body.clientHeight, overflow };
      body.scrollTop = 999999;
      return { ok: body.scrollTop > 0, sh: body.scrollHeight, ch: body.clientHeight, top: body.scrollTop, overflow };
    });
    expect(vScroll.ok, `纵向滚动异常: ${JSON.stringify(vScroll)}`).toBeTruthy();

    // 横向：表格内容超宽时 .ant-table-content（overflow-x:auto）可横向滚动
    const hScroll = await page.evaluate(() => {
      const t = document.querySelector<HTMLElement>('.ant-table-content');
      if (!t) return { ok: false, reason: 'no-table-content' };
      const overflow = getComputedStyle(t).overflowX;
      if (t.scrollWidth <= t.clientWidth)
        return { ok: false, reason: 'table-not-wider', sw: t.scrollWidth, cw: t.clientWidth, overflow };
      t.scrollLeft = 500;
      return { ok: t.scrollLeft > 0, sw: t.scrollWidth, cw: t.clientWidth, left: t.scrollLeft, overflow };
    });
    expect(hScroll.ok, `横向滚动异常: ${JSON.stringify(hScroll)}`).toBeTruthy();
  });

  test('新增弹窗按字段分类分组展示（Tabs）', async ({ page }) => {
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    await page.getByRole('button', { name: /新增/ }).click();
    await page.waitForSelector('.ant-modal:not([style*="display: none"])', { timeout: 8000 });
    // 字段带 Category → 弹窗内按分类 Tabs 分组展示
    const tabs = page.locator('.ant-modal:visible .ant-tabs-tab');
    await expect(tabs.first()).toBeVisible({ timeout: 8000 });
    const labels = await tabs.allTextContents();
    expect(labels.length).toBeGreaterThan(1);
    // 切换分组后展示该组字段
    await tabs.nth(1).click();
    await expect(page.locator('.ant-modal:visible .ant-tabs-tab-active')).toHaveText(labels[1]);
    // 关闭弹窗，避免影响后续用例
    await page.locator('.ant-modal:visible button:has-text("取 消")').click();
  });
});
