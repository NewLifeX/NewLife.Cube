/**
 * 页面呈现配置（preset）纯函数与注册表单测
 */
import { afterEach, describe, expect, it } from 'vitest';
import {
  applyFieldPreset,
  applyPagePreset,
  clearPagePresets,
  getPagePreset,
  normalizePresetPath,
  registerPagePreset,
} from '../pagePreset';
import type { FieldMeta } from '../../types/field';
import type { NormalizedPageFields } from '../types';

const field = (name: string, extra: Partial<FieldMeta> = {}): FieldMeta => ({
  name,
  displayName: name,
  typeName: 'String',
  ...extra,
});

const meta = (): NormalizedPageFields => ({
  setting: { enableAdd: true, enableSelect: true },
  listFields: [field('Id', { primaryKey: true }), field('Name'), field('Remark')],
  searchFields: [field('Name'), field('Enable', { typeName: 'Boolean' })],
  keyword: field('q', { kind: 'keyword' }),
  addFields: [field('Name'), field('Remark')],
  editFields: [field('Name'), field('Remark')],
  detailFields: [field('Name')],
  ui: {},
});

afterEach(() => clearPagePresets());

describe('applyFieldPreset', () => {
  it('应按 remove / patch / order / append 合并字段', () => {
    const result = applyFieldPreset(meta().listFields, {
      remove: ['id'],
      patch: { NAME: { label: '设备名', width: 160, align: 'center', fixed: 'left', renderer: 'tag' } },
      order: ['Remark', 'Name'],
      append: [field('Extra'), field('Name')],
    });

    expect(result.map((item) => item.name)).toEqual(['Remark', 'Name', 'Extra']);
    expect(result[1]).toMatchObject({
      header: '设备名',
      width: 160,
      textAlign: 'center',
      fixed: 'left',
      view: 'tag',
    });
    // 重复追加的 Name 被忽略
    expect(result.filter((item) => item.name === 'Name')).toHaveLength(1);
  });

  it('应保留未列在 order 中的字段并排在后面', () => {
    const result = applyFieldPreset(meta().listFields, { order: ['Remark'] });
    expect(result.map((item) => item.name)).toEqual(['Remark', 'Id', 'Name']);
  });
});

describe('applyPagePreset', () => {
  it('应合并页面设置、列表、表单与搜索补丁', () => {
    const result = applyPagePreset(meta(), {
      setting: { enableAdd: false },
      list: { remove: ['Id'] },
      form: { remove: ['Remark'], columns: 1 },
      search: { remove: ['q', 'Enable'], append: [field('Org')] },
    });

    expect(result.setting.enableAdd).toBe(false);
    expect(result.setting.enableSelect).toBe(true);
    expect(result.listFields.map((item) => item.name)).toEqual(['Name', 'Remark']);
    expect(result.addFields.map((item) => item.name)).toEqual(['Name']);
    expect(result.editFields.map((item) => item.name)).toEqual(['Name']);
    expect(result.keyword).toBeUndefined();
    expect(result.searchFields.map((item) => item.name)).toEqual(['Name', 'Org']);
  });

  it('应支持追加 keyword 查询项', () => {
    const result = applyPagePreset({ ...meta(), keyword: undefined }, {
      search: { append: [field('Q', { kind: 'keyword' })] },
    });
    expect(result.keyword?.name).toBe('Q');
  });

  it('preset 为空时原样返回', () => {
    const input = meta();
    expect(applyPagePreset(input, null)).toBe(input);
  });
});

describe('preset 注册表', () => {
  it('应按归一化路径查找', () => {
    registerPagePreset('/Admin/User/', { form: { columns: 1 } });
    expect(getPagePreset('/admin/user')?.form?.columns).toBe(1);
    expect(getPagePreset('admin/user/')?.form?.columns).toBe(1);
    expect(getPagePreset('/Admin/Role')).toBeNull();
  });

  it('应归一化空路径与尾斜杠', () => {
    expect(normalizePresetPath('')).toBe('');
    expect(normalizePresetPath('/')).toBe('/');
    expect(normalizePresetPath('Admin/User///')).toBe('/admin/user');
  });

  it('清空后不再命中', () => {
    registerPagePreset('/Admin/User', { form: { columns: 2 } });
    clearPagePresets();
    expect(getPagePreset('/Admin/User')).toBeNull();
  });
});
