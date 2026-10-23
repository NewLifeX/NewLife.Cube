/**
 * meta 归一测试：服务端原始 form/add 与 api-core 展开后的 editForm/addForm 两种结构
 */
import { describe, expect, it } from 'vitest';
import { normalizePageMeta } from '../utils/fieldMeta';
import type { BackendField } from '../engine/types';

const field = (name: string, extra: Partial<BackendField> = {}): BackendField => ({
  name,
  displayName: name,
  typeName: 'String',
  ...extra,
});

describe('normalizePageMeta', () => {
  it('应兼容服务端原始 form/add/detail 结构', () => {
    const meta = normalizePageMeta({
      form: [field('Name'), field('Remark')],
      add: [field('Name')],
      detail: [field('Name'), field('Remark')],
      search: [{ ...field('q'), kind: 'keyword' }],
      ui: { density: 'compact' },
    });

    expect(meta.editFields.map((item) => item.name)).toEqual(['Name', 'Remark']);
    expect(meta.addFields.map((item) => item.name)).toEqual(['Name']);
    expect(meta.detailFields.map((item) => item.name)).toEqual(['Name', 'Remark']);
    expect(meta.keyword?.name).toBe('q');
    expect(meta.ui.density).toBe('compact');
  });

  it('add/detail 省略时回退 form', () => {
    const meta = normalizePageMeta({ form: [field('Name')] });
    expect(meta.addFields.map((item) => item.name)).toEqual(['Name']);
    expect(meta.detailFields.map((item) => item.name)).toEqual(['Name']);
  });

  it('应兼容 api-core 展开后的 addForm/editForm 结构', () => {
    const meta = normalizePageMeta({
      addForm: [field('Name')],
      editForm: [field('Name'), field('Enable', { typeName: 'Boolean' })],
    });

    expect(meta.addFields.map((item) => item.name)).toEqual(['Name']);
    expect(meta.editFields.map((item) => item.name)).toEqual(['Name', 'Enable']);
  });
});
