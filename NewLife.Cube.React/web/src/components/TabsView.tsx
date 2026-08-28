/**
 * 多标签页（对齐 Vue 皮肤 components/TabsView.vue）
 *
 * 随路由自动添加标签；支持关闭单个/关闭其他/关闭全部（保留固定标签）。
 */
import { useEffect } from 'react';
import { Dropdown, Tabs } from 'antd';
import { DownOutlined } from '@ant-design/icons';
import { useLocation, useMatches, useNavigate } from 'react-router-dom';
import { useTabsStore } from '@/stores/tabs';
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

  const meta = (matches[matches.length - 1]?.handle ?? {}) as RouteMeta;

  // 路由变化时自动添加标签
  useEffect(() => {
    if (meta.tab === false) return;
    addTab({
      path: location.pathname,
      title: meta.title || '页面',
      closable: true,
    });
    setActive(location.pathname);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname]);

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

  const items = tabs.map((t) => ({ key: t.path, label: t.title, closable: t.closable && !t.fixed }));

  const moreMenu = {
    items: [
      { key: 'others', label: '关闭其他' },
      { key: 'all', label: '关闭全部' },
    ],
    onClick: ({ key }: { key: string }) => {
      if (key === 'others') closeOthers(activePath);
      if (key === 'all') closeAll();
    },
  };

  return (
    <div style={{ padding: '4px 16px 0', background: 'transparent' }}>
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
