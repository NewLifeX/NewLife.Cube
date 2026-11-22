import { computed, nextTick, ref, shallowRef } from 'vue';
import type { RouteRecordRaw } from 'vue-router';
import { defineStore } from 'pinia';
import { useBoolean } from '@sa/hooks';
import type { ElegantConstRoute, RouteKey } from '@elegant-router/types';
import { router } from '@/router';
import { cubeApi } from '@/api/cube';
import { SetupStoreId } from '@/enum';
import { createStaticRoutes } from '@/router/routes';
import { collectRoutePaths, createCubeMenus, createCubeRoutes } from '@/router/cube-routes';
import { transformElegantRoutesToVueRoutes } from '@/router/elegant/transform';
import { layouts, views } from '@/router/elegant/imports';
import { useAuthStore } from '../auth';
import { useTabStore } from '../tab';
import {
  getBreadcrumbsByRoute,
  getCacheRouteNames,
  getSelectedMenuKeyPathByKey,
  sortRoutesByOrder,
  transformMenuToSearchMenus,
  updateLocaleOfGlobalMenus
} from './shared';

export const useRouteStore = defineStore(SetupStoreId.Route, () => {
  const authStore = useAuthStore();
  const tabStore = useTabStore();
  const { bool: isInitConstantRoute, setBool: setIsInitConstantRoute } = useBoolean();
  const { bool: isInitAuthRoute, setBool: setIsInitAuthRoute } = useBoolean();

  /** 首页路由名（工作台） */
  const routeHome = ref(import.meta.env.VITE_ROUTE_HOME || 'workbench');

  /** constant routes */
  const constantRoutes = shallowRef<ElegantConstRoute[]>([]);

  /** auth routes（后端菜单树下发） */
  const authRoutes = shallowRef<ElegantConstRoute[]>([]);

  /** 当前用户可访问的全部菜单路径 */
  let cubePaths = new Set<string>();

  const removeRouteFns: (() => void)[] = [];

  /** Global menus */
  const menus = ref<App.Global.Menu[]>([]);
  const searchMenus = computed(() => transformMenuToSearchMenus(menus.value));

  /** Cache routes */
  const cacheRoutes = ref<RouteKey[]>([]);

  /**
   * Exclude cache routes
   *
   * for reset route cache
   */
  const excludeCacheRoutes = ref<RouteKey[]>([]);

  /** Global breadcrumbs */
  const breadcrumbs = computed(() => getBreadcrumbsByRoute(router.currentRoute.value, menus.value));

  /** Update global menus by locale */
  function updateGlobalMenusByLocale() {
    menus.value = updateLocaleOfGlobalMenus(menus.value);
  }

  /** Reset store */
  async function resetStore() {
    const routeStore = useRouteStore();

    routeStore.$reset();

    resetVueRoutes();

    menus.value = [];
    authRoutes.value = [];
    cubePaths = new Set();

    // after reset store, need to re-init constant route
    await initConstantRoute();
  }

  /** Reset vue routes */
  function resetVueRoutes() {
    removeRouteFns.forEach(fn => fn());
    removeRouteFns.length = 0;
  }

  /** init constant route（内置异常页/登录页；业务路由由后端菜单树动态下发） */
  async function initConstantRoute() {
    if (isInitConstantRoute.value) return;

    const { constantRoutes: staticConstantRoutes } = createStaticRoutes();
    constantRoutes.value = staticConstantRoutes;

    resetVueRoutes();
    addRoutesToVueRouter(transformElegantRoutesToVueRoutes(staticConstantRoutes, layouts, views));

    setIsInitConstantRoute(true);

    tabStore.initHomeTab();
  }

  /** Init auth route */
  async function initAuthRoute() {
    // check if user info is initialized
    if (!authStore.userInfo.userId) {
      await authStore.initUserInfo();
    }

    await initCubeAuthRoute();

    tabStore.initHomeTab();
  }

  /** 拉取魔方菜单树并注册动态路由 */
  async function initCubeAuthRoute() {
    try {
      const res = await cubeApi.menu.getMenuTree();
      const { routes } = createCubeRoutes(res.data ?? []);

      authRoutes.value = routes;

      const sortedRoutes = sortRoutesByOrder(routes);
      const vueRoutes = transformElegantRoutesToVueRoutes(sortedRoutes, layouts, views);

      resetVueRoutes();
      addRoutesToVueRouter(vueRoutes);

      menus.value = createCubeMenus(sortedRoutes);
      cacheRoutes.value = getCacheRouteNames(vueRoutes);
      cubePaths = collectRoutePaths(routes);

      setIsInitAuthRoute(true);
    } catch {
      // 菜单获取失败，回到登录页
      await authStore.resetStore();
    }
  }

  /**
   * Add routes to vue router
   *
   * @param routes Vue routes
   */
  function addRoutesToVueRouter(routes: RouteRecordRaw[]) {
    routes.forEach(route => {
      const removeFn = router.addRoute(route);
      addRemoveRouteFn(removeFn);
    });
  }

  /**
   * Add remove route fn
   *
   * @param fn
   */
  function addRemoveRouteFn(fn: () => void) {
    removeRouteFns.push(fn);
  }

  /**
   * Reset route cache
   *
   * @default router.currentRoute.value.name current route name
   * @param routeKey
   */
  async function resetRouteCache(routeKey?: RouteKey) {
    const routeName = routeKey || (router.currentRoute.value.name as RouteKey);

    excludeCacheRoutes.value.push(routeName);

    await nextTick();

    excludeCacheRoutes.value = [];
  }

  /**
   * Get is auth route exist
   *
   * @param routePath Route path
   */
  async function getIsAuthRouteExist(routePath: string) {
    return cubePaths.has(routePath);
  }

  /**
   * Get selected menu key path
   *
   * @param selectedKey Selected menu key
   */
  function getSelectedMenuKeyPath(selectedKey: string) {
    return getSelectedMenuKeyPathByKey(selectedKey, menus.value);
  }

  async function onRouteSwitchWhenLoggedIn() {
    await authStore.initUserInfo();
  }

  async function onRouteSwitchWhenNotLoggedIn() {
    // some global init logic if it does not need to be logged in
  }

  return {
    resetStore,
    routeHome,
    menus,
    searchMenus,
    updateGlobalMenusByLocale,
    cacheRoutes,
    excludeCacheRoutes,
    resetRouteCache,
    breadcrumbs,
    initConstantRoute,
    isInitConstantRoute,
    initAuthRoute,
    isInitAuthRoute,
    setIsInitAuthRoute,
    getIsAuthRouteExist,
    getSelectedMenuKeyPath,
    onRouteSwitchWhenLoggedIn,
    onRouteSwitchWhenNotLoggedIn
  };
});
