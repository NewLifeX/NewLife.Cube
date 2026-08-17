import { describe, expect, it } from 'vitest';
import { avatarInitial } from './avatar';

describe('avatarInitial', () => {
  it('chars=1 中文取首字', () => {
    expect(avatarInitial('管理员')).toBe('管');
    expect(avatarInitial('张三')).toBe('张');
  });

  it('chars=1 英文取首字母大写', () => {
    expect(avatarInitial('admin')).toBe('A');
    expect(avatarInitial('Admin')).toBe('A');
  });

  it('chars=2 中文取末尾两字', () => {
    expect(avatarInitial('管理员', 2)).toBe('理员');
    expect(avatarInitial('张三', 2)).toBe('张三');
  });

  it('chars=2 英文多词取首尾字母', () => {
    expect(avatarInitial('John Smith', 2)).toBe('JS');
  });

  it('chars=2 英文单词取前两字母', () => {
    expect(avatarInitial('admin', 2)).toBe('AD');
  });

  it('空名回落', () => {
    expect(avatarInitial('')).toBe('?');
    expect(avatarInitial(null)).toBe('?');
  });
});
