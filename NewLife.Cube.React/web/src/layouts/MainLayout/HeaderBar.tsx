/**
 * 顶栏（Logo + 面包屑 + 切换器 + 用户菜单）
 */
import { useMemo } from 'react';
import { Breadcrumb, Layout, Space } from 'antd';
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

export default function HeaderBar() {
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
    <Layout.Header
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 16px',
        height: 56,
        lineHeight: '56px',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
        <span style={{ fontSize: 18, fontWeight: 600, color: 'inherit' }}>{base.title}</span>
        <Breadcrumb items={crumbs} />
      </div>
      <Space size={4}>
        <NavbarSearch />
        <NotificationBell />
        <LanguageSwitch />
        <ThemeSwitcher />
        <ModeSwitcher />
        {userInfo && <UserMenu />}
      </Space>
    </Layout.Header>
  );
}
