import { defineStore } from 'pinia';
import { getConfig } from 'cube-front/core/configure';
import { getDataByKey } from '../utils/common';
import { type AxiosRequestConfig } from 'axios';
import request from '../utils/request';

export interface FlatMenuItem {
  id: string;
  name: string;
  path: string;
  title?: string;
  icon?: string;
  sort?: number;
  parentId?: string | null;
  active?: boolean;
}

export interface TreeMenuItem {
  id: string;
  name: string;
  path: string;
  title?: string;
  icon?: string;
  sort?: number;
  parentId?: string | null;
  parentMenu?: TreeMenuItem;
  children?: TreeMenuItem[];
  active?: boolean;
}

/** 平铺菜单转换成树形菜单 */
export function convertFlatMenuToTreeMenu(flatMenu: FlatMenuItem[]): TreeMenuItem[] {
  const menuMap: { [id: string]: TreeMenuItem } = {};

  // Create a map of menu items using their IDs as keys
  flatMenu.forEach((item) => {
    menuMap[item.id] = item;
  });

  const treeMenu: TreeMenuItem[] = [];

  // Iterate over the flat menu items and build the tree menu
  flatMenu.forEach((item) => {
    const menuItem = menuMap[item.id];

    if (item.parentId) {
      const parentItem = menuMap[item.parentId];
      if (parentItem) {
        parentItem.children = parentItem.children || [];
        parentItem.children.push(menuItem);
        menuItem.parentMenu = parentItem;
      }
    } else {
      treeMenu.push(menuItem);
    }
  });

  return treeMenu;
}

/** 树形菜单转换成平铺菜单 */
export function convertTreeMenuToFlatMenu(
  treeMenuList: TreeMenuItem[],
  parentMenu?: TreeMenuItem,
): FlatMenuItem[] {
  const flatMenu: FlatMenuItem[] = [];

  const flatten = (menuList: TreeMenuItem[], parentMenu2?: TreeMenuItem) => {
    menuList.forEach((item) => {
      item.parentMenu = parentMenu2;
      flatMenu.push(item);
      if (item.children) {
        flatten(item.children, item);
      }
    });
  };

  flatten(treeMenuList, parentMenu);

  return flatMenu;
}

/** 将菜单数据按照配置的字段名称进行转换，支持平铺数据和树形数据 */
const covertMenu = (list: Record<string, unknown>[]): TreeMenuItem[] => {
  const menuConfig = getConfig().menu;

  const convertItem = (item: Record<string, unknown>): TreeMenuItem => {
    const children = item[menuConfig.childrenField] as Record<string, unknown>[] | undefined;
    return {
      id: item[menuConfig.idField] as string,
      parentId: item[menuConfig.parentField] as string | null,
      name: item[menuConfig.nameField] as string,
      path: item[menuConfig.pathField] as string,
      title: item[menuConfig.titleField] as string,
      icon: item[menuConfig.iconField] as string,
      sort: item[menuConfig.sortField] as number,
      children: children ? covertMenu(children) : undefined,
    };
  };

  return list.map(convertItem);
};

const state: {
  /** 平铺菜单  */
  flatMenus: Array<FlatMenuItem> | undefined;
  /** 树形菜单 */
  treeMenus: Array<TreeMenuItem> | undefined;
  activeMenu: TreeMenuItem | undefined;
  loading: boolean;
  /** 菜单路由是否已注册 */
  routesRegistered: boolean;
} = {
  flatMenus: undefined,
  treeMenus: undefined,
  activeMenu: undefined,
  loading: false,
  routesRegistered: false,
};

const getTopLevelMenu = (menu: TreeMenuItem) => {
  if (menu.parentMenu) {
    return getTopLevelMenu(menu.parentMenu);
  }
  return menu;
};

export const useMenuStore = defineStore('menu', {
  state: () => state,
  getters: {
    hasMenus: (state) =>
      state.flatMenus &&
      state.treeMenus &&
      state.flatMenus.length > 0 &&
      state.treeMenus.length > 0,
    topLevelActiveMenu: (state) => {
      return state.activeMenu && getTopLevelMenu(state.activeMenu);
    },
  },
  actions: {
    setFlatMenus(data: Array<FlatMenuItem>) {
      this.flatMenus = data;
      this.treeMenus = convertFlatMenuToTreeMenu(data);
    },
    setTreeMenus(data: Array<TreeMenuItem>) {
      this.treeMenus = data;
      this.flatMenus = convertTreeMenuToFlatMenu(data);
    },
    setActiveMenu(menu: TreeMenuItem) {
      this.activeMenu = menu;
    },
    setActiveMenuByPath(path: string) {
      const activeMenu = this.flatMenus?.find((item) => item.path === path);
      this.setActiveMenu(activeMenu as TreeMenuItem);
    },
    /** 标记路由已注册 */
    markRoutesRegistered() {
      this.routesRegistered = true;
    },
    /** 重置路由注册状态（用于重新登录时） */
    resetRoutesRegistered() {
      this.routesRegistered = false;
    },
    async fetchMenuAsync() {
      if (this.loading) return;
      this.loading = true;
      // 如果没有菜单信息，则获取菜单信息
      if (!this.hasMenus) {
        try {
          const config = getConfig();
          const menuConfig = config.menu;
          const getMenuAxiosConfig = menuConfig.getMenuAxiosConfig;

          // 处理不同类型的 getMenuAxiosConfig
          const processMenuRequestAsync = async (requestConfig: AxiosRequestConfig) => {
            try {
              const res = await request(requestConfig);
              if (!res) {
                throw new Error('获取菜单数据失败');
              }

              const data = getDataByKey(res as never, menuConfig.dataKey);

              if (!data) {
                throw new Error('获取菜单数据失败');
              }

              if (menuConfig.isMenuTree) {
                const convertedData = covertMenu(data as Record<string, unknown>[]);
                this.setTreeMenus(convertedData);
              } else {
                const convertedData = covertMenu(data as Record<string, unknown>[]);
                // 转换树形菜单为平铺菜单
                this.setFlatMenus(convertTreeMenuToFlatMenu(convertedData));
              }
              this.setActiveMenuByPath(window.location.pathname);
            } catch (error) {
              console.error('获取菜单失败:', error);
              throw error;
            }
          };

          // 根据类型执行不同操作
          if (typeof getMenuAxiosConfig === 'function') {
            const configResult = getMenuAxiosConfig();
            if (configResult instanceof Promise) {
              // 处理返回 Promise 类型的配置
              await processMenuRequestAsync(await configResult);
            } else {
              await processMenuRequestAsync(configResult);
            }
          } else {
            // 处理直接配置对象
            await processMenuRequestAsync(getMenuAxiosConfig);
          }
        } catch (error) {
          console.error('加载菜单配置失败:', error);
        } finally {
          // 增加延时，避免下面的赋值触发更新再次请求
          setTimeout(() => {
            this.loading = false;
          }, 1000);
        }
      } else {
        this.loading = false;
      }
    },
  },
});
