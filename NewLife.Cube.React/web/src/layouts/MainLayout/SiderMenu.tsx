/**
 * 侧边栏菜单（动态菜单树 → antd Menu）
 */
import { useEffect, useMemo } from 'react';
import { Menu } from 'antd';
import { useLocation, useNavigate } from 'react-router-dom';
import { useUserStore } from '@/stores/user';
import { useMenuStore } from '@/stores/menu';
import { buildMenuItems } from '@/utils/menuItems';

export default function SiderMenu() {
  const menus = useUserStore((s) => s.menus);
  const setActivePath = useMenuStore((s) => s.setActivePath);
  const navigate = useNavigate();
  const location = useLocation();

  const items = useMemo(() => buildMenuItems(menus), [menus]);

  // 当前激活菜单：优先精确匹配，其次最近前缀
  const selectedKeys = useMemo(() => {
    const path = location.pathname;
    const hit = menus.find((m) => path.toLowerCase() === (m.url ?? '').toLowerCase());
    return hit ? [hit.url] : [];
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname, menus]);

  // 路由变化时更新激活菜单
  useEffect(() => {
    setActivePath(location.pathname);
  }, [location.pathname, setActivePath]);

  return (
    <Menu
      className="cube-shell-menu"
      mode="inline"
      items={items}
      selectedKeys={selectedKeys}
      onClick={({ key }) => {
        if (key && key !== location.pathname) navigate(key);
      }}
      style={{ height: '100%', borderRight: 0, overflowY: 'auto' }}
    />
  );
}
