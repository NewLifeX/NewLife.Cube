import { describe, expect, it } from 'vitest';
import { fileRowOf, sortKeyOf } from './useFilePage';

describe('useFilePage 纯函数', () => {
  it('fileRowOf 归一化目录/文件/父级行', () => {
    const dir = fileRowOf({ Name: 'Doc', FullName: 'Doc', Directory: true, Size: '1M', LastWrite: '2026-08-13 10:00:00' });
    expect(dir.directory).toBe(true);
    expect(dir.fullName).toBe('Doc');
    expect(dir.isParent).toBe(false);

    const file = fileRowOf({ name: 'a.txt', fullName: 'Doc/a.txt', directory: false, size: '12', lastWrite: '2026-08-13 10:00:00' });
    expect(file.directory).toBe(false);
    expect(file.fullName).toBe('Doc/a.txt');

    const parent = fileRowOf({ Name: '../', FullName: '', Directory: true });
    expect(parent.isParent).toBe(true);
  });

  it('sortKeyOf 仅接受 size/lastwrite，其余回落到 name', () => {
    expect(sortKeyOf('size')).toBe('size');
    expect(sortKeyOf('lastwrite')).toBe('lastwrite');
    expect(sortKeyOf('name')).toBe('name');
    expect(sortKeyOf('xxx')).toBe('name');
  });
});
