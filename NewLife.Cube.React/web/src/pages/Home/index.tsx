/**
 * 首页 / 工作台（AntD6 风格，命名与 MVC 版统一为 Dashboard）
 *
 * - 欢迎横幅：时间段问候 + 昵称 + 日期/角色 + 操作按钮（对齐 MVC Index/Dashboard）
 * - 工作台卡片（卡片三能力）：拖动排序 / 保存顺序 / 隐藏，布局按用户持久化到服务端（Parameter）
 * - KPI 卡行：后端工作台接口（/Admin/Index/Dashboard）数据优先，缺失时降级个人统计；点击跳转
 * - 性能监控：轮询 /Admin/Index/MonitorData（对齐 MVC 契约 {xs, series}）
 * - 快捷入口 + 常用菜单：菜单树叶子递归收集，接口数据优先
 * - 系统信息 / 个人信息：来自工作台接口，缺失时隐藏
 */
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import {
  AppstoreAddOutlined,
  AppstoreOutlined,
  BugOutlined,
  CalendarOutlined,
  ClockCircleOutlined,
  DashboardOutlined,
  DesktopOutlined,
  EyeInvisibleOutlined,
  EyeOutlined,
  FileTextOutlined,
  HolderOutlined,
  LineChartOutlined,
  MoreOutlined,
  ReloadOutlined,
  SaveOutlined,
  TeamOutlined,
  ThunderboltOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { App, Button, Card, Dropdown, Empty, Skeleton, Space, Tag } from 'antd';
import * as echarts from 'echarts';
import { useNavigate } from 'react-router-dom';
import { api } from '@/api';
import { useUserStore } from '@/stores/user';
import { getConfig } from '@/configure';
import { useDashboard, type DashboardKpi } from '@/hooks/useDashboard';
import type { LoginConfig, MenuItem } from '@cube/api-core';

/** KPI 语义名 → 图标（MVC Widget 部件名优先，兼容旧语义名降级数据） */
const KPI_ICONS: Record<string, ReactNode> = {
  // MVC Widget 部件名（后端 Dashboard 由 WidgetManager 驱动返回）
  UserCount: <TeamOutlined />,
  TodayLogin: <UserOutlined />,
  OnlineCount: <AppstoreOutlined />,
  Log24h: <FileTextOutlined />,
  Error24h: <BugOutlined />,
  CpuRate: <LineChartOutlined />,
  MyDays: <CalendarOutlined />,
  MyLogins: <ClockCircleOutlined />,
  // 兼容旧语义名（接口不可用时的降级数据）
  users: <TeamOutlined />,
  login: <UserOutlined />,
  online: <AppstoreOutlined />,
  log: <FileTextOutlined />,
  error: <BugOutlined />,
  cpu: <LineChartOutlined />,
};

/** 工作台卡片布局项（排序 + 隐藏），对应后端 WidgetLayout */
interface WidgetLayoutItem {
  sort?: number;
  hide?: boolean;
}

/** 工作台卡片清单：默认顺序 + 标题 + 图标（对齐 MVC 卡头图标样式） */
const WIDGET_DEFAULT_ORDER = ['kpi', 'monitor', 'quick', 'menus', 'profile', 'sysinfo'];
const WIDGET_TITLES: Record<string, string> = {
  kpi: '指标概览',
  monitor: '性能监控',
  quick: '快捷入口',
  menus: '常用菜单',
  profile: '个人信息',
  sysinfo: '系统信息',
};
const WIDGET_ICONS: Record<string, ReactNode> = {
  kpi: <DashboardOutlined />,
  monitor: <LineChartOutlined />,
  quick: <ThunderboltOutlined />,
  menus: <AppstoreOutlined />,
  profile: <UserOutlined />,
  sysinfo: <DesktopOutlined />,
};

/**
 * 布局 key 归一化：后端 Json 序列化强制 camelCase（字典 key 首字母小写），
 * KPI 部件名（UserCount）与卡片名（kpi）统一转首字母小写（userCount / kpi）后访问布局
 */
export const layoutKey = (name?: string) => (name ? name.charAt(0).toLowerCase() + name.slice(1) : '');

/** 按用户布局过滤 + 排序 KPI（隐藏项剔除，未排序项按后端顺序兜底） */
export const sortKpisByLayout = (all: DashboardKpi[], layout: Record<string, WidgetLayoutItem>) => {
  const order = (k: DashboardKpi) => layout[layoutKey(k.name)]?.sort ?? 999;
  return all.filter((k) => !layout[layoutKey(k.name)]?.hide).sort((a, b) => order(a) - order(b));
};

/** 用户已隐藏的 KPI（恢复面板用） */
export const filterHiddenKpis = (all: DashboardKpi[], layout: Record<string, WidgetLayoutItem>) =>
  all.filter((k) => layout[layoutKey(k.name)]?.hide);

export default function HomePage() {
  const { message } = App.useApp();
  const navigate = useNavigate();
  const userInfo = useUserStore((s) => s.userInfo);
  const menus = useUserStore((s) => s.menus);
  const { data: wb, reload: reloadWb } = useDashboard();
  const [config, setConfig] = useState<LoginConfig | null>(null);
  const [now, setNow] = useState(new Date());
  const chartRef = useRef<HTMLDivElement>(null);
  const chartInstRef = useRef<echarts.ECharts | null>(null);
  const [monitor, setMonitor] = useState<{ xs: string[]; cpu: number[]; mem: number[] }>({ xs: [], cpu: [], mem: [] });

  useEffect(() => {
    api.user
      .getLoginConfig()
      .then((res) => setConfig(res.data ?? null))
      .catch(() => {});
  }, []);

  // 时钟
  useEffect(() => {
    const t = setInterval(() => setNow(new Date()), 60000);
    return () => clearInterval(t);
  }, []);

  // 监控数据轮询（对齐 MVC MonitorData 契约：{ xs, series: [[cpu],[mem]] }）
  useEffect(() => {
    let cancelled = false;
    const poll = async () => {
      try {
        const res = await api.client.get('/Admin/Index/MonitorData');
        const d = (res.data as { data?: { xs?: string[]; series?: number[][] } })?.data ?? (res.data as { xs?: string[]; series?: number[][] });
        if (!d || cancelled) return;
        setMonitor((prev) => ({
          xs: [...prev.xs, ...(d.xs ?? [])].slice(-60),
          cpu: [...prev.cpu, ...(d.series?.[0] ?? [])].slice(-60),
          mem: [...prev.mem, ...(d.series?.[1] ?? [])].slice(-60),
        }));
      } catch {
        /* 接口不可用静默 */
      }
    };
    void poll();
    const t = setInterval(() => void poll(), 5000);
    return () => {
      cancelled = true;
      clearInterval(t);
    };
  }, []);

  // 性能曲线渲染（文字/边框色取主题 CSS 变量，明暗两套自适应）
  useEffect(() => {
    if (!chartRef.current || monitor.xs.length < 2) return;
    // 卡片隐藏后重新显示时，旧实例已挂在已卸载节点上，先释放再重建
    chartInstRef.current?.dispose();
    const textColor =
      getComputedStyle(document.body).getPropertyValue('--cube-text-muted').trim() || '#94a3b8';
    const borderColor =
      getComputedStyle(document.body).getPropertyValue('--cube-border-soft').trim() || 'rgba(148,163,184,.4)';
    const chart = echarts.init(chartRef.current);
    chartInstRef.current = chart;
    chart.setOption({
      tooltip: { trigger: 'axis' },
      legend: { data: ['CPU', '内存'], top: 0, right: 0, textStyle: { color: textColor } },
      grid: { left: 8, right: 8, top: 32, bottom: 0, containLabel: true },
      xAxis: {
        type: 'category',
        data: monitor.xs,
        boundaryGap: false,
        axisLine: { lineStyle: { color: borderColor } },
        axisLabel: { color: textColor },
      },
      yAxis: {
        type: 'value',
        min: 0,
        max: 100,
        splitLine: { lineStyle: { color: borderColor } },
        axisLabel: { color: textColor, formatter: '{value}%' },
      },
      series: [
        { name: 'CPU', type: 'line', smooth: true, showSymbol: false, data: monitor.cpu, lineStyle: { width: 2 }, areaStyle: { opacity: 0.1 } },
        { name: '内存', type: 'line', smooth: true, showSymbol: false, data: monitor.mem, lineStyle: { width: 2 }, areaStyle: { opacity: 0.1 } },
      ],
    });
    const onResize = () => chart.resize();
    window.addEventListener('resize', onResize);
    return () => {
      window.removeEventListener('resize', onResize);
      chart.dispose();
      if (chartInstRef.current === chart) chartInstRef.current = null;
    };
  }, [monitor]);

  // ── 工作台卡片布局（卡片三能力：拖动排序 / 保存顺序 / 隐藏）──
  const [layout, setLayout] = useState<Record<string, WidgetLayoutItem>>({});
  const [dragKey, setDragKey] = useState<string | null>(null);

  // 读取用户布局（后端 Parameter 持久化）
  useEffect(() => {
    let cancelled = false;
    api.client
      .get('/Admin/Index/GetWidgetLayout')
      .then((res) => {
        if (cancelled) return;
        const data = (res.data as { data?: Record<string, WidgetLayoutItem> })?.data;
        if (data && Object.keys(data).length) setLayout(data);
      })
      .catch(() => {
        /* 布局接口不可用静默 */
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const handleSaveLayout = () => {
    api.client
      .post('/Admin/Index/SaveWidgetLayout', layout)
      .then(() => message.success('布局已保存'))
      .catch(() => message.error('保存失败'));
  };

  const handleResetLayout = () => {
    setLayout({});
    api.client
      .post('/Admin/Index/ResetWidgetLayout')
      .then(() => message.success('布局已重置'))
      .catch(() => message.error('重置失败'));
  };

  const handleHideWidget = (key: string) =>
    setLayout((prev) => ({ ...prev, [key]: { ...(prev[key] ?? {}), hide: true } }));

  const handleUnhideWidget = (key: string) =>
    setLayout((prev) => {
      const next = { ...prev };
      next[key] = { ...(next[key] ?? {}), hide: false };
      return next;
    });

  // 拖拽落点：把拖拽卡插入目标卡位置，重新分配可见卡排序
  const handleDrop = (target: string) => {
    if (!dragKey || dragKey === target) {
      setDragKey(null);
      return;
    }
    setLayout((prev) => {
      const order = WIDGET_DEFAULT_ORDER.filter((k) => !prev[k]?.hide && k !== dragKey);
      const idx = order.indexOf(target);
      order.splice(idx < 0 ? order.length : idx, 0, dragKey);
      const next = { ...prev };
      order.forEach((k, i) => {
        next[k] = { ...(next[k] ?? {}), sort: i };
      });
      return next;
    });
    setDragKey(null);
  };

  // 常用菜单入口：递归收集叶子菜单（过滤后端区域根菜单 ~/ 与隐藏项）
  const topMenus = useMemo(() => {
    const leaves: MenuItem[] = [];
    const walk = (items: MenuItem[]) => {
      for (const item of items) {
        if (item.children?.length) {
          walk(item.children);
        } else if (item.visible !== false && item.url && item.url !== '~' && !item.url.startsWith('~/')) {
          leaves.push(item);
        }
      }
    };
    walk(menus);
    return leaves.slice(0, 12);
  }, [menus]);

  // KPI：接口部件数据（可见 + 已隐藏）优先，缺失时降级个人统计
  const allKpis = useMemo<DashboardKpi[]>(() => {
    if (wb?.kpis?.length || wb?.hiddenKpis?.length) return [...(wb.kpis ?? []), ...(wb.hiddenKpis ?? [])];
    return [
      { name: 'login', label: '我的登录', value: '—', color: 'green', trend: '累计登录次数' },
      { name: 'online', label: '在线状态', value: '在线', color: 'cyan', trend: '当前账号状态' },
      { name: 'users', label: '我的角色', value: userInfo?.roleName || '—', color: 'purple', trend: '当前角色' },
    ];
  }, [wb, userInfo]);

  // 可见 KPI：按用户布局隐藏过滤 + 排序（camelCase 布局 key，与后端 Json 输出对齐）
  const kpis = useMemo<DashboardKpi[]>(() => sortKpisByLayout(allKpis, layout), [allKpis, layout]);

  // 已隐藏 KPI（恢复面板用）
  const hiddenKpis = useMemo<DashboardKpi[]>(() => filterHiddenKpis(allKpis, layout), [allKpis, layout]);

  // ── KPI 小卡交互（对齐 MVC 工作台：拖动排序 + 隐藏/恢复）──
  const [kpiDragKey, setKpiDragKey] = useState<string | null>(null);

  // KPI 拖拽落点：把拖拽卡插入目标卡位置，重新分配可见 KPI 排序（部件名作为布局 key）
  const handleKpiDrop = (target: string) => {
    if (!kpiDragKey || kpiDragKey === target) {
      setKpiDragKey(null);
      return;
    }
    setLayout((prev) => {
      const visible = kpis.filter((k) => k.name !== kpiDragKey);
      const idx = visible.findIndex((k) => k.name === target);
      visible.splice(idx < 0 ? visible.length : idx, 0, kpis.find((k) => k.name === kpiDragKey)!);
      const next = { ...prev };
      visible.forEach((k, i) => {
        if (k.name) next[layoutKey(k.name)] = { ...(next[layoutKey(k.name)] ?? {}), sort: i };
      });
      return next;
    });
    setKpiDragKey(null);
  };

  const handleHideKpi = (k: DashboardKpi) =>
    setLayout((prev) => ({ ...prev, [layoutKey(k.name)]: { ...(prev[layoutKey(k.name)] ?? {}), hide: true } }));

  const handleUnhideKpi = (k: DashboardKpi) =>
    setLayout((prev) => {
      const next = { ...prev };
      next[layoutKey(k.name)] = { ...(next[layoutKey(k.name)] ?? {}), hide: false };
      return next;
    });

  // 快捷入口：接口数据优先，降级取常用菜单前 8 项
  const quickLinks = useMemo(() => {
    if (wb?.quickLinks?.length) return wb.quickLinks;
    return topMenus.slice(0, 8).map((m) => ({ name: m.displayName || m.name, url: m.url!, icon: m.icon }));
  }, [wb, topMenus]);

  const sysInfo = wb?.sysInfo;
  const userCard = wb?.user;
  const profile = wb?.profile as Record<string, unknown> | undefined;

  const today = `${now.getFullYear()}年${now.getMonth() + 1}月${now.getDate()}日 星期${'日一二三四五六'[now.getDay()]}`;
  const displayName = userCard?.displayName || userCard?.name || userInfo?.displayName || userInfo?.name || '用户';
  const roles = userCard?.roles?.join(' / ') || userInfo?.roleName || '';
  // 时间段问候（对齐 MVC Index/Dashboard：夜深了/早上好/中午好/下午好/晚上好）
  const greet =
    now.getHours() < 6
      ? '夜深了'
      : now.getHours() < 12
        ? '早上好'
        : now.getHours() < 14
          ? '中午好'
          : now.getHours() < 18
            ? '下午好'
            : '晚上好';

  // 工作台卡片布局：默认顺序 + 用户布局（排序/隐藏）
  const sortOf = (key: string) => layout[key]?.sort ?? WIDGET_DEFAULT_ORDER.indexOf(key);
  const visibleWidgets = WIDGET_DEFAULT_ORDER.filter((key) => !layout[key]?.hide).sort((a, b) => sortOf(a) - sortOf(b));
  const hiddenWidgets = WIDGET_DEFAULT_ORDER.filter((key) => layout[key]?.hide);
  const hasSysInfo = !!sysInfo && Object.keys(sysInfo).length > 0;
  const widgetsToRender = visibleWidgets.filter((key) => key !== 'sysinfo' || hasSysInfo);

  /** 渲染工作台卡片正文 */
  const renderWidgetBody = (key: string): ReactNode => {
    switch (key) {
      case 'kpi':
        return (
          <div className="cube-home-kpi-grid">
            {kpis.map((k) => (
              <div
                key={k.name || k.label}
                className="cube-home-kpi-card"
                onClick={(e) => {
                  // 拖动手柄 / 隐藏菜单（含 antd 弹层 portal，事件按组件树冒泡到卡片）不触发卡片跳转
                  const target = e.target as HTMLElement;
                  if (target.closest('.cube-home-kpi-actions') || target.closest('.ant-dropdown')) return;
                  if (k.url) navigate(k.url);
                }}
                onDragOver={(e) => {
                  if (kpiDragKey && kpiDragKey !== (k.name ?? '')) e.preventDefault();
                }}
                onDrop={(e) => {
                  e.preventDefault();
                  handleKpiDrop(k.name ?? '');
                }}
                style={k.url ? { cursor: 'pointer' } : undefined}
              >
                <div className="cube-home-kpi-head">
                  <div className="cube-home-kpi-head-left">
                    <div className={`cube-home-kpi-icon ${k.color || 'blue'}`}>{KPI_ICONS[k.name ?? ''] ?? null}</div>
                    <span className="cube-home-kpi-label">{k.label}</span>
                  </div>
                  <span className="cube-home-kpi-actions">
                    <span
                      className="cube-kpi-drag-handle"
                      title="拖动排序"
                      draggable
                      onDragStart={(e) => {
                        e.stopPropagation();
                        e.dataTransfer.effectAllowed = 'move';
                        e.dataTransfer.setData('text/plain', k.name ?? '');
                        setKpiDragKey(k.name ?? '');
                      }}
                      onDragEnd={() => setKpiDragKey(null)}
                    >
                      <HolderOutlined />
                    </span>
                    <Dropdown
                      trigger={['click']}
                      menu={{
                        items: [{ key: 'hide', icon: <EyeInvisibleOutlined />, label: '隐藏', onClick: () => handleHideKpi(k) }],
                      }}
                    >
                      <Button type="text" size="small" icon={<MoreOutlined />} />
                    </Dropdown>
                  </span>
                </div>
                <div>
                  <div className="cube-home-kpi-value">{k.value}</div>
                  {k.trend && <div className="cube-home-kpi-trend">{k.trend}</div>}
                </div>
              </div>
            ))}
            {!kpis.length && <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="暂无指标" />}
          </div>
        );
      case 'monitor':
        return (
          <>
            <div ref={chartRef} className="cube-home-monitor-chart" />
            {monitor.xs.length < 2 && (
              <div style={{ textAlign: 'center', color: 'var(--cube-text-muted)', padding: '20px 0' }}>等待监控数据…</div>
            )}
          </>
        );
      case 'quick':
        return (
          <div className="cube-home-quick-grid">
            {quickLinks.map((link) => (
              <div key={link.url} className="cube-home-quick-link" onClick={() => link.url && navigate(link.url)}>
                <div className="cube-home-quick-icon">
                  {link.icon ? <span style={{ fontSize: 16 }}>{link.icon}</span> : <AppstoreOutlined />}
                </div>
                <div>
                  <div className="cube-home-quick-title">{link.name}</div>
                  <div className="cube-home-quick-url">{link.url}</div>
                </div>
              </div>
            ))}
            {!quickLinks.length && <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="暂无快捷入口" />}
          </div>
        );
      case 'menus':
        return (
          <Skeleton loading={!topMenus.length && menus.length > 0} active>
            {topMenus.length ? (
              <div className="cube-home-menu-grid">
                {topMenus.map((item) => (
                  <Card
                    key={item.url ?? item.name}
                    size="small"
                    hoverable
                    onClick={() => item.url && navigate(item.url)}
                    styles={{ body: { padding: '12px 16px' } }}
                  >
                    <div className="cube-home-list-title">{item.displayName || item.name}</div>
                    <div className="cube-home-list-url">{item.url}</div>
                  </Card>
                ))}
              </div>
            ) : (
              <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="暂无可用菜单" />
            )}
          </Skeleton>
        );
      case 'profile':
        return (
          <div className="cube-home-info-list">
            <div className="cube-home-info-item">
              <span className="cube-home-info-key">登录次数</span>
              <span className="cube-home-info-value">{String(profile?.logins ?? '—')}</span>
            </div>
            <div className="cube-home-info-item">
              <span className="cube-home-info-key">最近登录</span>
              <span className="cube-home-info-value">{String(profile?.lastLogin ?? '—')}</span>
            </div>
            <div className="cube-home-info-item">
              <span className="cube-home-info-key">登录 IP</span>
              <span className="cube-home-info-value">{String(profile?.lastLoginIP ?? '—')}</span>
            </div>
            <div className="cube-home-info-item">
              <span className="cube-home-info-key">注册时间</span>
              <span className="cube-home-info-value">{String(profile?.registerTime ?? '—')}</span>
            </div>
          </div>
        );
      case 'sysinfo':
        return (
          <div className="cube-home-info-list">
            {hasSysInfo &&
              Object.entries(sysInfo).map(([k, v]) => (
                <div key={k} className="cube-home-info-item">
                  <span className="cube-home-info-key">{k}</span>
                  <span className="cube-home-info-value">{String(v ?? '')}</span>
                </div>
              ))}
          </div>
        );
      default:
        return null;
    }
  };

  return (
    <div className="cube-home-page">
      {/* 欢迎横幅 */}
      <div className="cube-home-hero">
        <div className="cube-home-hero-inner">
          <div className="cube-home-hero-main">
            <div
              className="cube-home-hero-avatar"
              title="进入个人中心"
              role="button"
              onClick={() => navigate('/profile')}
            >
              {userInfo?.avatar ? <img src={userInfo.avatar} alt="avatar" /> : <UserOutlined />}
            </div>
            <div className="cube-home-hero-copy">
              <Tag className="cube-home-hero-eyebrow" color="blue" variant="filled">
                {greet}
              </Tag>
              <h1 className="cube-home-hero-title" title="进入个人中心" onClick={() => navigate('/profile')}>
                {displayName}
              </h1>
              <p className="cube-home-hero-subtitle">
                欢迎回到 {getConfig().base.title} · {today}
                {roles ? ` · ${roles}` : ''} · 系统运行正常
              </p>
              {config?.loginTip && <p className="cube-home-hero-tip">{config.loginTip}</p>}
            </div>
          </div>
          <div className="cube-home-hero-actions">
            <Button
              type="text"
              icon={<ReloadOutlined />}
              title="刷新工作台数据"
              aria-label="刷新工作台数据"
              onClick={() => void reloadWb()}
            />
          </div>
        </div>
      </div>

      {/* 卡片布局工具栏：拖动提示 + 保存 / 恢复 / 重置（卡片三能力） */}
      <div className="cube-home-widget-toolbar">
        <span className="cube-home-widget-hint">
          <HolderOutlined /> 拖动卡片标题调整顺序，隐藏后可恢复
        </span>
        <Space wrap>
          <Button size="small" icon={<SaveOutlined />} onClick={handleSaveLayout}>
            保存布局
          </Button>
          <Dropdown
            menu={{
              items: [
                ...(hiddenWidgets.length
                  ? [
                      {
                        type: 'group' as const,
                        label: '工作台卡片',
                        children: hiddenWidgets.map((k) => ({
                          key: k,
                          label: WIDGET_TITLES[k],
                          icon: <EyeOutlined />,
                          onClick: () => handleUnhideWidget(k),
                        })),
                      },
                    ]
                  : []),
                ...(hiddenKpis.length
                  ? [
                      {
                        type: 'group' as const,
                        label: '指标小卡',
                        children: hiddenKpis.map((k) => ({
                          key: `kpi-${k.name ?? k.label}`,
                          label: k.label,
                          icon: <EyeOutlined />,
                          onClick: () => handleUnhideKpi(k),
                        })),
                      },
                    ]
                  : []),
              ],
            }}
            disabled={!hiddenWidgets.length && !hiddenKpis.length}
          >
            <Button size="small" icon={<AppstoreAddOutlined />} disabled={!hiddenWidgets.length && !hiddenKpis.length}>
              恢复卡片
            </Button>
          </Dropdown>
          <Button size="small" onClick={handleResetLayout}>
            重置布局
          </Button>
        </Space>
      </div>

      {/* 工作台卡片（可拖动排序 / 隐藏，保存后按用户持久化到服务端） */}
      {widgetsToRender.map((key) => (
        <div
          key={key}
          className="cube-home-widget"
          onDragOver={(e) => {
            if (dragKey && dragKey !== key) e.preventDefault();
          }}
          onDrop={(e) => {
            e.preventDefault();
            handleDrop(key);
          }}
        >
          <Card
            className="cube-home-card"
            size="small"
            title={
              <span className="cube-widget-title">
                <span
                  className="cube-widget-drag-handle"
                  title="拖动排序"
                  draggable
                  onDragStart={(e) => {
                    e.dataTransfer.effectAllowed = 'move';
                    e.dataTransfer.setData('text/plain', key);
                    setDragKey(key);
                  }}
                  onDragEnd={() => setDragKey(null)}
                >
                  <HolderOutlined />
                </span>
                <span className="cube-widget-icon">{WIDGET_ICONS[key]}</span>
                {WIDGET_TITLES[key]}
              </span>
            }
            extra={
              <Dropdown
                menu={{
                  items: [
                    { key: 'hide', icon: <EyeInvisibleOutlined />, label: '隐藏卡片', onClick: () => handleHideWidget(key) },
                  ],
                }}
              >
                <Button type="text" size="small" icon={<MoreOutlined />} />
              </Dropdown>
            }
          >
            {renderWidgetBody(key)}
          </Card>
        </div>
      ))}
    </div>
  );
}
