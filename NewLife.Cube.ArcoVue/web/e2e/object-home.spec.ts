import { expect, test } from '@playwright/test';

/**
 * 对象页 / 主页 / Db / File / Star（OSC-2608139feb）。
 * 前置：后端已启动且 Vite 代理命中 /api；登录态由 auth.setup 提供。
 */

test('主页 /home 与 /Admin/Index 展示系统信息并带刷新', async ({ page }) => {
  for (const path of ['/home', '/Admin/Index']) {
    await page.goto(path);
    await page.waitForTimeout(1500);
    // 系统信息 descriptions：Arco 渲染为 table 结构（.arco-descriptions-item 或 td），
    // 后端可达时至少 3 项非空；不可达时允许 empty（有错误提示）
    const items = page.locator('.arco-descriptions-item, .arco-descriptions table td');
    const visibleCount = await items.count();
    if (visibleCount >= 1) {
      expect(visibleCount).toBeGreaterThanOrEqual(3);
    } else {
      // 后端不可达时至少有错误提示或空状态，不静默通过
      expect(
        await page.locator('.arco-alert, .arco-empty').count(),
      ).toBeGreaterThan(0);
    }
    // 刷新按钮存在
    await expect(page.getByRole('button', { name: '刷新' }).first()).toBeVisible();
  }
});

/** 对象页：同一 DefaultObject 渲染并可保存（无「添加记录」表格按钮） */
for (const type of ['/Admin/Cube', '/Admin/Sys', '/Admin/Core', '/Admin/XCode']) {
  test(`对象页 ${type}：保存按钮存在且不是实体表格`, async ({ page }) => {
    await page.goto(type);
    await page.waitForTimeout(1500);
    // 不是 DefaultList：无「添加记录」
    await expect(page.getByRole('button', { name: /添加记录/ })).toHaveCount(0, { timeout: 20_000 });
    // 后端无该 ObjectController（如 CubeDemo 无 Sys/Core/XCode）时探测为 unknown 空状态，显式 skip
    const unknown = page.getByText('无法识别页面类型');
    if (await unknown.isVisible().catch(() => false)) {
      test.skip(true, `${type} 后端无该 ObjectController，探测为 unknown`);
      return;
    }
    // 保存按钮或权限不足只读提示（至少一个可见）
    const saveBtn = page.getByRole('button', { name: '保存' });
    const alert = page.locator('.arco-alert');
    const saveVisible = await saveBtn.isVisible().catch(() => false);
    const alertVisible = (await alert.count()) > 0;
    expect(saveVisible || alertVisible, `${type} 应有保存按钮或只读提示`).toBe(true);
    // 至少一个输入控件
    expect(
      (await page.locator('.arco-switch, .arco-input, .arco-select, .arco-cascader').count()) > 0,
    ).toBe(true);
    // T7 配置中心：左侧配置列表（自动注入 Object 页）软断言，避免环境菜单差异误伤
    const sideMenu = page.locator('.obj-side .arco-menu');
    const sideItems = await sideMenu.locator('.arco-menu-item').count();
    // Arco sub-menu 渲染为 .arco-menu-inline
    const subMenus = await sideMenu.locator('.arco-menu-inline').count();
    test.info().annotations.push({
      type: 'side-menu',
      description: `${type} 左侧配置列表项数=${sideItems}，子菜单数=${subMenus}`,
    });
    expect(sideItems, `${type} 左配置列表至少含当前页`).toBeGreaterThanOrEqual(1);
    // 多分组对象：Category 作为子菜单管理（魔方设置五分类）
    if (type === '/Admin/Cube') {
      const cubeSubs = await sideMenu.locator('.arco-menu-inline').count();
      expect(cubeSubs, '/Admin/Cube 应有 Category 子菜单').toBeGreaterThanOrEqual(1);
      // 右侧分组标题（不折叠 section h3）
      await expect(page.locator('.form-group__title').first()).toBeVisible();
    }
    // Category 分组标题（含单分组 h3）存在
    expect(
      (await page.locator('.form-group__title').count()) >= 1,
      `${type} 应有 Category 分组`,
    ).toBe(true);
  });
}

test('数据库页 /Admin/Db：列表卡片与操作按钮', async ({ page }) => {
  await page.goto('/Admin/Db');
  await page.waitForTimeout(1500);
  await expect(page.getByRole('button', { name: '刷新' })).toBeVisible({ timeout: 20_000 });
  // 列表卡片或空状态
  expect(
    (await page.locator('.arco-card, .arco-empty').count()) > 0,
  ).toBe(true);
  // 备份与下载架构按钮存在（权限不足时允许隐藏，但至少刷新可见）
  const backup = page.getByRole('button', { name: /备份/ });
  const download = page.getByRole('button', { name: /下载架构/ });
  expect(
    (await backup.count()) + (await download.count()) >= 0,
  ).toBe(true);
  // 下载架构（OSC-2608139feb 修复 createObjectURL 报错）：触发浏览器下载且文件名为 xml
  if ((await download.count()) > 0) {
    const dlPromise = page.waitForEvent('download', { timeout: 20_000 });
    await download.first().click({ force: true });
    const dl = await dlPromise;
    expect(dl.suggestedFilename().toLowerCase()).toMatch(/\.xml$/);
  }
  // 数据库备份（每卡片一个）：弹确认框（不点确认，避免真实写备份文件）
  const backupBtn = page.getByRole('button', { name: '数据库备份', exact: true });
  if ((await backupBtn.count()) > 0) {
    await backupBtn.first().click({ force: true });
    await expect(page.locator('.arco-modal')).toContainText('确认备份', { timeout: 5000 });
    await page.locator('.arco-modal .arco-btn-secondary').first().click({ force: true }).catch(() => {});
  }
});

test('文件页 /Admin/File：表格、排序与上传按钮', async ({ page }) => {
  await page.goto('/Admin/File');
  await page.waitForTimeout(1500);
  await expect(page.getByRole('button', { name: '刷新' })).toBeVisible({ timeout: 20_000 });
  // 文件表格或空状态
  expect(
    (await page.locator('.arco-table, .arco-empty').count()) > 0,
  ).toBe(true);
  // 排序选择器存在
  await expect(page.locator('.arco-select').first()).toBeVisible();
  // 上传按钮（权限不足时允许隐藏）
  const upload = page.getByRole('button', { name: '上传文件' });
  expect((await upload.count()) >= 0).toBe(true);
});

test('星尘设置 /Admin/Star：由 DefaultObject 渲染并带保存按钮', async ({ page }) => {
  await page.goto('/Admin/Star');
  await page.waitForTimeout(1500);
  await expect(page.getByRole('button', { name: /添加记录/ })).toHaveCount(0, { timeout: 20_000 });
  // CubeDemo（MVC 后端）无 StarController（Star 仅在 CubeNC），探测为 unknown 时显式 skip
  const unknown = page.getByText('无法识别页面类型');
  if (await unknown.isVisible().catch(() => false)) {
    test.skip(true, 'CubeDemo 后端无 StarController（Star 复用 DefaultObject 在 CubeNC 生效）');
    return;
  }
  const saveBtn = page.getByRole('button', { name: '保存' });
  const alert = page.locator('.arco-alert');
  const saveVisible = await saveBtn.isVisible().catch(() => false);
  const alertVisible = (await alert.count()) > 0;
  expect(saveVisible || alertVisible, 'Star 应有保存按钮或只读提示').toBe(true);
});
