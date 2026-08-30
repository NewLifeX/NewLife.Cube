/**
 * 通用列表页（包装组件）
 *
 * 先探测当前控制器是否为实体 CRUD 页（GetPage 返回 JSON），再分发渲染：
 * - 实体页   → EntityListPage（原通用列表页，store 驱动）
 * - 配置页   → ConfigPage（ConfigController 对象表单，如魔方设置 /Admin/Cube）
 * - 数据库页 → DbPage（DbController 特殊管理页 /Admin/Db）
 *
 * 探测逻辑独立于列表渲染（本组件仅用普通 useState/useEffect，不含 zustand store hook），
 * 避免在同一组件内「早退 return + useSyncExternalStore」组合触发 React 310 hook 计数错误。
 */
import { useEffect, useState } from 'react';
import { Card, Spin } from 'antd';
import { useLocation } from 'react-router-dom';
import { routeToApiPrefix } from '@/utils/url';
import { api } from '@/api';
import EntityListPage from './EntityListPage';
import ConfigPage from '@/views/config/ConfigPage';
import DbPage from '@/views/db/DbPage';

/** 页面种类：探测中 / 实体页 / 非实体页（配置或特殊页） */
type PageKind = 'probe' | 'entity' | 'special';

export default function DefaultListPage() {
  const location = useLocation();
  const type = routeToApiPrefix(location.pathname);
  const [kind, setKind] = useState<PageKind>('probe');

  // 探测：GetPage 返回 HTML（SPA 兜底）说明该控制器不是实体 CRUD 页
  useEffect(() => {
    let cancelled = false;
    setKind('probe');
    api.page
      .getPage(type)
      .then((res) => {
        if (cancelled) return;
        setKind(typeof res === 'string' || typeof res?.data === 'string' ? 'special' : 'entity');
      })
      .catch(() => {
        if (!cancelled) setKind('entity');
      });
    return () => {
      cancelled = true;
    };
  }, [type]);

  // 非实体页：按路径分发（数据库页 / 通用配置页）
  if (kind === 'special') {
    if (type.toLowerCase().endsWith('/db')) {
      return <DbPage type={type} />;
    }
    return <ConfigPage type={type} />;
  }

  // 实体页：渲染原通用列表页
  if (kind === 'entity') {
    return <EntityListPage key={type} type={type} />;
  }

  // 探测中：加载占位，避免空白闪屏
  return (
    <Card size="small" styles={{ body: { display: 'flex', justifyContent: 'center', padding: 48 } }}>
      <Spin />
    </Card>
  );
}
