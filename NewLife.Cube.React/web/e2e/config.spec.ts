/**
 * 配置中心（Config Hub）E2E
 *
 * 覆盖（配置中心标准场景）：
 * - 魔方设置页顶部显示配置切换器（5 核心 + 更多配置），当前项高亮
 * - 切换 5 个核心配置页均可达（无 404），配置表单正常渲染
 * - 更多配置下拉切换 短信/邮件/OAuth/访问规则，更多按钮高亮，面包屑正确
 * - 配置页每行一个配置项，右侧显示 Description
 */
import { expect, test } from '@playwright/test';

const NAV_TIMEOUT = 10000;

/** 核心配置（对齐 ConfigNav.CONFIG_NAV） */
const MAIN_PAGES = [
  { path: '/Admin/Core', label: '基本设置' },
  { path: '/Admin/Sys', label: '系统设置' },
  { path: '/Admin/Star', label: '星尘设置' },
  { path: '/Admin/XCode', label: '数据中间件' },
  { path: '/Admin/Cube', label: '魔方设置' },
];

/** 更多配置（对齐 ConfigNav.MORE_NAV） */
const MORE_PAGES = [
  { path: '/Admin/SmsConfig', label: '短信设置' },
  { path: '/Admin/MailConfig', label: '邮件设置' },
  { path: '/Admin/OAuthConfig', label: 'OAuth设置' },
  { path: '/Admin/AccessRule', label: '访问规则' },
];

test.describe('配置中心（Config Hub）', () => {
  test('魔方设置页顶部显示配置切换器且高亮当前项', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => {
      if (m.type() === 'error') errors.push(m.text());
    });
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/Admin/Cube');
    const nav = page.locator('.cube-config-nav');
    await expect(nav).toBeVisible({ timeout: NAV_TIMEOUT });

    // 5 个核心配置标签
    for (const p of MAIN_PAGES) {
      await expect(nav.getByText(p.label).first()).toBeVisible();
    }
    // 更多配置下拉入口
    await expect(nav.getByText(/更多配置/)).toBeVisible();

    // 当前项高亮为魔方设置
    await expect(page.locator('.ant-segmented-item-selected')).toContainText('魔方设置');
    expect(errors.filter((e) => !e.includes('favicon')), `控制台错误: ${errors.join('; ')}`).toEqual([]);
  });

  test('切换 5 个核心配置页均可达且配置表单渲染', async ({ page }) => {
    for (const p of MAIN_PAGES) {
      await page.goto(p.path);
      await expect(page.locator('.cube-config-nav')).toBeVisible({ timeout: NAV_TIMEOUT });
      // 当前项高亮
      await expect(page.locator('.ant-segmented-item-selected')).toContainText(p.label);
      // 配置字段容器渲染（非 404）
      await expect(page.locator('.cube-config-fields').first()).toBeVisible({ timeout: NAV_TIMEOUT });
    }
  });

  test('更多配置下拉切换 短信/邮件/OAuth/访问规则', async ({ page }) => {
    for (const p of MORE_PAGES) {
      await page.goto(p.path);
      await expect(page.locator('.cube-config-nav')).toBeVisible({ timeout: NAV_TIMEOUT });
      // 更多配置按钮高亮
      await expect(page.locator('.cube-config-nav-more')).toHaveClass(/active/);
      // 面包屑显示 系统管理 / 配置名
      await expect(page.locator('.cube-shell-header-breadcrumb')).toContainText(p.label);
    }

    // 从魔方设置通过下拉切换到短信设置
    await page.goto('/Admin/Cube');
    await page.locator('.cube-config-nav-more').click();
    await page.getByRole('menuitem', { name: '短信设置' }).click();
    await expect(page).toHaveURL(/\/Admin\/SmsConfig/);
    await expect(page.locator('.cube-config-nav-more')).toHaveClass(/active/);
  });

  test('配置页每行一个配置项且右侧显示 Description', async ({ page }) => {
    await page.goto('/Admin/Cube');
    await page.waitForSelector('.cube-config-row', { timeout: NAV_TIMEOUT });

    // 单行布局：至少一个配置行
    const rows = await page.locator('.cube-config-row').count();
    expect(rows).toBeGreaterThan(0);

    // 右侧 Description：至少一行展示描述
    await expect(page.locator('.cube-config-row .cube-config-desc').first()).toBeVisible();

    // 结构：找到带描述的配置行，Form.Item（标签+控件）在前，Description 在后（同一行右侧）
    const rowWithDesc = page.locator('.cube-config-row', { has: page.locator('.cube-config-desc') }).first();
    await expect(rowWithDesc.locator('.ant-form-item').first()).toBeVisible();
    await expect(rowWithDesc.locator('.cube-config-desc').first()).toBeVisible();
  });
});
