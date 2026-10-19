import { computed, ref, type ComputedRef, type Ref } from 'vue';
import { usePageApi } from './useCubeApi';
import type { MonitorPoint, MonitorSnapshot, WorkbenchData } from '../types/workbench';

/** 监控曲线最多保留的点数。5 秒一轮，约保留最近 5 分钟 */
export const MAX_MONITOR_POINTS = 60;

/** 监控轮询间隔（毫秒） */
export const MONITOR_POLL_INTERVAL = 5000;

/** 判断后端业务响应是否成功。兼容 code=0 与 code=200 两种历史约定 */
export function isSuccessCode(code: unknown): boolean {
  return code === 0 || code === 200;
}

/** 从 MonitorData 响应提取一个监控点。数据缺失时返回 null，避免把脏数据画进曲线 */
export function toMonitorPoint(snapshot: MonitorSnapshot | null | undefined): MonitorPoint | null {
  if (!snapshot) return null;

  const cpu = Number(snapshot.series?.[0]?.[0]);
  const memory = Number(snapshot.series?.[1]?.[0]);
  if (!Number.isFinite(cpu) || !Number.isFinite(memory)) return null;

  const label = snapshot.xs?.[0] || new Date().toLocaleTimeString();
  return {
    label,
    cpu: Math.min(100, Math.max(0, cpu)),
    memory: Math.min(100, Math.max(0, memory)),
    memUsed: snapshot.memUsed,
    memTotal: snapshot.memTotal,
  };
}

/** 工作台数据加载与监控轮询 */
export interface UseWorkbenchReturn {
  /** 聚合数据 */
  data: Ref<WorkbenchData | null>;
  /** 是否正在加载聚合数据 */
  loading: Ref<boolean>;
  /** 加载错误文本。空串表示无错误 */
  error: Ref<string>;
  /** 累计的监控点 */
  monitorPoints: Ref<MonitorPoint[]>;
  /** 最新监控点 */
  latestMonitor: ComputedRef<MonitorPoint | null>;
  /** 是否存在需要轮询的图表卡 */
  hasChartWidget: ComputedRef<boolean>;
  /** 加载工作台聚合数据 */
  load: () => Promise<void>;
  /** 拉取一次监控数据 */
  pollMonitor: () => Promise<void>;
  /** 启动监控轮询。重复调用不会创建第二个定时器 */
  startMonitorPolling: (interval?: number) => void;
  /** 停止监控轮询 */
  stopMonitorPolling: () => void;
}

/**
 * 工作台数据 Composable
 *
 * 聚合数据来自 `Admin/Index/Dashboard`，监控曲线来自 `Admin/Index/MonitorData`。
 * 页面只负责渲染，加载/错误/轮询生命周期集中在这里，便于单测覆盖。
 */
export function useWorkbench(): UseWorkbenchReturn {
  const api = usePageApi('Admin', 'Index');

  const data = ref<WorkbenchData | null>(null);
  const loading = ref(false);
  const error = ref('');
  const monitorPoints = ref<MonitorPoint[]>([]);

  let timer: ReturnType<typeof setInterval> | null = null;
  let polling = false;

  const latestMonitor = computed(() => monitorPoints.value[monitorPoints.value.length - 1] ?? null);
  const hasChartWidget = computed(() => data.value?.widgets?.some((item) => item.widgetType === 'Chart') === true);

  async function load(): Promise<void> {
    loading.value = true;
    error.value = '';
    try {
      const res = await api.getAction<WorkbenchData>('Dashboard');
      if (isSuccessCode(res?.code) && res.data) {
        data.value = res.data;
      } else {
        error.value = res?.message || '工作台数据加载失败';
      }
    } catch (ex) {
      error.value = ex instanceof Error ? ex.message : String(ex);
    } finally {
      loading.value = false;
    }
  }

  async function pollMonitor(): Promise<void> {
    // 上一轮未返回时跳过本轮，避免慢接口造成请求堆积
    if (polling) return;
    polling = true;
    try {
      const res = await api.getAction<MonitorSnapshot>('MonitorData');
      if (!isSuccessCode(res?.code)) return;

      const point = toMonitorPoint(res.data);
      if (point) monitorPoints.value = [...monitorPoints.value, point].slice(-MAX_MONITOR_POINTS);
    } catch {
      // 监控曲线属于增强信息，单次失败不打断工作台主流程，下一轮继续
    } finally {
      polling = false;
    }
  }

  function startMonitorPolling(interval = MONITOR_POLL_INTERVAL): void {
    if (timer) return;

    void pollMonitor();
    timer = setInterval(() => void pollMonitor(), interval);
  }

  function stopMonitorPolling(): void {
    if (timer) {
      clearInterval(timer);
      timer = null;
    }
  }

  return {
    data,
    loading,
    error,
    monitorPoints,
    latestMonitor,
    hasChartWidget,
    load,
    pollMonitor,
    startMonitorPolling,
    stopMonitorPolling,
  };
}
