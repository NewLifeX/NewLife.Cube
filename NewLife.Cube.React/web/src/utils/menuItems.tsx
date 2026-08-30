/**
 * 菜单构建工具：后端菜单树 → antd Menu items
 */
import type { MenuProps } from 'antd';
import type { MenuItem } from '@newlifex/api-core';
import { resolveIcon } from '@/utils/icon';

export type MenuItemType = Required<MenuProps>['items'][number];

/**
 * 是否为后端区域根菜单（`~` / `~/Area`）——仅指后端 API 区域，前端无对应页面，不可导航
 */
function isAreaRootUrl(url?: string): boolean {
  return !!url && (url === '~' || url.startsWith('~/'));
}

/**
 * 将后端菜单树转为 antd Menu items（自动映射图标、过滤不可见与后端区域根菜单）
 *
 * @param menus 后端菜单树（/Cube/MenuTree）
 * @returns antd Menu items
 */
export function buildMenuItems(menus: MenuItem[]): MenuItemType[] {
  const result: MenuItemType[] = [];
  for (const menu of menus) {
    if (menu.visible === false) continue;
    // 后端区域根菜单（~/Ai、~/Cube 等）前端无页面，直接跳过
    if (isAreaRootUrl(menu.url)) continue;
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
