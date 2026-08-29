/**
 * 工作台卡片三能力 E2E（WIDGET-1：拖动排序 / 保存顺序 / 隐藏）
 *
 * 覆盖：隐藏卡片 / 恢复卡片 / 保存布局并刷新保持，布局按用户持久化到服务端。
 * 以首页 / 为例。
 */
import { expect, test } from '@playwright/test';

test.describe('工作台卡片（卡片三能力）', () => {
  /** 隐藏指定卡片：点击卡片头「⋯」→ 隐藏卡片 */
  async function hideWidget(page: import('@playwright/test').Page, title: string) {
    const card = page.locator('.cube-home-widget', { hasText: title }).first();
    await card.locator('.ant-card-head .ant-btn').first().click();
    await page.locator('.ant-dropdown:visible').getByText('隐藏卡片').click();
  }

  /** 恢复指定卡片：点击「恢复卡片」→ 选择卡片名 */
  async function restoreWidget(page: import('@playwright/test').Page, title: string) {
    await page.getByRole('button', { name: /恢复卡片/ }).click();
    await page.locator('.ant-dropdown:visible').getByText(title, { exact: true }).click();
  }

  test('隐藏卡片后消失，恢复后重现', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByText('常用菜单')).toBeVisible({ timeout: 10000 });

    // 隐藏「快捷入口」卡片
    await hideWidget(page, '快捷入口');
    await expect(page.locator('.cube-home-widget', { hasText: '快捷入口' })).toHaveCount(0);

    // 恢复卡片
    await restoreWidget(page, '快捷入口');
    await expect(page.locator('.cube-home-widget', { hasText: '快捷入口' }).first()).toBeVisible({ timeout: 8000 });
  });

  test('保存布局后刷新保持', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByText('常用菜单')).toBeVisible({ timeout: 10000 });

    // 隐藏「快捷入口」并保存
    await hideWidget(page, '快捷入口');
    await page.getByRole('button', { name: /保存布局/ }).click();
    await expect(page.locator('.ant-message').getByText('布局已保存')).toBeVisible({ timeout: 8000 });

    // 刷新后仍保持隐藏
    await page.reload();
    await expect(page.getByText('常用菜单')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('.cube-home-widget', { hasText: '快捷入口' })).toHaveCount(0);

    // 清理：重置布局，避免影响其它用例
    await page.getByRole('button', { name: /重置布局/ }).click();
    await expect(page.locator('.ant-message').getByText('布局已重置')).toBeVisible({ timeout: 8000 });
  });
});
