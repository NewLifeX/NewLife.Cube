import { describe, expect, it } from 'vitest';
import { avatarInitial } from './avatar';

describe('avatarInitial', () => {
  it('中文取首字', () => {
    expect(avatarInitial('管理员')).toBe('管');
    expect(avatarInitial('张三')).toBe('张');
  });

  it('英文取首字母并大写', () => {
    expect(avatarInitial('admin')).toBe('A');
    expect(avatarInitial('Admin')).toBe('A');
    expect(avatarInitial('test')).toBe('T');
  });

  it('首字符为数字/符号时原样返回', () => {
    expect(avatarInitial('007')).toBe('0');
    expect(avatarInitial('_x')).toBe('_');
  });

  it('空名回落问号', () => {
    expect(avatarInitial('')).toBe('?');
    expect(avatarInitial('   ')).toBe('?');
    expect(avatarInitial(null)).toBe('?');
    expect(avatarInitial(undefined)).toBe('?');
  });

  it('emoji 等代理对按码点取首', () => {
    expect(avatarInitial('😀x')).toBe('😀');
  });
});
