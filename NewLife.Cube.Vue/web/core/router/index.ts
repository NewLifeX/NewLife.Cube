import { createRouter, createWebHistory, type Router } from 'vue-router';
import routes from '../routes';
import { initAppRoutes, isRoutesInitialized } from '../microAppRouter';
import { useUserStore } from '../stores/user';
import { useMenuStore } from '../stores/menu';
import { getAccessToken } from '../utils/token';
import { getUrlHashToken } from '../utils/token';
import { registerMenuRoutes } from '../utils/menuRoutes';
import { normalizeMenuUrl } from '../utils/url';

// 创建路由实例
const router: Router = createRouter({
  history: createWebHistory(),
  routes,
});

// 先初始化微前端应用路由（异步操作）
// 我们不会等待其完成再导出router，但会在导航守卫中检查初始化状态
initAppRoutes(router).catch((error) => {
  console.error('初始化微应用路由失败:', error);
});

// 记录需要重新导航的路径（动态路由刚注册）
let pendingNavigationPath: string | null = null;

// 全局导航守卫
router.beforeEach(async (to, from, next) => {
  // 检查是否需要重新导航到刚注册的动态路由
  if (pendingNavigationPath && to.path === pendingNavigationPath) {
    pendingNavigationPath = null;
  }

  // 为了调试，在全局对象上暴露状态检查函数
  if (typeof window !== 'undefined') {
    (window as unknown as Record<string, unknown>).microAppRouter = {
      isRoutesInitialized,
      router,
    };
  }

  // 先检查URL中是否有token并处理
  const hashToken = getUrlHashToken();

  // 如果URL中有hash token，重新路由到相同页面，保持原查询参数
  if (hashToken) {
    // 保留原查询参数，确保导航结束后hash消失
    window.location.hash = '';
    return next({ path: to.path, query: to.query });
  }

  if (!isRoutesInitialized() && to.path === '/loading') {
    // 如果是加载页面，直接放行
    return next();
  }

  // 检查微应用路由是否已初始化完成
  if (!isRoutesInitialized() && to.path !== '/login') {
    // 可以选择显示加载页面或重定向到特定页面
    console.log('等待微应用路由初始化完成...');
    // 可以添加一个加载中的页面
    return next({ path: '/loading', query: { redirect: to.fullPath } });
  }

  // 获取localStorage中的token
  const hasToken = !!getAccessToken();

  // 获取store
  const userStore = useUserStore();
  const menuStore = useMenuStore();

  // 判断路由是否需要认证（根据路由元信息）
  // 使用可选链和空值合并运算符，如果meta或auth未定义，默认需要认证
  const requireAuth = to.meta?.auth ?? true;

  // 如果已经在登录页且要去的也是登录页，直接放行
  if (from.path === '/login' && to.path === '/login') {
    return next();
  }

  // 如果有token
  if (hasToken) {
    // 如果去登录页，直接跳转到首页
    if (to.path === '/login') {
      if (to.query.redirect) {
        next({ path: to.query.redirect as string });
      } else {
        next({ path: '/' });
      }
    } else {
      // 如果没有用户信息，获取用户信息
      if (!userStore.hasUserInfo) {
        try {
          await userStore.fetchUserInfoAsync();
        } catch (error) {
          // 获取用户信息失败，可能是token无效，跳转到登录页
          console.error('获取用户信息失败:', error);
          next({ path: '/login' });
          return;
        }
      }

      // 如果没有菜单信息，获取菜单信息
      if (!menuStore.hasMenus) {
        try {
          await menuStore.fetchMenuAsync();
        } catch (error) {
          console.error('获取菜单信息失败:', error);
          // 获取菜单失败但不影响导航，继续放行
        }
      }

      // 如果有菜单但路由未注册，自动注册路由
      if (menuStore.hasMenus && !menuStore.routesRegistered && menuStore.flatMenus) {
        const registered = registerMenuRoutes(router, menuStore.flatMenus, to.path);
        menuStore.markRoutesRegistered();
        // 如果当前路径是刚注册的动态路由，需要重新导航
        if (registered?.currentPathNeedsRefresh) {
          // 使用 normalizedPath 格式存储（与注册的路由路径一致）
          pendingNavigationPath = normalizeMenuUrl(to.path);
          return next(false); // 取消当前导航，让 afterEach 触发重新导航
        }
      }

      // 更新当前活动菜单
      if (menuStore.hasMenus) {
        menuStore.setActiveMenuByPath(to.path);
      }

      // 继续导航
      next();
    }
  } else {
    // 无token的情况

    // 如果路由不需要认证，直接放行
    if (!requireAuth) {
      next();
    } else {
      // 需要认证的路由，重定向到登录页
      next({ path: '/login', query: { redirect: to.fullPath } });
    }
  }
});

// afterEach 用于处理动态路由注册后的重新导航
router.afterEach((to, from) => {
  if (pendingNavigationPath) {
    // 清除标记并重新导航（直接信任 pendingNavigationPath，不依赖 to.matched）
    const path = pendingNavigationPath;
    pendingNavigationPath = null;
    console.log(`Re-navigating to dynamic route: ${path}`);
    router.replace(path).catch(() => {
      // 忽略错误（可能是路由不存在或其他导航错误）
    });
  }
});

export default router;
