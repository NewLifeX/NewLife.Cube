/**
 * 顶栏（折叠按钮 + 页面标题 + 面包屑 + 切换器 + 用户菜单）
 */
import { useMemo } from 'react';
import { Breadcrumb, Button, Layout } from 'antd';
import { MenuFoldOutlined, MenuUnfoldOutlined } from '@ant-design/icons';
import { useLocation } from 'react-router-dom';
import { useUserStore } from '@/stores/user';
import { useMenuStore } from '@/stores/menu';
import { getConfig } from '@/configure';
import ThemeSwitcher from '@/components/ThemeSwitcher';
import ModeSwitcher from '@/components/ModeSwitcher';
import LanguageSwitch from '@/components/LanguageSwitch';
import NavbarSearch from '@/components/NavbarSearch';
import NotificationBell from '@/components/NotificationBell';
import UserMenu from '@/components/UserMenu';

export interface HeaderBarProps {
  /** 侧栏是否折叠 */
  collapsed?: boolean;
  /** 切换导航（桌面折叠 / 移动打开抽屉） */
  onToggle?: () => void;
}

export default function HeaderBar({ collapsed, onToggle }: HeaderBarProps) {
  const location = useLocation();
  const flatMenus = useMenuStore((s) => s.flatMenus);
  const userInfo = useUserStore((s) => s.userInfo);
  const { base } = getConfig();

  // 面包屑：当前菜单及其祖先（选择器取稳定数组引用，计算放 useMemo）
  const crumbs = useMemo(() => {
    const lower = location.pathname.toLowerCase();
    const current = flatMenus.find((m) => m.path && lower.endsWith(m.path.toLowerCase()));
    if (!current) return [{ title: base.title }];
    const chain: { title: string }[] = [];
    let node: { id: string | number; parentId?: string | number | null; title?: string; name: string } | undefined = current;
    let guard = 0;
    while (node && guard++ < 10) {
      chain.unshift({ title: node.title || node.name });
      const parentId: string | number | null | undefined = node.parentId;
      node = parentId != null ? flatMenus.find((m) => String(m.id) === String(parentId)) : undefined;
    }
    return chain;
  }, [flatMenus, location.pathname, base.title]);

  return (
    <Layout.Header className="cube-shell-header">
      <div className="cube-shell-header-inner">
        <div className="cube-shell-header-main">
          <Button
            className="cube-shell-sidebar-toggle"
            type="text"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={onToggle}
            aria-label="切换导航"
          />
          <div className="cube-shell-header-copy">
            <Breadcrumb className="cube-shell-header-breadcrumb" items={crumbs} />
          </div>
        </div>
        <div className="cube-shell-header-actions">
          <NavbarSearch />
          <NotificationBell />
          <LanguageSwitch />
          <ThemeSwitcher />
          <ModeSwitcher />
          {userInfo && <UserMenu />}
        </div>
      </div>
    </Layout.Header>
  );
}
