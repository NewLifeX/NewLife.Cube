/**
 * 地区管理页 E2E（/Cube/Area）：列表 | 地图 双模式
 *
 * 覆盖：页面加载列表正常、切换地图模式后 ECharts 中国地图渲染（canvas 出现）、无控制台错误。
 */
import { expect, test } from '@playwright/test';

const ROW = '.ant-table-tbody tr.ant-table-row';

test.describe('地区管理页（/Cube/Area 列表|地图双模式）', () => {
  test('页面加载显示列表与模式切换', async ({ page }) => {
    await page.goto('/Cube/Area');
    await expect(page.locator(ROW).first()).toBeVisible({ timeout: 15000 });

    // 模式切换 Segmented（列表 / 地图）
    await expect(page.getByText('列表', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('地图', { exact: true }).first()).toBeVisible();

    // 列表表头含经度/纬度
    await expect(page.locator('th', { hasText: '经度' })).toBeVisible();
  });

  test('切换地图模式渲染中国地图散点且无控制台错误', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/Cube/Area');
    await expect(page.locator(ROW).first()).toBeVisible({ timeout: 15000 });

    // 切换到地图模式
    await page.getByText('地图', { exact: true }).first().click();

    // ECharts 画布出现（data-testid="area-map" 内 canvas）
    const canvas = page.locator('[data-testid="area-map"] canvas').first();
    await expect(canvas).toBeVisible({ timeout: 15000 });

    // 地图模式有数据时出现 tooltip 相关实例；至少画布已渲染、无脚本错误
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });
});
