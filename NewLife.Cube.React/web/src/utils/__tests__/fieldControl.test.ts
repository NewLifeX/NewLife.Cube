/**
 * 字段控件映射单元测试（对齐 Vue 皮肤 fieldControl 测试）
 */
import { describe, expect, it } from 'vitest';
import {
  resolveControl,
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
