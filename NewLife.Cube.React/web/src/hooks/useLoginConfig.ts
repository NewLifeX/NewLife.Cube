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
 * @returns 验证码 ID / SVG 图片 / 刷新函数
 */
export function useCaptcha() {
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
    void refresh();
  }, [refresh]);

  return { captcha, refresh };
}
