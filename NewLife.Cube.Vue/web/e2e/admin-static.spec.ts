import { test, expect } from '@playwright/test';

/**
 * 系统管理区域的静态页回归：文件管理与数据库管理无法用 `/meta` 表达（目录浏览、二进制下载、
 * 备份动作），由 `apps/cube-admin/src/views/admin/{file,db}/index.vue` 整页接管。
 *
 * 文件管理受 `CubeSetting.EnableFileManager` 开关控制（默认关闭，关闭时后端所有文件接口返回 403），
 * 用例内临时开启并在结束时恢复，避免污染环境配置。断言只用结构选择器，不绑定界面语言。
 */
const apiBase = process.env.PLAYWRIGHT_API_URL || 'https://localhost:7116';

async function setFileManager(page: import('@playwright/test').Page, enabled: boolean) {
  const res = await page.request.get(`${apiBase}/api/Admin/Cube`, { ignoreHTTPSErrors: true });
  const body = await res.json();
  const model = body?.data ?? {};
  model.enableFileManager = enabled;

  const put = await page.request.put(`${apiBase}/api/Admin/Cube`, {
    data: model,
    ignoreHTTPSErrors: true,
  });
  expect(put.ok()).toBeTruthy();
}

// 全局配置开关不能并发切换，否则另一条用例读到中间态
test.describe.configure({ mode: 'serial' });

test.describe('系统管理静态页', () => {
  test('文件管理：开关控制目录列表与行内动作', async ({ page }) => {
    await setFileManager(page, true);
    try {
      await page.goto('/Admin/File');
      await expect(page.locator('h1')).toBeVisible({ timeout: 20000 });

      const row = page.locator('.el-table__body tr').first();
      await expect(row).toBeVisible({ timeout: 20000 });

      // 行内动作是 MVC 文件管理器的核心能力：下载/压缩/解压/复制/删除，少一个都算功能缺失
      expect(await row.locator('button').count()).toBeGreaterThanOrEqual(5);
    } finally {
      await setFileManager(page, false);
    }

    // 关闭后接口返回 403，页面给出开启提示且不再列出目录
    await page.goto('/Admin/File');
    await expect(page.locator('h1')).toBeVisible({ timeout: 20000 });
    await expect(page.getByText(/未启用|disabled|not enabled/i).first()).toBeVisible({ timeout: 20000 });
    expect(await page.locator('.el-table__body tr').count()).toBe(0);
  });

  test('数据库管理：渲染备份表单且数据库清单接口可用', async ({ page }) => {
    await page.goto('/Admin/Db');
    await expect(page.locator('h1')).toBeVisible({ timeout: 20000 });
    await expect(page.locator('.db-operations input').first()).toBeVisible({ timeout: 15000 });

    // 三个动作按钮齐备（文案随界面语言变化，按数量断言）
    expect(await page.locator('.db-operations button').count()).toBeGreaterThanOrEqual(3);

    // 数据库清单接口可用（存在且需登录态），页面据此展示可备份的数据库
    const res = await page.request.get(`${apiBase}/api/Admin/Db`, { ignoreHTTPSErrors: true });
    expect(res.ok()).toBeTruthy();
    expect((await res.json()).code).toBe(0);
  });
});
