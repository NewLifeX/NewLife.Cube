import { describe, expect, it } from 'vitest';
import type { FieldMeta } from '@/core/types/field';
import { ensureCurrentRoleOption } from './defaultRoleField';

describe('ensureCurrentRoleOption', () => {
  it('merges current DefaultRole into dataSource', () => {
    const field = {
      name: 'DefaultRole',
      typeName: 'String',
      dataSource: { 普通用户: '普通用户' },
    } as FieldMeta;
    ensureCurrentRoleOption(field, '访客');
    expect(field.dataSource?.['访客']).toBe('访客');
    expect(field.dataSource?.['普通用户']).toBe('普通用户');
  });
});
