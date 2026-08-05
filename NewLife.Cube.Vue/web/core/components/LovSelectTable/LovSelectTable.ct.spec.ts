import { test, expect } from '@playwright/test';

// 组件视觉回归：通过共享 gallery 渲染 story 变体并截图基线。
// 首次运行自动生成基线（通过）；之后每次运行做像素级 diff，回归即失败。
const GALLERY = process.env.CT_BASE_URL || 'http://127.0.0.1:5190/';

declare global {
  interface Window {
    mountStory: (id: string, props?: Record<string, unknown>) => void;
    setStoryProps: (patch: Record<string, unknown>) => void;
  }
}

async function openStory(page: import('@playwright/test').Page, id: string) {
  // 一步到位：直接打开带 ?story=<id> 的 URL，main.ts 的 mountOnlyStory 会在加载时
  // 挂载该故事并对 LovSelectTable 前缀自动打开弹窗（dialogVisible:true），
  // 与「evaluate(mountStory)+setStoryProps」等价，已有截图基线不会抖动。
  await page.goto(`${GALLERY}?story=${encodeURIComponent(id)}`);
  await page.waitForSelector('.el-table__row');
  // 等弹窗开场动画收尾，规避像素抖动导致基线 diff 失败（Element Plus 弹窗有 transition）
  await page.waitForFunction(() => {
    const d = document.querySelector('.el-dialog');
    return !!d && getComputedStyle(d).visibility === 'visible';
  });
  await page.waitForTimeout(400);
}

test('单选-打开弹窗', async ({ page }) => {
  await openStory(page, 'LovSelectTable/SingleOpen');
  await expect(page.locator('.el-dialog')).toHaveScreenshot();
});

test('多选-打开弹窗（含确定按钮）', async ({ page }) => {
  await openStory(page, 'LovSelectTable/MultiOpen');
  await expect(page.locator('.el-dialog')).toHaveScreenshot();
});

test('单选-回显已选高亮', async ({ page }) => {
  await openStory(page, 'LovSelectTable/SingleEcho');
  // 回显统计：单选已选 1 项（验证「选择后回显的已选统计」）
  await expect(page.locator('.lst-selected-count')).toHaveText('已选 1 项');
  await expect(page.locator('.el-dialog')).toHaveScreenshot();
});

test('多选-回显已选高亮', async ({ page }) => {
  await openStory(page, 'LovSelectTable/MultiEcho');
  // 回显统计：多选已选 2 项（验证「选择后回显的已选统计」）
  await expect(page.locator('.lst-selected-count')).toHaveText('已选 2 项');
  await expect(page.locator('.el-dialog')).toHaveScreenshot();
});

// 测试状态：多选 + 分页（23 条/2 页）+ 跨页选中 ——
// 第 1 页勾选 1 行（底部「已选 1 项」）→ 翻第 2 页勾选 2 行（底部「已选 3 项」），
// 验证翻页选择左下角统计正常 + 跨页选中累计（reserve-selection）。
test('多选-翻页跨页选中统计正常', async ({ page }) => {
  await openStory(page, 'LovSelectTable/PagedMultiSelection');

  // 第 1 页：勾选首行
  await page.locator('.el-table__row').first().locator('.el-checkbox').click();
  await expect(page.locator('.lst-selected-count')).toHaveText('已选 1 项');

  // 翻到第 2 页（23 条 / 每页 20 → 第 2 页 3 行）
  await page.click('.el-pagination .btn-next');
  await page.waitForFunction(() => {
    const rows = document.querySelectorAll('.el-table__row');
    return rows.length > 0 && rows.length < 20;
  });

  // 第 2 页：勾选前两行
  const rows = page.locator('.el-table__row');
  await rows.nth(0).locator('.el-checkbox').click();
  await rows.nth(1).locator('.el-checkbox').click();
  // 跨页累计：第 1 页 1 行 + 第 2 页 2 行 = 3 项
  await expect(page.locator('.lst-selected-count')).toHaveText('已选 3 项');

  await page.waitForTimeout(200);
  await expect(page.locator('.el-dialog')).toHaveScreenshot();
});

// 测试状态：多选 + 分页 + 跨页回显（modelValue=[1,2,21,22] 横跨两页）——
// 验证根因 C2 修复：打开后统计「已选 4 项」；翻到第 2 页本页已选行(21,22)勾选且统计保持；翻回第 1 页保持。
// 对应症状：点击回显时已选不正确 / 翻页后已选不正确 / 翻页后勾选才更新。
test('多选-跨页回显已选统计正确', async ({ page }) => {
  await openStory(page, 'LovSelectTable/PagedMultiEcho');

  // 打开：统计应为「已选 4 项」（修复前会被当前页勾选裁成「已选 2 项」）
  await expect(page.locator('.lst-selected-count')).toHaveText('已选 4 项');
  // 第 1 页(1-20) 仅 1,2 命中 → 2 行高亮
  await expect(page.locator('.lst-row--selected')).toHaveCount(2);

  // 翻到第 2 页（21-23）
  await page.click('.el-pagination .btn-next');
  await page.waitForFunction(() => {
    const rows = document.querySelectorAll('.el-table__row');
    return rows.length > 0 && rows.length < 20;
  });
  // 第 2 页本页已选行(21,22)应被回显勾选，且统计仍为「已选 4 项」
  await expect(page.locator('.lst-selected-count')).toHaveText('已选 4 项');
  await expect(page.locator('.lst-row--selected')).toHaveCount(2);

  // 翻回第 1 页，统计与勾选保持
  await page.click('.el-pagination .btn-prev');
  await page.waitForFunction(() => {
    const rows = document.querySelectorAll('.el-table__row');
    return rows.length === 20;
  });
  await expect(page.locator('.lst-selected-count')).toHaveText('已选 4 项');
  await expect(page.locator('.lst-row--selected')).toHaveCount(2);

  await page.waitForTimeout(200);
  await expect(page.locator('.el-dialog')).toHaveScreenshot();
});
