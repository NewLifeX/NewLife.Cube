import type { Page } from '@playwright/test';

const apiBase = process.env.PLAYWRIGHT_API_URL || 'https://localhost:7116';

/** 列表元数据里的字段名与实际列头文案（列头由后端按界面语言本地化） */
export interface ListFieldHeader {
  name: string;
  header: string;
}

/**
 * 取列表元数据里各列的字段名与列头文案。
 *
 * 必须在页面内 fetch：后端按 `Accept-Language` 本地化，用 APIRequestContext 取到的语言
 * 与界面语言不一致时列头会对不上（本机浏览器为 en-US，API 默认 zh-CN）。
 */
export async function readListFields(page: Page, metaPath: string): Promise<ListFieldHeader[]> {
  const token = (await page.evaluate(() => localStorage.getItem('token'))) || '';

  const fields = await page.evaluate(
    async ([base, path, tk]) => {
      const res = await fetch(`${base}${path}`, { headers: { Authorization: `Bearer ${tk}` } });
      const body = await res.json();
      const list: Array<{ name?: string; header?: string; displayName?: string }> =
        body?.data?.list ?? [];

      return list.map((field) => ({
        name: field.name ?? '',
        header: (field.header?.trim() || field.displayName?.trim() || field.name || '').trim(),
      }));
    },
    [apiBase, metaPath, token] as const,
  );

  return fields.filter((field) => field.name.length > 0 && field.header.length > 0);
}

/** 取列表应当出现的列头文案 */
export async function readListHeaders(page: Page, metaPath: string): Promise<string[]> {
  return (await readListFields(page, metaPath)).map((field) => field.header);
}
