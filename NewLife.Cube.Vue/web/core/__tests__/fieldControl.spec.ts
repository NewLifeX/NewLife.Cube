/**
 * 字段 → 控件 映射单元测试（唯一真理源 fieldControl.ts）
 *
 * 覆盖架构设计 §3.2 映射总表全矩阵：
 *   resolveControl      → ControlType（表单编辑）
 *   resolveSearchControl → SearchControlType（动态搜索）
 *   resolveListControl  → ListControlType（列表渲染）
 *
 * 规则：ItemType 优先于 TypeName；Guid→readonly；枚举/单选/多选→LOV；
 *       数值→inputNumber；大文本(Length>=300)→textarea；未知→input 兜底。
 *
 * 运行：pnpm test:unit core/__tests__/fieldControl.spec.ts
 */
import { describe, it, expect } from 'vitest';
import type { FieldMeta } from '../types/field';
import {
  resolveControl,
  resolveSearchControl,
  resolveListControl,
  isFullWidthControl,
  resolveNumberPrecision,
  resolveNumberStep,
} from '../utils/fieldControl';

/** 构造最小 FieldMeta 的便捷工厂 */
function f(partial: Partial<FieldMeta> & { name: string; typeName: string }): FieldMeta {
  return { itemType: undefined, length: 0, ...partial } as FieldMeta;
}

const ENUM_LOV = 'Enum.CubeDemo.Areas.Test.测试枚举';

describe('resolveControl（表单编辑控件）', () => {
  it('短文本 String(Length<300) → input', () => {
    expect(resolveControl(f({ name: 'ShortText', typeName: 'String', length: 50 }))).toBe('input');
  });

  it('大文本 String(Length>=300) → textarea', () => {
    expect(resolveControl(f({ name: 'LongText', typeName: 'String', length: 500 }))).toBe('textarea');
  });

  it('数值 Int32/Int64/Decimal/Double/Single → inputNumber', () => {
    for (const t of ['Int32', 'Int64', 'Decimal', 'Double', 'Single']) {
      expect(resolveControl(f({ name: 'N', typeName: t })), t).toBe('inputNumber');
    }
  });

  it('布尔 Boolean → switch', () => {
    expect(resolveControl(f({ name: 'Enable', typeName: 'Boolean' }))).toBe('switch');
  });

  it('日期 DateTime → datePicker', () => {
    expect(resolveControl(f({ name: 'CreateTime', typeName: 'DateTime' }))).toBe('datePicker');
  });

  it('时间 TimeSpan → timePicker', () => {
    expect(resolveControl(f({ name: 'Span', typeName: 'TimeSpan' }))).toBe('timePicker');
  });

  it('枚举(带 lovCode) → lov', () => {
    expect(resolveControl(f({ name: 'Kind', typeName: '测试枚举', lovCode: ENUM_LOV }))).toBe('lov');
  });

  it('singleSelect(itemType) + lovCode → lov', () => {
    expect(
      resolveControl(f({ name: 'SingleVal', typeName: 'Int32', itemType: 'singleSelect', lovCode: ENUM_LOV })),
    ).toBe('lov');
  });

  it('multipleSelect(itemType) + lovCode → lovMulti', () => {
    expect(
      resolveControl(
        f({ name: 'MultiVal', typeName: 'String', itemType: 'multipleSelect', lovCode: ENUM_LOV }),
      ),
    ).toBe('lovMulti');
  });

  it('lovCode + multiple=true → lovMulti', () => {
    expect(resolveControl(f({ name: 'X', typeName: 'String', lovCode: ENUM_LOV, multiple: true }))).toBe(
      'lovMulti',
    );
  });

  it('ItemType=file → upload', () => {
    expect(resolveControl(f({ name: 'FileUrl', typeName: 'String', itemType: 'file' }))).toBe('upload');
  });

  it('ItemType=image → image', () => {
    expect(resolveControl(f({ name: 'Avatar', typeName: 'String', itemType: 'image' }))).toBe('image');
  });

  it('ItemType=json → json', () => {
    expect(resolveControl(f({ name: 'JsonVal', typeName: 'String', itemType: 'json' }))).toBe('json');
  });

  it('ItemType=html → richHtml', () => {
    expect(resolveControl(f({ name: 'HtmlVal', typeName: 'String', itemType: 'html' }))).toBe('richHtml');
  });

  it('ItemType=markdown → richMarkdown', () => {
    expect(resolveControl(f({ name: 'Md', typeName: 'String', itemType: 'markdown' }))).toBe('richMarkdown');
  });

  it('ItemType=color → color', () => {
    expect(resolveControl(f({ name: 'ColorVal', typeName: 'String', itemType: 'color' }))).toBe('color');
  });

  it('ItemType=icon → icon', () => {
    expect(resolveControl(f({ name: 'IconVal', typeName: 'String', itemType: 'icon' }))).toBe('icon');
  });

  it('ItemType=mail → email', () => {
    expect(resolveControl(f({ name: 'MailVal', typeName: 'String', itemType: 'mail' }))).toBe('email');
  });

  it('ItemType=mobile → tel', () => {
    expect(resolveControl(f({ name: 'MobileVal', typeName: 'String', itemType: 'mobile' }))).toBe('tel');
  });

  it('ItemType=url → url', () => {
    expect(resolveControl(f({ name: 'UrlVal', typeName: 'String', itemType: 'url' }))).toBe('url');
  });

  it('Guid → readonly', () => {
    expect(resolveControl(f({ name: 'GuidVal', typeName: 'Guid' }))).toBe('readonly');
  });

  it('未知类型兜底 → input（永不白屏）', () => {
    expect(resolveControl(f({ name: 'Weird', typeName: 'SomeUnknownType' }))).toBe('input');
  });

  it('枚举类型名(无 lovCode) 兜底 → lov', () => {
    expect(resolveControl(f({ name: 'E', typeName: 'Enum' }))).toBe('lov');
  });
});

describe('resolveSearchControl（动态搜索控件）', () => {
  it('String → text', () => {
    expect(resolveSearchControl(f({ name: 'S', typeName: 'String' }))).toBe('text');
  });

  it('数值 → numberRange', () => {
    expect(resolveSearchControl(f({ name: 'N', typeName: 'Int32' }))).toBe('numberRange');
  });

  it('Boolean → switch', () => {
    expect(resolveSearchControl(f({ name: 'B', typeName: 'Boolean' }))).toBe('switch');
  });

  it('DateTime → datetimeRange（ListSearchBar 已消费）', () => {
    // 注：架构 §3.2 表标注为 dateRange，实现返回 datetimeRange，
    // ListSearchBar 已处理 datetimeRange 分支，内部一致。
    expect(resolveSearchControl(f({ name: 'D', typeName: 'DateTime' }))).toBe('datetimeRange');
  });

  it('TimeSpan → timeRange', () => {
    expect(resolveSearchControl(f({ name: 'T', typeName: 'TimeSpan' }))).toBe('timeRange');
  });

  it('file/image → fileExists', () => {
    expect(resolveSearchControl(f({ name: 'F', typeName: 'String', itemType: 'file' }))).toBe('fileExists');
    expect(resolveSearchControl(f({ name: 'I', typeName: 'String', itemType: 'image' }))).toBe('fileExists');
  });

  it('singleSelect → lov', () => {
    expect(resolveSearchControl(f({ name: 'S', typeName: 'Int32', itemType: 'singleSelect', lovCode: ENUM_LOV }))).toBe(
      'lov',
    );
  });

  it('multipleSelect → lovMulti', () => {
    expect(
      resolveSearchControl(f({ name: 'M', typeName: 'String', itemType: 'multipleSelect', lovCode: ENUM_LOV })),
    ).toBe('lovMulti');
  });

  it('lovCode + multiple → lovMulti', () => {
    expect(resolveSearchControl(f({ name: 'M', typeName: 'String', lovCode: ENUM_LOV, multiple: true }))).toBe(
      'lovMulti',
    );
  });

  it('mail/mobile/url → text', () => {
    expect(resolveSearchControl(f({ name: 'M', typeName: 'String', itemType: 'mail' }))).toBe('text');
    expect(resolveSearchControl(f({ name: 'P', typeName: 'String', itemType: 'mobile' }))).toBe('text');
    expect(resolveSearchControl(f({ name: 'U', typeName: 'String', itemType: 'url' }))).toBe('text');
  });
});

describe('resolveListControl（列表渲染控件）', () => {
  it('String → text', () => {
    expect(resolveListControl(f({ name: 'S', typeName: 'String' }))).toBe('text');
  });

  it('数值 → number', () => {
    expect(resolveListControl(f({ name: 'N', typeName: 'Decimal' }))).toBe('number');
  });

  it('Boolean → boolean', () => {
    expect(resolveListControl(f({ name: 'B', typeName: 'Boolean' }))).toBe('boolean');
  });

  it('DateTime → date', () => {
    expect(resolveListControl(f({ name: 'D', typeName: 'DateTime' }))).toBe('date');
  });

  it('TimeSpan → time', () => {
    expect(resolveListControl(f({ name: 'T', typeName: 'TimeSpan' }))).toBe('time');
  });

  it('image → image', () => {
    expect(resolveListControl(f({ name: 'I', typeName: 'String', itemType: 'image' }))).toBe('image');
  });

  it('file → file', () => {
    expect(resolveListControl(f({ name: 'F', typeName: 'String', itemType: 'file' }))).toBe('file');
  });

  it('color → color', () => {
    expect(resolveListControl(f({ name: 'C', typeName: 'String', itemType: 'color' }))).toBe('color');
  });

  it('icon → icon', () => {
    expect(resolveListControl(f({ name: 'I', typeName: 'String', itemType: 'icon' }))).toBe('icon');
  });

  it('json → json', () => {
    expect(resolveListControl(f({ name: 'J', typeName: 'String', itemType: 'json' }))).toBe('json');
  });

  it('html/markdown → html', () => {
    expect(resolveListControl(f({ name: 'H', typeName: 'String', itemType: 'html' }))).toBe('html');
    expect(resolveListControl(f({ name: 'M', typeName: 'String', itemType: 'markdown' }))).toBe('html');
  });

  it('mail/mobile/url → text', () => {
    expect(resolveListControl(f({ name: 'M', typeName: 'String', itemType: 'mail' }))).toBe('text');
    expect(resolveListControl(f({ name: 'P', typeName: 'String', itemType: 'mobile' }))).toBe('text');
    expect(resolveListControl(f({ name: 'U', typeName: 'String', itemType: 'url' }))).toBe('text');
  });

  it('singleSelect/multipleSelect → lov', () => {
    expect(resolveListControl(f({ name: 'S', typeName: 'Int32', itemType: 'singleSelect', lovCode: ENUM_LOV }))).toBe(
      'lov',
    );
    expect(
      resolveListControl(f({ name: 'M', typeName: 'String', itemType: 'multipleSelect', lovCode: ENUM_LOV })),
    ).toBe('lov');
  });

  it('Guid → readonly', () => {
    expect(resolveListControl(f({ name: 'G', typeName: 'Guid' }))).toBe('readonly');
  });

  it('lovCode/Enum → lov', () => {
    expect(resolveListControl(f({ name: 'K', typeName: '测试枚举', lovCode: ENUM_LOV }))).toBe('lov');
    expect(resolveListControl(f({ name: 'E', typeName: 'Enum' }))).toBe('lov');
  });
});

describe('isFullWidthControl（全宽控件）', () => {
  it('textarea/json/richHtml/richMarkdown/upload/image/lovMulti 全宽', () => {
    for (const c of ['textarea', 'json', 'richHtml', 'richMarkdown', 'upload', 'image', 'lovMulti']) {
      expect(isFullWidthControl(c), c).toBe(true);
    }
  });

  it('input/switch/datePicker 非全宽', () => {
    for (const c of ['input', 'switch', 'datePicker', 'lov', 'color', 'icon']) {
      expect(isFullWidthControl(c), c).toBe(false);
    }
  });
});

describe('resolveNumberPrecision / resolveNumberStep（数值精度）', () => {
  it('后端返回有效精度（scale>0）→ 直接用返回的精度', () => {
    // Decimal Scale=2 → 2 位
    expect(resolveNumberPrecision(f({ name: 'DecimalVal', typeName: 'Decimal', scale: 2 }))).toBe(2);
    // Double 显式配置 3 位 → 3 位（即使类型默认是 8）
    expect(resolveNumberPrecision(f({ name: 'DoubleVal', typeName: 'Double', scale: 3 }))).toBe(3);
    // 步进随返回精度
    expect(resolveNumberStep(f({ name: 'DecimalVal', typeName: 'Decimal', scale: 2 }))).toBe(0.01);
  });

  it('后端返回 0 精度（未配置）→ 按类型给默认精度', () => {
    // scale=0 视为未配置：单精度 → 4 位，双精度 → 8 位
    expect(resolveNumberPrecision(f({ name: 'FloatVal', typeName: 'Single', scale: 0 }))).toBe(4);
    expect(resolveNumberPrecision(f({ name: 'DoubleVal', typeName: 'Double', scale: 0 }))).toBe(8);
    // 后端干脆没下发 scale，同样走默认
    expect(resolveNumberPrecision(f({ name: 'DoubleVal', typeName: 'Double' }))).toBe(8);
    // 十进制未配置 → 2 位默认
    expect(resolveNumberPrecision(f({ name: 'DecimalVal', typeName: 'Decimal', scale: 0 }))).toBe(2);
  });

  it('整数类型（Int32/Int64）→ 0 位小数，与 scale 无关', () => {
    expect(resolveNumberPrecision(f({ name: 'Int32Val', typeName: 'Int32' }))).toBe(0);
    expect(resolveNumberPrecision(f({ name: 'Int64Val', typeName: 'Int64', scale: 0 }))).toBe(0);
    expect(resolveNumberStep(f({ name: 'Int32Val', typeName: 'Int32' }))).toBe(1);
  });

  it('未识别数值类型 → undefined（不限制进度）', () => {
    expect(resolveNumberPrecision(f({ name: 'X', typeName: 'UnknownNum' }))).toBeUndefined();
  });
});
