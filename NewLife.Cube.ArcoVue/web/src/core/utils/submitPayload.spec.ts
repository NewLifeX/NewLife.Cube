import { describe, expect, it } from 'vitest';
import { isFieldRequired, prepareSubmitPayload } from './submitPayload';
import type { FieldMeta } from '../types/field';

const f = (partial: Partial<FieldMeta> & Pick<FieldMeta, 'name' | 'typeName'>): FieldMeta => ({
  ...partial,
  name: partial.name,
  typeName: partial.typeName,
});

describe('prepareSubmitPayload', () => {
  it('strips identity pk and empty numerics on add', () => {
    const fields = [
      f({ name: 'Id', typeName: 'Int32', primaryKey: true }),
      f({ name: 'Name', typeName: 'String', nullable: false }),
      f({ name: 'Sort', typeName: 'Int32', nullable: true }),
      f({ name: 'Code', typeName: 'String', nullable: false }),
    ];
    const payload = prepareSubmitPayload(
      { Id: 0, Name: '信息中心', Sort: '', Code: 'IC', Remark: '' },
      fields,
      { mode: 'add', pkField: 'Id' },
    );
    expect(payload).toEqual({ Name: '信息中心', Code: 'IC' });
    expect(payload).not.toHaveProperty('Id');
    expect(payload).not.toHaveProperty('Sort');
  });

  it('keeps pk on edit', () => {
    const fields = [f({ name: 'Id', typeName: 'Int32', primaryKey: true }), f({ name: 'Name', typeName: 'String' })];
    const payload = prepareSubmitPayload(
      { Id: 3, Name: 'x' },
      fields,
      { mode: 'edit', pkField: 'Id' },
    );
    expect(payload).toEqual({ Id: 3, Name: 'x' });
  });
});

describe('isFieldRequired', () => {
  it('matches backend !Nullable for non-pk', () => {
    expect(isFieldRequired(f({ name: 'Code', typeName: 'String', nullable: false }))).toBe(true);
    expect(isFieldRequired(f({ name: 'Remark', typeName: 'String', nullable: true }))).toBe(false);
    expect(isFieldRequired(f({ name: 'Id', typeName: 'Int32', primaryKey: true, nullable: false }))).toBe(false);
  });
});
