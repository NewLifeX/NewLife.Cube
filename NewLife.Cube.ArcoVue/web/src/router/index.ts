import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router';
import RootLayout from '@/layouts/RootLayout.vue';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { useUserProfileStore } from '@/stores/userProfile';
import { useAppStore } from '@/stores/app';
import { registerLeafRoutes } from '@/core/utils/menuRoutes';

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/login/index.vue'),
    meta: { public: true },
  },
  {
    path: '/forgot-password',
    name: 'ForgotPassword',
    component: () => import('@/views/login/forgot-password.vue'),
    meta: { public: true },
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/views/login/register.vue'),
    meta: { public: true },
  },
  {
    path: '/',
    name: 'Layout',
    component: RootLayout,
    redirect: '/home',
    children: [
      {
        path: 'home',
        name: 'Home',
        component: () => import('@/views/home/index.vue'),
        meta: { title: '首页' },
      },
      {
        /** 工作台首页（菜单 visible=false 不注册动态路由，此处静态兜底）；经 pageKind=home 复用 DefaultHome */
        path: 'Admin/Index',
        name: 'AdminIndex',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '首页' },
      },
      {
        /** 数据库管理（菜单可能不注册动态路由时兜底）；经 pageKind=custom 挂专用页 */
        path: 'Admin/Db',
        name: 'AdminDb',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '数据库' },
      },
      {
        /** 文件管理（菜单 visible=false）；经 pageKind=custom 挂专用页 */
        path: 'Admin/File',
        name: 'AdminFile',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '文件' },
      },
      {
        /** 星尘设置（菜单 visible=false）；经探测落入 DefaultObject */
        path: 'Admin/Star',
        name: 'AdminStar',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '星尘设置' },
      },
      {
        /** 魔方设置（ConfigController<CubeSetting>）；经探测落入 DefaultObject */
        path: 'Admin/Cube',
        name: 'AdminCube',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '魔方设置' },
      },
      {
        /** 系统设置（有控制器则渲染，无则 unknown 空状态） */
        path: 'Admin/Sys',
        name: 'AdminSys',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '系统设置' },
      },
      {
        /** 核心设置（有控制器则渲染，无则 unknown 空状态） */
        path: 'Admin/Core',
        name: 'AdminCore',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: '核心设置' },
      },
      {
        /** XCode 设置（有控制器则渲染，无则 unknown 空状态） */
        path: 'Admin/XCode',
        name: 'AdminXCode',
        component: () => import('@/views/dynamic/DynamicPage.vue'),
        meta: { title: 'XCode 设置' },
      },
      {
        path: 'account/security',
        redirect: { path: '/account', query: { tab: 'security' } },
      },
      {
        path: 'account',
        name: 'AccountCenter',
        component: () => import('@/views/account/AccountCenter.vue'),
        meta: { title: '个人信息' },
      },
      {
        /** 兼容旧链接：打开外观抽屉并回首页，不占用内容页签 */
        path: 'settings/appearance',
        name: 'AppearanceSettings',
        component: { render: () => null },
        beforeEnter: (_to, _from, next) => {
          useAppStore().openAppearanceDrawer();
          next({ path: '/home', replace: true });
        },
      },
    ],
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

let routesLoaded = false;

export function resetMenuRoutesFlag() {
  routesLoaded = false;
}

router.beforeEach(async (to, _from, next) => {
  // SSO 回跳：#token= / #refreshToken=
  if (typeof window !== 'undefined' && window.location.hash) {
    const { parseHashTokens } = await import('@/views/login/loginConfig');
    const { persistSession } = await import('@/views/login/sessionTokens');
    const tokens = parseHashTokens(window.location.hash);
    if (tokens.token) {
      persistSession(tokens.token, tokens.refreshToken);
      const url = new URL(window.location.href);
      url.hash = '';
      window.history.replaceState(null, '', url.pathname + url.search);
    }
  }

  // 分享链接：?embed=1&token=xxx → 持久化短令牌并进入无导航壳
  {
    const { readQueryEmbed, enterEmbedMode } = await import('@/core/utils/embedMode');
    const { persistSession } = await import('@/views/login/sessionTokens');
    const q = readQueryEmbed(
      typeof window !== 'undefined'
        ? window.location.search
        : new URLSearchParams(to.query as Record<string, string>).toString(),
    );
    if (q.embed) {
      enterEmbedMode();
      if (q.token) persistSession(q.token);
    }
  }

  const isPublic = !!to.meta.public;
  const token = cubeApi.tokenManager.getToken();

  if (!token && !isPublic) {
    next({ path: '/login', query: { redirect: to.fullPath } });
    return;
  }

  if (token && !routesLoaded && !isPublic) {
    const userStore = useUserStore();
    const profileStore = useUserProfileStore();
    const { isEmbedMode: embedNow } = await import('@/core/utils/embedMode');
    let needsRefresh = false;
    try {
      if (!userStore.menus?.length) {
        await userStore.fetchMenus();
      }
      if (!profileStore.loaded) {
        profileStore.bootstrapLocal();
        await profileStore.loadFromServer();
      }
      const { currentPathNeedsRefresh } = registerLeafRoutes(
        router,
        userStore.menus || [],
        to.path,
      );
      needsRefresh = currentPathNeedsRefresh;
    } catch {
      // 分享令牌可能拉菜单失败，下面统一 ensure 当前 path
    }
    // 分享/embed：菜单为空或不含当前页时仍注册 DynamicPage，避免白屏
    if (embedNow()) {
      const { ensureDynamicLeafRoute } = await import('@/core/utils/menuRoutes');
      if (ensureDynamicLeafRoute(router, to.path)) needsRefresh = true;
    }
    routesLoaded = true;
    if (needsRefresh) {
      next({ ...to, replace: true });
      return;
    }
  }

  next();
});

export default router;
