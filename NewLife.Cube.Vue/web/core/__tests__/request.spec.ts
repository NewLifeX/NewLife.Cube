/**
 * request.ts 单元测试（cube-vue 请求层 UI 逻辑）
 *
 * 范围：仅验证 cube-vue 在 @cube/api-core 之上叠加的、与 UI 强相关的行为：
 *   1. redirectToLogin —— 登出 / 重登录跳转（清 token、带 redirect_uri）；
 *   2. onUnauthorized(handleUnauthorized) —— 401 去重、自身请求判定、跳登录页 / 未授权页；
 *   3. onResponseError(showErrorNotification) —— 网络错误中文提示 / 其余走 autoNotification；
 *   4. onBusinessError —— 业务错误弹窗；
 *   5. createApiClient 接线（unwrapResponse:true 等）。
 *
 * 非 UI 的底层逻辑（host 拼接、/api 补全、token 注入、错误归一化等）已在 @cube/api-core 覆盖，
 * 此处通过 vi.mock('@cube/api-core') 捕获传给 createApiClient 的回调后直接驱动，避免真实网络。
 *
 * 运行：pnpm test:unit core/__tests__/request.spec.ts
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

// 共享可变状态与 spy：vi.hoisted 保证在 vi.mock 工厂之前初始化
const h = vi.hoisted(() => {
  const notification = {
    info: vi.fn(),
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
    autoNotification: vi.fn(),
  };
  const intl = {
    get: vi.fn((_key: string) => ({ d: (def: string) => def })),
    getLocale: vi.fn(() => 'zh-CN'),
  };
  return {
    captured: null as Record<string, unknown> | null,
    config: {
      request: { baseUrl: 'http://localhost:5000' },
      auth: {
        oauthUrl: '/Sso/Login?name=NewLife&source=front-end&redirect_uri=',
        reLoginParams: { loginPageUrl: '/login' },
      },
      user: {
        getUserInfoAxiosConfig: () => ({ method: 'GET', url: '/Admin/User/Info' }),
      },
    },
    gotoPage: vi.fn(),
    notification,
    intl,
    getSession: vi.fn(() => null),
    setSession: vi.fn(),
    removeAllCookie: vi.fn(),
    getAccessToken: vi.fn(() => null),
    removeAccessToken: vi.fn(),
  };
});

vi.mock('@cube/api-core', () => ({
  createApiClient: (options: Record<string, unknown>) => {
    h.captured = options;
    return { get: vi.fn(), post: vi.fn(), request: vi.fn() };
  },
  TokenManager: class {
    constructor(_storage: unknown) {}
  },
}));

vi.mock('../configure', () => ({ getConfig: () => h.config }));
vi.mock('../components/Notification', () => ({ default: h.notification }));
vi.mock('../i18n', () => ({ intl: h.intl }));
vi.mock('../utils/storage', () => ({
  getSession: h.getSession,
  setSession: h.setSession,
  removeAllCookie: h.removeAllCookie,
}));
vi.mock('../utils/token', () => ({
  getAccessToken: h.getAccessToken,
  removeAccessToken: h.removeAccessToken,
}));
vi.mock('../utils/router', () => ({ gotoPage: h.gotoPage }));

// 重新加载模块，保证 isErrorFlag 等模块级状态干净、captured 重新捕获
async function loadRequest() {
  vi.resetModules();
  return (await import('../utils/request')) as any;
}

function setPath(path: string) {
  window.history.replaceState(null, '', path);
}

describe('request.ts — cube-vue 请求层 UI 逻辑', () => {
  let request: any;
  let opts: any;

  beforeEach(async () => {
    vi.clearAllMocks();
    request = await loadRequest();
    opts = h.captured;
  });

  afterEach(() => {
    setPath('/');
    vi.useRealTimers();
  });

  describe('createApiClient 接线', () => {
    it('以 unwrapResponse:true 创建（与 cubeApi.client 不同）', () => {
      expect(opts.unwrapResponse).toBe(true);
      expect(opts.withCredentials).toBe(true);
      expect(opts.tokenHeaderPrefix).toBe('bearer ');
      expect(opts.baseURL).toBe('http://localhost:5000');
    });
  });

  describe('redirectToLogin', () => {
    it('默认跳转到 reLoginParams.loginPageUrl 并带 redirect_uri', () => {
      setPath('/dashboard');
      request.redirectToLogin();
      expect(h.removeAccessToken).toHaveBeenCalled();
      expect(h.removeAllCookie).toHaveBeenCalled();
      expect(h.gotoPage).toHaveBeenCalledWith(expect.stringContaining('/login?redirect_uri='));
    });

    it('传入 loginPageUrl 参数时使用该地址', () => {
      setPath('/dashboard');
      request.redirectToLogin({ loginPageUrl: '/custom-login' });
      expect(h.gotoPage).toHaveBeenCalledWith(expect.stringContaining('/custom-login?redirect_uri='));
    });
  });

  describe('onUnauthorized（401 处理）', () => {
    it('非自身 401 且不在 /unauthorized 页 → 延迟跳转 /unauthorized', () => {
      vi.useFakeTimers();
      setPath('/dashboard');
      opts.onUnauthorized('/Some/Other/Action');
      vi.advanceTimersByTime(150);
      expect(h.gotoPage).toHaveBeenCalledWith(expect.stringContaining('/unauthorized?language=zh_CN'));
    });

    it('已在 /unauthorized 页 → 不跳转', () => {
      setPath('/unauthorized');
      opts.onUnauthorized('/Some/Other');
      expect(h.gotoPage).not.toHaveBeenCalled();
    });

    it('自身 401（/Admin/User/Info）且不在登录页 → 跳登录页', () => {
      setPath('/dashboard');
      opts.onUnauthorized('/Admin/User/Info');
      expect(h.gotoPage).toHaveBeenCalledWith(expect.stringContaining('/login?redirect_uri='));
    });

    it('自身 401 但已在登录页 → 不重复跳转', () => {
      setPath('/login');
      opts.onUnauthorized('/Admin/User/Info');
      expect(h.gotoPage).not.toHaveBeenCalled();
    });

    it('401 去重：连续两次仅处理一次', () => {
      vi.useFakeTimers();
      setPath('/dashboard');
      opts.onUnauthorized('/Some/Other');
      opts.onUnauthorized('/Some/Other');
      vi.advanceTimersByTime(150);
      expect(h.gotoPage).toHaveBeenCalledTimes(1);
    });
  });

  describe('onResponseError（错误弹窗）', () => {
    it('网络错误 → 中文网络异常提示（不走 autoNotification）', () => {
      opts.onResponseError({ isNetwork: true, message: 'net err', description: '' });
      expect(h.notification.error).toHaveBeenCalledWith({ message: '网络请求异常' });
      expect(h.notification.autoNotification).not.toHaveBeenCalled();
    });

    it('非网络错误 → autoNotification(error, message, description)', () => {
      opts.onResponseError({ isNetwork: false, message: '服务器错误', description: '详情' });
      expect(h.notification.autoNotification).toHaveBeenCalledWith('error', '服务器错误', '详情');
      expect(h.notification.error).not.toHaveBeenCalled();
    });
  });

  describe('onBusinessError（业务错误弹窗）', () => {
    it('有 message 时弹错误通知', () => {
      opts.onBusinessError(1, '业务失败');
      expect(h.notification.error).toHaveBeenCalledWith({ message: '业务失败' });
    });

    it('无 message 时不弹窗', () => {
      opts.onBusinessError(1, '');
      expect(h.notification.error).not.toHaveBeenCalled();
    });
  });
});
