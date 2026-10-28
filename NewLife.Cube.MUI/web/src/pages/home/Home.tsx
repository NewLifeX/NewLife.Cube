import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Divider,
  Grid,
  List,
  ListItem,
  ListItemText,
  Typography,
} from '@mui/material';
import RefreshIcon from '@mui/icons-material/Refresh';
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
} from '@newlifex/cube';
import api from '@/api';

/** 个人信息卡行标签，顺序与共享视图模型一致 */
const PROFILE_LABELS = ['登录次数', '最后登录', '登录地址', '注册时间'];

/** KPI 语义色 → MUI 调色板色值 */
const KPI_COLORS: Record<string, string> = {
  blue: '#1976d2',
  green: '#2e7d32',
  cyan: '#00838f',
  orange: '#ed6c02',
  red: '#d32f2f',
  purple: '#7b1fa2',
  grey: '#616161',
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
    return <Typography color="text.secondary">正在采集监控数据…</Typography>;
  }

  return (
    <Box>
      <Box component="svg" viewBox={`0 0 ${chart.width} ${chart.height}`} sx={{ width: '100%', height: 120 }}>
        <polyline points={chart.cpuPoints} fill="none" stroke="#1976d2" strokeWidth={2} />
        <polyline points={chart.memPoints} fill="none" stroke="#2e7d32" strokeWidth={2} />
      </Box>
      <Box sx={{ display: 'flex', gap: 2, fontSize: 12, color: 'text.secondary' }}>
        <span>CPU {latest?.cpu ?? 0}%</span>
        <span>内存 {latest?.memory ?? 0}%</span>
        {latest?.memUsed != null && latest?.memTotal ? (
          <span>
            {latest.memUsed} / {latest.memTotal} MB
          </span>
        ) : null}
      </Box>
    </Box>
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
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error && !view) {
    return (
      <Alert
        severity="error"
        action={
          <Button color="inherit" size="small" onClick={() => void load()}>
            重试
          </Button>
        }
      >
        {`工作台加载失败：${error}`}
      </Alert>
    );
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2, mb: 2 }}>
        <Box>
          <Typography variant="h5">
            {greetingOf(new Date().getHours())}，{view?.displayName || '-'}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {today}
            {view?.roleText ? ` · ${view.roleText}` : ''}
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          {layoutEditing ? (
            <>
              <Button variant="outlined" disabled={savingLayout} onClick={() => void saveLayoutChanges()}>保存</Button>
              <Button variant="outlined" disabled={savingLayout} onClick={() => void resetLayoutChanges()}>重置</Button>
              <Button variant="text" onClick={cancelLayoutEdit}>完成</Button>
            </>
          ) : (
            <Button variant="outlined" onClick={startLayoutEdit}>编辑布局</Button>
          )}
          <Button variant="outlined" startIcon={<RefreshIcon />} disabled={loading} onClick={() => void load()}>
            刷新
          </Button>
        </Box>
      </Box>

      {layoutTip ? (
        <Alert severity="info" sx={{ mb: 2 }}>
          {layoutTip}
        </Alert>
      ) : null}

      {layoutEditing && hiddenWidgets.length ? (
        <Box sx={{ mb: 2 }}>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>已隐藏的卡片</Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {hiddenWidgets.map((widget) => (
              <Chip key={widget.name} label={`${widget.title}（点击恢复）`} onClick={() => restoreWidget(widget.name)} />
            ))}
          </Box>
        </Box>
      ) : null}

      {error ? (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {error}
        </Alert>
      ) : null}

      {view?.kpis.length ? (
        <Grid container spacing={2} sx={{ mb: 2 }}>
          {view.kpis.map((kpi) => (
            <Grid key={kpi.name} item xs={12} sm={6} md={3}>
              <Card
                sx={{
                  height: '100%',
                  borderLeft: 4,
                  borderColor: KPI_COLORS[kpi.color] ?? KPI_COLORS.blue,
                  cursor: kpi.url ? 'pointer' : 'default',
                }}
                onClick={() => (kpi.url ? navigate(kpi.url) : undefined)}
              >
                <CardContent>
                  <Typography variant="body2" color="text.secondary">
                    {kpi.label}
                  </Typography>
                  <Typography variant="h4" sx={{ mt: 0.5 }}>
                    {kpi.value}
                  </Typography>
                  {kpi.trend ? (
                    <Typography variant="caption" color="text.secondary">
                      {kpi.trend}
                    </Typography>
                  ) : null}
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      ) : null}

      {visibleWidgets.length ? (
        <Grid container spacing={2}>
          {visibleWidgets.map((widget) => (
            <Grid
              key={widget.name}
              item
              xs={12}
              md={widget.cols}
              draggable={layoutEditing}
              onDragStart={() => { draggingRef.current = widget.name; }}
              onDragOver={(e) => e.preventDefault()}
              onDrop={() => onDropOn(widget.name)}
            >
              <Card sx={{ height: '100%' }} data-widget={widget.name}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography variant="subtitle1" sx={{ flex: 1 }}>
                      {widget.title}
                    </Typography>
                    {layoutEditing ? (
                      <Button size="small" onClick={() => hideWidget(widget.name)}>隐藏</Button>
                    ) : null}
                  </Box>
                  <WidgetContent widget={widget} monitorPoints={monitorPoints} onNavigate={(url) => navigate(url)} />
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      ) : null}

      {view && !view.hasContent ? (
        <Typography color="text.secondary" sx={{ py: 6, textAlign: 'center' }}>
          暂无工作台数据
        </Typography>
      ) : null}
    </Box>
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
      <Box>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1.5 }}>
          <Box
            sx={{
              width: 44,
              height: 44,
              borderRadius: '50%',
              bgcolor: 'primary.main',
              color: 'primary.contrastText',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontWeight: 600,
            }}
          >
            {widget.profile.initial}
          </Box>
          <Box>
            <Typography sx={{ fontWeight: 600 }}>{widget.profile.displayName}</Typography>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mt: 0.5 }}>
              <Chip
                size="small"
                color={widget.profile.online ? 'success' : 'default'}
                label={widget.profile.online ? '在线' : '离线'}
              />
              {widget.profile.roles.map((role) => (
                <Chip key={role} size="small" variant="outlined" label={role} />
              ))}
            </Box>
          </Box>
        </Box>
        <Grid container spacing={1}>
          {widget.profile.rows.map((row, index) => (
            <Grid key={row.key} item xs={6}>
              <Typography variant="caption" color="text.secondary">
                {PROFILE_LABELS[index] || row.key}
              </Typography>
              <Typography variant="body2">{row.text}</Typography>
            </Grid>
          ))}
        </Grid>
      </Box>
    );
  }

  if (widget.kind === 'quickLinks') {
    if (!widget.links?.length) return <Typography color="text.secondary">暂无快捷入口</Typography>;
    return (
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
        {widget.links.map((link) => (
          <Chip key={link.url} clickable label={link.name} variant="outlined" onClick={() => onNavigate(link.url)} />
        ))}
      </Box>
    );
  }

  if (widget.kind === 'loginLog') {
    const sections = [
      { title: '最近登录', items: widget.logins ?? [], empty: '暂无登录记录' },
      { title: '在线用户', items: widget.onlines ?? [], empty: '暂无在线用户' },
    ];
    return (
      <Grid container spacing={2}>
        {sections.map((section) => (
          <Grid key={section.title} item xs={12} sm={6}>
            <Typography variant="caption" color="text.secondary">
              {section.title}
            </Typography>
            {section.items.length ? (
              <List dense disablePadding>
                {section.items.map((item, index) => (
                  <ListItem key={`${section.title}-${index}`} disableGutters>
                    <ListItemText primary={item.title} secondary={`${item.subtitle} · ${item.time}`} />
                  </ListItem>
                ))}
              </List>
            ) : (
              <Typography variant="body2" color="text.secondary">
                {section.empty}
              </Typography>
            )}
          </Grid>
        ))}
      </Grid>
    );
  }

  if (widget.kind === 'kv' && widget.rows?.length) {
    return (
      <Box>
        {widget.rows.map((row, index) => (
          <Box key={row.key}>
            {index > 0 ? <Divider /> : null}
            <Box sx={{ display: 'flex', gap: 2, py: 0.75 }}>
              <Typography variant="body2" color="text.secondary" sx={{ minWidth: 96 }}>
                {row.key}
              </Typography>
              {row.link ? (
                <Typography
                  variant="body2"
                  component="a"
                  href={row.link.href}
                  target={row.link.external ? '_blank' : undefined}
                  rel="noopener noreferrer"
                >
                  {row.link.text}
                </Typography>
              ) : (
                <Typography variant="body2">{row.text}</Typography>
              )}
            </Box>
          </Box>
        ))}
      </Box>
    );
  }

  if (widget.kind === 'text') return <Typography variant="body2">{widget.text}</Typography>;

  return <Typography color="text.secondary">暂无数据</Typography>;
}

