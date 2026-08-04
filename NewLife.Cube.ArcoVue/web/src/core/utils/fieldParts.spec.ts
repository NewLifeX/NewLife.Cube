import { describe, expect, it } from 'vitest';
import { emptyFieldParts, resolveFieldsForKind, type FieldParts } from './fieldParts';
import type { FieldMeta } from '../types/field';

const f = (name: string): FieldMeta => ({ name, typeName: 'String' });

describe('fieldParts', () => {
  it('empty parts fallback', () => {
    const parts = emptyFieldParts();
    expect(resolveFieldsForKind('detail', parts)).toEqual([]);
    expect(resolveFieldsForKind('edit', parts)).toEqual([]);
  });

  it('add falls back to edit; edit falls back to add', () => {
    const parts: FieldParts = {
      ...emptyFieldParts(),
      add: [f('AddField')],
      edit: [f('EditField')],
    };
    expect(resolveFieldsForKind('add', parts).map((x) => x.name)).toEqual(['AddField']);
    expect(resolveFieldsForKind('edit', parts).map((x) => x.name)).toEqual(['EditField']);
    const onlyAdd: FieldParts = { ...emptyFieldParts(), add: [f('A')] };
    expect(resolveFieldsForKind('edit', onlyAdd).map((x) => x.name)).toEqual(['A']);
    const onlyEdit: FieldParts = { ...emptyFieldParts(), edit: [f('E')] };
    expect(resolveFieldsForKind('add', onlyEdit).map((x) => x.name)).toEqual(['E']);
  });

  it('detail falls back edit then list; search/list no fallback', () => {
    const parts: FieldParts = {
      ...emptyFieldParts(),
      edit: [f('Edit')],
      list: [f('List')],
    };
    expect(resolveFieldsForKind('detail', parts).map((x) => x.name)).toEqual(['Edit']);
    const onlyList: FieldParts = { ...emptyFieldParts(), list: [f('List')] };
    expect(resolveFieldsForKind('detail', onlyList).map((x) => x.name)).toEqual(['List']);
    expect(resolveFieldsForKind('search', onlyList)).toEqual([]);
    expect(resolveFieldsForKind('list', onlyList).map((x) => x.name)).toEqual(['List']);
  });

  it('detail prefers detail partition when present', () => {
    const parts: FieldParts = {
      ...emptyFieldParts(),
      detail: [f('D')],
      edit: [f('E')],
      list: [f('L')],
    };
    expect(resolveFieldsForKind('detail', parts).map((x) => x.name)).toEqual(['D']);
  });
});
