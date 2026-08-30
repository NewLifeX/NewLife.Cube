/**
 * 首页 / 工作台（AntD5 风格）
 *
 * - 欢迎横幅：问候 + 日期 + 角色 + 操作按钮
 * - 工作台卡片（卡片三能力）：拖动排序 / 保存顺序 / 隐藏，布局按用户持久化到服务端（Parameter）
 * - KPI 卡行：后端工作台接口（/Admin/Index/Workbench）数据优先，缺失时降级个人统计
 * - 性能监控：轮询 /Admin/Index/MonitorData（对齐 MVC 契约 {xs, series}）
 * - 快捷入口 + 常用菜单：菜单树叶子递归收集，接口数据优先
 * - 系统信息 / 个人信息：来自工作台接口，缺失时隐藏
 */
import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import {
  AppstoreAddOutlined,
  AppstoreOutlined,
  BugOutlined,
  EyeInvisibleOutlined,
  EyeOutlined,
  FileTextOutlined,
  HolderOutlined,
  LineChartOutlined,
  MoreOutlined,
  ReloadOutlined,
  SafetyOutlined,
  SaveOutlined,
  TeamOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { App, Button, Card, Dropdown, Empty, Skeleton, Space, Tag } from 'antd';
import * as echarts from 'echarts';
import { useNavigate } from 'react-router-dom';
import { api } from '@/api';
import { useUserStore } from '@/stores/user';
import { getConfig } from '@/configure';
import { useWorkbench, type WorkbenchKpi } from '@/hooks/useWorkbench';
import type { LoginConfig, MenuItem } from '@cube/api-core';

/** KPI 语义名 → 图标 */
const KPI_ICONS: Record<string, ReactNode> = {
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

/** 工作台卡片清单：默认顺序 + 标题 */
const WIDGET_DEFAULT_ORDER = ['kpi', 'monitor', 'quick', 'menus', 'profile', 'sysinfo'];
const WIDGET_TITLES: Record<string, string> = {
  kpi: '指标概览',
  monitor: '性能监控',
  quick: '快捷入口',
  menus: '常用菜单',
  profile: '个人信息',
  sysinfo: '系统信息',
};

export default function HomePage() {
  const { message } = App.useApp();
  const navigate = useNavigate();
  const userInfo = useUserStore((s) => s.userInfo);
  const menus = useUserStore((s) => s.menus);
  const { data: wb, reload: reloadWb } = useWorkbench();
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

  // 性能曲线渲染
  useEffect(() => {
    if (!chartRef.current || monitor.xs.length < 2) return;
    // 卡片隐藏后重新显示时，旧实例已挂在已卸载节点上，先释放再重建
    chartInstRef.current?.dispose();
    const chart = echarts.init(chartRef.current);
    chartInstRef.current = chart;
    chart.setOption({
      tooltip: { trigger: 'axis' },
      legend: { data: ['CPU', '内存'], top: 0, right: 0, textStyle: { color: '#94a3b8' } },
      grid: { left: 8, right: 8, top: 32, bottom: 0, containLabel: true },
      xAxis: {
        type: 'category',
        data: monitor.xs,
        boundaryGap: false,
        axisLine: { lineStyle: { color: 'rgba(148,163,184,.4)' } },
        axisLabel: { color: '#94a3b8' },
      },
      yAxis: {
        type: 'value',
        min: 0,
        max: 100,
        splitLine: { lineStyle: { color: 'rgba(148,163,184,.18)' } },
        axisLabel: { color: '#94a3b8', formatter: '{value}%' },
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

  // KPI：接口数据优先，缺失时降级个人统计
  const kpis = useMemo<WorkbenchKpi[]>(() => {
    if (wb?.kpis?.length) return wb.kpis;
    return [
      { name: 'login', label: '我的登录', value: '—', color: 'green', trend: '累计登录次数' },
      { name: 'online', label: '在线状态', value: '在线', color: 'cyan', trend: '当前账号状态' },
      { name: 'users', label: '我的角色', value: userInfo?.roleName || '—', color: 'purple', trend: '当前角色' },
    ];
  }, [wb, userInfo]);

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
              <div key={k.name || k.label} className="cube-home-kpi-card">
                <div className="cube-home-kpi-head">
                  <div className={`cube-home-kpi-icon ${k.color || 'blue'}`}>{KPI_ICONS[k.name ?? ''] ?? null}</div>
                  <span className="cube-home-kpi-label">{k.label}</span>
                </div>
                <div>
                  <div className="cube-home-kpi-value">{k.value}</div>
                  {k.trend && <div className="cube-home-kpi-trend">{k.trend}</div>}
                </div>
              </div>
            ))}
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
            <div className="cube-home-hero-avatar">
              {userInfo?.avatar ? <img src={userInfo.avatar} alt="avatar" /> : <UserOutlined />}
            </div>
            <div>
              <Tag className="cube-home-hero-eyebrow" color="blue" variant="filled">
                欢迎回来
              </Tag>
              <h1 className="cube-home-hero-title">{displayName}</h1>
              <p className="cube-home-hero-subtitle">
                {today} {roles ? ` · ${roles}` : ''} · 系统运行正常
              </p>
              {config?.loginTip && <p className="cube-home-hero-tip">{config.loginTip}</p>}
            </div>
          </div>
          <div className="cube-home-hero-actions">
            <Button icon={<ReloadOutlined />} onClick={() => void reloadWb()}>
              刷新
            </Button>
            <Button icon={<SafetyOutlined />} onClick={() => navigate('/profile/security')}>
              安全中心
            </Button>
            <Button type="primary" icon={<AppstoreOutlined />} onClick={() => navigate('/Admin/User')}>
              用户管理
            </Button>
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
              items: hiddenWidgets.map((k) => ({
                key: k,
                label: WIDGET_TITLES[k],
                icon: <EyeOutlined />,
                onClick: () => handleUnhideWidget(k),
              })),
            }}
            disabled={!hiddenWidgets.length}
          >
            <Button size="small" icon={<AppstoreAddOutlined />} disabled={!hiddenWidgets.length}>
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
