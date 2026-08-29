/**
 * 菜单 Store（对齐 Vue 皮肤 stores/menu.ts）
 *
 * 维护菜单树 ↔ 扁平列表转换、当前激活菜单、菜单路径匹配。
 * 原始菜单树数据由 @cube/auth-logic 的 user store 提供（fetchMenus → /Cube/MenuTree）。
 */
import { create } from 'zustand';
import type { MenuItem } from '@cube/api-core';
import { getConfig } from '@/configure';

/** 扁平菜单项 */
export interface FlatMenuItem {
  id: string | number;
  name: string;
  path: string;
  title?: string;
  icon?: string;
  parentId?: string | number | null;
  visible?: boolean;
  children?: FlatMenuItem[];
}

const { menu: menuCfg } = getConfig();

/** 树形菜单 → 扁平列表 */
export function flattenMenu(tree: MenuItem[], parentId: string | number | null = null): FlatMenuItem[] {
  const result: FlatMenuItem[] = [];
  const { visibleField } = menuCfg;
  for (const item of tree) {
    // 可见性过滤
    if (visibleField && (item as unknown as Record<string, unknown>)[visibleField] === false) continue;
    const flat: FlatMenuItem = {
      id: item.id,
      name: item.name,
      path: item.url ?? '',
      title: item.displayName,
      icon: item.icon,
      parentId,
      visible: item.visible,
    };
    result.push(flat);
    if (item.children?.length) {
      const kids = flattenMenu(item.children, item.id);
      if (kids.length) flat.children = kids;
      result.push(...kids);
    }
  }
  return result;
}

/** 按路径在扁平菜单中查找（大小写不敏感，endsWith 匹配） */
function matchMenu(menus: FlatMenuItem[], path: string): FlatMenuItem | undefined {
  const lower = path.toLowerCase();
  return menus.find((m) => m.path && lower.endsWith(m.path.toLowerCase()));
}

/** 按路径解析菜单标题：命中返回菜单名，未命中返回 fallback */
export function resolveMenuTitle(menus: FlatMenuItem[], path: string, fallback = ''): string {
  const m = matchMenu(menus, path);
  return m?.title || m?.name || fallback;
}

interface MenuState {
  /** 扁平菜单 */
  flatMenus: FlatMenuItem[];
  /** 当前激活路径 */
  activePath: string;
  /** 设置菜单 */
  setFlatMenus: (menus: MenuItem[]) => void;
  /** 设置激活路径 */
  setActivePath: (path: string) => void;
  /** 按路径查找菜单（含子菜单） */
  findMenu: (path: string) => FlatMenuItem | undefined;
  /** 重置 */
  reset: () => void;
}

export const useMenuStore = create<MenuState>((set, get) => ({
  flatMenus: [],
  activePath: '',

  setFlatMenus: (menus: MenuItem[]) => {
    set({ flatMenus: flattenMenu(menus) });
  },

  setActivePath: (path: string) => set({ activePath: path }),

  findMenu: (path: string) => matchMenu(get().flatMenus, path),

  reset: () => set({ flatMenus: [], activePath: '' }),
}));

export default useMenuStore;
