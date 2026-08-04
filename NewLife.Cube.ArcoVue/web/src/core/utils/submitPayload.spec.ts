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

  it('normalizes string numbers to native types on submit (OSC-0008)', () => {
    const fields = [
      f({ name: 'RoleId', typeName: 'Int32', nullable: true }),
      f({ name: 'Score', typeName: 'Decimal', nullable: true }),
      f({ name: 'Kind', typeName: 'Enum', nullable: true }),
      f({ name: 'Enabled', typeName: 'Boolean', nullable: false }),
    ];
    const payload = prepareSubmitPayload(
      { RoleId: '1', Score: '12.5', Kind: '2', Enabled: 'true' },
      fields,
      { mode: 'add', pkField: 'Id' },
    );
    expect(payload).toEqual({ RoleId: 1, Score: 12.5, Kind: 2, Enabled: true });
  });

  it('submits empty string for nullable String field (OSC-0008)', () => {
    const fields = [f({ name: 'Remark', typeName: 'String', nullable: true })];
    const payload = prepareSubmitPayload({ Remark: '' }, fields, { mode: 'add', pkField: 'Id' });
    expect(payload).toEqual({ Remark: '' });
  });

  it('keeps boolean false and filters empty numeric (OSC-0008)', () => {
    const fields = [
      f({ name: 'Enabled', typeName: 'Boolean', nullable: false }),
      f({ name: 'Sort', typeName: 'Int32', nullable: true }),
    ];
    const payload = prepareSubmitPayload(
      { Enabled: false, Sort: '' },
      fields,
      { mode: 'add', pkField: 'Id' },
    );
    expect(payload).toEqual({ Enabled: false });
    expect(payload).not.toHaveProperty('Sort');
  });

  it('joins multi-select arrays with comma', () => {
    const fields = [f({ name: 'Tags', typeName: 'String', multiple: true })];
    const payload = prepareSubmitPayload(
      { Tags: ['a', 'b'] },
      fields,
      { mode: 'add', pkField: 'Id' },
    );
    expect(payload).toEqual({ Tags: 'a,b' });
  });
});

describe('isFieldRequired', () => {
  it('matches backend !Nullable for non-pk', () => {
    expect(isFieldRequired(f({ name: 'Code', typeName: 'String', nullable: false }))).toBe(true);
    expect(isFieldRequired(f({ name: 'Remark', typeName: 'String', nullable: true }))).toBe(false);
    expect(isFieldRequired(f({ name: 'Id', typeName: 'Int32', primaryKey: true, nullable: false }))).toBe(false);
  });
});
