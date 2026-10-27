import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  appendMonitorPoint,
  buildMonitorChart,
  buildWorkbenchView,
  buildWidgetLayout,
  createWidgetLayoutLoader,
  createWorkbenchLoader,
  mergeWidgetsWithLayout,
  moveWidget,
  MONITOR_POLL_INTERVAL,
  type MonitorPoint,
  type WidgetLayoutMap,
  type WorkbenchData,
  type WorkbenchWidgetView,
} from '@newlifex/page-logic';
import api from '@/api';

/** 个人信息卡行标签，顺序与共享视图模型一致 */
const PROFILE_LABELS = ['登录次数', '最后登录', '登录地址', '注册时间'];

/** KPI 语义色 → Tailwind 边框类 */
const KPI_BORDER: Record<string, string> = {
  blue: 'border-l-blue-500',
  green: 'border-l-green-600',
  cyan: 'border-l-cyan-600',
  orange: 'border-l-orange-500',
  red: 'border-l-red-600',
  purple: 'border-l-purple-600',
  grey: 'border-l-gray-500',
};

function greetingOf(hour: number): string {
  if (hour < 6) return '夜深了';
  if (hour < 12) return '早上好';
  if (hour < 14) return '中午好';
  if (hour < 18) return '下午好';
  return '晚上好';
}

function MonitorCardBody({ points }: { points: MonitorPoint[] }) {
  const chart = useMemo(() => buildMonitorChart(points, 320, 120), [points]);
  const latest = points[points.length - 1];

  if (!points.length) {
    return <p className="text-sm text-muted-foreground">正在采集监控数据…</p>;
  }

  return (
    <div>
      <svg viewBox={`0 0 ${chart.width} ${chart.height}`} className="h-32 w-full" preserveAspectRatio="none">
        <polyline points={chart.cpuPoints} fill="none" stroke="#3b82f6" strokeWidth={2} vectorEffect="non-scaling-stroke" />
        <polyline points={chart.memPoints} fill="none" stroke="#16a34a" strokeWidth={2} vectorEffect="non-scaling-stroke" />
      </svg>
      <div className="flex gap-4 text-xs text-muted-foreground">
        <span>CPU {latest?.cpu ?? 0}%</span>
        <span>内存 {latest?.memory ?? 0}%</span>
        {latest?.memUsed != null && latest?.memTotal ? (
          <span>
            {latest.memUsed} / {latest.memTotal} MB
          </span>
        ) : null}
      </div>
    </div>
  );
}

export default function Home() {
  const navigate = useNavigate();
  const loader = useMemo(() => createWorkbenchLoader(api), []);
  const layoutLoader = useMemo(() => createWidgetLayoutLoader(api), []);
  const [raw, setRaw] = useState<WorkbenchData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [monitorPoints, setMonitorPoints] = useState<MonitorPoint[]>([]);

  // --- 卡片布局：拖动排序 / 隐藏 / 持久化 ---
  const [layout, setLayout] = useState<WidgetLayoutMap>({});
  const [visibleWidgets, setVisibleWidgets] = useState<WorkbenchWidgetView[]>([]);
  const [hiddenWidgets, setHiddenWidgets] = useState<WorkbenchWidgetView[]>([]);
  const [layoutEditing, setLayoutEditing] = useState(false);
  const [savingLayout, setSavingLayout] = useState(false);
  const [layoutTip, setLayoutTip] = useState('');
  const draggingRef = useRef('');
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const pollingRef = useRef(false);

  const view = useMemo(() => (raw ? buildWorkbenchView(raw) : null), [raw]);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const result = await loader.load();
      if (result.data) {
        setRaw(result.data);
        const map = await layoutLoader.load();
        setLayout(map);
        const merged = mergeWidgetsWithLayout(buildWorkbenchView(result.data).widgets, map);
        setVisibleWidgets(merged.visible);
        setHiddenWidgets(merged.hidden);
      } else setError(result.error || '工作台加载失败');
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : String(ex));
    } finally {
      setLoading(false);
    }
  }, [loader, layoutLoader]);

  const applyLayout = useCallback((map: WidgetLayoutMap, widgets = view?.widgets ?? []) => {
    const merged = mergeWidgetsWithLayout(widgets, map);
    setVisibleWidgets(merged.visible);
    setHiddenWidgets(merged.hidden);
  }, [view]);

  const startLayoutEdit = () => {
    setLayoutEditing(true);
    setLayoutTip('拖动卡片调整顺序，点击隐藏按钮可隐藏卡片');
  };

  const cancelLayoutEdit = () => {
    setLayoutEditing(false);
    setLayoutTip('');
    applyLayout(layout);
  };

  const onDropOn = (target: string) => {
    if (!layoutEditing || !draggingRef.current) return;
    setVisibleWidgets((list) => moveWidget(list, draggingRef.current, target));
    draggingRef.current = '';
  };

  const hideWidget = (name: string) => {
    setVisibleWidgets((list) => {
      const target = list.find((item) => item.name === name);
      if (!target) return list;
      setHiddenWidgets((hidden) => [...hidden, target]);
      return list.filter((item) => item.name !== name);
    });
  };

  const restoreWidget = (name: string) => {
    setHiddenWidgets((hidden) => {
      const target = hidden.find((item) => item.name === name);
      if (!target) return hidden;
      setVisibleWidgets((list) => [...list, target]);
      return hidden.filter((item) => item.name !== name);
    });
  };

  const saveLayoutChanges = async () => {
    setSavingLayout(true);
    try {
      const map = buildWidgetLayout(visibleWidgets, hiddenWidgets);
      if (await layoutLoader.save(map)) {
        setLayout(map);
        setLayoutTip('布局已保存');
      } else setLayoutTip('布局保存失败');
    } finally {
      setSavingLayout(false);
    }
  };

  const resetLayoutChanges = async () => {
    setSavingLayout(true);
    try {
      if (await layoutLoader.reset()) {
        setLayout({});
        setLayoutTip('布局已重置');
        applyLayout({});
      } else setLayoutTip('布局重置失败');
    } finally {
      setSavingLayout(false);
    }
  };

  const pollMonitor = useCallback(async () => {
    if (pollingRef.current) return;
    pollingRef.current = true;
    try {
      const point = await loader.pollMonitor();
      setMonitorPoints((prev) => appendMonitorPoint(prev, point));
    } catch {
      // 监控曲线属于增强信息，单次失败不打断工作台主流程
    } finally {
      pollingRef.current = false;
    }
  }, [loader]);

  const stopPolling = useCallback(() => {
    if (!timerRef.current) return;
    clearInterval(timerRef.current);
    timerRef.current = null;
  }, []);

  const startPolling = useCallback(() => {
    if (timerRef.current) return;
    void pollMonitor();
    timerRef.current = setInterval(() => void pollMonitor(), MONITOR_POLL_INTERVAL);
  }, [pollMonitor]);

  useEffect(() => {
    void load();
    return stopPolling;
  }, [load, stopPolling]);

  useEffect(() => {
    if (view?.hasChartWidget) startPolling();
    else stopPolling();
  }, [view?.hasChartWidget, startPolling, stopPolling]);

  const today = new Date().toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    weekday: 'long',
  });

  if (loading && !view) {
    return <p className="py-12 text-center text-sm text-muted-foreground">正在加载工作台…</p>;
  }

  if (error && !view) {
    return (
      <div className="rounded-lg border border-destructive/40 bg-destructive/5 p-6 text-sm">
        <p className="font-medium text-destructive">工作台加载失败：{error}</p>
        <Button className="mt-3" variant="outline" onClick={() => void load()}>
          重试
        </Button>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold">
            {greetingOf(new Date().getHours())}，{view?.displayName || '-'}
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {today}
            {view?.roleText ? ` · ${view.roleText}` : ''}
          </p>
        </div>
        <div className="flex gap-2">
          {layoutEditing ? (
            <>
              <Button variant="outline" disabled={savingLayout} onClick={() => void saveLayoutChanges()}>保存</Button>
              <Button variant="outline" disabled={savingLayout} onClick={() => void resetLayoutChanges()}>重置</Button>
              <Button variant="ghost" onClick={cancelLayoutEdit}>完成</Button>
            </>
          ) : (
            <Button variant="outline" onClick={startLayoutEdit}>编辑布局</Button>
          )}
          <Button variant="outline" disabled={loading} onClick={() => void load()}>
            刷新
          </Button>
        </div>
      </div>

      {layoutTip ? (
        <p className="mb-4 rounded-md border border-blue-300 bg-blue-50 p-3 text-sm text-blue-800">{layoutTip}</p>
      ) : null}

      {layoutEditing && hiddenWidgets.length ? (
        <div className="mb-4">
          <p className="mb-2 text-sm text-muted-foreground">已隐藏的卡片</p>
          <div className="flex flex-wrap gap-2">
            {hiddenWidgets.map((widget) => (
              <button
                key={widget.name}
                type="button"
                className="rounded-full border px-3 py-1 text-sm hover:bg-muted"
                onClick={() => restoreWidget(widget.name)}
              >
                {widget.title}（点击恢复）
              </button>
            ))}
          </div>
        </div>
      ) : null}

      {error ? (
        <p className="mb-4 rounded-md border border-amber-300 bg-amber-50 p-3 text-sm text-amber-800">{error}</p>
      ) : null}

      {view?.kpis.length ? (
        <div className="mb-4 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {view.kpis.map((kpi) => (
            <div
              key={kpi.name}
              className={`rounded-lg border border-l-4 bg-card p-4 ${KPI_BORDER[kpi.color] ?? KPI_BORDER.blue} ${
                kpi.url ? 'cursor-pointer hover:shadow-sm' : ''
              }`}
              onClick={() => (kpi.url ? navigate(kpi.url) : undefined)}
            >
              <p className="text-sm text-muted-foreground">{kpi.label}</p>
              <p className="mt-1 text-3xl font-bold">{kpi.value}</p>
              {kpi.trend ? <p className="mt-1 text-xs text-muted-foreground">{kpi.trend}</p> : null}
            </div>
          ))}
        </div>
      ) : null}

      {visibleWidgets.length ? (
        <div className="grid grid-cols-12 gap-4">
          {visibleWidgets.map((widget) => (
            <div
              key={widget.name}
              className="col-span-12 rounded-lg border bg-card p-4"
              data-widget={widget.name}
              style={{ gridColumn: `span ${widget.cols} / span ${widget.cols}` }}
              draggable={layoutEditing}
              onDragStart={() => { draggingRef.current = widget.name; }}
              onDragOver={(e) => e.preventDefault()}
              onDrop={() => onDropOn(widget.name)}
            >
              <div className="mb-3 flex items-center gap-2">
                <h2 className="text-base font-semibold">{widget.title}</h2>
                {layoutEditing ? (
                  <button type="button" className="ml-auto text-sm underline" onClick={() => hideWidget(widget.name)}>隐藏</button>
                ) : null}
              </div>
              <WidgetContent widget={widget} monitorPoints={monitorPoints} onNavigate={(url) => navigate(url)} />
            </div>
          ))}
        </div>
      ) : null}

      {view && !view.hasContent ? (
        <p className="py-16 text-center text-sm text-muted-foreground">暂无工作台数据</p>
      ) : null}
    </div>
  );
}

function WidgetContent({
  widget,
  monitorPoints,
  onNavigate,
}: {
  widget: WorkbenchWidgetView;
  monitorPoints: MonitorPoint[];
  onNavigate: (url: string) => void;
}) {
  if (widget.kind === 'monitor') return <MonitorCardBody points={monitorPoints} />;

  if (widget.kind === 'profile' && widget.profile) {
    return (
      <div>
        <div className="mb-3 flex items-center gap-3">
          <div className="flex h-11 w-11 items-center justify-center rounded-full bg-primary font-semibold text-primary-foreground">
            {widget.profile.initial}
          </div>
          <div>
            <p className="font-medium">{widget.profile.displayName}</p>
            <div className="mt-1 flex flex-wrap gap-1">
              <Badge variant={widget.profile.online ? 'default' : 'secondary'}>
                {widget.profile.online ? '在线' : '离线'}
              </Badge>
              {widget.profile.roles.map((role) => (
                <Badge key={role} variant="outline">
                  {role}
                </Badge>
              ))}
            </div>
          </div>
        </div>
        <div className="grid grid-cols-2 gap-2 text-sm">
          {widget.profile.rows.map((row, index) => (
            <div key={row.key}>
              <p className="text-xs text-muted-foreground">{PROFILE_LABELS[index] || row.key}</p>
              <p>{row.text}</p>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (widget.kind === 'quickLinks') {
    if (!widget.links?.length) return <p className="text-sm text-muted-foreground">暂无快捷入口</p>;
    return (
      <div className="flex flex-wrap gap-2">
        {widget.links.map((link) => (
          <Button key={link.url} variant="outline" size="sm" onClick={() => onNavigate(link.url)}>
            {link.name}
          </Button>
        ))}
      </div>
    );
  }

  if (widget.kind === 'loginLog') {
    const sections = [
      { title: '最近登录', items: widget.logins ?? [], empty: '暂无登录记录' },
      { title: '在线用户', items: widget.onlines ?? [], empty: '暂无在线用户' },
    ];
    return (
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        {sections.map((section) => (
          <div key={section.title}>
            <p className="mb-1 text-xs text-muted-foreground">{section.title}</p>
            {section.items.length ? (
              <ul className="divide-y">
                {section.items.map((item, index) => (
                  <li key={`${section.title}-${index}`} className="flex items-center justify-between gap-2 py-1 text-sm">
                    <span className="font-medium">{item.title}</span>
                    <span className="text-xs text-muted-foreground">
                      {item.subtitle} · {item.time}
                    </span>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-sm text-muted-foreground">{section.empty}</p>
            )}
          </div>
        ))}
      </div>
    );
  }

  if (widget.kind === 'kv' && widget.rows?.length) {
    return (
      <dl className="divide-y">
        {widget.rows.map((row) => (
          <div key={row.key} className="flex gap-4 py-1.5 text-sm">
            <dt className="min-w-24 text-muted-foreground">{row.key}</dt>
            <dd>
              {row.link ? (
                <a href={row.link.href} target={row.link.external ? '_blank' : undefined} rel="noopener noreferrer">
                  {row.link.text}
                </a>
              ) : (
                row.text
              )}
            </dd>
          </div>
        ))}
      </dl>
    );
  }

  if (widget.kind === 'text') return <p className="text-sm">{widget.text}</p>;

  return <p className="text-sm text-muted-foreground">暂无数据</p>;
}

