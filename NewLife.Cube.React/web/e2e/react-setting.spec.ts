/**
 * React 皮肤设置 E2E（ReactSetting + ReactController + ConfigNav 排开）
 *
 * 验证：
 * - 配置导航一字排开（基本设置…React设置…访问规则，无"更多配置"下拉）
 * - React 设置页可维护（表单风格字段存在，分类 表单/导航）
 * - 切换表单风格 vertical 后，实体编辑表单变为"标签一行控件一行"（保存自动刷新）
 * - 还原为 inline（避免影响其它测试）
 */
import { expect, test } from '@playwright/test';

const ROW = '.ant-table-tbody tr.ant-table-row';
const TABLE_TIMEOUT = 15000;

/** 配置导航全部项（一字排开时应全部可见） */
const NAV_ALL = [
  '基本设置',
  '系统设置',
  '星尘设置',
  '数据中间件',
  '魔方设置',
  'React设置',
  '短信设置',
  '邮件设置',
  'OAuth设置',
  '访问规则',
];

test.describe('React 皮肤设置（/Admin/React）', () => {
  test('配置导航一字排开：全部配置页可见且无下拉', async ({ page }) => {
    await page.goto('/Admin/Cube');
    const nav = page.locator('.cube-config-nav');
    for (const label of NAV_ALL) {
      await expect(nav.getByText(label, { exact: true }).first()).toBeVisible();
    }
    await expect(nav.getByText(/更多配置/)).toHaveCount(0);
  });

  test('切换表单风格为 vertical 后编辑表单标签一行控件一行（保存自动刷新）', async ({ page }) => {
    // 1. 打开 React 设置页，断言字段存在
    await page.goto('/Admin/React');
    await expect(page.locator('input[placeholder="表单风格"]')).toBeVisible();
    await expect(page.getByText('表单', { exact: true }).first()).toBeVisible();

    // 2. 改为 vertical 并保存（保存后自动刷新使全局生效）
    await page.locator('input[placeholder="表单风格"]').fill('vertical');
    await page.locator('button', { hasText: /保存设置/ }).first().click();
    await expect(page.locator('.ant-message').getByText('保存成功')).toBeVisible({ timeout: 8000 });
    // 保存后自动刷新
    await page.waitForLoadState('load', { timeout: 10000 }).catch(() => undefined);
    await expect(page.locator('input[placeholder="表单风格"]')).toHaveValue('vertical', { timeout: 10000 });

    // 3. 打开用户编辑表单：vertical 风格（标签一行 + 控件一行），无 inline 三栏
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    await page.locator(ROW).first().locator('.cube-op-btn-op-edit').click();
    await expect(page.locator('.ant-modal').locator('.cube-form-vertical-item').first()).toBeVisible({ timeout: 8000 });
    await expect(page.locator('.ant-modal').locator('.cube-form-inline-cell')).toHaveCount(0);
    await expect(page.locator('.ant-modal').locator('.ant-form-item-label').first()).toBeVisible();
    await page.locator('.ant-modal button', { hasText: /取\s*消/ }).first().click();

    // 4. 还原为 inline（避免影响其它测试）
    await page.goto('/Admin/React');
    await page.locator('input[placeholder="表单风格"]').fill('inline');
    await page.locator('button', { hasText: /保存设置/ }).first().click();
    await expect(page.locator('.ant-message').getByText('保存成功')).toBeVisible({ timeout: 8000 });
    await page.waitForLoadState('load', { timeout: 10000 }).catch(() => undefined);
    await expect(page.locator('input[placeholder="表单风格"]')).toHaveValue('inline', { timeout: 10000 });
  });

  test('文本框清除图标开关：默认隐藏，开启后显示清空叉叉（保存自动刷新）', async ({ page }) => {
    /** 打开 React 设置并确保开关处于指定状态（幂等） */
    const ensureSwitch = async (checked: boolean) => {
      await page.goto('/Admin/React');
      const row = page.locator('.cube-config-row', { hasText: '文本框清除图标' }).first();
      const sw = row.locator('.ant-switch');
      const isOn = (await sw.getAttribute('class'))?.includes('ant-switch-checked') ?? false;
      if (isOn !== checked) await sw.click();
      await page.locator('button', { hasText: /保存设置/ }).first().click();
      await expect(page.locator('.ant-message').getByText('保存成功')).toBeVisible({ timeout: 8000 });
      await page.waitForLoadState('load', { timeout: 10000 }).catch(() => undefined);
    };

    // 1. 开启清除图标：用户编辑表单的文本输入框显示清空叉叉
    await ensureSwitch(true);
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    await page.locator(ROW).first().locator('.cube-op-btn-op-edit').click();
    await expect(page.locator('.ant-modal input[placeholder="名称"]')).toBeVisible({ timeout: 8000 });
    await page.locator('.ant-modal input[placeholder="名称"]').fill('abc');
    await expect(
      page
        .locator('.ant-modal input[placeholder="名称"]')
        .locator('xpath=ancestor::*[contains(@class,"ant-input-affix-wrapper")]')
        .locator('.ant-input-clear-icon:not(.ant-input-clear-icon-hidden)')
        .first(),
    ).toBeVisible({ timeout: 8000 });

    // 2. 关闭清除图标：表单文本输入框无清空叉叉
    await page.locator('.ant-modal button', { hasText: /取\s*消/ }).first().click();
    await ensureSwitch(false);
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    await page.locator(ROW).first().locator('.cube-op-btn-op-edit').click();
    await expect(page.locator('.ant-modal input[placeholder="名称"]')).toBeVisible({ timeout: 8000 });
    await page.locator('.ant-modal input[placeholder="名称"]').fill('abc');
    const clearCount = await page.locator('.ant-modal .ant-input-clear-icon:not(.ant-input-clear-icon-hidden)').count();
    expect(clearCount).toBe(0);
  });
});
