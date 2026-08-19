import { describe, expect, it } from 'vitest';
import {
  buildViewFilterParam,
  cleanSearchParams,
  collectSearchKeys,
  matchesViewFilter,
  parseUrlSearch,
  resolveStatEntries,
} from './searchFilters';
import type { FieldMeta } from '@/core/types/field';
import type { ViewFilter } from './viewProfile';

function field(name: string, extra: Partial<FieldMeta> = {}): FieldMeta {
  return { name, displayName: name, typeName: 'String', ...extra } as FieldMeta;
}

describe('buildViewFilterParam (OSC-260819e483 P2)', () => {
  it('empty filter → undefined (不传键，服务端与今日一致)', () => {
    expect(buildViewFilterParam(null)).toBeUndefined();
    expect(buildViewFilterParam(undefined)).toBeUndefined();
    expect(buildViewFilterParam({ logic: 'all', conditions: [] })).toBeUndefined();
  });

  it('non-empty filter → JSON string with logic=all/any, conditions camelCase', () => {
    const s = buildViewFilterParam({
      logic: 'any',
      conditions: [
        { field: 'Name', op: 'contains', value: '公司' },
        { field: 'Enable', op: 'eq', value: true },
      ],
    });
    expect(s).toBe(
      '{"logic":"any","conditions":[{"field":"Name","op":"contains","value":"公司"},{"field":"Enable","op":"eq","value":true}]}',
    );
  });

  it('round-trip: 后端 ParseViewFilter 能按同构结构消费', () => {
    const filter: ViewFilter = { logic: 'all', conditions: [{ field: 'Sort', op: 'gt', value: 1 }] };
    const s = buildViewFilterParam(filter)!;
    const parsed = JSON.parse(s);
    expect(parsed.logic).toBe('all');
    expect(parsed.conditions[0]).toEqual({ field: 'Sort', op: 'gt', value: 1 });
  });
});

describe('collectSearchKeys', () => {
  it('collects field keys plus reserved Q/dtStart/dtEnd, no more _min/_max (OSC-0016)', () => {
    const keys = collectSearchKeys([
      field('Name'),
      field('Age', { typeName: 'Int32' }),
      field('Birth', { typeName: 'DateTime', itemType: 'date' }),
      field('On', { typeName: 'Boolean' }),
    ]);
    expect(keys.has('Name')).toBe(true);
    expect(keys.has('Age')).toBe(true);
    expect(keys.has('Birth')).toBe(true);
    expect(keys.has('On')).toBe(true);
    // 不再产生 _min/_max 假范围键
    expect(keys.has('Age_min')).toBe(false);
    expect(keys.has('Age_max')).toBe(false);
    expect(keys.has('Birth_min')).toBe(false);
    expect(keys.has('Birth_max')).toBe(false);
    // 保留键（OSC-0016）
    expect(keys.has('Q')).toBe(true);
    expect(keys.has('dtStart')).toBe(true);
    expect(keys.has('dtEnd')).toBe(true);
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

  it('gt/lte 数字与 after/before 日期比较（替代旧 between）', () => {
    const f = {
      logic: 'all' as const,
      conditions: [{ field: 'Age', op: 'gte' as const, value: 18 }],
    };
    expect(matchesViewFilter({ age: 20 }, f, [])).toBe(true);
    expect(matchesViewFilter({ age: 10 }, f, [])).toBe(false);
    const df = {
      logic: 'all' as const,
      conditions: [{ field: 'CreateTime', op: 'after' as const, value: '2026-01-01' }],
    };
    expect(matchesViewFilter({ createTime: '2026-06-01' }, df, [])).toBe(true);
    expect(matchesViewFilter({ createTime: '2025-06-01' }, df, [])).toBe(false);
  });

  it('gte/lte 空值行不命中（空值返回 na，不得纳入 >=/<=，与 isNull 语义区分）', () => {
    const gte = {
      logic: 'all' as const,
      conditions: [{ field: 'Age', op: 'gte' as const, value: 18 }],
    };
    expect(matchesViewFilter({ age: 20 }, gte, [])).toBe(true);
    expect(matchesViewFilter({ age: 10 }, gte, [])).toBe(false);
    expect(matchesViewFilter({ age: null }, gte, [])).toBe(false);
    expect(matchesViewFilter({ age: '' }, gte, [])).toBe(false);
    expect(matchesViewFilter({}, gte, [])).toBe(false);
    const lte = {
      logic: 'all' as const,
      conditions: [{ field: 'Age', op: 'lte' as const, value: 18 }],
    };
    expect(matchesViewFilter({ age: 10 }, lte, [])).toBe(true);
    expect(matchesViewFilter({ age: 20 }, lte, [])).toBe(false);
    expect(matchesViewFilter({ age: null }, lte, [])).toBe(false);
    expect(matchesViewFilter({ age: '' }, lte, [])).toBe(false);
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

  it('neq 反义匹配', () => {
    const f = { logic: 'all' as const, conditions: [{ field: 'Type', op: 'neq' as const, value: '1' }] };
    expect(matchesViewFilter({ type: 1 }, f, [])).toBe(false);
    expect(matchesViewFilter({ type: 2 }, f, [])).toBe(true);
  });

  it('contains / notContains 字符包含', () => {
    const f = { logic: 'all' as const, conditions: [{ field: 'Name', op: 'contains' as const, value: '公司' }] };
    expect(matchesViewFilter({ name: '上海分公司' }, f, [])).toBe(true);
    expect(matchesViewFilter({ name: '行政部' }, f, [])).toBe(false);
    const g = { logic: 'all' as const, conditions: [{ field: 'Name', op: 'notContains' as const, value: '公司' }] };
    expect(matchesViewFilter({ name: '行政部' }, g, [])).toBe(true);
    expect(matchesViewFilter({ name: '上海分公司' }, g, [])).toBe(false);
  });

  it('isNull / notNull 空值判定', () => {
    const nullF = { logic: 'all' as const, conditions: [{ field: 'Manager', op: 'isNull' as const }] };
    expect(matchesViewFilter({ manager: null }, nullF, [])).toBe(true);
    expect(matchesViewFilter({ manager: '' }, nullF, [])).toBe(true);
    expect(matchesViewFilter({ manager: 5 }, nullF, [])).toBe(false);
    const notNullF = { logic: 'all' as const, conditions: [{ field: 'Manager', op: 'notNull' as const }] };
    expect(matchesViewFilter({ manager: 5 }, notNullF, [])).toBe(true);
    expect(matchesViewFilter({ manager: null }, notNullF, [])).toBe(false);
  });

  it('gt/gte/lt/lte 数字比较（不含范围）', () => {
    const gt = { logic: 'all' as const, conditions: [{ field: 'Age', op: 'gt' as const, value: 18 }] };
    expect(matchesViewFilter({ age: 20 }, gt, [])).toBe(true);
    expect(matchesViewFilter({ age: 18 }, gt, [])).toBe(false);
    const gte = { logic: 'all' as const, conditions: [{ field: 'Age', op: 'gte' as const, value: 18 }] };
    expect(matchesViewFilter({ age: 18 }, gte, [])).toBe(true);
    const lt = { logic: 'all' as const, conditions: [{ field: 'Age', op: 'lt' as const, value: 18 }] };
    expect(matchesViewFilter({ age: 10 }, lt, [])).toBe(true);
    expect(matchesViewFilter({ age: 18 }, lt, [])).toBe(false);
    const lte = { logic: 'all' as const, conditions: [{ field: 'Age', op: 'lte' as const, value: 18 }] };
    expect(matchesViewFilter({ age: 18 }, lte, [])).toBe(true);
  });

  it('after / before 日期字符串字典序比较', () => {
    const after = { logic: 'all' as const, conditions: [{ field: 'CreateTime', op: 'after' as const, value: '2026-01-01' }] };
    expect(matchesViewFilter({ createTime: '2026-06-01' }, after, [])).toBe(true);
    expect(matchesViewFilter({ createTime: '2025-12-31' }, after, [])).toBe(false);
    const before = { logic: 'all' as const, conditions: [{ field: 'CreateTime', op: 'before' as const, value: '2026-01-01' }] };
    expect(matchesViewFilter({ createTime: '2025-12-31' }, before, [])).toBe(true);
    expect(matchesViewFilter({ createTime: '2026-06-01' }, before, [])).toBe(false);
  });
});
