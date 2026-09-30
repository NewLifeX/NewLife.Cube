import { describe, expect, it } from 'vitest';
import { missingLovValues } from './useListQuery';

describe('missingLovValues', () => {
  it('dataSource 已有旧值时，新值仍要请求', () => {
    expect(missingLovValues(['1', '2', '1'], { '2': '缓存' }, { '1': '已有' })).toEqual([]);
    expect(missingLovValues(['1', '3'], {}, { '1': '已有' })).toEqual(['3']);
  });
});