/**
 * Object 页探测缓存（方案 A）：sessionStorage 跨 F5，菜单指纹变化自动失效。
 *
 * 指纹覆盖所有带 url 的菜单节点（id / url / visible / 展示名），
 * 新注册或增删改菜单后指纹不同 → 丢弃旧缓存并重新探测。
 */
import type { MenuItem } from '@cube/api-core';

/** sessionStorage 键；升版改名以丢弃不兼容结构 */
export const OBJECT_KIND_CACHE_KEY = 'cube.objectKindCache.v1';

export interface ObjectKindCacheState {
  /** 菜单指纹；不一致则整表失效 */
  fingerprint: string;
  /** type 路径（小写）→ 是否为 Object 配置页 */
  entries: Record<string, boolean>;
}

/** 规范化 type 路径作缓存键 */
export function normalizeObjectTypeKey(type: string): string {
  return type.replace(/^\/+/, '').replace(/\/+$/, '').toLowerCase();
}

/**
 * 菜单指纹：所有带 url 节点的 id|url|visible|displayName，排序后拼接。
 * 任意增删改（含可见性、改名、改 URL）都会改变指纹。
 */
export function fingerprintMenus(menus: MenuItem[] | null | undefined): string {
  const lines: string[] = [];
  const walk = (items: MenuItem[]) => {
    for (const m of items ?? []) {
      const url = (m.url ?? '').trim();
      if (url) {
        lines.push(
          [
            String(m.id ?? ''),
            url.toLowerCase(),
            m.visible === false ? '0' : '1',
            (m.displayName || m.name || '').trim(),
          ].join('\t'),
        );
      }
      if (m.children?.length) walk(m.children);
    }
  };
  walk(menus ?? []);
  lines.sort();
  return lines.join('\n');
}

function safeStorage(storage?: Storage): Storage | null {
  if (storage) return storage;
  try {
    if (typeof sessionStorage === 'undefined') return null;
    return sessionStorage;
  } catch {
    return null;
  }
}

/** 读取持久化缓存；损坏或非对象时返回 null */
export function readObjectKindCache(storage?: Storage): ObjectKindCacheState | null {
  const store = safeStorage(storage);
  if (!store) return null;
  try {
    const raw = store.getItem(OBJECT_KIND_CACHE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<ObjectKindCacheState>;
    if (typeof parsed.fingerprint !== 'string' || !parsed.entries || typeof parsed.entries !== 'object') {
      return null;
    }
    const entries: Record<string, boolean> = {};
    for (const [k, v] of Object.entries(parsed.entries)) {
      if (typeof v === 'boolean') entries[normalizeObjectTypeKey(k)] = v;
    }
    return { fingerprint: parsed.fingerprint, entries };
  } catch {
    return null;
  }
}

/** 写入持久化缓存 */
export function writeObjectKindCache(state: ObjectKindCacheState, storage?: Storage): void {
  const store = safeStorage(storage);
  if (!store) return;
  try {
    store.setItem(OBJECT_KIND_CACHE_KEY, JSON.stringify(state));
  } catch {
    /* quota / 隐私模式：忽略，退化为仅内存 */
  }
}

/** 清除持久化缓存 */
export function clearObjectKindCache(storage?: Storage): void {
  const store = safeStorage(storage);
  if (!store) return;
  try {
    store.removeItem(OBJECT_KIND_CACHE_KEY);
  } catch {
    /* ignore */
  }
}

/**
 * 按当前菜单指纹同步内存 Map：
 * - 指纹与 session 一致 → 灌入 entries
 * - 否则清空 Map（旧探测结果作废）
 */
export function hydrateObjectKindCache(
  fingerprint: string,
  memory: Map<string, boolean>,
  storage?: Storage,
): void {
  memory.clear();
  const stored = readObjectKindCache(storage);
  if (!stored || stored.fingerprint !== fingerprint) return;
  for (const [k, v] of Object.entries(stored.entries)) {
    memory.set(normalizeObjectTypeKey(k), v);
  }
}

/** 将内存 Map 按指纹写回 sessionStorage */
export function persistObjectKindCache(
  fingerprint: string,
  memory: Map<string, boolean>,
  storage?: Storage,
): void {
  const entries: Record<string, boolean> = {};
  memory.forEach((v, k) => {
    entries[normalizeObjectTypeKey(k)] = v;
  });
  writeObjectKindCache({ fingerprint, entries }, storage);
}
