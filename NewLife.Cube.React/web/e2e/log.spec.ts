/**
 * 审计日志页 E2E（性能追踪 TraceId → TraceUrl 超链接）
 *
 * 验证：页面加载 / 「追踪」列表头 / TraceId 行渲染为星尘 TraceUrl 超链接（文字「追踪」、新窗口打开）。
 * 后端 LogController.ListFields：TraceId 配 url=StarWeb/trace?id={TraceId} + text=追踪 + target=_blank（对齐 MVC）。
 */
import { expect, test } from '@playwright/test';

const ROW = '.ant-table-tbody tr.ant-table-row';
const TABLE_TIMEOUT = 15000;

test.describe('审计日志页（/Admin/Log）', () => {
  test('列表页加载无错误且展示数据', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/Admin/Log');
    await expect(page.locator(ROW).first()).toBeVisible({ timeout: TABLE_TIMEOUT });
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('性能追踪列：表头「追踪」，TraceId 行渲染星尘 TraceUrl 超链接', async ({ page }) => {
    await page.goto('/Admin/Log');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });

    // 表头为「追踪」（对齐 MVC，不再是默认「性能追踪」）
    await expect(page.locator('th', { hasText: /^追踪$/ }).first()).toBeVisible();

    // TraceId 非空行渲染为超链接：文字「追踪」、href 含 trace?id={TraceId}、新窗口打开
    const traceLinks = page.locator(`${ROW} a[href*="trace?id="]`);
    await expect(traceLinks.first()).toBeVisible({ timeout: 8000 });
    await expect(traceLinks.first()).toHaveText('追踪');
    await expect(traceLinks.first()).toHaveAttribute('target', '_blank');
    // href 为星尘 TraceUrl（含真实 TraceId，而非固定文字）
    const href = await traceLinks.first().getAttribute('href');
    expect(href).toMatch(/^https?:\/\/.+\/trace\?id=[0-9a-f]+$/i);
    expect(href).not.toContain(encodeURIComponent('追踪'));
  });
});
