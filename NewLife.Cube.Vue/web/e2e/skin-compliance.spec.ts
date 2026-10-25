/**
 * Vue 皮肤列表页合规 E2E（SKIN-5）
 *
 * 分工：`presentation.spec.ts` 覆盖呈现架构链路、`parameter-section.spec.ts` 覆盖 Section 与 CRUD；
 * 本文件只验证列表页的皮肤级合规——数据行/操作列齐备、工具栏入口齐备、
 * 搜索无结果时显示空状态而不是白屏。
 *
 * 运行前需启动 CubeDemo（https://localhost:7116）与 Vue 开发服务器（默认 5187）：
 *   pnpm test:e2e --grep "皮肤列表页"
 */
import { test, expect } from '@playwright/test';

const testFieldPath = '/Test/TestField';

/** 通过真实接口播种一条测试字段记录（页面当前角色未开放新增按钮） */
async function seedRow(request: import('@playwright/test').APIRequestContext, name: string) {
  const apiBase = process.env.PLAYWRIGHT_API_URL || 'https://localhost:7116';
  const res = await request.post(`${apiBase}/api/Test/TestField`, {
    data: {
      ShortText: name,
      LongText: '',
      FileUrl: '',
      Avatar: '',
      JsonVal: '',
      HtmlVal: '',
      MarkdownVal: '',
      ColorVal: '',
      IconVal: '',
      MailVal: '',
      MobileVal: '',
      UrlVal: '',
      UserId: '',
      MultiVal: '',
      ListMVal: '',
    },
    ignoreHTTPSErrors: true,
  });
  expect(res.ok()).toBeTruthy();
  const body = await res.json();
  expect(body.code).toBe(0);

  return body?.data?.id ?? body?.data?.ID;
}

async function removeRow(request: import('@playwright/test').APIRequestContext, id: unknown) {
  const apiBase = process.env.PLAYWRIGHT_API_URL || 'https://localhost:7116';
  if (id) await request.delete(`${apiBase}/api/Test/TestField?ids=${id}`, { ignoreHTTPSErrors: true });
}

test.describe('皮肤列表页合规', () => {
  test('列表页：数据行、操作列与工具栏入口齐备', async ({ page, request }) => {
    const name = `E2E-SKIN-${Date.now()}`;
    const id = await seedRow(request, name);

    try {
      await page.goto(testFieldPath);
      await expect(page.locator('.el-table')).toBeVisible({ timeout: 20000 });
      await expect(page.getByText(name)).toBeVisible({ timeout: 20000 });
      // 断言不写死文案：操作列取最后一列，工具栏按钮按结构断言
      await expect(page.locator('.el-table__header th').last()).toBeVisible();
      expect(await page.locator('.cube-table button').count()).toBeGreaterThan(1);
    } finally {
      await removeRow(request, id);
    }
  });

  test('列表页：搜索无结果时显示空状态而非白屏，清空后回到列表', async ({ page, request }) => {
    const name = `E2E-SKIN-EMPTY-${Date.now()}`;
    const id = await seedRow(request, name);

    try {
      await page.goto(testFieldPath);
      await expect(page.locator('.el-table')).toBeVisible({ timeout: 20000 });

      // 搜索框占位符随界面语言变化，按结构取首个可见输入框
      const input = page.locator('.cube-table input:not([type="hidden"]):visible').first();
      await expect(input).toBeVisible();
      await input.fill(`__no_such_record_${Date.now()}`);
      await input.press('Enter');
      await expect(page.locator('.el-table__empty-text, .el-empty').first()).toBeVisible({ timeout: 20000 });

      await input.fill(name);
      await input.press('Enter');
      await expect(page.getByText(name)).toBeVisible({ timeout: 20000 });
    } finally {
      await removeRow(request, id);
    }
  });
});
