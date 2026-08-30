/**
 * 登录配置 Hook：加载 /Auth/LoginConfig
 */
import { useCallback, useEffect, useState } from 'react';
import { api } from '@/api';
import type { LoginConfig, CaptchaResult } from '@cube/api-core';

/**
 * 加载登录页配置
 *
 * @returns 登录配置 / 加载状态 / 加载失败信息 / 刷新函数
 */
export function useLoginConfig() {
  const [config, setConfig] = useState<LoginConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.user.getLoginConfig();
      setConfig(res.data ?? null);
      setError('');
    } catch {
      setError('登录配置加载失败');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return { config, loading, error, reload: load };
}

/**
 * 加载图片验证码（SVG 算数题）
 *
 * @param enabled 是否启用（跟随 LoginConfig.login.captcha / register.captcha），关闭时不请求验证码接口
 * @returns 验证码 ID / SVG 图片 / 刷新函数
 */
export function useCaptcha(enabled = true) {
  const [captcha, setCaptcha] = useState<CaptchaResult | null>(null);

  const refresh = useCallback(async () => {
    try {
      const res = await api.user.getCaptcha();
      setCaptcha(res.data ?? null);
    } catch {
      setCaptcha(null);
    }
  }, []);

  useEffect(() => {
    // 开关关闭时不请求验证码；关闭→开启（配置异步加载完成）时自动触发首次拉取
    if (!enabled) {
      setCaptcha(null);
      return;
    }
    void refresh();
  }, [refresh, enabled]);

  return { captcha, refresh };
}
