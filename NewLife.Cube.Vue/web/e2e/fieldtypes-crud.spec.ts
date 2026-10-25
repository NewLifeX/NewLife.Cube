/**
 * 字段类型全覆盖 E2E（TestField）
 *
 * 验证 `/Test/TestField` 列表把各种数据类型的字段都渲染成列（ItemType/TypeName 驱动的列头），
 * 并通过真实接口播种记录确认数据可落到列表。
 *
 * 说明：当前角色在测试页没有新增按钮，表单控件的按类型渲染已由单测
 * `core/views/components/FormContent.fieldtypes.spec.ts` 与 `fieldControl.spec.ts` 覆盖。
 *
 * 运行：pnpm test:e2e --grep "字段类型全覆盖"
 */
import { test, expect } from '@playwright/test';
import { readListHeaders } from './metaHelper';

const basePath = '/Test/TestField';

test.describe('字段类型全覆盖', () => {
  test('列表按字段类型渲染列头', async ({ page }) => {
    await page.goto(basePath);
    await expect(page.locator('.el-table__header th').nth(2)).toBeVisible({ timeout: 20000 });

    // 列头文案来自后端元数据（按界面语言本地化），先取 /meta 再逐列比对，避免写死中文/英文
    const expected = await readListHeaders(page, '/api/Test/TestField/meta');
    expect(expected.length).toBeGreaterThan(10);

    const headers = (await page.locator('.el-table__header th').allInnerTexts()).join('|');
    for (const name of expected) {
      expect(headers).toContain(name);
    }
  });

  test('播种的记录出现在列表中', async ({ page, request }) => {
    const apiBase = process.env.PLAYWRIGHT_API_URL || 'https://localhost:7116';
    const name = `E2E-TYPE-${Date.now()}`;

    const created = await request.post(`${apiBase}/api/Test/TestField`, {
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
    expect(created.ok()).toBeTruthy();
    const body = await created.json();
    expect(body.code).toBe(0);
    const id = body?.data?.id ?? body?.data?.ID;

    try {
      await page.goto(basePath);
      await expect(page.locator('.el-table')).toBeVisible({ timeout: 20000 });
      await expect(page.getByText(name)).toBeVisible({ timeout: 20000 });
    } finally {
      if (id) await request.delete(`${apiBase}/api/Test/TestField?ids=${id}`, { ignoreHTTPSErrors: true });
    }
  });
});
