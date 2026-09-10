import { describe, expect, it, vi } from 'vitest';

vi.mock('@/api', () => ({
  default: { automation: { recipients: vi.fn() }, page: { getList: vi.fn() } },
}));

import { normalizeRecipient, normalizeSelectIds } from './recipient';

describe('normalizeSelectIds 单选标量/多选数组', () => {
  it('标量与数组都归一成正整数列表', () => {
    expect(normalizeSelectIds(8)).toEqual([8]);
    expect(normalizeSelectIds([8, 9])).toEqual([8, 9]);
    expect(normalizeSelectIds(null)).toEqual([]);
    expect(normalizeSelectIds('')).toEqual([]);
    expect(normalizeSelectIds(0)).toEqual([]);
  });
});

describe('normalizeRecipient 接收人归一', () => {
  it('Pascal/camel/Id/iD 兼容', () => {
    expect(normalizeRecipient({ id: 5, name: 'x', displayName: '张三' })).toEqual({
      id: 5,
      name: 'x',
      displayName: '张三',
    });
    expect(normalizeRecipient({ Id: 6, Name: 'y' })).toEqual({ id: 6, name: 'y', displayName: 'y' });
    expect(normalizeRecipient({ iD: 7, fullName: '李四' })).toEqual({
      id: 7,
      name: '',
      displayName: '李四',
    });
  });

  it('非法 id 返回 null', () => {
    expect(normalizeRecipient({ id: 0, name: 'a' })).toBeNull();
    expect(normalizeRecipient({ id: 'x', name: 'a' })).toBeNull();
    expect(normalizeRecipient({ name: 'a' })).toBeNull();
  });
});
