import { describe, expect, it } from 'vitest';
import type { MenuItem } from '@cube/api-core';
import {
  OBJECT_KIND_CACHE_KEY,
  clearObjectKindCache,
  fingerprintMenus,
  hydrateObjectKindCache,
  normalizeObjectTypeKey,
  persistObjectKindCache,
  readObjectKindCache,
  writeObjectKindCache,
} from './objectKindCache';

function menu(partial: Partial<MenuItem> & Pick<MenuItem, 'id' | 'name'>): MenuItem {
  return {
    displayName: partial.displayName || partial.name,
    parentID: partial.parentID ?? 0,
    url: partial.url ?? '',
    visible: partial.visible ?? true,
    children: partial.children ?? [],
    permissions: partial.permissions,
    ...partial,
  };
}

/** 内存版 Storage，供单测隔离 sessionStorage */
function memoryStorage(): Storage {
  const map = new Map<string, string>();
  return {
    get length() {
      return map.size;
    },
    clear() {
      map.clear();
    },
    getItem(key: string) {
      return map.has(key) ? map.get(key)! : null;
    },
    key(index: number) {
      return [...map.keys()][index] ?? null;
    },
    removeItem(key: string) {
      map.delete(key);
    },
    setItem(key: string, value: string) {
      map.set(key, String(value));
    },
  };
}

describe('fingerprintMenus', () => {
  it('同构菜单指纹稳定（顺序无关）', () => {
    const a = [
      menu({ id: 1, name: 'A', url: '/Admin/User' }),
      menu({ id: 2, name: 'B', url: '/Admin/Cube', displayName: '魔方' }),
    ];
    const b = [
      menu({ id: 2, name: 'B', url: '/Admin/Cube', displayName: '魔方' }),
      menu({ id: 1, name: 'A', url: '/Admin/User' }),
    ];
    expect(fingerprintMenus(a)).toBe(fingerprintMenus(b));
  });

  it('增删改 url / visible / 展示名会改变指纹', () => {
    const base = [menu({ id: 1, name: 'Cube', url: '/Admin/Cube', displayName: '魔方设置' })];
    const fp = fingerprintMenus(base);
    expect(fingerprintMenus([menu({ id: 1, name: 'Cube', url: '/Admin/Cube', displayName: '魔方' })])).not.toBe(
      fp,
    );
    expect(fingerprintMenus([menu({ id: 1, name: 'Cube', url: '/Admin/Cube', visible: false })])).not.toBe(fp);
    expect(
      fingerprintMenus([
        ...base,
        menu({ id: 2, name: 'Sys', url: '/Admin/Sys', displayName: '系统设置' }),
      ]),
    ).not.toBe(fp);
  });
});

describe('objectKindCache persistence', () => {
  it('同指纹 hydrate 命中；指纹变化清空内存', () => {
    const store = memoryStorage();
    const fp = 'fp-a';
    const memory = new Map<string, boolean>();
    memory.set('admin/cube', true);
    memory.set('admin/user', false);
    persistObjectKindCache(fp, memory, store);

    const next = new Map<string, boolean>();
    hydrateObjectKindCache(fp, next, store);
    expect(next.get('admin/cube')).toBe(true);
    expect(next.get('admin/user')).toBe(false);

    hydrateObjectKindCache('fp-b', next, store);
    expect(next.size).toBe(0);
  });

  it('读写规范化 type 键', () => {
    expect(normalizeObjectTypeKey('/Admin/Cube/')).toBe('admin/cube');
    const store = memoryStorage();
    writeObjectKindCache(
      { fingerprint: 'x', entries: { '/Admin/Star': true } },
      store,
    );
    const read = readObjectKindCache(store);
    expect(read?.entries['admin/star']).toBe(true);
    expect(store.getItem(OBJECT_KIND_CACHE_KEY)).toContain('fingerprint');
  });

  it('clearObjectKindCache 删除存储', () => {
    const store = memoryStorage();
    writeObjectKindCache({ fingerprint: 'z', entries: {} }, store);
    clearObjectKindCache(store);
    expect(readObjectKindCache(store)).toBeNull();
  });
});
