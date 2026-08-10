import { test, expect, type Page } from '@playwright/test';

const GALLERY = process.env.CT_BASE_URL || 'http://127.0.0.1:5190/';

type OpenMode = 'select' | 'dialog' | undefined;

/**
 * 挂载指定 story 并按需交互：
 *  - select：点击 el-select 展开下拉，截图 .el-select__popper
 *  - dialog：点击列表型触发按钮，等待 .el-dialog + 表格行，截图 .el-dialog
 *  - undefined：仅等待 .lov-select 渲染，截图 .lov-select
 */
async function openLovSelect(page: Page, id: string, mode: OpenMode = undefined) {
  // 一步到位：直接打开带 ?story=<id> 的 URL，main.ts 在加载时挂载该故事（选择器型不自动开弹窗）。
  // 后续 select/dialog 模式下的点击交互仍需测试显式触发，以截图中间态。
  await page.goto(`${GALLERY}?story=${encodeURIComponent(id)}`);

  if (mode === 'select') {
    await page.waitForSelector('.el-select');
    await page.click('.el-select');
    await page.waitForSelector('.el-select__popper .el-select-dropdown__item');
    await page.waitForTimeout(300); // 等下拉动画稳定
  } else if (mode === 'dialog') {
    await page.waitForSelector('.lov-select');
    await page.click('.lov-select .el-input-group__append .el-button');
    await page.waitForSelector('.el-dialog');
    await page.waitForSelector('.el-table__row');
    await page.waitForTimeout(400); // 等弹窗动画 + 列表数据渲染稳定
  } else {
    await page.waitForSelector('.lov-select');
  }
}

test('EnumSingle 下拉渲染', async ({ page }) => {
  await openLovSelect(page, 'LovSelect/EnumSingle', 'select');
  await expect(page.locator('.el-select__popper')).toHaveScreenshot();
});

test('EnumMulti 多选下拉渲染', async ({ page }) => {
  await openLovSelect(page, 'LovSelect/EnumMulti', 'select');
  await expect(page.locator('.el-select__popper')).toHaveScreenshot();
});

test('ListSingleClosed 只读输入框 + 搜索按钮', async ({ page }) => {
  await openLovSelect(page, 'LovSelect/ListSingleClosed');
  await expect(page.locator('.lov-select')).toHaveScreenshot();
});

test('ListMultiClosed 多选只读输入框', async ({ page }) => {
  await openLovSelect(page, 'LovSelect/ListMultiClosed');
  await expect(page.locator('.lov-select')).toHaveScreenshot();
});

test('ListSingleEcho 回显已选文本', async ({ page }) => {
  await openLovSelect(page, 'LovSelect/ListSingleEcho');
  await expect(page.locator('.lov-select')).toHaveScreenshot();
});

test('ListSingleOpen 弹窗含 LovSelectTable', async ({ page }) => {
  await openLovSelect(page, 'LovSelect/ListSingleClosed', 'dialog');
  await expect(page.locator('.el-dialog')).toHaveScreenshot();
});

test('ListMultiOpen 多选弹窗含 LovSelectTable', async ({ page }) => {
  await openLovSelect(page, 'LovSelect/ListMultiClosed', 'dialog');
  await expect(page.locator('.el-dialog')).toHaveScreenshot();
});

// 测试状态：ENUM 单选交互后回显 —— 展开下拉选「启用」，关闭后 input 应回显「启用」（验证回显数据正常）
test('EnumSingle 选择后回显数据正常', async ({ page }) => {
  await openLovSelect(page, 'LovSelect/EnumSingle', 'select');
  await page.click('.el-select__popper .el-select-dropdown__item:has-text("启用")');
  // 下拉关闭后，单选选择框应回显所选 label「启用」
  await expect(page.locator('.el-select')).toContainText('启用');
  await page.waitForTimeout(200); // 等回显文本稳定
  await expect(page.locator('.lov-select')).toHaveScreenshot();
});
