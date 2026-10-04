import { describe, expect, it } from 'vitest';
import { resolveListRefreshKind } from './listRefreshGate';
import { listRequestSignature } from './listRequestSignature';

describe('resolveListRefreshKind', () => {
  it('mount / typePath → bootstrap；其余 → loadData', () => {
    expect(resolveListRefreshKind('mount')).toBe('bootstrap');
    expect(resolveListRefreshKind('typePath')).toBe('bootstrap');
    expect(resolveListRefreshKind('toolbar')).toBe('loadData');
    expect(resolveListRefreshKind('page')).toBe('loadData');
    expect(resolveListRefreshKind('search')).toBe('loadData');
    expect(resolveListRefreshKind('filter')).toBe('loadData');
    expect(resolveListRefreshKind('sort')).toBe('loadData');
    expect(resolveListRefreshKind('viewSwitch')).toBe('loadData');
  });
});

describe('listRequestSignature reuse gate (T2-6)', () => {
  const base = {
    typePath: 'Admin/Department',
    tenantCode: '',
    pageIndex: 0,
    pageSize: 20,
    sorts: '',
    search: '',
    viewFilter: '',
  };

  it('仅渲染差异（签名相等）可复用；筛选/排序/分页/关键字/租户任一不同不可复用', () => {
    const a = listRequestSignature(base);
    expect(listRequestSignature({ ...base })).toBe(a);
    expect(listRequestSignature({ ...base, search: 'q' })).not.toBe(a);
    expect(listRequestSignature({ ...base, sorts: 'Name:asc' })).not.toBe(a);
    expect(listRequestSignature({ ...base, pageIndex: 1 })).not.toBe(a);
    expect(listRequestSignature({ ...base, viewFilter: '{"c":1}' })).not.toBe(a);
    expect(listRequestSignature({ ...base, tenantCode: 'north' })).not.toBe(a);
  });
});
