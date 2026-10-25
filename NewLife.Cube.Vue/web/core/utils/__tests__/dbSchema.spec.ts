/**
 * 数据库管理页数据归一化测试
 *
 * 后端返回 ApiResponse 包裹的数组，页面需要稳定结构；同时覆盖列键标识与精度展示两个渲染辅助。
 *
 * 运行：pnpm test:unit core/utils/__tests__/dbSchema.spec.ts
 */
import { describe, expect, it } from 'vitest';
import {
  columnFlag,
  normalizeDbList,
  normalizeDiffList,
  normalizeSchemaList,
  precisionText,
} from '../dbSchema';

describe('dbSchema 数据库页数据归一化', () => {
  it('应该解包 ApiResponse.data 并补齐缺省值 当返回信封时', () => {
    const list = normalizeDbList({
      code: 0,
      data: [{ name: 'Cube', type: 'Sqlite', version: '3.44', entities: 4, tables: 9, backups: 2 }],
    });

    expect(list).toHaveLength(1);
    expect(list[0]).toMatchObject({
      name: 'Cube',
      type: 'Sqlite',
      version: '3.44',
      entities: 4,
      tables: 9,
      backups: 2,
      driver: '',
      driverVersion: '',
    });
  });

  it('应该兼容裸数组 当接口未包裹时', () => {
    expect(normalizeDbList([{ name: 'Log', entities: '3' }])[0]).toMatchObject({ name: 'Log', entities: 3 });
  });

  it('应该返回空列表 当响应缺数据时', () => {
    expect(normalizeDbList(undefined)).toEqual([]);
    expect(normalizeSchemaList({ code: 0 })).toEqual([]);
    expect(normalizeDiffList(null)).toEqual([]);
  });

  it('应该逐列归一化表结构 当列字段类型不一致时', () => {
    const [table] = normalizeSchemaList({
      code: 0,
      data: [
        {
          name: '菜单',
          tableName: 'Menu',
          count: '12',
          columns: [{ name: 'Id', primaryKey: true, nullable: 'true', length: '4' }],
        },
      ],
    });

    expect(table.count).toBe(12);
    expect(table.columns[0]).toMatchObject({
      name: 'Id',
      primaryKey: true,
      nullable: true,
      length: 4,
      identity: false,
      unique: false,
    });
  });

  it('应该归一化模型差异条目', () => {
    const [diff] = normalizeDiffList({
      code: 0,
      data: [{ name: 'Menu', tableName: 'Menu', hasEntityModel: true, extraColumns: ['Old'], xmlFragment: '<Column />' }],
    });

    expect(diff.hasEntityModel).toBe(true);
    expect(diff.extraColumns).toEqual(['Old']);
    expect(diff.xmlFragment).toBe('<Column />');
  });

  it('应该按自增 > 主键 > 唯一索引给出列标识', () => {
    expect(columnFlag({ identity: true, primaryKey: true, unique: true })).toBe('AI');
    expect(columnFlag({ identity: false, primaryKey: true, unique: true })).toBe('PK');
    expect(columnFlag({ identity: false, primaryKey: false, unique: true })).toBe('UQ');
    expect(columnFlag({ identity: false, primaryKey: false, unique: false })).toBe('');
  });

  it('应该仅在存在精度或小数位时输出精度文本', () => {
    expect(precisionText({ precision: 18, scale: 2 })).toBe('(18, 2)');
    expect(precisionText({ precision: 0, scale: 0 })).toBe('');
  });
});
