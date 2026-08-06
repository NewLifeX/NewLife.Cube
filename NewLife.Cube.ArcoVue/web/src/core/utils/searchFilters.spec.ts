import { describe, expect, it } from 'vitest';
import {
  cleanSearchParams,
  collectSearchKeys,
  filterToSearchParams,
  matchesViewFilter,
  parseUrlSearch,
  resolveStatEntries,
} from './searchFilters';
import type { FieldMeta } from '@/core/types/field';

function field(name: string, extra: Partial<FieldMeta> = {}): FieldMeta {
  return { name, displayName: name, typeName: 'String', ...extra } as FieldMeta;
}

describe('collectSearchKeys', () => {
  it('collects single key for scalar fields and _min/_max for range fields', () => {
    const keys = collectSearchKeys([
      field('Name'),
      field('Age', { typeName: 'Int32' }),
      field('Birth', { typeName: 'DateTime', itemType: 'date' }),
      field('On', { typeName: 'Boolean' }),
    ]);
    expect(keys.has('Name')).toBe(true);
    expect(keys.has('Age_min')).toBe(true);
    expect(keys.has('Age_max')).toBe(true);
    expect(keys.has('Birth_min')).toBe(true);
    expect(keys.has('Birth_max')).toBe(true);
    expect(keys.has('On')).toBe(true);
    expect(keys.has('Age')).toBe(false);
  });
});

describe('cleanSearchParams', () => {
  it('drops unknown keys and empty values, keeps false/0/arrays', () => {
    const keys = new Set(['Name', 'Age', 'Enable', 'Tags']);
    const out = cleanSearchParams(
      {
        Name: 'a',
        Unknown: 'x',
        Age: 0,
        Enable: false,
        Empty: '',
        Null: null,
        Undef: undefined,
        Tags: ['1', '2'],
        NoTags: [],
      },
      keys,
    );
    expect(out).toEqual({ Name: 'a', Age: 0, Enable: false, Tags: ['1', '2'] });
  });
});

describe('parseUrlSearch', () => {
  it('keeps legal keys, stringifies scalars, keeps arrays, drops empties', () => {
    const keys = new Set(['Name', 'Status', 'Enable']);
    const out = parseUrlSearch(
      {
        Name: '张三',
        Status: ['1', '2'],
        Enable: '',
        Other: 'ignored',
      },
      keys,
    );
    expect(out).toEqual({ Name: '张三', Status: ['1', '2'] });
  });
});

describe('resolveStatEntries', () => {
  it('returns entries with non-null values only', () => {
    expect(resolveStatEntries({ Total: 10, Null: null, Empty: undefined, Zero: 0 })).toEqual([
      { key: 'Total', value: '10' },
      { key: 'Zero', value: '0' },
    ]);
  });

  it('returns empty array when stat is null', () => {
    expect(resolveStatEntries(null)).toEqual([]);
  });
});

describe('filterToSearchParams (OSC-0015)', () => {
  const keys = new Set(['Status', 'Name', 'Age_min', 'Age_max']);

  it('empty filter yields no params', () => {
    expect(filterToSearchParams(null, [], keys)).toEqual({ params: {}, clientOnly: false });
    expect(filterToSearchParams({ logic: 'all', conditions: [] }, [], keys)).toEqual({
      params: {},
      clientOnly: false,
    });
  });

  it('eq emits field=value and array joins by comma', () => {
    const r = filterToSearchParams(
      {
        logic: 'all',
        conditions: [
          { field: 'Status', op: 'eq', value: 1 },
          { field: 'Role', op: 'eq', value: ['a', 'b'] },
        ],
      },
      [],
      new Set(['Status', 'Role']),
    );
    expect(r.params).toEqual({ Status: 1, Role: 'a,b' });
    expect(r.clientOnly).toBe(false);
  });

  it('between emits _min/_max and supports one-sided', () => {
    const r = filterToSearchParams(
      {
        logic: 'all',
        conditions: [{ field: 'Age', op: 'between', value: 18, value2: 60 }],
      },
      [],
      keys,
    );
    expect(r.params).toEqual({ Age_min: 18, Age_max: 60 });
    const one = filterToSearchParams(
      { logic: 'all', conditions: [{ field: 'Age', op: 'between', value: 18 }] },
      [],
      keys,
    );
    expect(one.params).toEqual({ Age_min: 18 });
  });

  it('unknown fields are stripped by keys and empty values dropped', () => {
    const r = filterToSearchParams(
      {
        logic: 'all',
        conditions: [
          { field: 'Unknown', op: 'eq', value: 1 },
          { field: 'Name', op: 'eq', value: '' },
        ],
      },
      [],
      keys,
    );
    expect(r.params).toEqual({});
  });

  it('logic=any with >1 conditions flags clientOnly', () => {
    const r = filterToSearchParams(
      {
        logic: 'any',
        conditions: [
          { field: 'Status', op: 'eq', value: 1 },
          { field: 'Name', op: 'eq', value: 'x' },
        ],
      },
      [],
      keys,
    );
    expect(r.params).toEqual({ Status: 1, Name: 'x' });
    expect(r.clientOnly).toBe(true);
  });
});

describe('matchesViewFilter (OSC-0015)', () => {
  it('any logic passes when at least one condition matches', () => {
    const f = {
      logic: 'any' as const,
      conditions: [
        { field: 'Status', op: 'eq' as const, value: 1 },
        { field: 'Name', op: 'eq' as const, value: 'x' },
      ],
    };
    expect(matchesViewFilter({ status: 2, name: 'x' }, f, [])).toBe(true);
    expect(matchesViewFilter({ status: 2, name: 'y' }, f, [])).toBe(false);
  });

  it('all logic requires every condition', () => {
    const f = {
      logic: 'all' as const,
      conditions: [
        { field: 'Status', op: 'eq' as const, value: 1 },
        { field: 'Name', op: 'eq' as const, value: 'x' },
      ],
    };
    expect(matchesViewFilter({ status: 1, name: 'x' }, f, [])).toBe(true);
    expect(matchesViewFilter({ status: 1, name: 'y' }, f, [])).toBe(false);
  });

  it('between matches numeric and date-string ranges', () => {
    const f = {
      logic: 'all' as const,
      conditions: [{ field: 'Age', op: 'between' as const, value: 18, value2: 60 }],
    };
    expect(matchesViewFilter({ age: 20 }, f, [])).toBe(true);
    expect(matchesViewFilter({ age: 10 }, f, [])).toBe(false);
    const df = {
      logic: 'all' as const,
      conditions: [{ field: 'CreateTime', op: 'between' as const, value: '2026-01-01', value2: '2026-12-31' }],
    };
    expect(matchesViewFilter({ createTime: '2026-06-01' }, df, [])).toBe(true);
    expect(matchesViewFilter({ createTime: '2025-06-01' }, df, [])).toBe(false);
  });

  it('empty filter always matches', () => {
    expect(matchesViewFilter({}, { logic: 'all', conditions: [] }, [])).toBe(true);
  });

  it('枚举下拉字符串值匹配数字行值（GetList camelCase 行 + 值集 select 传值）', () => {
    // 后端 Department 返回 type=1（数字），筛选构建器值控件 select 提交字符串 '1'
    const f = {
      logic: 'all' as const,
      conditions: [{ field: 'Type', op: 'eq' as const, value: '1' }],
    };
    expect(matchesViewFilter({ type: 1, name: '公司' }, f, [])).toBe(true);
    expect(matchesViewFilter({ type: 2, name: '部门' }, f, [])).toBe(false);
    // PascalCase 行也能匹配（容错）
    expect(matchesViewFilter({ Type: 1, Name: '公司' }, f, [])).toBe(true);
  });
});
