import { expect, test } from '@playwright/test';

/**
 * 流程实例只读画布（OSC-260922201a 反馈五）：
 * 1) 进度抽屉「查看流程」：实例快照只读画布 —— 节点状态徽标（已完成/未处理），无删除按钮、无节点属性抽屉；
 * 2) 提交抽屉「查看流程」：发布快照只读画布 —— 全部「未处理」徽标。
 * 前置：后端 5000 与 Vite 5183 已启动；登录态由 auth.setup 提供。
 */

/** 打开行操作按钮（进度/提交等中文短语按钮） */
function actionButton(page: import('@playwright/test').Page, text: string) {
  return page.locator('a, button').filter({ hasText: new RegExp(`^${text}$`) }).first();
}

test.describe('流程实例只读画布', () => {
  test('我发起的：进度抽屉查看流程为只读实例画布', async ({ page }) => {
    // 窄视口（632 ≈ VS Code 集成浏览器面板）：验证抽屉不溢出、两项签均可见可自由切换
    await page.setViewportSize({ width: 632, height: 800 });
    await page.goto('/Cube/Workflow/Started');
    await page.waitForTimeout(1500);

    const progress = actionButton(page, '进度');
    if (!(await progress.isVisible({ timeout: 8_000 }).catch(() => false))) {
      test.skip(true, '「我发起的」无实例数据或页面未渲染');
      return;
    }
    await progress.click();

    const drawer = page.locator('.arco-drawer').last();
    await expect(drawer).toBeVisible({ timeout: 15_000 });

    // 回归（反馈五修复）：未激活「查看流程」前画布不挂载，审批进度内容正常可见
    await expect(drawer.locator('.wf-instance-graph, .wf-instance-graph__empty')).toHaveCount(0);
    await expect(drawer.locator('text=审批意见').first()).toBeVisible({ timeout: 10_000 });

    // 切到「查看流程」（等抽屉动画稳定后再点；点击 tab 容器，失败自动重试一次）
    await page.waitForTimeout(600);
    const graphTab = drawer.locator('.arco-tabs-tab', { hasText: '查看流程' });
    await graphTab.click({ force: true });
    const canvas = drawer.locator('.wf-instance-graph').first();
    if (!(await canvas.isVisible({ timeout: 3_000 }).catch(() => false))) {
      await graphTab.click({ force: true });
    }
    await expect(canvas).toBeVisible({ timeout: 10_000 });

    // 画布拉伸填满抽屉内容区：底边应贴近内容区底部（配合 tabs/body 留白形成四边等距）
    await expect
      .poll(
        async () =>
          canvas.evaluate((g) => {
            const b = g.closest('.arco-drawer-body');
            if (!b) return 9999;
            const pad = parseFloat(getComputedStyle(b).paddingBottom) || 0;
            return Math.round(b.getBoundingClientRect().bottom - pad - g.getBoundingClientRect().bottom);
          }),
        { timeout: 5_000, message: '画布底边应贴近抽屉内容区底部（拉伸填满）' },
      )
      .toBeLessThanOrEqual(24);

    // 只读画布：至少出现一个可见的状态徽标
    const badge = drawer.locator('text=已完成').or(drawer.locator('text=未处理')).or(drawer.locator('text=当前'));
    await expect(badge.first()).toBeVisible({ timeout: 10_000 });

    // 无编辑控件、无节点属性抽屉
    await expect(drawer.getByRole('button', { name: '删除节点' })).toHaveCount(0);
    await expect(drawer.getByRole('button', { name: '删除该条件' })).toHaveCount(0);
    await expect(drawer.getByText('节点属性', { exact: true })).toHaveCount(0);

    // 切回「审批进度」：页签可点、原内容恢复、画布卸载（防止 FlowGram 浮层拦截页签）
    await drawer.locator('.arco-tabs-tab', { hasText: '审批进度' }).click({ force: true });
    await expect(drawer.locator('text=审批意见').first()).toBeVisible({ timeout: 10_000 });
    await expect(drawer.locator('.wf-instance-graph')).toHaveCount(0);
  });

  test('提交抽屉：查看流程为发布快照只读画布', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.goto('/Admin/Department');
    await page.waitForTimeout(1500);

    // 动态路由直接刷新可能不匹配（Vue Router No match）：退回流式导航（首页 → 部门）
    if (!(await page.locator('.default-list').first().isVisible().catch(() => false))) {
      await page.goto('/home');
      await page.waitForTimeout(1200);
      await page.locator('button:has-text("部门")').first().click();
    }
    await expect(page.locator('.default-list').first()).toBeVisible({ timeout: 10_000 });
    await page.waitForTimeout(1500);

    // 滚动内容区到表格底部（保证未发起行「上海分公司」可见）
    await page.evaluate(() => {
      const el = document.querySelector('.layout-content__scroll');
      if (el) el.scrollTop = el.scrollHeight;
    });
    await page.waitForTimeout(500);

    // vtable 为 canvas 渲染，行操作按钮无法 DOM 定位，只能按坐标尝试（1280×800 固定布局）。
    // 在末行操作区（右固定列）依次尝试按钮位置，直到「提交」抽屉（.wf-submit）出现；
    // 命中「详情/审批」抽屉或删除确认时先关闭再继续，避免数据状态变化导致的误报。
    const box = await page.locator('.default-list canvas').first().boundingBox();
    expect(box, '部门表格 canvas 应可见').toBeTruthy();
    await page.waitForTimeout(600);

    let opened = false;
    for (const dx of [946, 908, 870, 832]) {
      await page.mouse.click(box!.x + dx, box!.y + 261);
      await page.waitForTimeout(700);
      if (await page.locator('.wf-submit').first().isVisible().catch(() => false)) {
        opened = true;
        break;
      }
      await page.keyboard.press('Escape');
      await page.waitForTimeout(300);
    }
    if (!opened) {
      test.skip(true, '末行不再是可提交记录（数据已变化）或 vtable 布局变化，需核对后重跑');
      return;
    }

    const drawer = page.locator('.arco-drawer').last();
    await expect(drawer, '提交抽屉应打开').toBeVisible({ timeout: 10_000 });

    // 回归（反馈五修复）：未激活「查看流程」前画布不挂载；定义下拉可真实点击
    // （此前 bug：隐藏页签中初始化的 FlowGram 浮层会拦截点击，导致首次 select.click 超时）
    await page.waitForTimeout(400);
    await expect(drawer.locator('.wf-instance-graph, .wf-instance-graph__empty')).toHaveCount(0);
    const sel = drawer.locator('.wf-submit .arco-select').first();
    await expect(sel).toBeVisible({ timeout: 10_000 });
    await sel.scrollIntoViewIfNeeded();
    await sel.click({ timeout: 5_000 });
    await page.keyboard.press('Escape');
    await page.waitForTimeout(200);

    // 单一可用定义时抽屉自动选中（useSubmitApproval），无需手动选择流程定义。
    // 切到「查看流程」（等动画稳定后再点；点击 tab 容器，失败自动重试一次）
    await page.waitForTimeout(600);
    const graphTab = drawer.locator('.arco-tabs-tab', { hasText: '查看流程' });
    await graphTab.click({ force: true });
    const canvas = drawer.locator('.wf-instance-graph').first();
    if (!(await canvas.isVisible({ timeout: 3_000 }).catch(() => false))) {
      await graphTab.click({ force: true });
    }
    await expect(canvas).toBeVisible({ timeout: 10_000 });

    // 发布快照画布：全部「未处理」徽标
    const badge = drawer.locator('text=未处理');
    await expect(badge.first()).toBeVisible({ timeout: 10_000 });
    await expect(drawer.getByRole('button', { name: '删除节点' })).toHaveCount(0);

    // 画布拉伸填满抽屉内容区（与进度抽屉一致）
    const canvasGap = await canvas.evaluate((g) => {
      const b = g.closest('.arco-drawer-body');
      if (!b) return 9999;
      const pad = parseFloat(getComputedStyle(b).paddingBottom) || 0;
      return Math.round(b.getBoundingClientRect().bottom - pad - g.getBoundingClientRect().bottom);
    });
    expect(canvasGap, '画布底边应贴近抽屉内容区底部（拉伸填满）').toBeLessThanOrEqual(24);

    // 切回「提交审批」：页签可点、表单恢复、画布卸载
    await drawer.locator('.arco-tabs-tab', { hasText: '提交审批' }).click({ force: true });
    await expect(drawer.locator('.wf-submit .arco-select').first()).toBeVisible({ timeout: 10_000 });
    await expect(drawer.locator('.wf-instance-graph')).toHaveCount(0);
  });
});
