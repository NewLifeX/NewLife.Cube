/**
 * AI 助手 + 通知铃铛 E2E（CMP-8 / CMP-7）
 *
 * 前置：被测实例需开启 AISwitch（系统设置 → 魔方设置 → AI 分类勾选 AISwitch），
 *       否则 AI 悬浮球不渲染（对齐 Vue 开关门控），本组用例将失败。
 *
 * AI 助手：悬浮按钮打开面板（空状态提示 + 快捷指令）→ 关闭恢复；
 *         发送消息渲染用户气泡 + 助手反馈（正常回复或错误提示），页面不崩；
 *         列表页显示"分析当前数据/系统诊断"快捷指令，表单页显示"帮我填表/分析当前记录"。
 * 通知铃铛：顶栏按钮可见可点击，无 JS 错误（纯 UI 入口）。
 */
import { expect, test } from '@playwright/test';

test.describe('AI 助手（CMP-8）', () => {
  test('悬浮按钮打开面板（列表页快捷指令），关闭后恢复', async ({ page }) => {
    await page.goto('/Admin/User');
    const fab = page.locator('.cube-ai-fab');
    await expect(fab).toBeVisible({ timeout: 10000 });

    await page.evaluate(() => {
      (document.querySelector('.cube-ai-fab') as HTMLButtonElement | null)?.click();
    });
    await expect(page.locator('.cube-ai-panel')).toBeVisible({ timeout: 8000 });
    await expect(page.getByText('我是 AI 助手').first()).toBeVisible({ timeout: 8000 });

    // 列表页快捷指令：分析当前数据 + 系统诊断
    await expect(page.getByRole('button', { name: /分析当前数据/ }).first()).toBeVisible({ timeout: 8000 });
    await expect(page.getByRole('button', { name: /系统诊断/ }).first()).toBeVisible({ timeout: 8000 });

    // 关闭面板
    await page.evaluate(() => {
      (document.querySelector('.cube-ai-header-close') as HTMLButtonElement | null)?.click();
    });
    await expect(page.locator('.cube-ai-panel')).toBeHidden({ timeout: 8000 });
    await expect(page.locator('.cube-ai-fab')).toBeVisible({ timeout: 8000 });
  });

  test('表单页显示帮我填表快捷指令', async ({ page }) => {
    // 编辑表单页：URL 带 ?id= → 表单页 + 编辑态
    await page.goto('/Admin/User/Edit?id=1');
    await expect(page.locator('.cube-ai-fab')).toBeVisible({ timeout: 10000 });

    await page.evaluate(() => {
      (document.querySelector('.cube-ai-fab') as HTMLButtonElement | null)?.click();
    });
    await expect(page.locator('.cube-ai-panel')).toBeVisible({ timeout: 8000 });

    // 编辑表单页：帮我填表 + 分析当前记录 + 系统诊断
    await expect(page.getByRole('button', { name: /帮我填表/ }).first()).toBeVisible({ timeout: 8000 });
    await expect(page.getByRole('button', { name: /分析当前记录/ }).first()).toBeVisible({ timeout: 8000 });
  });

  test('发送消息渲染用户气泡与助手反馈，页面不崩', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/Admin/User');
    await expect(page.locator('.cube-ai-fab')).toBeVisible({ timeout: 10000 });
    await page.evaluate(() => {
      (document.querySelector('.cube-ai-fab') as HTMLButtonElement | null)?.click();
    });

    const input = page.locator('.cube-ai-input input[type="text"]');
    await expect(input).toBeVisible({ timeout: 8000 });
    await input.fill('你好');
    await page.keyboard.press('Enter');

    // 用户气泡回显输入文本
    await expect(page.locator('.cube-ai-row.user .cube-ai-bubble').first()).toContainText('你好', { timeout: 8000 });

    // 助手反馈出现（正常回复或错误提示均视为 UI 反馈），等待流式结束
    await expect(page.locator('.cube-ai-row:not(.user) .cube-ai-bubble').first()).toBeVisible({ timeout: 25000 });

    // 无 JS 控制台错误（favicon 404 忽略）
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });
});

test.describe('通知铃铛（CMP-7）', () => {
  test('顶栏铃铛按钮可见可点击无错误', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/');
    const bell = page.locator('button[aria-label="通知"]');
    await expect(bell).toBeVisible({ timeout: 10000 });

    await page.evaluate(() => {
      (document.querySelector('button[aria-label="通知"]') as HTMLButtonElement | null)?.click();
    });
    await page.waitForTimeout(500);

    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });
});
