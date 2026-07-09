import { type TreeMenuItem } from '@newlifex/cube-vue/core/stores/menu';

/**
 * 判断 childMenu 的祖先是否是parentMenu
 * @param childMenu 子菜单
 * @param parentMenu 父菜单
 * @returns 是否为子菜单
 */
export function isChildMenu(childMenu?: TreeMenuItem, parentMenu?: TreeMenuItem): boolean {
  if (!childMenu || !parentMenu) {
    return false;
  }

  if (parentMenu.id === childMenu.id) {
    return true;
  } else if (childMenu.parentMenu) {
    return isChildMenu(childMenu.parentMenu, parentMenu);
  }

  return false;
}

/**
 * 获取菜单的标题
 * @param menu 菜单项
 * @returns 菜单标题
 */
export function renderMenuTitle(menu: TreeMenuItem): string {
  const lastPathSegment = menu.path.split('/').filter(Boolean).pop() || '...';
  return menu.title || menu.name || lastPathSegment;
}

/**
 * 检查菜单是否有子菜单
 * @param menu 菜单项
 * @returns 是否有子菜单
 */
export function hasChildren(menu?: TreeMenuItem): boolean {
  return Boolean(menu?.children && menu.children.length > 0);
}

/**
 * 获取顶层菜单
 * @param menu 菜单项
 * @returns 顶层菜单
 */
export function getTopLevelMenu(menu: TreeMenuItem): TreeMenuItem {
  if (menu.parentMenu) {
    return getTopLevelMenu(menu.parentMenu);
  }
  return menu;
}
