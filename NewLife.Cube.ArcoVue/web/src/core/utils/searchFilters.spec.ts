import { describe, expect, it } from 'vitest';
import {
  cleanSearchParams,
  collectSearchKeys,
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
