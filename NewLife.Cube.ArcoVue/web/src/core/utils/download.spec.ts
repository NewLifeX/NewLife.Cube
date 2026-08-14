import { describe, expect, it } from 'vitest';
import { blobOf, filenameOf } from './download';

describe('download', () => {
  it('blobOf 直接 Blob 原样返回', () => {
    const blob = new Blob(['abc']);
    expect(blobOf(blob)).toBe(blob);
  });

  it('blobOf 从 AxiosResponse 形态解 data', () => {
    const blob = new Blob(['abc']);
    expect(blobOf({ data: blob, status: 200 })).toBe(blob);
  });

  it('blobOf 非 Blob 返回 null', () => {
    expect(blobOf({ data: 'xml' })).toBeNull();
    expect(blobOf(null)).toBeNull();
    expect(blobOf(undefined)).toBeNull();
  });

  it('filenameOf 解析 Content-Disposition 并回落默认名', () => {
    expect(filenameOf("attachment; filename=\"Cube.xml\"", 'a.xml')).toBe('Cube.xml');
    expect(filenameOf("attachment; filename*=UTF-8''%E9%AD%94%E6%96%B9.xml", 'a.xml')).toBe('魔方.xml');
    expect(filenameOf(undefined, 'a.xml')).toBe('a.xml');
  });
});
