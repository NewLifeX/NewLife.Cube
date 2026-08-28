/**
 * Token 响应式登录态单元测试（E4）
 *
 * 覆盖：useIsLoggedIn 初始值、setToken 触发 CUBE_TOKEN_EVENT 后变为已登录、
 * clearToken 后回到未登录（事件监听与清理）。
 */
import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { api } from '@/api';
import { useIsLoggedIn } from '@/stores/user';

describe('useIsLoggedIn 响应式登录态', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('无 token 时初始为未登录', () => {
    const { result } = renderHook(() => useIsLoggedIn());
    expect(result.current).toBe(false);
  });

  it('已有 token 时初始为已登录', () => {
    api.tokenManager.setToken('existing-token');
    const { result } = renderHook(() => useIsLoggedIn());
    expect(result.current).toBe(true);
  });

  it('setToken 触发事件后变为已登录', () => {
    const { result } = renderHook(() => useIsLoggedIn());
    expect(result.current).toBe(false);
    act(() => {
      api.tokenManager.setToken('new-token');
    });
    expect(result.current).toBe(true);
  });

  it('clearToken 触发事件后回到未登录', () => {
    api.tokenManager.setToken('t');
    const { result } = renderHook(() => useIsLoggedIn());
    expect(result.current).toBe(true);
    act(() => {
      api.tokenManager.clearToken();
    });
    expect(result.current).toBe(false);
  });
});
