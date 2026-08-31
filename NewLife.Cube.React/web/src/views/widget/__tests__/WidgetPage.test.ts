/**
 * WidgetPage 工作台部件页工具函数单元测试
 *
 * 覆盖：部件可见性文本（对齐 MVC Index.cshtml：仅管理员 / 所有用户 / 权限列表）。
 */
import { describe, expect, it } from 'vitest';
import { visibleText, type WidgetItem } from '../WidgetPage';

function w(partial: Partial<WidgetItem>): WidgetItem {
  return { name: 'x', title: 'x', enable: true, ...partial };
}

describe('visibleText 部件可见性文本', () => {
  it('AdminOnly → 仅管理员', () => {
    expect(visibleText(w({ adminOnly: true }))).toBe('仅管理员');
    expect(visibleText(w({ adminOnly: true, permission: '管理员' }))).toBe('仅管理员');
  });

  it('有权限 → 权限列表，无权限 → 所有用户', () => {
    expect(visibleText(w({ adminOnly: false, permission: '管理员,运营' }))).toBe('管理员,运营');
    expect(visibleText(w({ adminOnly: false, permission: '' }))).toBe('所有用户');
    expect(visibleText(w({ adminOnly: false }))).toBe('所有用户');
  });
});
