/**
 * 列设置组件单测：可见字段过滤 + 列配置持久化 payload
 */
import { describe, expect, it } from 'vitest';
import { filterVisibleFields } from '../ColumnSetting';
import type { FieldMapping } from '@newlifex/field-mapping';

/** 构造一个 FieldMapping */
function mk(name: string, visible?: boolean): FieldMapping {
  return {
    field: {
      name,
      displayName: name,
      typeName: 'String',
      ...(visible !== undefined ? { visible } : {}),
    },
  } as unknown as FieldMapping;
}

/** 构造列配置 payload（本地状态 → 提交结构） */
export function buildColumnPayload(order: string[], hidden: string[]): Record<string, unknown> {
  return { listOrder: order, listHidden: hidden };
}

describe('列设置', () => {
  it('filterVisibleFields 过滤 visible=false 的字段', () => {
    const fields = [mk('Id', true), mk('Name'), mk('Secret', false), mk('Remark', true)];
    const out = filterVisibleFields(fields).map((f) => f.field.name);
    expect(out).toEqual(['Id', 'Name', 'Remark']);
  });

  it('filterVisibleFields 保留未标记 visible 的字段（默认可见）', () => {
    const fields = [mk('Name'), mk('Code', false)];
    const out = filterVisibleFields(fields).map((f) => f.field.name);
    expect(out).toEqual(['Name']);
  });

  it('filterVisibleFields 空数组安全', () => {
    expect(filterVisibleFields([])).toEqual([]);
  });

  it('列配置 payload 结构（listOrder + listHidden）', () => {
    expect(buildColumnPayload(['Id', 'Name'], ['Secret'])).toEqual({
      listOrder: ['Id', 'Name'],
      listHidden: ['Secret'],
    });
    // 恢复默认：空数组
    expect(buildColumnPayload([], [])).toEqual({ listOrder: [], listHidden: [] });
  });
});
