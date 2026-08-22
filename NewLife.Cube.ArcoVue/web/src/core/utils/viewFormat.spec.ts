import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import type { ViewFormatRule } from '@/core/utils/viewProfile';
import {
  FORMAT_PRESET_COLORS,
  formatApplyOptions,
  formatRuleNeedsCondition,
  moveFormatRule,
  newFormatRule,
  resolveCardTitleFormat,
  resolveCardTitleFormatColor,
  resolveCellFormat,
  resolveCellFormatColor,
  resolveRowFormat,
  resolveRowSideColor,
  ruleMatchesRow,
  seedFormatRulesOnOpen,
} from './viewFormat';

function f(name: string, extra?: Partial<FieldMeta>): FieldMeta {
  return { name, displayName: name, typeName: 'String', ...extra };
}

const fields = [f('Enable', { typeName: 'Boolean' }), f('Name')];

function rule(partial: Partial<ViewFormatRule> & Pick<ViewFormatRule, 'apply' | 'field'>): ViewFormatRule {
  return {
    id: partial.id || 'f_1',
    color: partial.color || '#FFF7E8',
    op: partial.op || 'eq',
    value: partial.value,
    ...partial,
  };
}

describe('formatApplyOptions', () => {
  it('card 仅 side,row；table 四项且 side 在 row 前', () => {
    expect(formatApplyOptions('card')).toEqual(['side', 'row']);
    expect(formatApplyOptions('table')).toEqual(['cell', 'side', 'row', 'column']);
    expect(formatApplyOptions('tree')).toEqual(['cell', 'side', 'row', 'column']);
  });
});

describe('FORMAT_PRESET_COLORS', () => {
  it('30 个 hex，含红橙绿蓝紫', () => {
    expect(FORMAT_PRESET_COLORS).toHaveLength(30);
    for (const c of FORMAT_PRESET_COLORS) {
      expect(c).toMatch(/^#[0-9A-F]{6}$/);
    }
    expect(FORMAT_PRESET_COLORS).toEqual(expect.arrayContaining(['#F53F3F', '#00B42A', '#165DFF', '#FF7D00', '#722ED1']));
  });
});

describe('seedFormatRulesOnOpen', () => {
  it('空规则种第一条默认字段', () => {
    const seeded = seedFormatRulesOnOpen([], { firstField: 'Name', apply: 'cell' });
    expect(seeded).toHaveLength(1);
    expect(seeded![0].field).toBe('Name');
    expect(seeded![0].apply).toBe('cell');
  });

  it('已有字段不改；缺字段补第一列', () => {
    expect(seedFormatRulesOnOpen([rule({ apply: 'row', field: 'Enable' })], { firstField: 'Name', apply: 'cell' })).toBeNull();
    const filled = seedFormatRulesOnOpen([rule({ apply: 'row', field: '' })], { firstField: 'Name', apply: 'cell' });
    expect(filled![0].field).toBe('Name');
  });
});

describe('moveFormatRule', () => {
  it('换序', () => {
    const a = newFormatRule({ apply: 'cell', field: 'A' });
    const b = newFormatRule({ apply: 'row', field: 'B' });
    const moved = moveFormatRule([a, b], 1, 0);
    expect(moved[0].field).toBe('B');
    expect(moved[1].field).toBe('A');
  });

  it('越界原样拷贝', () => {
    const a = newFormatRule({ apply: 'cell', field: 'A' });
    expect(moveFormatRule([a], 0, 3)).toEqual([a]);
  });
});

describe('ruleMatchesRow / 双通道', () => {
  const row = { Enable: false, Name: 'x' };

  it('空值 eq 不命中', () => {
    expect(
      ruleMatchesRow(row, rule({ apply: 'row', field: 'Name', op: 'eq', value: '' }), fields),
    ).toBe(false);
    expect(
      ruleMatchesRow(row, rule({ apply: 'row', field: 'Name', op: 'eq', value: 'x' }), fields),
    ).toBe(true);
  });

  it('side 不进背景通道；背景与 side 可同时命中', () => {
    const rules: ViewFormatRule[] = [
      rule({ apply: 'side', field: 'Enable', op: 'eq', value: false, color: '#FF0000' }),
      rule({ apply: 'row', field: 'Enable', op: 'eq', value: false, color: '#FFF7E8' }),
    ];
    expect(resolveRowSideColor(row, rules, fields)).toBe('#FF0000');
    expect(resolveCellFormatColor(row, 'Name', rules, fields)).toBe('#FFF7E8');
    expect(resolveCellFormatColor(row, 'Enable', rules, fields)).toBe('#FFF7E8');
  });

  it('先匹配胜出：上条整行压过下条单元格', () => {
    const rules: ViewFormatRule[] = [
      rule({ apply: 'row', field: 'Enable', op: 'eq', value: false, color: '#AAAAAA' }),
      rule({ apply: 'cell', field: 'Enable', op: 'eq', value: false, color: '#BBBBBB' }),
    ];
    expect(resolveCellFormatColor(row, 'Enable', rules, fields)).toBe('#AAAAAA');
    expect(resolveCellFormatColor(row, 'Name', rules, fields)).toBe('#AAAAAA');
  });

  it('cell 仅条件字段列', () => {
    const rules: ViewFormatRule[] = [
      rule({ apply: 'cell', field: 'Enable', op: 'eq', value: false, color: '#111111' }),
    ];
    expect(resolveCellFormatColor(row, 'Enable', rules, fields)).toBe('#111111');
    expect(resolveCellFormatColor(row, 'Name', rules, fields)).toBeUndefined();
  });

  it('column 无条件铺满该列（不看操作符/值）', () => {
    expect(formatRuleNeedsCondition('column')).toBe(false);
    expect(formatRuleNeedsCondition('cell')).toBe(true);
    const on = { Enable: true, Name: 'y' };
    const col: ViewFormatRule[] = [
      rule({ apply: 'column', field: 'Name', op: 'eq', value: '', color: '#ABCDEF' }),
    ];
    expect(resolveCellFormatColor(on, 'Name', col, fields)).toBe('#ABCDEF');
    expect(resolveCellFormatColor(on, 'Enable', col, fields)).toBeUndefined();
    expect(resolveCellFormatColor(on, 'Name', [rule({ apply: 'column', field: '' })], fields)).toBeUndefined();
  });

  it('bold 随命中规则一起生效', () => {
    const hit = rule({ apply: 'row', field: 'Enable', op: 'eq', value: false, color: '#FFF7E8', bold: true });
    expect(resolveCellFormat(row, 'Name', [hit], fields)).toEqual({ color: '#FFF7E8', bold: true });
    expect(resolveCardTitleFormat(row, [hit], fields)?.bold).toBe(true);
    expect(resolveCellFormat(row, 'Name', [rule({ apply: 'row', field: 'Enable', op: 'eq', value: false })], fields)?.bold).toBe(false);
  });

  it('resolveRowFormat 仅整行，供勾选/操作列', () => {
    const cellOnly: ViewFormatRule[] = [
      rule({ apply: 'cell', field: 'Enable', op: 'eq', value: false, color: '#111111' }),
    ];
    expect(resolveRowFormat(row, cellOnly, fields)).toBeUndefined();
    const rowRule: ViewFormatRule[] = [
      rule({ apply: 'cell', field: 'Enable', op: 'eq', value: false, color: '#111111' }),
      rule({ apply: 'row', field: 'Enable', op: 'eq', value: false, color: '#ABCDEF', bold: true }),
    ];
    expect(resolveRowFormat(row, rowRule, fields)).toEqual({ color: '#ABCDEF', bold: true });
  });

  it('resolveCardTitleFormatColor 忽略 cell/column/side', () => {
    const rules: ViewFormatRule[] = [
      rule({ apply: 'side', field: 'Enable', op: 'eq', value: false, color: '#111111' }),
      rule({ apply: 'cell', field: 'Enable', op: 'eq', value: false, color: '#222222' }),
      rule({ apply: 'row', field: 'Enable', op: 'eq', value: false, color: '#333333' }),
    ];
    expect(resolveCardTitleFormatColor(row, rules, fields)).toBe('#333333');
  });
});
