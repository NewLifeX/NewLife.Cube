/**
 * useWorkbench 单元测试
 *
 * 覆盖工作台聚合数据加载、错误分支、监控点归一化与 5 秒轮询生命周期。
 * 运行：pnpm test:unit core/__tests__/useWorkbench.spec.ts
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

const h = vi.hoisted(() => ({
  getAction: vi.fn(),
}));

vi.mock('../composables/useCubeApi', () => ({
  usePageApi: () => ({ getAction: h.getAction }),
}));

import {
  MAX_MONITOR_POINTS,
  toMonitorPoint,
  useWorkbench,
} from '../composables/useWorkbench';

const dashboard = {
  user: { name: 'admin', displayName: '管理员', roles: ['Administrators'] },
  kpis: [{ name: 'UserCount', label: '用户总数', value: '12', color: 'blue' }],
  widgets: [{ name: 'Monitor', title: '性能监控', widgetType: 'Chart', cols: 8, data: null }],
  quickLinks: [],
};

describe('useWorkbench — 聚合数据加载', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('加载成功时写入数据并结束 loading 当 Dashboard 返回 code=0', async () => {
    h.getAction.mockResolvedValue({ code: 0, message: '', data: dashboard });
    const wb = useWorkbench();

    await wb.load();

    expect(h.getAction).toHaveBeenCalledWith('Dashboard');
    expect(wb.data.value?.kpis?.[0]?.value).toBe('12');
    expect(wb.loading.value).toBe(false);
    expect(wb.error.value).toBe('');
    expect(wb.hasChartWidget.value).toBe(true);
  });

  it('业务失败时保留错误消息且不写入数据 当 Dashboard 返回非成功码', async () => {
    h.getAction.mockResolvedValue({ code: 500, message: '数据库不可用', data: null });
    const wb = useWorkbench();

    await wb.load();

    expect(wb.data.value).toBeNull();
    expect(wb.error.value).toBe('数据库不可用');
    expect(wb.loading.value).toBe(false);
  });

  it('网络异常时把异常消息写入 error 当接口抛错', async () => {
    h.getAction.mockRejectedValue(new Error('network down'));
    const wb = useWorkbench();

    await wb.load();

    expect(wb.error.value).toBe('network down');
    expect(wb.loading.value).toBe(false);
  });

  it('没有图表部件时不启动轮询 当 widgets 只有内容卡', async () => {
    h.getAction.mockResolvedValue({ code: 0, data: { ...dashboard, widgets: [{ name: 'Profile', title: '个人信息', widgetType: 'Content', data: {} }] } });
    const wb = useWorkbench();

    await wb.load();

    expect(wb.hasChartWidget.value).toBe(false);
  });
});

describe('useWorkbench — 监控轮询', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('首轮立即拉取并按间隔追加监控点 当启动轮询时', async () => {
    h.getAction.mockResolvedValue({
      code: 0,
      data: { xs: ['10:00:00'], series: [[10], [20]], memUsed: 512, memTotal: 1024 },
    });
    const wb = useWorkbench();

    wb.startMonitorPolling(1000);
    await vi.advanceTimersByTimeAsync(0);
    expect(h.getAction).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(1000);
    expect(h.getAction).toHaveBeenCalledTimes(2);
    expect(wb.monitorPoints.value).toHaveLength(2);
    expect(wb.monitorPoints.value[0]).toMatchObject({ label: '10:00:00', cpu: 10, memory: 20 });
  });

  it('重复启动不会创建第二个定时器 当 startMonitorPolling 被重复调用', async () => {
    h.getAction.mockResolvedValue({ code: 0, data: { xs: ['t'], series: [[1], [2]] } });
    const wb = useWorkbench();

    wb.startMonitorPolling(1000);
    wb.startMonitorPolling(1000);
    await vi.advanceTimersByTimeAsync(0);
    await vi.advanceTimersByTimeAsync(1000);

    expect(h.getAction).toHaveBeenCalledTimes(2);
  });

  it('停止轮询后不再发请求 当调用 stopMonitorPolling', async () => {
    h.getAction.mockResolvedValue({ code: 0, data: { xs: ['t'], series: [[1], [2]] } });
    const wb = useWorkbench();

    wb.startMonitorPolling(1000);
    await vi.advanceTimersByTimeAsync(0);
    wb.stopMonitorPolling();
    await vi.advanceTimersByTimeAsync(3000);

    expect(h.getAction).toHaveBeenCalledTimes(1);
  });

  it('单次监控失败不打断后续轮询 当接口抛错', async () => {
    h.getAction.mockRejectedValueOnce(new Error('timeout')).mockResolvedValue({
      code: 0,
      data: { xs: ['t'], series: [[3], [4]] },
    });
    const wb = useWorkbench();

    wb.startMonitorPolling(1000);
    await vi.advanceTimersByTimeAsync(0);
    await vi.advanceTimersByTimeAsync(1000);

    expect(h.getAction).toHaveBeenCalledTimes(2);
    expect(wb.monitorPoints.value).toHaveLength(1);
  });

  it('监控点最多保留 MAX_MONITOR_POINTS 个 当持续轮询', async () => {
    h.getAction.mockResolvedValue({ code: 0, data: { xs: ['t'], series: [[1], [2]] } });
    const wb = useWorkbench();

    for (let i = 0; i < MAX_MONITOR_POINTS + 2; i++) await wb.pollMonitor();

    expect(wb.monitorPoints.value).toHaveLength(MAX_MONITOR_POINTS);
  });
});

describe('toMonitorPoint — 监控快照归一化', () => {
  it('返回空值 当快照为空', () => {
    expect(toMonitorPoint(null)).toBeNull();
    expect(toMonitorPoint(undefined)).toBeNull();
  });

  it('把数值收敛到 0-100 且保留内存信息', () => {
    expect(toMonitorPoint({ xs: ['09:30:00'], series: [[120], [-5]], memUsed: 2048, memTotal: 8192 })).toEqual({
      label: '09:30:00',
      cpu: 100,
      memory: 0,
      memUsed: 2048,
      memTotal: 8192,
    });
  });

  it('系列缺失时返回 null 当接口结构不完整', () => {
    expect(toMonitorPoint({ xs: ['t'], series: [] })).toBeNull();
    expect(toMonitorPoint({ xs: ['t'], series: [[Number.NaN], [1]] })).toBeNull();
  });
});
