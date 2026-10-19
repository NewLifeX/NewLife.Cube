/**
 * `core/engine/logic.ts` 纯函数单测
 *
 * 覆盖：空值过滤口径、分页与查询参数拼装、导出扩展名、导入结果解析、
 * 列显隐合并、主键容错、整行回传清洗、日期显示格式化、必填判定。
 *
 * 运行：pnpm test:unit core/engine/__tests__/logic.spec.ts
 */
import { describe, expect, it } from 'vitest';
import {
  buildExportParams,
  buildQueryParams,
  cleanRowForUpdate,
  dateStamp,
  defaultColumnVisibility,
  dropEmptyParams,
  entityNameFromPrefix,
  extractImportRows,
  findMissingRequiredField,
  formatCellDateTime,
  inferBlobExtension,
  isBlankCell,
  isZeroRowImport,
  mergeColumnVisibility,
  parseColumnVisibility,
  parseImportResult,
  primaryKeyFromFields,
  resolveRowId,
  withPagination,
  buildDownloadName,
} from '../../engine/logic';
import type { FieldMeta } from '../../types/field';

const str = (name: string, displayName?: string, extra: Partial<FieldMeta> = {}): FieldMeta => ({
  name,
  displayName: displayName ?? name,
  typeName: 'String',
  ...extra,
});

describe('engine/logic 空值过滤', () => {
  it('应该丢弃 undefined/null/空串 当筛选条件未填时', () => {
    expect(
      dropEmptyParams({ a: undefined, b: null, c: '', d: 'x', e: [], f: {} }),
    ).toEqual({ d: 'x', e: [], f: {} });
  });

  it('应该保留 0 与 false 当数值/布尔条件为假值时', () => {
    // 这是后端筛选最容易踩的坑：把 0/false 当「没填」丢掉，就永远查不出「数值=0」「布尔=否」
    expect(dropEmptyParams({ count: 0, enable: false })).toEqual({ count: 0, enable: false });
  });

  it('应该把关键字并成 q 且不包含分页参数 当拼装查询条件时', () => {
    const params = buildQueryParams({ keyword: '  abc  ', filters: { Name: 'x', Empty: '' } });
    expect(params).toEqual({ q: 'abc', Name: 'x' });
    expect(params).not.toHaveProperty('pageIndex');
  });

  it('应该忽略关键字 当引擎关闭 keyword 时', () => {
    expect(buildQueryParams({ keyword: 'abc', keywordEnabled: false })).toEqual({});
  });

  it('应该带上 format 与当前筛选 当拼装导出参数时', () => {
    expect(buildExportParams({ format: 'csv', keyword: 'k', filters: { Kind: 2 } })).toEqual({
      format: 'csv',
      q: 'k',
      Kind: 2,
    });
  });

  it('应该由数据层注入分页 当调用 withPagination 时', () => {
    expect(withPagination({ q: 'k' }, 3, 50)).toEqual({ pageIndex: 3, pageSize: 50, q: 'k' });
  });
});

describe('engine/logic 文件下载', () => {
  it.each([
    ['application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', '.xlsx'],
    ['application/vnd.ms-excel', '.xlsx'],
    ['text/csv', '.csv'],
    ['application/json', '.json'],
    ['application/xml', '.xml'],
    ['application/zip', '.zip'],
    ['', '.xlsx'],
    [undefined, '.xlsx'],
  ])('应该识别 %s 为 %s 当推断导出扩展名时', (type, ext) => {
    expect(inferBlobExtension(type)).toBe(ext);
  });

  it('应该取最后一段路径 当从实体前缀推业务名时', () => {
    expect(entityNameFromPrefix('/api/Equipments/Equipment')).toBe('Equipment');
    expect(entityNameFromPrefix('')).toBe('export');
  });

  it('应该拼出带时间戳的文件名 当生成下载名时', () => {
    expect(buildDownloadName('Equipment', '20261019083000', '.xlsx')).toBe(
      'Equipment_20261019083000.xlsx',
    );
  });

  it('应该输出 yyyyMMddHHmmss 当用固定时钟生成时间戳时', () => {
    expect(dateStamp(new Date(2026, 9, 19, 8, 3, 7))).toBe('20261019080307');
  });
});

describe('engine/logic 导入结果', () => {
  it('应该判定为 0 行 当后端文案是外层「共0行」或内层「成功[0]行」时', () => {
    expect(isZeroRowImport('导入[点检项目.xlsx] 共0行')).toBe(true);
    expect(isZeroRowImport('导入[a.csv]，共[10]行，成功[0]行，1行无效！')).toBe(true);
  });

  it('应该不判定为 0 行 当确有落库行数时', () => {
    expect(isZeroRowImport('导入[a.xlsx] 共3行')).toBe(false);
    expect(isZeroRowImport('导入[a.csv]，共[10]行，成功[9]行')).toBe(false);
    expect(isZeroRowImport(undefined)).toBe(false);
  });

  it('应该优先解析「成功[N]行」再退到「共N行」当提取行数时', () => {
    expect(extractImportRows('共[10]行，成功[3]行')).toBe(3);
    expect(extractImportRows('导入[x.xlsx] 共7行')).toBe(7);
    expect(extractImportRows('导入完成')).toBe(null);
  });

  it('应该按 code===0 判定成功并保留原始文案 当解析导入响应时', () => {
    expect(parseImportResult({ code: 0, message: '导入[x] 共0行' })).toMatchObject({
      ok: true,
      rows: 0,
    });
    expect(parseImportResult({ code: -1, message: '表头不匹配' })).toMatchObject({
      ok: false,
      message: '表头不匹配',
    });
  });
});

describe('engine/logic 列显隐', () => {
  const columns = [
    { key: 'code', label: '编号' },
    { key: 'name', label: '名称' },
  ];

  it('应该默认全显示 当没有历史配置时', () => {
    expect(defaultColumnVisibility(columns)).toEqual({ code: true, name: true });
    expect(mergeColumnVisibility(columns, null)).toEqual({ code: true, name: true });
  });

  it('应该只接受布尔值并丢弃未知键 当合并历史配置时', () => {
    expect(mergeColumnVisibility(columns, { code: false, name: 'yes', extra: true })).toEqual({
      code: false,
      name: true,
    });
  });

  it('应该对新增列默认显示 当历史配置里没有该列时', () => {
    expect(mergeColumnVisibility([...columns, { key: 'kind', label: '类型' }], { code: false })).toEqual({
      code: false,
      name: true,
      kind: true,
    });
  });

  it.each([
    ['not-json', null],
    ['', null],
    ['[1,2]', null],
    ['{"code":false}', { code: false }],
  ])('应该把坏数据退回默认值 当解析 localStorage 原文 %s 时', (raw, expected) => {
    expect(parseColumnVisibility(raw)).toEqual(expected);
  });
});

describe('engine/logic 主键与回传载荷', () => {
  it('应该按元数据主键取值并容错大小写 当行数据键名不一致时', () => {
    expect(resolveRowId({ ID: 7 }, 'Id')).toBe(7);
    expect(resolveRowId({ id: 'x' })).toBe('x');
    expect(resolveRowId({ Name: 'a' })).toBeUndefined();
    // 0 是合法主键，不能被当成「没填」
    expect(resolveRowId({ id: 0 })).toBe(0);
  });

  it('应该取 primaryKey 字段名 当推导主键口径时', () => {
    expect(primaryKeyFromFields([{ name: 'Name' }, { name: 'EquipmentId', primaryKey: true }])).toBe(
      'EquipmentId',
    );
    expect(primaryKeyFromFields([{ name: 'Name' }])).toBe('id');
  });

  it('应该剔除与 camelCase 同名的 PascalCase 别名键 当清洗整行回传载荷时', () => {
    // 后端回包同时带 enable:true 与 Enable:1，PUT 时后出现的 PascalCase 会反向覆盖目标值，
    // 导致「接口返回成功但库里没变」的静默失败
    expect(cleanRowForUpdate({ enable: false, Enable: 1, name: 'a', Remark: 'r' })).toEqual({
      enable: false,
      name: 'a',
      Remark: 'r',
    });
  });
});

describe('engine/logic 单元格格式化', () => {
  it('应该按模式切片 当渲染日期/日期时间/时间时', () => {
    expect(formatCellDateTime('2026-10-19T08:30:00', 'date')).toBe('2026-10-19');
    expect(formatCellDateTime('2026-10-19T08:30:00', 'datetime')).toBe('2026-10-19 08:30:00');
    expect(formatCellDateTime('2026-10-19T08:30', 'datetime')).toBe('2026-10-19 08:30');
    expect(formatCellDateTime('08:30', 'time')).toBe('08:30:00');
    expect(formatCellDateTime('', 'date')).toBe('');
    expect(formatCellDateTime(undefined, 'datetime')).toBe('');
  });

  it('应该把空值判为空串而 0/false 不算空 当决定显示占位符时', () => {
    expect(isBlankCell(null)).toBe(true);
    expect(isBlankCell('  ')).toBe(true);
    expect(isBlankCell(0)).toBe(false);
    expect(isBlankCell(false)).toBe(false);
  });
});

describe('engine/logic 必填判定', () => {
  it('应该返回第一个未填的必填字段 当非空字符串字段留空时', () => {
    // nullable:false 且非主键 → isRequiredField 判必填（口径唯一来源 fieldControl）
    const fields = [str('Code', '编号', { nullable: false }), str('Name', '名称', { nullable: true })];
    expect(findMissingRequiredField({ Code: '', Name: '' }, fields)?.name).toBe('Code');
    expect(findMissingRequiredField({ Code: 'A', Name: '' }, fields)).toBeNull();
  });

  it('应该容忍大小写键名 当详情返回 PascalCase 键时', () => {
    const fields = [str('Code', '编号', { nullable: false })];
    expect(findMissingRequiredField({ code: 'A' }, fields)).toBeNull();
  });

  it('应该把空数组视为未填 当多选字段提交时', () => {
    const fields = [str('Tags', '标签', { nullable: false, multiple: true })];
    expect(findMissingRequiredField({ Tags: [] }, fields)?.name).toBe('Tags');
    expect(findMissingRequiredField({ Tags: ['a'] }, fields)).toBeNull();
  });
});
