import { beforeEach, describe, expect, it, vi } from 'vitest';
const mocks = vi.hoisted(() => ({ post: vi.fn(), config: { auth: { tokenKey: 'token', tokenRefresh: { enabled: false } }, request: { baseUrl: '/proxy/api', timeout: 1234, additionalRequestHeaders: { 'X-Tenant': 'demo' } } } }));
vi.mock('axios', () => ({ default: { post: mocks.post } }));
vi.mock('../configure', () => ({ getConfig: () => mocks.config }));
vi.mock('../i18n', () => ({ getCurrentLanguage: () => 'zh-CN' }));
import { createTokenRefreshPolicy, getRememberLogin, setRememberLogin, rememberLoginSession } from '../utils/tokenRefresh';
import { getUrlHashToken, getAccessToken, getRefreshToken, removeAccessToken, setAccessToken, setRefreshToken } from '../utils/token';

const response = (data: Record<string, unknown> = { accessToken: 'new', refreshToken: 'next' }, code = 0, contentType = 'application/json') => ({ data: { code, data }, headers: { 'content-type': contentType } });
function login() {
  setAccessToken('old');
  setRefreshToken('refresh');
  rememberLoginSession(true, 'tester');
}
describe('记住我刷新策略', () => {
  beforeEach(() => {
    localStorage.clear();
    mocks.post.mockReset();
    mocks.config.auth.tokenRefresh.enabled = false;
  });
  it('默认关闭；旧刷新凭据不会授予资格', () => {
    login();
    expect(createTokenRefreshPolicy().isEnabled()).toBe(false);
    mocks.config.auth.tokenRefresh.enabled = true;
    expect(createTokenRefreshPolicy().isEnabled()).toBe(true);
    setRememberLogin(false);
    expect(createTokenRefreshPolicy().isEnabled()).toBe(false);
    setRememberLogin(true);
    expect(getRememberLogin()).toBe(true);
    expect(localStorage.getItem('cube.rememberLogin')).toBe('true');
    expect(createTokenRefreshPolicy().isEnabled()).toBe(false);
  });
  it.each(['#', '#/home#', '#/home?'])('hash登录保存访问令牌、刷新令牌和用户名（%s）', prefix => {
    login();
    mocks.config.auth.tokenRefresh.enabled = true;
    window.history.replaceState(null, '', `${prefix}token=sso-user&refresh_token=next-refresh&u=tester%2F2`);
    try {
      expect(getUrlHashToken()).toBe('sso-user');
      expect(getAccessToken()).toBe('sso-user');
      expect(getRefreshToken()).toBe('next-refresh');
      expect(localStorage.getItem('cube.tokenRefreshUser')).toBe('tester/2');
      expect(createTokenRefreshPolicy().isEnabled()).toBe(true);
    } finally {
      window.history.replaceState(null, '', '/');
    }
  });
  it('配置关闭时只采用访问令牌', () => {
    login();
    window.history.replaceState(null, '', '#token=next-user&refresh_token=next-refresh&u=tester2');
    try {
      expect(getUrlHashToken()).toBe('next-user');
      expect(getRefreshToken()).toBeNull();
      expect(localStorage.getItem('cube.tokenRefreshUser')).toBeNull();
    } finally {
      window.history.replaceState(null, '', '/');
    }
  });
  it('hash缺少刷新凭据时清除旧刷新会话', () => {
    login();
    window.history.replaceState(null, '', '#token=sso-user');
    try {
      expect(getUrlHashToken()).toBe('sso-user');
      expect(getRefreshToken()).toBeNull();
      expect(localStorage.getItem('cube.tokenRefreshEligible')).toBeNull();
      expect(localStorage.getItem('cube.tokenRefreshUser')).toBeNull();
    } finally {
      window.history.replaceState(null, '', '/');
    }
  });
  it('独立请求保留代理路径、配置并轮换令牌', async () => {
    login();
    mocks.config.auth.tokenRefresh.enabled = true;
    const policy = createTokenRefreshPolicy();
    const sessionKey = policy.getSessionKey();
    mocks.post.mockResolvedValue(response());
    await policy.refresh();
    expect(mocks.post).toHaveBeenCalledWith('/proxy/Auth/Refresh', { refreshToken: 'refresh', userName: 'tester' }, { timeout: 1234, withCredentials: true, headers: { 'Accept-Language': 'zh-CN', 'X-Tenant': 'demo' } });
    expect(getAccessToken()).toBe('new');
    expect(getRefreshToken()).toBe('next');
    expect(policy.isEnabled()).toBe(true);
    expect(policy.getSessionKey()).toBe(sessionKey);
  });
  it('兼容旧字段且省略新 refresh 时保留原值', async () => {
    login();
    mocks.post.mockResolvedValue(response({ access_token: 'snake' }));
    await createTokenRefreshPolicy().refresh();
    expect(getAccessToken()).toBe('snake');
    expect(getRefreshToken()).toBe('refresh');
  });
  it.each([response({}, 0), response({ accessToken: 'bad' }, 401), response({ accessToken: 'bad' }, 0, 'text/html')])('拒绝无效响应且不覆盖令牌', async value => {
    login();
    mocks.post.mockResolvedValue(value);
    await expect(createTokenRefreshPolicy().refresh()).rejects.toThrow('Invalid refresh response');
    expect(getAccessToken()).toBe('old');
  });
  it.each(['logout', 'switch', 'uncheck'])('迟到响应不会复活会话：%s', async action => {
    login();
    let resolve!: (value: ReturnType<typeof response>) => void;
    mocks.post.mockReturnValue(new Promise(r => { resolve = r; }));
    const policy = createTokenRefreshPolicy();
    const sessionKey = policy.getSessionKey();
    const pending = policy.refresh();
    if (action === 'logout') removeAccessToken();
    if (action === 'switch') setAccessToken('other-user');
    if (action === 'uncheck') setRememberLogin(false);
    expect(policy.getSessionKey()).not.toBe(sessionKey);
    resolve(response());
    await expect(pending).rejects.toThrow('Refresh session changed');
    expect(getAccessToken()).toBe(action === 'logout' ? null : action === 'switch' ? 'other-user' : 'old');
    expect(getRefreshToken()).toBe('refresh');
  });
});
