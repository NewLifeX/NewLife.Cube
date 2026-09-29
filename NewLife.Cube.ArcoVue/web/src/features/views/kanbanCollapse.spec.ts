import { describe, expect, it } from 'vitest';
import {
  kanbanCollapsedStorageKey,
  readKanbanCollapsed,
  toggleCollapsed,
  writeKanbanCollapsed,
} from './useKanbanBoard';

function memoryStorage(): Storage {
  const map = new Map<string, string>();
  return {
    get length() {
      return map.size;
    },
    clear() {
      map.clear();
    },
    getItem(key) {
      return map.has(key) ? map.get(key)! : null;
    },
    key(index) {
      return [...map.keys()][index] ?? null;
    },
    removeItem(key) {
      map.delete(key);
    },
    setItem(key, value) {
      map.set(key, value);
    },
  };
}

describe('toggleCollapsed（OSC-260926c2b8 看板列折叠集合）', () => {
  it('折叠后再切换回到展开', () => {
    const once = toggleCollapsed([], 'a');
    expect(once).toEqual(['a']);

    const twice = toggleCollapsed(once, 'a');
    expect(twice).toEqual([]);
  });

  it('多列折叠互不影响，逐个可回退', () => {
    let keys: string[] = [];
    keys = toggleCollapsed(keys, 'a');
    keys = toggleCollapsed(keys, 'b');
    expect(keys).toEqual(['a', 'b']);

    keys = toggleCollapsed(keys, 'a');
    expect(keys).toEqual(['b']);
  });

  it('刷新后仍读出已折叠列，全部展开则删掉记录', () => {
    const storage = memoryStorage();
    const key = kanbanCollapsedStorageKey('Admin/Department', 'view-1', 'Type');
    writeKanbanCollapsed(storage, key, ['2', '3']);
    expect(readKanbanCollapsed(storage, key)).toEqual(['2', '3']);
    expect(kanbanCollapsedStorageKey('Admin/Department', 'view-2', 'Type')).not.toBe(key);

    writeKanbanCollapsed(storage, key, []);
    expect(storage.getItem(key)).toBeNull();
    expect(readKanbanCollapsed(storage, key)).toEqual([]);
    expect(readKanbanCollapsed(storage, 'missing')).toEqual([]);
  });

  it('坏的折叠记录当作全部展开', () => {
    const storage = memoryStorage();
    storage.setItem('bad', '{');
    storage.setItem('obj', '{"a":1}');
    expect(readKanbanCollapsed(storage, 'bad')).toEqual([]);
    expect(readKanbanCollapsed(storage, 'obj')).toEqual([]);
  });

  it('不修改原数组（纯函数）', () => {
    const src = ['a'];
    const out = toggleCollapsed(src, 'b');

    expect(src).toEqual(['a']);
    expect(out).toEqual(['a', 'b']);
  });
});
