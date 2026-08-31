/**
 * React 皮肤设置 E2E（ReactSetting + ReactController + ConfigNav 排开）
 *
 * 验证：
 * - 配置导航一字排开（基本设置…React设置…访问规则，无"更多配置"下拉）
 * - React 设置页可维护（表单风格字段存在，分类 表单/导航）
 * - 切换表单风格 vertical 后，实体编辑表单变为"标签一行控件一行"（保存自动刷新）
 * - 还原为 inline（避免影响其它测试）
 *
 * ⚠️ 并发约束：本套件「导航排开」测试需要 ReactSetting.configNavFlat=true，
 * 而 config.spec 依赖 configNavFlat=false。两者在同一 ReactSetting 上配置相反，
 * **并发运行（workers>1）会互相覆盖导致偶发失败**——这两个 spec 必须串行运行
 * （--workers=1）或分开运行。测试结束已恢复 false 以降低冲突概率。
 */
import { expect, test, type Page } from '@playwright/test';

const ROW = '.ant-table-tbody tr.ant-table-row';
const TABLE_TIMEOUT = 15000;

/**
 * 用 API 直接更新 React 皮肤配置（幂等：GET 当前值合并 patch 后 PUT）。
 *
 * 规避 ConfigPage switch 点击后 Form 值异步同步的时序问题（曾偶发提交旧值导致导航排开切换失败）；
 * 改配置后需 reload 重置 useReactSetting store 缓存，否则同页后续访问读到旧值。
 * 先导航到同源页面再 evaluate（about:blank 读取 localStorage 会抛 SecurityError）。
 */
async function setReactConfig(page: Page, patch: Record<string, unknown>) {
  await page.goto('/Admin/React');
  await page.evaluate(async (patch) => {
    const t = localStorage.getItem('token') || '';
    const headers = { Authorization: 'Bearer ' + t, 'Content-Type': 'application/json' };
    const getRes = await fetch('/api/Admin/React', { headers });
    const cur = (await getRes.json()).data ?? {};
    const body = {
      FormStyle: cur.formStyle ?? 'inline',
      DescMode: cur.descMode ?? 1,
      InputClear: cur.inputClear ?? false,
      ConfigNavFlat: cur.configNavFlat ?? false,
      ...patch,
    };
    await fetch('/api/Admin/React', { method: 'PUT', headers, body: JSON.stringify(body) });
  }, patch);
  // reload 重置 useReactSetting store 缓存，使后续页面读到新配置
  await page.reload();
}

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
    // 1. API 开启导航排开
    await setReactConfig(page, { ConfigNavFlat: true });

    // 2. 断言配置导航一字排开：全部配置页可见且无「更多配置」下拉
    await page.goto('/Admin/Cube');
    const nav = page.locator('.cube-config-nav');
    for (const label of NAV_ALL) {
      await expect(nav.getByText(label, { exact: true }).first()).toBeVisible();
    }
    await expect(nav.getByText(/更多配置/)).toHaveCount(0);

    // 3. API 还原为关闭（避免影响配置中心 config.spec：核心配置 + 更多下拉）
    await setReactConfig(page, { ConfigNavFlat: false });
    // 刷新重置 store 缓存，自检导航恢复 Segmented + 更多配置下拉
    await page.reload();
    await expect(page.locator('.cube-config-nav').getByText(/更多配置/)).toBeVisible({ timeout: 8000 });
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

    // 4. API 还原为 inline（避免影响其它测试）
    await setReactConfig(page, { FormStyle: 'inline' });
    await page.goto('/Admin/React');
    await expect(page.locator('input[placeholder="表单风格"]')).toHaveValue('inline', { timeout: 10000 });
  });

  test('文本框清除图标开关：默认隐藏，开启后显示清空叉叉（保存自动刷新）', async ({ page }) => {
    // 1. API 开启清除图标：用户编辑表单的文本输入框显示清空叉叉
    await setReactConfig(page, { InputClear: true });
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

    // 2. 关闭清除图标：表单文本输入框无清空叉叉（reload 重置 store 缓存）
    await page.locator('.ant-modal button', { hasText: /取\s*消/ }).first().click();
    await setReactConfig(page, { InputClear: false });
    await page.reload();
    await page.goto('/Admin/User');
    await page.waitForSelector(ROW, { timeout: TABLE_TIMEOUT });
    await page.locator(ROW).first().locator('.cube-op-btn-op-edit').click();
    await expect(page.locator('.ant-modal input[placeholder="名称"]')).toBeVisible({ timeout: 8000 });
    await page.locator('.ant-modal input[placeholder="名称"]').fill('abc');
    const clearCount = await page.locator('.ant-modal .ant-input-clear-icon:not(.ant-input-clear-icon-hidden)').count();
    expect(clearCount).toBe(0);
  });
});
