import { describe, expect, it, vi } from 'vitest';

vi.mock('@/api', () => ({
  default: {
    page: {
      getPage: vi.fn(async () => ({
        data: {
          list: [{ Name: 'RoleId' }, { Name: 'Amount' }],
          search: [{ Name: 'Amount' }],
        },
      })),
      getFields: vi.fn(async () => ({ data: [] })),
    },
    automation: {
      meta: vi.fn(async () => ({
        data: [
          { Name: 'RoleId', DisplayName: '角色' },
          { Name: 'Amount', DisplayName: '金额' },
          { Name: 'SecretKey', DisplayName: '密钥' },
          { Name: 'InternalFlag', DisplayName: '内部标记' },
        ],
      })),
    },
  },
}));

vi.mock('@/core/utils/lov-api', () => ({
  enrichFieldsWithEnumDataSource: vi.fn(async () => undefined),
  enrichFieldsWithLookup: vi.fn(async () => undefined),
}));

import { loadEntityFilterFields } from './listFieldMeta';

describe('loadEntityFilterFields 候选与后端白名单对齐（OSC-260903e2a4）', () => {
  it('AutomationMeta 全字段仅富化、不扩充候选', async () => {
    const fields = await loadEntityFilterFields('Admin/OscDemo');
    expect(fields.map((f) => f.name).sort()).toEqual(['Amount', 'RoleId']);
    expect(fields.find((f) => f.name === 'RoleId')?.displayName).toBe('角色');
    expect(fields.map((f) => f.name)).not.toContain('SecretKey');
    expect(fields.map((f) => f.name)).not.toContain('InternalFlag');
  });
});
