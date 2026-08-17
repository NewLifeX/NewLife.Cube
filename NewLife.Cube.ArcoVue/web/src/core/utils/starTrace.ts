/**
 * 星尘追踪 URL（对齐 NewLife.CubeNC StarHelper.BuildUrl）
 */
export function buildStarTraceUrl(
  starWeb: string | null | undefined,
  traceId: string | null | undefined,
): string | null {
  const id = (traceId ?? '').trim();
  if (!id) return null;
  let web = (starWeb ?? '').trim();
  if (!web) return null;
  if (web.includes('{traceId}')) return web.split('{traceId}').join(id);
  if (!web.endsWith('/')) web += '/';
  return `${web}trace?id=${encodeURIComponent(id)}`;
}
