import { describe, expect, it } from 'vitest';
import { formatDocumentTitle } from './documentTitle';

describe('formatDocumentTitle', () => {
  it('页面 + 显示名称', () => {
    expect(formatDocumentTitle('系统设置', 'NewLife.Cube')).toBe('系统设置 / NewLife.Cube');
  });

  it('无页面时仅显示名称', () => {
    expect(formatDocumentTitle('', 'NewLife.Cube')).toBe('NewLife.Cube');
    expect(formatDocumentTitle(null, 'CubeDemo')).toBe('CubeDemo');
  });

  it('无显示名称时回落默认', () => {
    expect(formatDocumentTitle('用户', '')).toBe('用户 / 魔方管理平台');
    expect(formatDocumentTitle('用户', null)).toBe('用户 / 魔方管理平台');
  });
});
