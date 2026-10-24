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

const basePath = '/Test/TestField';

test.describe('字段类型全覆盖', () => {
  test('列表按字段类型渲染列头', async ({ page }) => {
    await page.goto(basePath);
    await expect(page.locator('.el-table')).toBeVisible({ timeout: 20000 });

    // 文本 / 数值 / 布尔 / 枚举 / 附件 / 颜色 / 图标 / 邮箱 / 手机 / 链接 / 单选多选 / 操作列
    for (const name of ['短文本', '长文本', '金额', 'Enabled', 'Kind', 'Attachment', 'Avatar', '颜色', 'Icon', '邮箱', 'Mobile', 'URL', '单选', '多选', '操作']) {
      await expect(page.getByRole('columnheader', { name, exact: true })).toBeVisible();
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
