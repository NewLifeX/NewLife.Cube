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
    // 首查完成=遮罩消失；有数据看表体行，没数据/失败看空态文案
    if (root.querySelector('.el-loading-mask')) return false;
    return (
      !!root.querySelector('.el-table__row') ||
      !!root.querySelector('.el-table__empty-text') ||
      !!root.querySelector('.ctp-total')
    );
  });
  await page.waitForTimeout(250);
}

const table = (page: import('@playwright/test').Page) => page.locator('.cube-table');

test('默认态-完整列表页五段结构', async ({ page }) => {
  await openStory(page, 'CubeTable/Default');

  // 自上而下：操作栏 → 搜索栏 → 统计/表格/分页主面板
  const sections = page.locator('.cube-table > *');
  await expect(sections.nth(0)).toHaveClass(/cta/);
  await expect(sections.nth(1)).toHaveClass(/cts-wrap/);
  await expect(sections.nth(2)).toHaveClass(/ct-panel/);

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
  // 分页栏口径：左「共 N 条」，右分页器 + 每页条数
  await expect(page.locator('.ctp-total')).toHaveText('共 3 条');
  await expect(page.locator('.ctp-group .el-pagination')).toBeVisible();
  await expect(table(page)).toHaveScreenshot();
});

test('只读态-写入口全部消失', async ({ page }) => {
  await openStory(page, 'CubeTable/ReadOnly');

  await expect(table(page).getByRole('button', { name: '添加' })).toHaveCount(0);
  await expect(table(page).getByRole('button', { name: /批量删除/ })).toHaveCount(0);
  await expect(table(page).getByRole('button', { name: '导入' })).toHaveCount(0);
  await expect(table(page).locator('.el-table__row').first().getByRole('button', { name: '删除' })).toHaveCount(0);
  // 查询类动作不受只读影响
  await expect(table(page).getByRole('button', { name: '导出' })).toBeVisible();
  await expect(table(page)).toHaveScreenshot();
});

test('空态-暂无数据且操作栏仍在', async ({ page }) => {
  await openStory(page, 'CubeTable/Empty');

  await expect(table(page)).toContainText('暂无数据');
  await expect(table(page).locator('.el-table__row')).toHaveCount(0);
  await expect(table(page).getByRole('button', { name: '添加' })).toBeVisible();
  await expect(table(page)).toHaveScreenshot();
});

test('失败态-原因就地显示在空表格里', async ({ page }) => {
  await openStory(page, 'CubeTable/LoadFail');

  // 不再顶部弹错误条：失败原因直接落在表格空态里，用户不必在两处之间对号
  await expect(table(page).locator('.el-table__empty-text')).toContainText('元数据接口不可用（CT 桩）');
  await expect(page.locator('.ct-error')).toHaveCount(0);
  await expect(table(page)).toHaveScreenshot();
});

test('指标行-图标/环比/设置列齿轮与刷新', async ({ page }) => {
  await openStory(page, 'CubeTable/Metrics');

  const bar = page.locator('.ctm');
  await expect(bar.locator('.ctm-item')).toHaveCount(3);
  await expect(bar.locator('.ctm-item').nth(1)).toContainText('运行中');
  await expect(bar.locator('.ctm-item').nth(1)).toContainText('↑1');
  await expect(bar.locator('.ctm-divider')).toHaveCount(2);
  await expect(bar.locator('.ctm-icon')).toHaveCount(3);
  await expect(bar.locator('.ctcs-trigger')).toBeVisible();
  await expect(bar.locator('.ctm-controls .el-button').last()).toBeVisible();
  await expect(bar).toHaveScreenshot();
});

test('搜索-收起态第一行三格，展开后每行三列栅格', async ({ page }) => {
  await openStory(page, 'CubeTable/Default');

  const search = page.locator('.cts-wrap');
  // 收起态：第一行三格 = 关键字 + 第一个条件 + 动作钮（展开/重置/搜索），其余条件不渲染
  await expect(search.locator('.cts-item')).toHaveCount(1);
  await expect(search.locator('.cts-toggle')).toContainText('展开');
  await expect(search.locator('.cts-actions')).toHaveCount(1);
  await expect(search).toHaveScreenshot();

  await search.locator('.cts-toggle').click();
  await expect(search.locator('.cts-item')).toHaveCount(6);
  await expect(search.locator('.cts-toggle')).toContainText('收起');
  // 三列栅格：DOM 格序 = 关键字 → 动作格 → 条件×N（动作格靠 CSS 钉回第一行最右列）
  const grid = search.locator('.cts-grid');
  // 中文 locale 下 getComputedStyle 用全角逗号分隔列表值，正则分隔符无关
  await expect(grid).toHaveCSS('grid-template-columns', /^\d+(\.\d+)?px[ ,，]*\d+(\.\d+)?px[ ,，]*\d+(\.\d+)?px$/);
  await expect(grid.locator('> *').first()).toHaveClass(/cts-input/);
  await expect(grid.locator('> *').nth(1)).toHaveClass(/cts-cell--end/);
  await expect(grid.locator('> *').nth(2)).toHaveClass(/cts-item/);
  await expect(grid.locator('> .cts-cell--end').getByRole('button', { name: '搜索' })).toBeVisible();
  // 展开后动作格仍钉在第一行行尾（与关键字同 y、在最右列），不随条件流到尾行
  const box = (sel: string) => grid.locator(sel).boundingBox().then((b) => ({ x: b!.x, y: Math.round(b!.y) }));
  const inputBox = await box('> .cts-input');
  const endBox = await box('> .cts-cell--end');
  const firstItemBox = await box('> .cts-item >> nth=0');
  expect(endBox.y).toBe(inputBox.y);
  expect(endBox.x).toBeGreaterThan(firstItemBox.x);
  // 每行三列：第二格（条件1）自动流从第二行开始（第一行被动作格钉掉一列）
  const secondItemBox = await box('> .cts-item >> nth=1');
  expect(secondItemBox.y).toBeGreaterThan(inputBox.y);
  await expect(search).toHaveScreenshot();
});

test('搜索-只有一个条件时也是同一套一体式栏', async ({ page }) => {
  await openStory(page, 'CubeTable/OneFilter');

  const search = page.locator('.cts-wrap');
  // 收起态就把唯一条件露在第一行
  await expect(search.locator('.cts-item')).toHaveCount(1);
  await search.locator('.cts-toggle').click();
  await expect(search.locator('.cts-item')).toHaveCount(1);
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

test('行为-关键字回车即查并收窄结果', async ({ page }) => {
  await openStory(page, 'CubeTable/Default');

  await page.locator('.cts-input input').fill('注塑机 B');
  await page.locator('.cts-input input').press('Enter');
  await expect(table(page).locator('.el-table__row')).toHaveCount(1);
  await expect(table(page)).toContainText('注塑机 B');

  // 展开后用重置清空关键字，回到三条
  await page.locator('.cts-toggle').click();
  await page.locator('.cts-actions').getByRole('button', { name: '重置' }).click();
  await expect(table(page).locator('.el-table__row')).toHaveCount(3);
});

test('行为-设置列可隐藏列并记住选择', async ({ page }) => {
  await openStory(page, 'CubeTable/Default');

  const remarkHeader = page.getByRole('columnheader', { name: '备注' });
  await expect(remarkHeader).toBeVisible();

  await page.locator('.ctcs-trigger').click();
  // EP 的 el-checkbox 原生 input 是隐藏的，点 input 会「element is not visible」——点 label 文本
  await page.locator('.el-popover').filter({ hasText: '展示列' }).getByText('备注', { exact: true }).click();
  await expect(remarkHeader).toHaveCount(0);

  // localStorage 记忆（键按实体前缀分桶）
  const stored = await page.evaluate(() => localStorage.getItem('cube-table:columns:/api/Demo/Device'));
  expect(stored).toContain('"Remark":false');
});

test('行为-每页条数换档走独立下拉并回第 1 页', async ({ page }) => {
  await openStory(page, 'CubeTable/Default');

  await expect(page.locator('.ctp-total')).toHaveText('共 3 条');
  // 每页条数是独立下拉（与 MES 分页栏一致），不在 el-pagination 的 layout 里
  await expect(page.locator('.ctp-group .el-pagination .sizes')).toHaveCount(0);

  await page.locator('.ctp-size').click();
  await page.locator('.el-select-dropdown__item').filter({ hasText: '10 条/页' }).last().click();
  // EP 单选把选中文本渲染在 span 里（input 是搜索框，值为空），断言可见文本而不是 input value
  await expect(page.locator('.ctp-size')).toContainText('10 条/页');
  // 换档后引擎回第 1 页：页码高亮仍是第一页，三条数据也仍在一页内
  await expect(page.locator('.ctp-group .el-pager li.is-active')).toHaveText('1');
  await expect(table(page).locator('.el-table__row')).toHaveCount(3);
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

test('行为-操作列超过 4 个动作收进更多', async ({ page }) => {
  await openStory(page, 'CubeTable/RowActions');

  const firstRow = table(page).locator('.el-table__row').first();
  // 内置三键直接显示，第 4 个位置是「更多」；追加的两个动作不铺在行里
  await expect(firstRow.getByRole('button', { name: '查看' })).toBeVisible();
  await expect(firstRow.getByRole('button', { name: '编辑' })).toBeVisible();
  await expect(firstRow.getByRole('button', { name: '删除' })).toBeVisible();
  await expect(firstRow.getByRole('button', { name: '更多' })).toBeVisible();
  await expect(firstRow.getByRole('button', { name: '复制' })).toHaveCount(0);
  await expect(table(page)).toHaveScreenshot();

  // 点开「更多」出现两个后加动作；点「复制」真触发回调（借新增弹窗作证）
  await firstRow.getByRole('button', { name: '更多' }).click();
  // 每行都挂了一个 popper（未展开的也在 DOM 里），必须限定到当前可见的那份
  const popper = page.locator('.el-dropdown__popper:visible');
  const menu = popper.locator('.el-dropdown-menu__item');
  await expect(menu.filter({ hasText: '复制' })).toBeVisible();
  await expect(menu.filter({ hasText: '归档' })).toBeVisible();
  await expect(popper).toHaveScreenshot();
  await menu.filter({ hasText: '复制' }).click();
  await expect(page.locator('.el-dialog')).toContainText('新增');
});
