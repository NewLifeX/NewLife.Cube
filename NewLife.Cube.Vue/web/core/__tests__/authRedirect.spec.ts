import { afterEach, describe, expect, it } from 'vitest';
import { createMemoryHistory, createRouter } from 'vue-router';
import { appRelativePath, normalizeAuthRedirect } from '../utils/authRedirect';

describe('认证回跳安全边界', () => {
  const original = window.router;
  afterEach(() => { window.router = original; });
  it.each(['/login', '/loading', '/unauthorized', '//evil.example/a', 'https://evil.example/a', '/\\evil.example/a'])('拒绝认证循环和外站 %s', value => {
    expect(normalizeAuthRedirect(value)).toBe('/');
  });
  it('保留前缀与业务语言参数', () => {
    expect(normalizeAuthRedirect('/portal/items/1?language=en')).toBe('/portal/items/1?language=en');
  });
  it.each(['/portal/', '/nested/app/'])('采用实际路由基址 %s', base => {
    window.router = createRouter({ history: createMemoryHistory(base), routes: [] });
    expect(normalizeAuthRedirect(base + 'login?a=1')).toBe('/');
    expect(normalizeAuthRedirect(base + 'unauthorized')).toBe('/');
    expect(appRelativePath(base + 'loading')).toBe('/loading');
    expect(appRelativePath(base.slice(0, -1) + '-other/login')).toBe(base.slice(0, -1) + '-other/login');
  });
});
