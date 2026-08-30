/**
 * 字段控件映射单元测试（对齐 Vue 皮肤 fieldControl 测试）
 */
import { describe, expect, it } from 'vitest';
import {
  DEFAULT_CATEGORY,
  groupByCategory,
  hasCategory,
  resolveControl,
  resolveDescription,
  resolveSearchControl,
  resolveListControl,
  widgetToControl,
  serializeSubmitModel,
  resolveNumberPrecision,
  isFullWidthControl,
} from '@/utils/fieldControl';
import { toFieldMeta } from '@/types/field';
import type { FieldMeta } from '@/types/field';
import type { DataField } from '@cube/api-core';

function f(partial: Partial<DataField>): FieldMeta {
  return toFieldMeta({ name: 'F', typeName: 'String', ...partial });
}

describe('resolveControl 表单控件映射', () => {
  it('ItemType 优先于 TypeName', () => {
    expect(resolveControl(f({ itemType: 'json', typeName: 'String' }))).toBe('json');
    expect(resolveControl(f({ itemType: 'image' }))).toBe('image');
    expect(resolveControl(f({ itemType: 'mail' }))).toBe('email');
    expect(resolveControl(f({ itemType: 'mobile' }))).toBe('tel');
    expect(resolveControl(f({ itemType: 'color' }))).toBe('color');
    expect(resolveControl(f({ itemType: 'html' }))).toBe('richHtml');
    expect(resolveControl(f({ itemType: 'markdown' }))).toBe('richMarkdown');
  });

  it('Guid → readonly', () => {
    expect(resolveControl(f({ typeName: 'Guid' }))).toBe('readonly');
  });

  it('lovCode → lov / lovMulti', () => {
    expect(resolveControl(f({ typeName: 'Int32', lovCode: 'Enum.Kind' }))).toBe('lov');
    expect(resolveControl(f({ typeName: 'String', lovCode: 'Enum.Tags', multiple: true }))).toBe('lovMulti');
  });

  it('CLR 类型映射', () => {
    expect(resolveControl(f({ typeName: 'Boolean' }))).toBe('switch');
    expect(resolveControl(f({ typeName: 'DateTime' }))).toBe('datePicker');
    expect(resolveControl(f({ typeName: 'TimeSpan' }))).toBe('timePicker');
    expect(resolveControl(f({ typeName: 'Int32' }))).toBe('inputNumber');
    expect(resolveControl(f({ typeName: 'Decimal' }))).toBe('inputNumber');
    expect(resolveControl(f({ typeName: 'Enum' }))).toBe('lov');
  });

  it('大文本 → textarea，普通 → input', () => {
    expect(resolveControl(f({ typeName: 'String', length: 300 }))).toBe('textarea');
    expect(resolveControl(f({ typeName: 'String', length: 50 }))).toBe('input');
  });
});

describe('resolveSearchControl 搜索控件映射', () => {
  it('范围类控件', () => {
    expect(resolveSearchControl(f({ typeName: 'Int32' }))).toBe('numberRange');
    expect(resolveSearchControl(f({ typeName: 'DateTime' }))).toBe('datetimeRange');
    expect(resolveSearchControl(f({ typeName: 'TimeSpan' }))).toBe('timeRange');
  });

  it('LOV / 开关 / 附件存在性', () => {
    expect(resolveSearchControl(f({ typeName: 'Int32', lovCode: 'Enum.Kind' }))).toBe('lov');
    expect(resolveSearchControl(f({ typeName: 'String', lovCode: 'Enum.T', multiple: true }))).toBe('lovMulti');
    expect(resolveSearchControl(f({ typeName: 'Boolean' }))).toBe('switch');
    expect(resolveSearchControl(f({ itemType: 'file' }))).toBe('fileExists');
  });

  it('默认文本', () => {
    expect(resolveSearchControl(f({ typeName: 'String' }))).toBe('text');
  });
});

describe('resolveListControl 列表单元格映射', () => {
  it('类型映射', () => {
    expect(resolveListControl(f({ typeName: 'Boolean' }))).toBe('boolean');
    expect(resolveListControl(f({ typeName: 'DateTime' }))).toBe('date');
    expect(resolveListControl(f({ typeName: 'Int32' }))).toBe('number');
    expect(resolveListControl(f({ typeName: 'String', lovCode: 'Enum.K' }))).toBe('lov');
    expect(resolveListControl(f({ itemType: 'image' }))).toBe('image');
    expect(resolveListControl(f({ itemType: 'color' }))).toBe('color');
    expect(resolveListControl(f({ typeName: 'Guid' }))).toBe('readonly');
  });
});

describe('widgetToControl @cube widget → 皮肤控件', () => {
  it('全量映射不丢', () => {
    expect(widgetToControl('text')).toBe('input');
    expect(widgetToControl('textarea')).toBe('textarea');
    expect(widgetToControl('number')).toBe('inputNumber');
    expect(widgetToControl('switch')).toBe('switch');
    expect(widgetToControl('date')).toBe('datePicker');
    expect(widgetToControl('datetime')).toBe('datePicker');
    expect(widgetToControl('lov')).toBe('lov');
    expect(widgetToControl('lovMulti')).toBe('lovMulti');
    expect(widgetToControl('image')).toBe('image');
    expect(widgetToControl('file')).toBe('upload');
    expect(widgetToControl('json')).toBe('json');
    expect(widgetToControl('html')).toBe('richHtml');
    expect(widgetToControl('markdown')).toBe('richMarkdown');
    expect(widgetToControl('color')).toBe('color');
    expect(widgetToControl('icon')).toBe('icon');
  });
});

describe('serializeSubmitModel 提交序列化', () => {
  it('多选数组 → 逗号分隔字符串', () => {
    const fields = [
      { name: 'Tags', multiple: true, typeName: 'String' },
      { name: 'Name', typeName: 'String' },
    ] as FieldMeta[];
    const out = serializeSubmitModel({ Tags: ['a', 'b', 'c'], Name: 'x' }, fields);
    expect(out).toEqual({ Tags: 'a,b,c', Name: 'x' });
  });

  it('itemType multipleSelect 同样合并', () => {
    const fields = [{ name: 'Tags', itemType: 'multipleSelect', typeName: 'String' }] as FieldMeta[];
    const out = serializeSubmitModel({ Tags: ['1', '2'] }, fields);
    expect(out.Tags).toBe('1,2');
  });

  it('非数组原样透传', () => {
    const fields = [{ name: 'Tags', multiple: true, typeName: 'String' }] as FieldMeta[];
    const out = serializeSubmitModel({ Tags: 'a', Num: 3 }, fields);
    expect(out).toEqual({ Tags: 'a', Num: 3 });
  });
});

describe('resolveNumberPrecision / isFullWidthControl', () => {
  it('精度规则', () => {
    expect(resolveNumberPrecision(f({ typeName: 'Decimal', scale: 2 }))).toBe(2);
    expect(resolveNumberPrecision(f({ typeName: 'Decimal' }))).toBe(2);
    expect(resolveNumberPrecision(f({ typeName: 'Double' }))).toBe(8);
    expect(resolveNumberPrecision(f({ typeName: 'Single' }))).toBe(4);
    expect(resolveNumberPrecision(f({ typeName: 'Int32' }))).toBeUndefined();
  });

  it('全宽控件', () => {
    expect(isFullWidthControl('textarea')).toBe(true);
    expect(isFullWidthControl('json')).toBe(true);
    expect(isFullWidthControl('input')).toBe(false);
  });
});

describe('resolveDescription 描述裁剪', () => {
  it('裁掉 displayName 前缀与首部标点', () => {
    expect(resolveDescription(f({ displayName: '名称', description: '名称。实体名称' }))).toBe('实体名称');
    expect(resolveDescription(f({ displayName: '短文本', description: '短文本。必填的短文本示例' }))).toBe('必填的短文本示例');
  });

  it('与 displayName 相同 → 空串（不展示）', () => {
    expect(resolveDescription(f({ displayName: '名称', description: '名称' }))).toBe('');
  });

  it('无描述 → 空串', () => {
    expect(resolveDescription(f({ displayName: '名称' }))).toBe('');
    expect(resolveDescription(f({ description: '   ' }))).toBe('');
  });

  it('不以 displayName 开头 → 原样保留（仅去首部标点）', () => {
    expect(resolveDescription(f({ displayName: '名称', description: 'RPC服务端口。默认1882' }))).toBe('RPC服务端口。默认1882');
    expect(resolveDescription(f({ displayName: '名称', description: '。前缀标点' }))).toBe('前缀标点');
  });
});

describe('hasCategory 是否需分组', () => {
  it('全部无分类 → false（平铺）', () => {
    expect(hasCategory([f({ name: 'A' }), f({ name: 'B' })])).toBe(false);
  });

  it('任一字段有分类 → true（分组）', () => {
    expect(hasCategory([f({ name: 'A' }), f({ name: 'B', category: '扩展' })])).toBe(true);
  });

  it('纯空白分类视为无分类', () => {
    expect(hasCategory([f({ name: 'A', category: '  ' })])).toBe(false);
  });
});

describe('groupByCategory 按分类分组', () => {
  it('全部无分类归入默认组', () => {
    const g = groupByCategory([f({ name: 'A' }), f({ name: 'B' })]);
    expect(g).toEqual([{ category: DEFAULT_CATEGORY, fields: [f({ name: 'A' }), f({ name: 'B' })] }]);
  });

  it('按分类分组并保序，无分类归入默认组', () => {
    const fields = [
      f({ name: 'Name', category: '基本信息' }),
      f({ name: 'Enable' }),
      f({ name: 'Secret', category: '扩展' }),
      f({ name: 'Remark', category: '基本信息' }),
    ];
    const g = groupByCategory(fields);
    expect(g.map((x) => x.category)).toEqual(['基本信息', DEFAULT_CATEGORY, '扩展']);
    expect(g[0].fields.map((x) => x.name)).toEqual(['Name', 'Remark']);
    expect(g[1].fields.map((x) => x.name)).toEqual(['Enable']);
    expect(g[2].fields.map((x) => x.name)).toEqual(['Secret']);
  });

  it('分类前后空白被修剪', () => {
    const g = groupByCategory([f({ name: 'A', category: ' 基本信息 ' }), f({ name: 'B', category: '基本信息' })]);
    expect(g).toHaveLength(1);
    expect(g[0].category).toBe('基本信息');
  });
});
