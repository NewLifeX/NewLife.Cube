/**
 * 菜单构建工具：后端菜单树 → antd Menu items
 */
import type { MenuProps } from 'antd';
import type { MenuItem } from '@cube/api-core';
import { resolveIcon } from '@/utils/icon';

export type MenuItemType = Required<MenuProps>['items'][number];

/**
 * 将后端菜单树转为 antd Menu items（自动映射图标、过滤不可见）
 *
 * @param menus 后端菜单树（/Cube/MenuTree）
 * @returns antd Menu items
 */
export function buildMenuItems(menus: MenuItem[]): MenuItemType[] {
  const result: MenuItemType[] = [];
  for (const menu of menus) {
    if (menu.visible === false) continue;
    const children = menu.children?.length ? buildMenuItems(menu.children) : undefined;
    const item: MenuItemType = {
      key: menu.url || `menu-${menu.id}`,
      icon: resolveIcon(menu.icon),
      label: menu.displayName || menu.name,
      children,
    };
    result.push(item);
  }
  return result;
}

export default buildMenuItems;
