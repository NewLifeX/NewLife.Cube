/** 浏览器标题：`页面名 / 显示名称`；缺页名时仅显示名称 */
export function formatDocumentTitle(pageTitle: string | null | undefined, displayName: string | null | undefined): string {
  const page = (pageTitle ?? '').trim();
  const sys = (displayName ?? '').trim() || '魔方管理平台';
  return page ? `${page} / ${sys}` : sys;
}
