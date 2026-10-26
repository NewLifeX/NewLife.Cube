import { test, expect } from '@playwright/test';

/**
 * 表单必填校验 E2E（SKIN-6）
 *
 * 断言结构与选择器，不依赖界面语言：
 * 1. 打开新增弹窗，直接保存 → 出现 `.fmc-error`，弹窗保持打开（提交被前端拦截）
 * 2. 取消 → 弹窗关闭
 *
 * 运行：pnpm test:e2e --grep "表单必填拦截"
 */
test.describe('表单必填校验', () => {
  test('新增弹窗必填为空时拦截提交并提示', async ({ page }) => {
    await page.goto('/Admin/Parameter');
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 20000 });

    await page.locator('[data-role="add"]').click();

    const dialog = page.locator('.el-dialog');
    await expect(dialog).toBeVisible({ timeout: 15000 });

    // 直接保存：必填项为空应被拦截
    await dialog.locator('.el-dialog__footer .el-button--primary').click();
    await expect(dialog.locator('.fmc-error').first()).toBeVisible({ timeout: 10000 });

    // 弹窗仍在，说明没有发起保存并关闭
    await expect(dialog).toBeVisible();

    // 取消关闭，避免脏数据
    await dialog.locator('.el-dialog__footer .el-button').first().click();
    await expect(dialog).toBeHidden({ timeout: 10000 });
  });
});
