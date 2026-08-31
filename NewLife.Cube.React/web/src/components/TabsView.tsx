/**
 * 多标签页（对齐 Vue 皮肤 components/TabsView.vue）
 *
 * 随路由自动添加标签；支持关闭单个/关闭其他/关闭全部（保留固定标签）。
 */
import { useEffect, useMemo } from 'react';
import { Dropdown, Tabs } from 'antd';
import { DownOutlined } from '@ant-design/icons';
import { useLocation, useMatches, useNavigate } from 'react-router-dom';
import { useTabsStore } from '@/stores/tabs';
import { useMenuStore, resolveMenuTitle } from '@/stores/menu';
import { resolveConfigTitle } from '@/views/config/ConfigNav';
import type { RouteMeta } from '@/router';

export default function TabsView() {
  const tabs = useTabsStore((s) => s.tabs);
  const activePath = useTabsStore((s) => s.activePath);
  const addTab = useTabsStore((s) => s.addTab);
  const removeTab = useTabsStore((s) => s.removeTab);
  const closeOthers = useTabsStore((s) => s.closeOthers);
  const closeAll = useTabsStore((s) => s.closeAll);
  const setActive = useTabsStore((s) => s.setActive);
  const navigate = useNavigate();
  const location = useLocation();
  const matches = useMatches();

  const lastMatch = matches[matches.length - 1];
  const meta = (lastMatch?.handle ?? {}) as RouteMeta;
  // catch-all 路由（dynamic 标志）承载所有动态实体页，标签标题需从菜单解析
  const isCatchAll = meta.dynamic === true;
  const flatMenus = useMenuStore((s) => s.flatMenus);

  // 标签标题：静态路由用 meta.title；动态实体页用菜单名，菜单未就绪时回退 meta.title；
  // 配置中心页（左侧隐藏菜单，不在菜单树中）优先按静态清单解析标题
  const tabTitle = useMemo(() => {
    if (!isCatchAll) return meta.title || '页面';
    const configTitle = resolveConfigTitle(location.pathname);
    if (configTitle) return configTitle;
    return resolveMenuTitle(flatMenus, location.pathname, meta.title || '页面');
  }, [isCatchAll, meta.title, flatMenus, location.pathname]);

  // 路由变化或标题就绪（菜单异步加载）时自动添加/更新标签
  useEffect(() => {
    if (meta.tab === false) return;
    addTab({
      path: location.pathname,
      title: tabTitle,
      closable: !meta.fixed,
      fixed: meta.fixed === true,
    });
    setActive(location.pathname);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname, tabTitle]);

  const handleEdit = (targetKey: string | React.MouseEvent | React.KeyboardEvent, action: 'add' | 'remove') => {
    if (action !== 'remove') return;
    const key = targetKey as string;
    const wasActive = activePath === key;
    removeTab(key);
    if (wasActive) {
      const nextActive = useTabsStore.getState().activePath;
      if (nextActive) navigate(nextActive);
      else navigate('/');
    }
  };

  // 仅剩固定首页（工作台）时隐藏整条标签栏，避免单独占一行；有业务标签时才显示
  const hasBusinessTab = tabs.some((t) => !t.fixed);
  if (!hasBusinessTab) return null;

  const items = tabs.map((t) => ({ key: t.path, label: t.title, closable: t.closable && !t.fixed }));

  const moreMenu = {
    items: [
      { key: 'others', label: '关闭其他' },
      { key: 'all', label: '关闭全部' },
    ],
    onClick: ({ key }: { key: string }) => {
      if (key === 'others') closeOthers(activePath);
      if (key === 'all') {
        closeAll();
        // 关闭全部后仅剩固定首页：导航回工作台，标签栏随之隐藏
        const next = useTabsStore.getState().activePath;
        if (next) navigate(next);
      }
    },
  };

  return (
    <div className="cube-shell-tabs" style={{ padding: '0 16px 0' }}>
      <Tabs
        type="editable-card"
        hideAdd
        size="small"
        items={items}
        activeKey={activePath}
        onChange={(key) => navigate(key)}
        onEdit={handleEdit}
        tabBarExtraContent={
          tabs.length > 1 ? (
            <Dropdown menu={moreMenu} trigger={['click']}>
              <DownOutlined style={{ fontSize: 12, cursor: 'pointer', padding: '0 8px' }} />
            </Dropdown>
          ) : null
        }
        style={{ marginBottom: 0 }}
      />
    </div>
  );
}
