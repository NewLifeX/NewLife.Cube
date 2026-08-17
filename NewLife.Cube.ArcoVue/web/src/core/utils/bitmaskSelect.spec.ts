import { describe, expect, it } from 'vitest';
import { bitmaskToKeys, isBitmaskMultiSelect, keysToBitmask } from './bitmaskSelect';
import { buildStarTraceUrl } from './starTrace';

describe('bitmaskSelect', () => {
  const field = {
    typeName: 'Int32',
    itemType: 'multipleSelect',
    dataSource: { '1': '登录', '2': '注册', '4': '发码' },
  };

  it('detects bitmask multi', () => {
    expect(isBitmaskMultiSelect(field)).toBe(true);
    expect(isBitmaskMultiSelect({ ...field, typeName: 'String' })).toBe(false);
  });

  it('round-trips bits', () => {
    expect(bitmaskToKeys(3, ['1', '2', '4'])).toEqual(['1', '2']);
    expect(keysToBitmask(['1', '4'])).toBe(5);
    expect(keysToBitmask([])).toBe(0);
  });
});

describe('buildStarTraceUrl', () => {
  it('builds default trace path', () => {
    expect(buildStarTraceUrl('https://star.example.com', 'abc')).toBe(
      'https://star.example.com/trace?id=abc',
    );
  });

  it('supports {traceId} template', () => {
    expect(buildStarTraceUrl('https://s/trace?id={traceId}', 't1')).toBe(
      'https://s/trace?id=t1',
    );
  });

  it('returns null when missing', () => {
    expect(buildStarTraceUrl('', 't')).toBeNull();
    expect(buildStarTraceUrl('https://s', '')).toBeNull();
  });
});
