/**
 * 参数管理页面 E2E（标准动态页：查询区 / 列表 / 表单全部来自 `/meta`）
 *
 * 验证参数页使用默认引擎提供的查询区/表格/工具栏，并正确渲染列头与新增表单。
 *
 * 断言不写死界面文案：页面表头与表单标签来自后端 `/meta`（按请求语言本地化），
 * 因此先取元数据的 displayName/header，再逐列/逐项比对，界面语言切换不会让用例失效。
 *
 * 运行：pnpm test:e2e --grep "参数管理"
 */
import { test, expect } from '@playwright/test';
import { readListHeaders, readMeta } from './metaHelper';

const basePath = '/Admin/Parameter';

test.describe('参数管理（标准动态页）', () => {
  test('列表页加载且查询区/表格/操作列齐备', async ({ page }) => {
    await page.goto(basePath);
    await expect(page.locator('.el-table')).toBeVisible({ timeout: 20000 });

    // 查询区：搜索框 + 查询/重置按钮（工具栏里还有隐藏的导入 input，按可见性取第一个）
    await expect(page.locator('.cube-table input:not([type="hidden"]):visible').first()).toBeVisible();
    expect(await page.locator('.cube-table button').count()).toBeGreaterThan(1);
    // 数据区：最后一列是操作列
    await expect(page.locator('.el-table__header th').last()).toBeVisible();
  });

  test('列头与元数据一致', async ({ page }) => {
    await page.goto(basePath);
    await expect(page.locator('.el-table__header th').nth(2)).toBeVisible({ timeout: 20000 });

    const expected = await readListHeaders(page, '/api/Admin/Parameter/meta');
    expect(expected.length).toBeGreaterThan(5);

    const headers = (await page.locator('.el-table__header th').allInnerTexts()).join('|');
    for (const name of expected.slice(0, 5)) {
      expect(headers).toContain(name);
    }
  });

  test('查看弹窗按元数据渲染字段与标签', async ({ page }) => {
    await page.goto(basePath);
    const row = page.locator('.el-table__body tr').first();
    await expect(row).toBeVisible({ timeout: 20000 });

    // 行内操作列的第一个按钮是「查看」（文案随语言变化，按位置取；当前 demo 角色只有查看权限）
    await row.locator('.el-table__cell:last-child button').first().click();

    const dialog = page.locator('.el-dialog').first();
    await expect(dialog).toBeVisible({ timeout: 15000 });
    // 字段由 /meta 异步下发，等待表单渲染完成后再比对文案
    await expect(dialog.locator('.fmc-field').first()).toBeVisible({ timeout: 15000 });

    // 弹窗里的字段标签来自后端 meta.detail，逐项比对，不写死中文/英文（主键不出现在详情里）
    const meta = await readMeta(page, '/api/Admin/Parameter/meta');
    const labels = (meta.detail ?? [])
      .filter((field) => !field.primaryKey)
      .map((field) => (field.header?.trim() || field.displayName?.trim() || '').trim())
      .filter((label) => label.length > 0);
    expect(labels.length).toBeGreaterThan(5);

    const dialogText = await dialog.innerText();
    for (const label of labels.slice(0, 5)) {
      expect(dialogText).toContain(label);
    }
  });

  test('新增表单按元数据渲染字段与标签', async ({ page }) => {
    await page.goto(basePath);
    await expect(page.locator('.el-table__body tr').first()).toBeVisible({ timeout: 20000 });

    // 工具栏第一个按钮是「新增」（文案随语言变化，按位置取）
    await page.locator('.cube-table button').first().click();

    const dialog = page.locator('.el-dialog').first();
    await expect(dialog).toBeVisible({ timeout: 15000 });
    // 字段由 /meta 异步下发，等待表单渲染完成后再比对文案
    await expect(dialog.locator('.fmc-field').first()).toBeVisible({ timeout: 15000 });

    // 表单字段与标签来自后端 meta.add（主键不在新增表单里），逐项比对
    const meta = await readMeta(page, '/api/Admin/Parameter/meta');
    const labels = (meta.add ?? [])
      .filter((field) => !field.primaryKey)
      .map((field) => (field.header?.trim() || field.displayName?.trim() || '').trim())
      .filter((label) => label.length > 0);
    expect(labels.length).toBeGreaterThan(5);

    const dialogText = await dialog.innerText();
    for (const label of labels.slice(0, 5)) {
      expect(dialogText).toContain(label);
    }

    // 不落数据：直接关掉弹窗
    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden({ timeout: 10000 });
  });
});

