/**
 * GetPage 会话级短缓存：DynamicPage 探测与 DefaultList.loadFields 共用，
 * 避免同 typePath 在首屏连续打两次 GetPage。
 */

const TTL_MS = 30_000;
const cache = new Map<string, { at: number; data: unknown }>();

function keyOf(typePath: string): string {
  return typePath.replace(/^\/+/, '').replace(/\/+$/, '');
}

/** 读取未过期缓存；过期或不存在返回 undefined */
export function peekPageMeta(typePath: string): unknown | undefined {
  const k = keyOf(typePath);
  const hit = cache.get(k);
  if (!hit) return undefined;
  if (Date.now() - hit.at > TTL_MS) {
    cache.delete(k);
    return undefined;
  }
  return hit.data;
}

/** 写入/覆盖缓存 */
export function setPageMeta(typePath: string, data: unknown): void {
  cache.set(keyOf(typePath), { at: Date.now(), data });
}

/** 带缓存的 GetPage：命中则直接返回，否则调用 fetcher 并缓存 */
export async function getPageCached(
  typePath: string,
  fetcher: () => Promise<unknown>,
): Promise<unknown> {
  const hit = peekPageMeta(typePath);
  if (hit !== undefined) return hit;
  const data = await fetcher();
  setPageMeta(typePath, data);
  return data;
}

/** 登出或切换租户时清空 */
export function clearPageMetaCache(): void {
  cache.clear();
}
