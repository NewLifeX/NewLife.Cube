import { describe, expect, it } from 'vitest';
import { selectListColumns } from './listColumns';
import type { FieldMeta } from '../types/field';

describe('selectListColumns', () => {
  it('keeps GetPage list fields even when visible is false (backend default)', () => {
    const fields: FieldMeta[] = [
      { name: 'Id', typeName: 'Int32', visible: false, displayName: '编号' },
      { name: 'Name', typeName: 'String', visible: false, displayName: '名称' },
      { name: '', typeName: 'String', visible: true },
    ];
    const cols = selectListColumns(fields);
    expect(cols.map((c) => c.name)).toEqual(['Id', 'Name']);
  });

  it('excludes synthetic Url/dataAction ops link columns (OSC-2608178bdb)', () => {
    const fields: FieldMeta[] = [
      { name: 'Name', typeName: 'String', hasTypeName: true, url: '/d?id={Id}' },
      { name: 'Log', typeName: 'String', hasTypeName: false, url: '/log?id={Id}' },
      { name: 'Run', typeName: 'String', hasTypeName: false, url: '/run', dataAction: 'action' },
    ];
    expect(selectListColumns(fields).map((c) => c.name)).toEqual(['Name']);
  });
});
