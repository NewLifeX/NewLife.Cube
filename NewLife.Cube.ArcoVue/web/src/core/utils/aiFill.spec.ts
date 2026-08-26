import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import { mergeFillFormValues, parseFillFormValue } from './aiFill';

function f(partial: Partial<FieldMeta> & { name: string }): FieldMeta {
  return { typeName: 'String', ...partial };
}

describe('parseFillFormValue', () => {
  it('解析 kind/values', () => {
    const v = parseFillFormValue(JSON.stringify({ kind: 'fill_form', values: { Name: '张三' } }));
    expect(v).toEqual({ Name: '张三' });
  });

  it('非法 JSON / 缺 kind 返回 null', () => {
    expect(parseFillFormValue('{')).toBeNull();
    expect(parseFillFormValue(JSON.stringify({ values: { a: 1 } }))).toBeNull();
    expect(parseFillFormValue(JSON.stringify({ kind: 'fill_form', values: [1] }))).toBeNull();
  });
});

describe('mergeFillFormValues', () => {
  const fields: FieldMeta[] = [
    f({ name: 'Id', primaryKey: true }),
    f({ name: 'Name', displayName: '名称' }),
    f({ name: 'Remark', displayName: '备注', readOnly: true }),
    f({ name: 'Enable', displayName: '启用' }),
  ];

  it('只写入可写字段且大小写不敏感', () => {
    const model: Record<string, unknown> = { Id: 1, Name: '', Enable: false };
    const filled = mergeFillFormValues(model, { name: '李四', enable: true, id: 9, remark: 'x' }, fields);
    expect(model.Name).toBe('李四');
    expect(model.Enable).toBe(true);
    expect(model.Id).toBe(1);
    expect(filled).toEqual(['名称', '启用']);
  });

  it('空 values 不改 model', () => {
    const model: Record<string, unknown> = { Name: 'a' };
    expect(mergeFillFormValues(model, null, fields)).toEqual([]);
    expect(model.Name).toBe('a');
  });
});
