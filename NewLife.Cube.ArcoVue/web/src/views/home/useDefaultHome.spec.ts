import { describe, expect, it } from 'vitest';
import {
  assemblyTableRows,
  flattenRows,
  formatSizeMb,
  mainEntries,
  processTableRows,
} from './useDefaultHome';

describe('useDefaultHome 纯函数', () => {
  it('flattenRows 对 {name,value} 行原样输出', () => {
    const rows = [{ name: 'A', value: '1' }, { name: 'B', value: '2' }];
    expect(flattenRows(rows)).toEqual([
      { name: 'A', value: '1' },
      { name: 'B', value: '2' },
    ]);
  });

  it('flattenRows 把普通对象摊成属性/值两列，跳过空值', () => {
    const rows = [{ moduleName: 'x.dll', version: '1.0', empty: null, nested: { a: 1 } }];
    expect(flattenRows(rows)).toEqual([
      { name: 'moduleName', value: 'x.dll' },
      { name: 'version', value: '1.0' },
      { name: 'nested', value: '{"a":1}' },
    ]);
  });

  it('mainEntries 跳过 null/空串，对象 JSON 化', () => {
    const main = { os: 'Windows', cpu: '', extra: { a: 1 }, nullKey: null };
    expect(mainEntries(main)).toEqual([
      { label: 'os', value: 'Windows' },
      { label: 'extra', value: '{"a":1}' },
    ]);
  });

  it('formatSizeMb 字节转 MB 保留两位小数，非正数返回空串', () => {
    expect(formatSizeMb(1024 * 1024)).toBe('1.00 MB');
    expect(formatSizeMb((2.5 * 1024 * 1024).toFixed(0))).toBe('2.50 MB');
    expect(formatSizeMb(0)).toBe('');
    expect(formatSizeMb(null)).toBe('');
    expect(formatSizeMb('abc')).toBe('');
  });

  it('processTableRows 映射七列并转换大小', () => {
    const rows = [
      {
        name: 'x.dll',
        companyName: 'NewLife',
        productName: '魔方',
        description: '核心库',
        version: '1.0.0',
        size: 1024 * 1024,
        fileName: 'C:\\x.dll',
      },
      { name: 'y.dll', size: null },
    ];
    expect(processTableRows(rows)).toEqual([
      {
        name: 'x.dll',
        companyName: 'NewLife',
        productName: '魔方',
        description: '核心库',
        version: '1.0.0',
        size: '1.00 MB',
        fileName: 'C:\\x.dll',
      },
      {
        name: 'y.dll',
        companyName: '',
        productName: '',
        description: '',
        version: '',
        size: '',
        fileName: '',
      },
    ]);
  });

  it('assemblyTableRows 映射六列：名称/显示名/文件版本/版本/编译时间/文件位置', () => {
    const rows = [
      {
        name: 'NewLife.Cube',
        title: '魔方',
        fileVersion: '6.8.2026.0814',
        version: '6.8.2026.814',
        compileTime: '2026-08-14 10:00',
        location: 'C:\\x\\NewLife.Cube.dll',
      },
      { name: 'empty' },
    ];
    expect(assemblyTableRows(rows)).toEqual([
      {
        name: 'NewLife.Cube',
        title: '魔方',
        fileVersion: '6.8.2026.0814',
        version: '6.8.2026.814',
        compileTime: '2026-08-14 10:00',
        location: 'C:\\x\\NewLife.Cube.dll',
      },
      {
        name: 'empty',
        title: '',
        fileVersion: '',
        version: '',
        compileTime: '',
        location: '',
      },
    ]);
  });
});
