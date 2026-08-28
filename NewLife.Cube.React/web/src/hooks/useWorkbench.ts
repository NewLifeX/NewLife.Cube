/**
 * 工作台数据 Hook
 *
 * 优先请求后端聚合接口 `/Admin/Index/Workbench`（HOME-3），
 * 接口不可用时自动降级为空数据（页面继续用菜单/用户信息渲染，不白屏）。
 */
import { useCallback, useEffect, useState } from 'react';
import { api } from '@/api';

/** 工作台 KPI 卡 */
export interface WorkbenchKpi {
  /** 语义名（用于图标映射：users/login/online/log/error/cpu） */
  name?: string;
  /** 标题 */
  label: string;
  /** 数值 */
  value: string;
  /** 趋势/说明文案 */
  trend?: string;
  /** 点击跳转 */
  url?: string;
  /** 图标配色（blue/green/cyan/orange/red/purple/grey） */
  color?: string;
}

/** 快捷入口 */
export interface WorkbenchQuickLink {
  name: string;
  url: string;
  icon?: string;
}

/** 工作台聚合数据 */
export interface WorkbenchData {
  user?: {
    name?: string;
    displayName?: string;
    roles?: string[];
    online?: boolean;
    logins?: number;
    lastLogin?: string;
    lastLoginIP?: string;
    registerTime?: string;
  };
  kpis?: WorkbenchKpi[];
  quickLinks?: WorkbenchQuickLink[];
  profile?: Record<string, unknown>;
  sysInfo?: Record<string, string>;
}

export interface UseWorkbenchResult {
  data: WorkbenchData | null;
  loading: boolean;
  reload: () => Promise<void>;
}

/**
 * 加载工作台聚合数据
 *
 * @returns 工作台数据 / 加载状态 / 刷新函数
 */
export function useWorkbench(): UseWorkbenchResult {
  const [data, setData] = useState<WorkbenchData | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.client.get('/Admin/Index/Workbench');
      const body = res.data as { data?: WorkbenchData };
      if (body?.data) {
        setData(body.data);
        return;
      }
    } catch {
      // 接口不可用（旧版后端）：降级为空数据
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return { data, loading, reload: load };
}

export default useWorkbench;
