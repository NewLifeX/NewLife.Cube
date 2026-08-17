import { describe, expect, it } from 'vitest';
import { resolveRowUrl, toSpaPath } from './useListOpsLinks';

describe('useListOpsLinks helpers', () => {
  it('resolveRowUrl 替换占位', () => {
    expect(resolveRowUrl('/Admin/Log?id={Id}', { Id: 7 })).toBe('/Admin/Log?id=7');
    expect(resolveRowUrl('/x?id={Id}', {})).toBe('/x?id=');
  });

  it('resolveRowUrl：{ID} 命中 camelCase id（OSC-2608178bdb 冒烟回归）', () => {
    expect(resolveRowUrl('/Cube/Area?parentId={ID}', { id: 110000 })).toBe(
      '/Cube/Area?parentId=110000',
    );
    expect(resolveRowUrl('/Admin/UserToken?userId={ID}', { id: 42, name: 'admin' })).toBe(
      '/Admin/UserToken?userId=42',
    );
    expect(resolveRowUrl('/Cube/Area?Id={ParentID}', { parentID: 100, id: 110101 })).toBe(
      '/Cube/Area?Id=100',
    );
  });

  it('toSpaPath 保留站内 path', () => {
    expect(toSpaPath('/Admin/User')).toBe('/Admin/User');
    expect(toSpaPath('Admin/User')).toBe('/Admin/User');
  });
});
