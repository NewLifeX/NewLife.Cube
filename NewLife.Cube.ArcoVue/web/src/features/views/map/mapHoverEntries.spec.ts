import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref } from '@/core/utils/viewProfile';
import {
  buildHoverEntries,
  findHoverField,
  hoverFieldValue,
  resolveHoverTitle,
} from './mapHoverEntries';

/** 字段元数据（PascalCase 主键名——GetPage 元数据形态） */
const fields = [
  { name: 'ID', displayName: '编码', typeName: 'Int32' },
  { name: 'Name', displayName: '名称', typeName: 'String' },
  { name: 'Kind', displayName: '类型', typeName: 'String' },
  { name: 'TelCode', displayName: '区号', typeName: 'String' },
  { name: 'ZipCode', displayName: '邮编', typeName: 'String' },
  { name: 'GeoHash', displayName: '地址编码', typeName: 'String' },
] as unknown as FieldMeta[];

/** 行数据（camelCase——API 行数据形态） */
const row: Record<string, unknown> = {
  id: 620821,
  name: '泾川',
  kind: '县',
  telCode: '0933',
  zipCode: '744300',
  geoHash: 'ws0fugz90',
  remark: '',
};

const col = (key: string, visible = true) => ({ key, visible, frozen: false }) as ColumnPref;

describe('mapHoverEntries（OSC-261004d7f4 悬停卡片取值修复）', () => {
  it('字段查找大小写不敏感：PascalCase 与 camelCase 均命中同一字段', () => {
    expect(findHoverField(fields, 'Kind')?.name).toBe('Kind');
    expect(findHoverField(fields, 'kind')?.name).toBe('Kind');
    expect(findHoverField(fields, 'TELCODE')?.name).toBe('TelCode');
    expect(findHoverField(fields, '')).toBeUndefined();
  });

  it('列 key 与字段名大小写一致（PascalCase）正常格式化取值', () => {
    expect(hoverFieldValue(row, fields, 'Kind')).toBe('县');
    expect(hoverFieldValue(row, fields, 'TelCode')).toBe('0933');
  });

  it('列 key 为 camelCase（与字段名大小写不一致）仍能取值（对齐卡片视图 findBodyField）', () => {
    // 修复前：严格相等查找失败 → 整列显示 “-”
    expect(hoverFieldValue(row, fields, 'kind')).toBe('县');
    expect(hoverFieldValue(row, fields, 'telCode')).toBe('0933');
    expect(hoverFieldValue(row, fields, 'zipCode')).toBe('744300');
  });

  it('字段元数据完全缺失时按列 key 容错取值兜底', () => {
    expect(hoverFieldValue(row, [], 'name')).toBe('泾川');
    expect(hoverFieldValue(row, [], 'geoHash')).toBe('ws0fugz90');
  });

  it('空值/缺失值显示 “-”', () => {
    expect(hoverFieldValue(row, fields, 'remark')).toBe('-');
    expect(hoverFieldValue(row, fields, 'notExist')).toBe('-');
  });

  it('条目构建：过滤不可见列与标题字段，label 优先 displayName（大小写容错）', () => {
    const columns = [col('Name'), col('kind'), col('TelCode'), col('ZipCode', false), col('ID')];
    const entries = buildHoverEntries(row, fields, columns, undefined, 'Name', 10);
    expect(entries.map((e) => [e.label, e.value])).toEqual([
      ['类型', '县'], // camelCase 列 key → 命中 Kind 字段取显示名
      ['区号', '0933'],
      ['编码', '620821'],
    ]);
  });

  it('条目构建：titles 覆盖优先，maxRows 截断', () => {
    const columns = [col('kind'), col('telCode'), col('geoHash')];
    const entries = buildHoverEntries(row, fields, columns, { kind: '自定义类型' }, undefined, 2);
    expect(entries).toEqual([
      { label: '自定义类型', value: '县' },
      { label: '区号', value: '0933' },
    ]);
    expect(entries.length).toBe(2);
  });

  it('标题解析：titleField 大小写容错（走完整格式化）、回落主键、再回落「详情」', () => {
    expect(resolveHoverTitle(row, fields, 'name', 'ID')).toBe('泾川');
    expect(resolveHoverTitle(row, fields, 'NAME', 'ID')).toBe('泾川');
    // titleField 无对应字段元数据：按 key 容错直取
    expect(resolveHoverTitle(row, [], 'name', 'ID')).toBe('泾川');
    // titleField 缺失：回落主键
    expect(resolveHoverTitle(row, fields, undefined, 'ID')).toBe('620821');
    // 全空：回落「详情」
    expect(resolveHoverTitle({}, fields, undefined, 'ID')).toBe('详情');
  });
});
