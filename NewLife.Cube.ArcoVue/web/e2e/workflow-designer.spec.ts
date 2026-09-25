import { expect, test } from '@playwright/test';

/**
 * 流程设计器（OSC-260922201a 反馈六）：
 * 1) 深色主题下画布背景与节点卡片跟随主题（--g-editor-background 与卡片语义色变量）；
 * 2) 「新建审批流程」弹窗：标题、字段顺序（流程名称在前、实体在后）、实体下拉友好名（菜单中文名优先）。
 * 前置：后端 5000 与 Vite 5183 已启动；登录态由 auth.setup 提供。
 */

const DESIGNER = '/Cube/Workflow/Designer?id=7503818474315943936';

test.describe('流程设计器', () => {
  test('深色主题画布与新建审批流程弹窗', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto(DESIGNER);
    await page.waitForTimeout(2500);
    await expect(page.locator('.wf-flowcanvas')).toBeVisible({ timeout: 15_000 });

    // 1) 主题适配：浅色保持原视觉，深色跟随主题（画布底色 + 节点卡片）
    const readColors = () =>
      page.evaluate(() => {
        const pg = document.querySelector('.gedit-playground');
        const card = document.querySelector('.gedit-flow-activity-node > div');
        return {
          pg: pg ? getComputedStyle(pg).backgroundColor : '',
          card: card ? getComputedStyle(card).backgroundColor : '',
        };
      });
    const setTheme = (theme: string) =>
      page.evaluate((t) => {
        document.body.setAttribute('arco-theme', t);
        document.documentElement.setAttribute('arco-theme', t);
      }, theme);

    await setTheme('light');
    await page.waitForTimeout(500);
    const light = await readColors();
    expect(light.pg, '浅色下画布应保持原浅灰').toBe('rgb(242, 243, 245)');
    expect(light.card, '浅色下节点卡应为白色').toBe('rgb(255, 255, 255)');

    await setTheme('dark');
    await page.waitForTimeout(500);
    const dark = await readColors();
    expect(dark.pg, '深色下画布应跟随主题').toBe('rgba(255, 255, 255, 0.08)');
    expect(dark.card, '深色下节点卡应跟随主题').toBe('rgb(35, 35, 36)');

    // 2) 分支条件卡片摘要：枚举值显示友好名（类型 2 → 部门）
    await expect(
      page.locator('.gedit-flow-activity-node').filter({ hasText: '条件1' }).first(),
    ).toContainText('类型 等于 部门', { timeout: 10_000 });

    // 3) 开始/结束节点不允许删除（无删除按钮）；业务节点保留
    await expect(page.locator('.gedit-flow-activity-node[data-node-id="start"] [title="删除节点"]')).toHaveCount(0);
    await expect(page.locator('.gedit-flow-activity-node[data-node-id="end"] [title="删除节点"]')).toHaveCount(0);
    await expect(
      page.locator('.gedit-flow-activity-node[data-node-id="n1"] [title="删除节点"]'),
    ).toHaveCount(1);

    // 4) 点画布空白打开「流程属性」→ 新建审批流程
    await page.locator('.wf-designer__flowgram').click({ position: { x: 80, y: 80 }, force: true });
    await page.waitForTimeout(800);

    // 发起条件文案无歧义：标签 + 说明符（提交准入校验，不会自动发起）
    await expect(page.locator('.wf-filter-head__label')).toHaveText('发起条件');
    await expect(page.locator('.wf-filter-head__hint')).toBeVisible();
    await page.locator('.wf-filter-head__hint').hover();
    await expect(page.locator('.arco-tooltip-content').last()).toContainText('不会自动发起');

    const createBtn = page.getByRole('button', { name: '新建审批流程' });
    await expect(createBtn).toBeVisible({ timeout: 8_000 });
    await createBtn.click();
    await page.waitForTimeout(600);

    // 页面存在多个 .arco-modal（如隐藏的「添加审批节点」），按标题过滤
    const modal = page.locator('.arco-modal').filter({ hasText: '新建审批流程' });
    await expect(modal).toBeVisible({ timeout: 8_000 });
    await expect(modal.locator('.arco-modal-header')).toContainText('新建审批流程');

    // 字段顺序：流程名称在前、实体在后
    const items = await modal.locator('.arco-form-item').allInnerTexts();
    const nameIdx = items.findIndex((t) => t.includes('流程名称'));
    const entityIdx = items.findIndex((t) => t.includes('实体'));
    expect(nameIdx, '「流程名称」字段应存在').toBeGreaterThanOrEqual(0);
    expect(entityIdx, '「实体」字段应存在').toBeGreaterThanOrEqual(0);
    expect(nameIdx, '字段顺序：流程名称应在实体之前').toBeLessThan(entityIdx);

    // 5) 实体下拉友好名：菜单中文名优先（「部门」等）
    await modal.locator('.arco-select').first().click();
    await page.waitForTimeout(600);
    const options = await page.locator('.arco-select-option').allInnerTexts();
    expect(options.length, '实体下拉应有可选项').toBeGreaterThan(0);
    expect(options.join(','), '实体下拉应显示中文友好名（如「部门」）').toContain('部门');
  });
});
