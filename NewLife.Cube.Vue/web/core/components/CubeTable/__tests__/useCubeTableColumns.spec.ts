/**
 * `useCubeTableColumns` 单测：列显隐的读取、持久化与「新增列默认显示」
 *
 * 存储用内存实现注入（不碰 jsdom 的 localStorage），
 * 断言的是用户能感知的三件事：勾掉了就没了、刷新后还是勾掉的、新加的列默认是显示。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/useCubeTableColumns.spec.ts
 */
import { describe, expect, it } from 'vitest';
import { ref } from 'vue';
import { useCubeTableColumns } from '../useCubeTableColumns';
import { FIELDS } from './fakeCtx';
import type { FieldMeta } from '../../../types/field';

/** 内存存储桩 */
function memoryStorage(initial: Record<string, string> = {}) {
  const map = new Map<string, string>(Object.entries(initial));
  return {
    calls: [] as string[],
    getItem(key: string) {
      this.calls.push(`get:${key}`);
      return map.get(key) ?? null;
    },
    setItem(key: string, value: string) {
      this.calls.push(`set:${key}`);
      map.set(key, value);
    },
    dump: map,
  };
}

function makeFields(...names: Array<keyof typeof FIELDS>): FieldMeta[] {
  return names.map((n) => FIELDS[n]);
}

describe('useCubeTableColumns', () => {
  it('应该默认全显示 当没有历史配置时', () => {
    const storage = memoryStorage();
    const columns = useCubeTableColumns(ref(makeFields('id', 'name')), {
      prefix: () => '/api/Equipments/Equipment',
      enabled: true,
      storage,
    });
    expect(columns.visibility.value).toEqual({ Id: true, Name: true });
    expect(columns.items.value).toEqual([
      { key: 'Id', label: '编号' },
      { key: 'Name', label: '名称' },
    ]);
  });

  it('应该按实体前缀分桶 当未指定存储键时', () => {
    const storage = memoryStorage();
    const columns = useCubeTableColumns(ref(makeFields('name')), {
      prefix: () => '/api/Molds/Mold',
      enabled: true,
      storage,
    });
    expect(columns.storageKey.value).toBe('cube-table:columns:/api/Molds/Mold');
    expect(storage.calls[0]).toBe('get:cube-table:columns:/api/Molds/Mold');
  });

  it('应该沿用页面给的历史键 当业务页迁移旧配置时', () => {
    const storage = memoryStorage({ 'legacy:equipment:columns': '{"Name":false}' });
    const columns = useCubeTableColumns(ref(makeFields('name', 'count')), {
      prefix: () => '/api/Equipments/Equipment',
      enabled: true,
      storageKey: 'legacy:equipment:columns',
      storage,
    });
    expect(columns.visibility.value).toEqual({ Name: false, Count: true });
  });

  it('应该记住用户的选择 当取消勾选某列时', () => {
    const storage = memoryStorage();
    const columns = useCubeTableColumns(ref(makeFields('name', 'count')), {
      prefix: () => '/api/X/X',
      enabled: true,
      storage,
    });
    columns.setVisible('Count', false);
    expect(columns.visibility.value.Count).toBe(false);
    expect(JSON.parse(storage.dump.get('cube-table:columns:/api/X/X') ?? '{}')).toEqual({
      Name: true,
      Count: false,
    });
  });

  it('应该让新出现的列默认显示 当元数据加了字段而历史配置里没有它时', () => {
    // 否则升级后「某一列凭空消失」，用户只能靠恢复默认找回
    const storage = memoryStorage({ '/k': '{"Name":false}' });
    const fields = ref(makeFields('name'));
    const columns = useCubeTableColumns(fields, {
      prefix: () => '/api/X/X',
      enabled: true,
      storageKey: '/k',
      storage,
    });
    expect(columns.visibility.value).toEqual({ Name: false });

    fields.value = makeFields('name', 'count');
    expect(columns.visibility.value).toEqual({ Name: false, Count: true });
  });

  it('应该恢复全部列并落盘 当用户点恢复默认时', () => {
    const storage = memoryStorage({ '/k': '{"Name":false}' });
    const columns = useCubeTableColumns(ref(makeFields('name')), {
      prefix: () => '/api/X/X',
      enabled: true,
      storageKey: '/k',
      storage,
    });
    expect(columns.visibility.value.Name).toBe(false);
    columns.reset();
    expect(columns.visibility.value.Name).toBe(true);
    expect(storage.dump.get('/k')).toBe('{"Name":true}');
  });

  it('应该按历史列序返回列 当用户调整过列序时', () => {
    const storage = memoryStorage({
      '/k': '{"Name":true,"Count":true}',
      '/k:order': '["Count","Name"]',
    });
    const columns = useCubeTableColumns(ref(makeFields('name', 'count')), {
      prefix: () => '/api/X/X',
      enabled: true,
      storageKey: '/k',
      storage,
    });

    expect(columns.items.value.map((item) => item.key)).toEqual(['Count', 'Name']);
    expect(columns.order.value).toEqual(['Count', 'Name']);
  });

  it('应该保存列序 当用户上移/下移列时', () => {
    const storage = memoryStorage();
    const columns = useCubeTableColumns(ref(makeFields('name', 'count')), {
      prefix: () => '/api/X/X',
      enabled: true,
      storage,
    });

    columns.setOrder(['Count', 'Name']);

    expect(columns.items.value.map((item) => item.key)).toEqual(['Count', 'Name']);
    expect(JSON.parse(storage.dump.get('cube-table:columns:/api/X/X:order') ?? '[]')).toEqual([
      'Count',
      'Name',
    ]);
  });

  it('应该把新列追加到末尾 当列序配置里还没有该列时', () => {
    const storage = memoryStorage({ '/k:order': '["Name"]' });
    const fields = ref(makeFields('name'));
    const columns = useCubeTableColumns(fields, {
      prefix: () => '/api/X/X',
      enabled: true,
      storageKey: '/k',
      storage,
    });

    fields.value = makeFields('name', 'count');

    expect(columns.items.value.map((item) => item.key)).toEqual(['Name', 'Count']);
  });

  it('应该恢复默认列序并落盘 当用户点恢复默认时', () => {
    const storage = memoryStorage({ '/k:order': '["Count","Name"]' });
    const columns = useCubeTableColumns(ref(makeFields('name', 'count')), {
      prefix: () => '/api/X/X',
      enabled: true,
      storageKey: '/k',
      storage,
    });

    columns.reset();

    expect(columns.items.value.map((item) => item.key)).toEqual(['Name', 'Count']);
    expect(JSON.parse(storage.dump.get('/k:order') ?? '[]')).toEqual(['Name', 'Count']);
  });

  it('应该容忍坏数据与存储不可用 当 localStorage 里是垃圾或禁用时', () => {
    const broken = memoryStorage({ '/k': 'not-json{' });
    const columns = useCubeTableColumns(ref(makeFields('name')), {
      prefix: () => '/api/X/X',
      enabled: true,
      storageKey: '/k',
      storage: broken,
    });
    expect(columns.visibility.value).toEqual({ Name: true });

    const denied = {
      getItem() {
        throw new Error('disabled');
      },
      setItem() {
        throw new Error('disabled');
      },
    };
    const second = useCubeTableColumns(ref(makeFields('name')), {
      prefix: () => '/api/X/X',
      enabled: true,
      storageKey: '/k',
      storage: denied,
    });
    // 存储不可用时本次会话仍然可用（默认全显示），只是记不住
    expect(second.visibility.value).toEqual({ Name: true });
    expect(() => second.setVisible('Name', false)).not.toThrow();
  });
});
