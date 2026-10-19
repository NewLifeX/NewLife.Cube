/**
 * CubeTable 组件视觉回归（CT）
 *
 * 走共享 gallery：`?story=<id>` 深链挂载变体，`toHaveScreenshot()` 做像素基线。
 * 基线目录 `*-snapshots/` 已被 .gitignore 忽略（本机首次运行自动生成并通过）。
 *
 * 每个变体除了截图，还钉一句「用户能不能做成这件事」的行为断言，
 * 避免只有像素、没有语义。
 *
 * 运行：pnpm test:ct   或   npx playwright test --config playwright-ct.config.ts CubeTable
 * 更新基线：pnpm test:ct:update
 */
import { test, expect } from '@playwright/test';

const GALLERY = process.env.CT_BASE_URL || 'http://127.0.0.1:5190/';

declare global {
  interface Window {
    mountStory: (id: string, props?: Record<string, unknown>) => void;
    setStoryProps: (patch: Record<string, unknown>) => void;
    unmountStory: () => void;
  }
}

/** 打开变体并等首查落地（表体出现行 / 或空态出现），避免截到 loading 中间帧 */
async function openStory(page: import('@playwright/test').Page, id: string) {
  await page.goto(`${GALLERY}?story=${encodeURIComponent(id)}`);
  // gallery 的 #gallery-nav 在 ?story= 深链下是空壳，但样式仍占位并拦截指针事件，
  // 组件区域在它下面 → 点击全部超时。它不属于被测组件，直接摘掉。
  await page.evaluate(() => document.getElementById('gallery-nav')?.remove());
  await page.waitForSelector('.cube-table');
  await page.waitForFunction(() => {
    const root = document.querySelector('.cube-table');
    if (!root) return false;
    // 首查完成=遮罩消失；有数据看表体行，没数据看空态文案
    if (root.querySelector('.el-loading-mask')) return false;
    return (
      !!root.querySelector('.el-table__row') ||
      !!root.querySelector('.el-table__empty-block') ||
      !!root.querySelector('.ct-error')
    );
  });
  await page.waitForTimeout(250);
}

const table = (page: import('@playwright/test').Page) => page.locator('.cube-table');

test('默认态-完整列表页', async ({ page }) => {
  await openStory(page, 'CubeTable/Default');

  // 值集列翻成标签（不是码值）、布尔列是开关、日期列已格式化、空值给占位
  const firstRow = table(page).locator('.el-table__row').first();
  await expect(firstRow).toContainText('注塑机 A');
  // Status=1 → 「启用」必须出现在这一行（只写 toContainText 会被同名列头混过去）
  await expect(firstRow).toContainText('启用');
  await expect(table(page).locator('.el-table__row').nth(1)).toContainText('禁用');
  await expect(table(page).locator('.el-table__row').nth(2)).toContainText('草稿');
  await expect(firstRow).toContainText('2026-10-19 08:30:00');
  await expect(firstRow).toContainText('2026-10-18');
  await expect(firstRow).toContainText('—');
  // 数值 0 必须显示成 0，不能当空值吞掉
  await expect(table(page).locator('.el-table__row').nth(1)).toContainText('0');
  await expect(table(page)).toHaveScreenshot();
});

test('只读态-写入口全部消失', async ({ page }) => {
  await openStory(page, 'CubeTable/ReadOnly');

  await expect(table(page).getByRole('button', { name: '新增' })).toHaveCount(0);
  await expect(table(page).getByRole('button', { name: /批量删除/ })).toHaveCount(0);
  await expect(table(page).getByRole('button', { name: '导入' })).toHaveCount(0);
  await expect(table(page).locator('.el-table__row').first().getByRole('button', { name: '删除' })).toHaveCount(0);
  // 查询类动作不受只读影响
  await expect(table(page).getByRole('button', { name: '导出' })).toBeVisible();
  await expect(table(page)).toHaveScreenshot();
});

test('空态-暂无数据且工具栏仍在', async ({ page }) => {
  await openStory(page, 'CubeTable/Empty');

  await expect(table(page)).toContainText('暂无数据');
  await expect(table(page).locator('.el-table__row')).toHaveCount(0);
  await expect(table(page).getByRole('button', { name: '新增' })).toBeVisible();
  await expect(table(page)).toHaveScreenshot();
});

test('失败态-错误条与就地重试', async ({ page }) => {
  await openStory(page, 'CubeTable/LoadFail');

  await expect(page.locator('.ct-error')).toBeVisible();
  await expect(page.locator('.ct-error')).toContainText('元数据接口不可用（CT 桩）');
  await expect(table(page)).toHaveScreenshot();
});

test('条件过多-收起态只给四个并可展开', async ({ page }) => {
  await openStory(page, 'CubeTable/ManyFilters');

  const search = page.locator('.cts');
  await expect(search).toContainText('更多(2)');
  await expect(search.locator('.cts-item')).toHaveCount(4);
  await expect(search).toHaveScreenshot();

  await search.locator('.cts-toggle').click();
  await expect(search).toContainText('收起');
  await expect(search.locator('.cts-item')).toHaveCount(6);
  await expect(search).toHaveScreenshot();
});

test('新增弹窗-字段控件与必填标记', async ({ page }) => {
  await openStory(page, 'CubeTable/FormDialog');

  const dialog = page.locator('.el-dialog');
  await expect(dialog).toBeVisible();
  await expect(dialog).toContainText('新增');
  await expect(dialog).toContainText('名称');
  // 必填口径来自 fieldControl.isRequiredField：名称 nullable:false → 带星号（FormContent 的 .fmc-required）
  const nameItem = dialog.locator('.fmc-field').filter({ hasText: '名称' }).first();
  await expect(nameItem.locator('.fmc-required')).toHaveCount(1);
  await expect(dialog.getByRole('button', { name: '保存' })).toBeVisible();
  await expect(dialog).toHaveScreenshot();
});

test('行为-勾选后批量删除按钮可用并带条数', async ({ page }) => {
  await openStory(page, 'CubeTable/Default');

  const batch = table(page).getByRole('button', { name: /批量删除/ });
  await expect(batch).toBeDisabled();

  await table(page).locator('.el-table__row').first().locator('.el-checkbox').click();
  await expect(batch).toBeEnabled();
  await expect(batch).toContainText('（1）');

  await batch.click();
  const confirmBox = page.locator('.el-message-box');
  await expect(confirmBox).toContainText('1 条');
  await confirmBox.getByRole('button', { name: '删除' }).click();
  await expect(table(page).locator('.el-table__row')).toHaveCount(2);
});

test('行为-关键字搜索走 q 并收窄结果', async ({ page }) => {
  await openStory(page, 'CubeTable/Default');

  await page.locator('.cts-keyword input').fill('注塑机 B');
  await page.getByRole('button', { name: '查询' }).click();
  await expect(table(page).locator('.el-table__row')).toHaveCount(1);
  await expect(table(page)).toContainText('注塑机 B');

  await page.getByRole('button', { name: '重置' }).click();
  await expect(table(page).locator('.el-table__row')).toHaveCount(3);
});

test('行为-设置列可隐藏列并记住选择', async ({ page }) => {
  await openStory(page, 'CubeTable/Default');

  const remarkHeader = page.getByRole('columnheader', { name: '备注' });
  await expect(remarkHeader).toBeVisible();

  await page.locator('.ct-cols-trigger').click();
  // EP 的 el-checkbox 原生 input 是隐藏的，点 input 会「element is not visible」——点 label 文本
  await page.locator('.el-popover').filter({ hasText: '展示列' }).getByText('备注', { exact: true }).click();
  await expect(remarkHeader).toHaveCount(0);

  // localStorage 记忆（键按实体前缀分桶）
  const stored = await page.evaluate(() => localStorage.getItem('cube-table:columns:/api/Demo/Device'));
  expect(stored).toContain('"Remark":false');
});

test('行为-行内删除先确认，取消不动数据', async ({ page }) => {
  await openStory(page, 'CubeTable/Default');

  await table(page).locator('.el-table__row').first().getByRole('button', { name: '删除' }).click();
  const confirmBox = page.locator('.el-message-box');
  await expect(confirmBox).toBeVisible();
  await confirmBox.getByRole('button', { name: '取消' }).click();
  await expect(table(page).locator('.el-table__row')).toHaveCount(3);

  await table(page).locator('.el-table__row').first().getByRole('button', { name: '删除' }).click();
  await confirmBox.getByRole('button', { name: '删除' }).click();
  await expect(table(page).locator('.el-table__row')).toHaveCount(2);
});

test('行为-行内编辑先取详情再回显', async ({ page }) => {
  await openStory(page, 'CubeTable/Default');

  await table(page).locator('.el-table__row').first().getByRole('button', { name: '编辑' }).click();
  const dialog = page.locator('.el-dialog');
  await expect(dialog).toContainText('编辑');
  await expect(dialog.locator('input').first()).not.toHaveValue('');
});
