import { describe, expect, it } from 'vitest';
import { ICON_COMPONENTS } from './iconComponents';
import type { FieldMeta } from '@/core/types/field';
import {
  APPEARANCE_ICONS,
  DEFAULT_MENU_ICON,
  FA_ICON_MAP,
  VIEW_KIND_ICONS,
  fieldIcon,
  menuIcon,
} from './iconRegistry';

/** 图标名是否已按需登记组件（iconComponents.ts 唯一引入点） */
function isIconValid(type: string): boolean {
  return type in ICON_COMPONENTS;
}

function f(partial: Partial<FieldMeta> & { name: string }): FieldMeta {
  return {
    typeName: 'String',
    ...partial,
  };
}

/** 收集全部注册图标名并断言均已在 iconComponents.ts 登记组件 */
function assertAllValid(types: string[]) {
  for (const t of types) {
    expect(isIconValid(t), `未登记组件: ${t}`).toBe(true);
  }
}

describe('VIEW_KIND_ICONS', () => {
  it('覆盖 6 个 ViewKind 且值非空且有效', () => {
    const kinds = ['table', 'tree', 'card', 'kanban', 'calendar', 'gantt'];
    expect(Object.keys(VIEW_KIND_ICONS).sort()).toEqual([...kinds].sort());
    assertAllValid(Object.values(VIEW_KIND_ICONS));
  });

  it('6 视图图标各不相同', () => {
    expect(new Set(Object.values(VIEW_KIND_ICONS)).size).toBe(6);
  });
});

describe('APPEARANCE_ICONS', () => {
  it('覆盖 light/dark/system 且有效', () => {
    expect(Object.keys(APPEARANCE_ICONS).sort()).toEqual(['dark', 'light', 'system']);
    assertAllValid(Object.values(APPEARANCE_ICONS));
  });
});

describe('fieldIcon', () => {
  it('itemType 特殊字段', () => {
    expect(fieldIcon(f({ name: 'Pic', itemType: 'image' }))).toBe('pic');
    expect(fieldIcon(f({ name: 'Avatar', itemType: 'avatar' }))).toBe('pic');
    expect(fieldIcon(f({ name: 'F', itemType: 'file' }))).toBe('file-text');
    expect(fieldIcon(f({ name: 'F', itemType: 'attachment' }))).toBe('file-text');
    expect(fieldIcon(f({ name: 'U', itemType: 'url' }))).toBe('link');
    expect(fieldIcon(f({ name: 'M', itemType: 'mail' }))).toBe('mail');
    expect(fieldIcon(f({ name: 'E', itemType: 'email' }))).toBe('mail');
    expect(fieldIcon(f({ name: 'P', itemType: 'mobile' }))).toBe('phone');
    expect(fieldIcon(f({ name: 'P', itemType: 'phone' }))).toBe('phone');
  });

  it('typeName 常规类型', () => {
    expect(fieldIcon(f({ name: 'B', typeName: 'Boolean' }))).toBe('switch');
    expect(fieldIcon(f({ name: 'D', typeName: 'DateTime' }))).toBe('time');
    expect(fieldIcon(f({ name: 'D', typeName: 'Date' }))).toBe('time');
    expect(fieldIcon(f({ name: 'T', typeName: 'TimeSpan' }))).toBe('time');
    expect(fieldIcon(f({ name: 'I', typeName: 'Int32' }))).toBe('list-numbers');
    expect(fieldIcon(f({ name: 'D', typeName: 'Double' }))).toBe('list-numbers');
    expect(fieldIcon(f({ name: 'E', typeName: 'Enum' }))).toBe('tag');
    expect(fieldIcon(f({ name: 'G', typeName: 'Guid' }))).toBe('key');
  });

  it('Map 外键（lovCode 非 Enum. 前缀）→ link；枚举 lovCode → tag', () => {
    expect(fieldIcon(f({ name: 'DeptId', typeName: 'Int32', lovCode: 'List.Dept' }))).toBe('link');
    expect(fieldIcon(f({ name: 'Kind', typeName: 'Int32', lovCode: 'Enum.Kind' }))).toBe('tag');
  });

  it('primaryKey → key', () => {
    expect(fieldIcon(f({ name: 'Id', typeName: 'Int32', primaryKey: true }))).toBe('key');
  });

  it('默认 String → font-size，且所有分支图标有效', () => {
    expect(fieldIcon(f({ name: 'Name' }))).toBe('font-size');
    const all = Object.values(VIEW_KIND_ICONS).concat(Object.values(APPEARANCE_ICONS));
    assertAllValid(all.concat(['pic', 'file-text', 'link', 'mail', 'phone', 'switch', 'time', 'list-numbers', 'tag', 'key', 'font-size']));
  });
});

describe('ICON_COMPONENTS 覆盖', () => {
  it('视图/外观/字段/菜单/工具栏全部注册图标名均已登记组件', () => {
    const all = new Set<string>();
    Object.values(VIEW_KIND_ICONS).forEach((x) => all.add(x));
    Object.values(APPEARANCE_ICONS).forEach((x) => all.add(x));
    Object.values(FA_ICON_MAP).forEach((x) => all.add(x));
    all.add(DEFAULT_MENU_ICON);
    // 字段图标分支
    [
      'pic',
      'file-text',
      'link',
      'mail',
      'phone',
      'switch',
      'time',
      'tag',
      'list-numbers',
      'key',
      'font-size',
    ].forEach((x) => all.add(x));
    // 工具栏 / 视图配置硬编码图标
    [
      'filter',
      'background-color',
      'connection-box',
      'search',
      'more',
      'more-one',
      'download',
      'export',
      'delete',
      'layout-one',
      'down',
      'up',
      'refresh',
      'remind',
      'check',
      'save',
      'edit',
      'copy',
      'undo',
      'menu-fold',
      'menu-unfold',
      'close',
      'info',
      'drag',
      'preview-open',
      'preview-close',
      'left-bar',
      'right-bar',
      'lightning',
      'robot-one',
      'send',
      'file-addition',
      'plus',
      'full-screen',
      'off-screen',
    ].forEach((x) => all.add(x));
    // 产品命名专用
    all.add('cube-three');
    assertAllValid([...all]);
  });
});

describe('FA_ICON_MAP + menuIcon', () => {
  it('FA_ICON_MAP 全部图标名有效', () => {
    assertAllValid(Object.values(FA_ICON_MAP));
  });

  it('菜单显示名精确匹配优先（魔方管理 → cube-three）', () => {
    // fa-tachometer 虽映射 dashboard，但显示名「魔方管理」命中 MENU_NAME_ICONS 优先
    expect(menuIcon({ icon: 'fa-tachometer', displayName: '魔方管理', name: 'Cube' })).toBe('cube-three');
  });

  it('FA_ICON_MAP 命中（含 fa- 前缀）', () => {
    expect(menuIcon({ icon: 'fa-user', displayName: '用户', name: 'User' })).toBe('people');
    expect(menuIcon({ icon: 'fa-table', displayName: '表格', name: 'Table' })).toBe('list-checkbox');
  });

  it('FA_ICON_MAP 命中（无 fa- 前缀）', () => {
    expect(menuIcon({ icon: 'list', displayName: '列表', name: 'List' })).toBe('list');
    expect(menuIcon({ icon: 'grid', displayName: '网格', name: 'Grid' })).toBe('grid-four');
  });

  it('未命中走名称关键词兜底', () => {
    expect(menuIcon({ icon: 'fa-unknown', displayName: '系统日志', name: 'Log' })).toBe('history');
    expect(menuIcon({ icon: 'fa-unknown', displayName: '用户管理', name: 'UserMgr' })).toBe('people');
    expect(menuIcon({ name: '设置中心' })).toBe('setting');
  });

  it('关键词也未命中走默认兜底（app）', () => {
    expect(menuIcon({ icon: 'fa-zzz', displayName: '神秘模块', name: 'Xyz' })).toBe(DEFAULT_MENU_ICON);
    expect(menuIcon({ name: 'Abc' })).toBe(DEFAULT_MENU_ICON);
  });

  it('icon 为空时走关键词/默认，且默认图标有效', () => {
    expect(isIconValid(DEFAULT_MENU_ICON)).toBe(true);
    expect(menuIcon({ displayName: '菜单', name: 'Menu' })).toBe('list');
  });
});
