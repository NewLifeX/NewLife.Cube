/**
 * DbPage 数据库管理页工具函数单元测试
 *
 * 覆盖：连接字符串密码隐藏（对齐 MVC ProtectedKey.Hide：password/pass/pwd 键值替换为 {***}）。
 */
import { describe, expect, it } from 'vitest';
import { hideConnSecret } from '../DbPage';

describe('hideConnSecret 连接字符串密码隐藏', () => {
  it('隐藏 Password 键的值（保留其余键值对）', () => {
    expect(hideConnSecret('Data Source=..;Password=123456;User ID=admin')).toBe(
      'Data Source=..;Password={***};User ID=admin',
    );
  });

  it('大小写不敏感匹配 password/pass/pwd', () => {
    expect(hideConnSecret('Server=x;PASSWORD=abc')).toBe('Server=x;PASSWORD={***}');
    expect(hideConnSecret('Server=x;pass=abc;Data=1')).toBe('Server=x;pass={***};Data=1');
    expect(hideConnSecret('Server=x;Pwd=abc')).toBe('Server=x;Pwd={***}');
  });

  it('无密码键时原样返回', () => {
    expect(hideConnSecret('Data Source=..\\Data\\Cube.db;provider=sqlite')).toBe(
      'Data Source=..\\Data\\Cube.db;provider=sqlite',
    );
  });

  it('空值 / 无等号片段安全处理', () => {
    expect(hideConnSecret('')).toBe('');
    expect(hideConnSecret('Server=x;Data Source=..;')).toBe('Server=x;Data Source=..;');
  });
});
