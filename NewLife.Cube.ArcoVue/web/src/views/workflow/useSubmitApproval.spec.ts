import { describe, expect, it } from 'vitest';
import { normalizeWfTypePath, wfDefinitionIdOf } from './useSubmitApproval';

describe('normalizeWfTypePath（提交审批 TypePath 对齐）', () => {
  it('去掉列表页前导 /，与定义 Admin/Department 对齐', () => {
    expect(normalizeWfTypePath('/Admin/Department')).toBe('Admin/Department');
    expect(normalizeWfTypePath('Admin/Department')).toBe('Admin/Department');
    expect(normalizeWfTypePath('///Admin/Department')).toBe('Admin/Department');
  });

  it('空/空白安全', () => {
    expect(normalizeWfTypePath('')).toBe('');
    expect(normalizeWfTypePath(null)).toBe('');
    expect(normalizeWfTypePath('  /Admin/User  ')).toBe('Admin/User');
  });
});

describe('wfDefinitionIdOf（雪花 Id 禁止 Number 丢精度）', () => {
  it('字符串原样保留，超 MAX_SAFE_INTEGER 不经 Number', () => {
    const sid = '7501276170837360640';
    expect(wfDefinitionIdOf(sid)).toBe(sid);
    // Number 会丢精度 → 不能走这条路径
    expect(String(Number(sid))).not.toBe(sid);
  });

  it('空值安全', () => {
    expect(wfDefinitionIdOf(null)).toBe('');
    expect(wfDefinitionIdOf(undefined)).toBe('');
    expect(wfDefinitionIdOf('')).toBe('');
  });
});
