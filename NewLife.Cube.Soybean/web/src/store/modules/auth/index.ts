import { computed, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import { defineStore } from 'pinia';
import { useLoading } from '@sa/hooks';
import { cubeApi } from '@/api/cube';
import type { CubeUserInfo } from '@/api/cube';
import { useRouterPush } from '@/hooks/common/router';
import { localStg } from '@/utils/storage';
import { SetupStoreId } from '@/enum';
import { $t } from '@/locales';
import { useRouteStore } from '../route';
import { useTabStore } from '../tab';
import { clearAuthStorage, getToken } from './shared';

export const useAuthStore = defineStore(SetupStoreId.Auth, () => {
  const route = useRoute();
  const routeStore = useRouteStore();
  const tabStore = useTabStore();
  const { toLogin, redirectFromLogin } = useRouterPush(false);
  const { loading: loginLoading, startLoading, endLoading } = useLoading();

  const token = ref(getToken());

  const userInfo: Api.Auth.UserInfo = reactive({
    userId: '',
    userName: '',
    roles: [],
    buttons: []
  });

  /** 魔方原始用户信息（权限串/角色/租户等，页面逻辑层消费） */
  const cubeInfo = reactive<Partial<CubeUserInfo>>({});

  /** is super role in static route */
  const isStaticSuper = computed(() => {
    const { VITE_AUTH_ROUTE_MODE, VITE_STATIC_SUPER_ROLE } = import.meta.env;

    return VITE_AUTH_ROUTE_MODE === 'static' && userInfo.roles.includes(VITE_STATIC_SUPER_ROLE);
  });

  /** Is login */
  const isLogin = computed(() => Boolean(token.value));

  /** Reset auth store */
  async function resetStore() {
    recordUserId();

    // 通知后端注销（令牌已失效时静默忽略）
    try {
      await cubeApi.user.logout();
    } catch {
      // ignore
    }

    clearAuthStorage();

    clearAuthState();

    tabStore.cacheTabs();

    // 先重建常量路由（登录页），动态路由注册后登录页已被移除，直接跳转会失败
    await routeStore.resetStore();

    if (!route.meta.constant) {
      await toLogin();
    }
  }

  /** 显式清空登录态；不能依赖 $reset，store 插件会把它覆盖成恢复初始快照（含已登录 token） */
  function clearAuthState() {
    token.value = '';
    Object.assign(userInfo, {
      userId: '',
      userName: '',
      roles: [] as string[],
      buttons: [] as string[]
    });
    Object.keys(cubeInfo).forEach(key => delete cubeInfo[key as keyof CubeUserInfo]);
  }

  /** Record the user ID of the previous login session Used to compare with the current user ID on next login */
  function recordUserId() {
    if (!userInfo.userId) {
      return;
    }

    // Store current user ID locally for next login comparison
    localStg.set('lastLoginUserId', userInfo.userId);
  }

  /**
   * Check if current login user is different from previous login user If different, clear all tabs
   *
   * @returns {boolean} Whether to clear all tabs
   */
  function checkTabClear(): boolean {
    if (!userInfo.userId) {
      return false;
    }

    const lastLoginUserId = localStg.get('lastLoginUserId');

    // Clear all tabs if current user is different from previous user
    if (lastLoginUserId !== userInfo.userId) {
      localStg.remove('globalTabs');
      tabStore.clearTabs();

      return true;
    }

    return false;
  }

  /**
   * Login
   *
   * @param userName User name
   * @param password Password
   * @param [opts] 登录附加参数（验证码/记住）或是否登录后跳转
   */
  async function login(
    userName: string,
    password: string,
    opts: { captchaId?: string; captchaCode?: string; remember?: boolean; redirect?: boolean } | boolean = true
  ) {
    const options = typeof opts === 'boolean' ? { redirect: opts } : opts;
    const redirect = options.redirect ?? true;

    startLoading();

    try {
      const res = await cubeApi.user.loginWithPassword(userName, password, {
        captchaId: options.captchaId,
        captchaCode: options.captchaCode,
        remember: options.remember
      });
      const loginToken = res.data;

      if (loginToken?.accessToken) {
        const pass = await loginByToken(loginToken);

        if (pass) {
          // Check if the tab needs to be cleared
          const isClear = checkTabClear();
          let needRedirect = redirect;

          if (isClear) {
            // If the tab needs to be cleared,it means we don't need to redirect.
            needRedirect = false;
          }
          await redirectFromLogin(needRedirect);

          window.$notification?.success({
            title: $t('page.login.common.loginSuccess'),
            message: $t('page.login.common.welcomeBack', { userName: userInfo.userName }),
            duration: 4500
          });
        }
      } else {
        // 登录成功但无令牌：账号待激活等场景
        window.$message?.warning(loginToken?.targets?.length ? `账号待激活：${loginToken.targets.join('、')}` : '账号待激活');
      }
    } catch {
      // 错误提示由请求层统一处理
    } finally {
      endLoading();
    }
  }

  async function loginByToken(loginToken: { accessToken: string; refreshToken?: string }) {
    // 1. stored in the localStorage, the later requests need it in headers
    localStg.set('token', loginToken.accessToken);
    if (loginToken.refreshToken) localStg.set('refreshToken', loginToken.refreshToken);

    // 2. get user info
    const pass = await getUserInfo();

    if (pass) {
      token.value = loginToken.accessToken;

      return true;
    }

    return false;
  }

  /** 直接写入访问令牌（SSO 回跳携带 #token=），用户信息由 initUserInfo 补全 */
  function applyToken(accessToken: string) {
    localStg.set('token', accessToken);
    token.value = accessToken;
  }

  async function getUserInfo() {
    try {
      const res = await cubeApi.user.info();
      const info = res.data;

      if (!info) return false;

      // update store
      Object.assign(cubeInfo, info);
      Object.assign(userInfo, {
        userId: String(info.id ?? ''),
        userName: info.name || info.displayName || '',
        roles: [] as string[],
        buttons: [] as string[]
      });

      return true;
    } catch {
      return false;
    }
  }

  async function initUserInfo() {
    const hasToken = getToken();

    if (hasToken) {
      const pass = await getUserInfo();

      if (!pass) {
        resetStore();
      }
    }
  }

  return {
    token,
    userInfo,
    cubeInfo,
    isStaticSuper,
    isLogin,
    loginLoading,
    resetStore,
    applyToken,
    login,
    initUserInfo
  };
});
