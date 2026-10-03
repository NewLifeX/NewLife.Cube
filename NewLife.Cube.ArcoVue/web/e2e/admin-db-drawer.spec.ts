import { test, expect } from '@playwright/test';

/**
 * Admin/Db 数据库卡片「更多 → 实体」抽屉回归（OSC-2610012e35）。
 *
 * 覆盖：a-table 列注册（#columns 插槽）、显示名/备注拆分、数字列右对齐、省略+Tooltip（含 hover）、
 * 数据字典下钻与返回（实体模型与纯表双数据源）、菜单不再提供「表」「差异」。
 * 背景：a-table-column 直接放默认插槽会被 Arco 当作裸 <table> 内容渲染，列不注册导致表格空白。
 *
 * 前置：后端（默认 http://localhost:5000）与 Vite dev（默认 5183）已启动，admin/admin 可登录。
 */
test('数据库抽屉：实体列表 / 数据字典 均渲染可见内容', async ({ page }) => {
  await page.goto('/Admin/Db');
  await page.locator('.db-card').first().waitFor({ timeout: 30_000 });

  const cubeCard = page.locator('.db-card', { hasText: 'Cube' }).first();
  const target = (await cubeCard.count()) > 0 ? cubeCard : page.locator('.db-card').first();
  const moreBtn = target.getByRole('button', { name: /更多/ });
  const drawer = page.locator('.arco-drawer');
  const pick = (label: string) =>
    page.locator('.arco-dropdown-option:visible').filter({ hasText: label }).first();

  // 1) 「更多」菜单不再提供「表」「差异」（OSC 修订）
  await moreBtn.click();
  await expect(pick('实体')).toBeVisible();
  await expect(page.locator('.arco-dropdown-option:visible').filter({ hasText: '表' })).toHaveCount(0);
  await expect(page.locator('.arco-dropdown-option:visible').filter({ hasText: '差异' })).toHaveCount(0);
  await pick('实体').click();

  // 2) 实体列表：显示名=描述首句、备注=描述余下部分（末列、省略渲染）
  await expect(drawer.locator('.arco-drawer-title')).toHaveText(/^实体 · /);
  // 抽屉宽度与实体对象 详情/编辑/添加 宽抽屉一致（RECORD_DRAWER_WIDE = 720）
  await expect(drawer).toHaveCSS('width', '720px');
  await expect
    .poll(() => drawer.locator('.arco-table-element tbody tr').count(), { timeout: 10_000 })
    .toBeGreaterThan(0);
  const listHeaders = (await drawer.locator('.arco-table-element thead th').allInnerTexts()).map((t) =>
    t.trim(),
  );
  expect(listHeaders).toEqual(['显示名', '表名', '行数', '备注']);
  const ruleRow = drawer.locator('.arco-table-element tbody tr', { hasText: 'AccessRule' }).first();
  await expect(ruleRow).toBeVisible();
  expect((await ruleRow.locator('td').nth(0).innerText()).trim()).toBe('访问规则');
  expect((await ruleRow.locator('td').nth(3).innerText()).trim()).toContain('控制系统访问');
  // 数字列（行数）右对齐；备注启用省略渲染（td-content 包裹）
  await expect(ruleRow.locator('td').nth(2).locator('.arco-table-cell')).toHaveCSS('text-align', 'right');
  await expect(ruleRow.locator('td').nth(3).locator('.arco-table-td-content')).toHaveCount(1);

  // 3) 点击实体名称进入数据字典（同一抽屉）：标题为实体中文名，列对齐实体架构定义
  await ruleRow.locator('td').nth(0).locator('a, .arco-link').first().click();
  await expect(drawer.locator('.arco-drawer-title')).toHaveText(/^数据字典 · 访问规则/);
  await expect(drawer).toHaveCSS('width', '720px');
  await expect
    .poll(() => drawer.locator('.arco-table-element tbody tr').count(), { timeout: 10_000 })
    .toBeGreaterThan(0);
  const fieldHeaders = (await drawer.locator('.arco-table-element thead th').allInnerTexts()).map((t) =>
    t.trim(),
  );
  expect(fieldHeaders).toEqual(['显示名', '字段名', '类型', '长度', '精度', '主键', '允许空', '备注']);
  await expect(drawer.locator('.arco-table-element tbody')).toContainText(/AI|PK/);
  // 数字列（长度）右对齐
  await expect(
    drawer.locator('.arco-table-element tbody tr').first().locator('td').nth(3).locator('.arco-table-cell'),
  ).toHaveCSS('text-align', 'right');

  // 类型列截断单元格 hover 出 Tooltip（仅溢出时提示完整内容）
  const kindRow = drawer.locator('.arco-table-element tbody tr', { hasText: 'Kind' }).first();
  await kindRow.locator('td').nth(2).hover();
  await expect(page.locator('.arco-tooltip:visible')).toContainText('AccessActionKinds', { timeout: 5_000 });
  await page.mouse.move(10, 10);

  // 4) 返回实体列表
  await drawer.getByText('返回实体列表').click();
  await expect(drawer.locator('.arco-drawer-title')).toHaveText(/^实体 · /);
  await expect
    .poll(() => drawer.locator('.arco-table-element tbody tr').count(), { timeout: 10_000 })
    .toBeGreaterThan(0);

  // 5) 纯表（无实体模型）：追加行可看字段字典（数据源=数据库架构）
  const pureRow = drawer.locator('.arco-table-element tbody tr', { hasText: '无实体模型' }).first();
  await expect(pureRow).toBeVisible();
  const pureName = (await pureRow.locator('td').nth(0).innerText()).trim();
  await pureRow.locator('td').nth(0).locator('a, .arco-link').first().click();
  await expect(drawer.locator('.arco-drawer-title')).toHaveText(`数据字典 · ${pureName}`);
  await expect
    .poll(() => drawer.locator('.arco-table-element tbody tr').count(), { timeout: 10_000 })
    .toBeGreaterThan(0);
});
