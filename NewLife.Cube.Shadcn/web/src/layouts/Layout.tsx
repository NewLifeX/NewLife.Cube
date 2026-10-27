import { useState, useEffect } from 'react';
import { Outlet, useNavigate, useLocation, Link } from 'react-router-dom';
import {
  PanelLeftClose, PanelLeft, Sun, Moon, ChevronDown, ChevronRight, LogOut, User,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { closeAllTabs, closeTab, createTab, openTab, shouldAutoCollapseSidebar, type TabItem } from '@newlifex/page-logic';
import api from '@/api';
import type { MenuItem as CubeMenuItem } from '@newlifex/api-core';

export default function Layout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { collapsed, toggleCollapse, darkMode, toggleDark, siteName, setSiteInfo } = useAppStore();
  const { userInfo, fetchUserInfo, logout } = useUserStore();

  const [menus, setMenus] = useState<CubeMenuItem[]>([]);
  const [userMenuOpen, setUserMenuOpen] = useState(false);

  // --- 多标签页 ---
  const [tabs, setTabs] = useState<TabItem[]>([]);
  const [activeTabId, setActiveTabId] = useState('');

  useEffect(() => {
    const tab = createTab(location.pathname, location.pathname + location.search, location.pathname);
    setTabs((prev) => openTab(prev, tab));
    setActiveTabId(tab.id);
  }, [location.pathname, location.search]);

  const activateTab = (tab: TabItem) => {
    if (tab.id === activeTabId) return;
    navigate(tab.fullPath);
  };

  const closeTabAndNavigate = (id: string) => {
    setTabs((prev) => {
      const result = closeTab(prev, id, activeTabId);
      if (result.nextActiveId) {
        setActiveTabId(result.nextActiveId);
        const next = result.tabs.find((t) => t.id === result.nextActiveId);
        if (next) navigate(next.fullPath);
      }
      return result.tabs;
    });
  };

  const closeAll = () => {
    const remaining = closeAllTabs(tabs);
    setTabs(remaining);
    const home = remaining[0];
    if (home) {
      setActiveTabId(home.id);
      navigate(home.fullPath);
    }
  };
  const [isNarrow, setIsNarrow] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  useEffect(() => {
    if (!api.tokenManager.getToken()) { navigate('/login'); return; }
    api.menu.getMenuTree().then((res) => setMenus(res.data ?? [])).catch(() => {});
    // 站点品牌由匿名接口 /Cube/Site 提供；LoginConfig 只含登录能力
    api.site.getSite().then((res) => { if (res.data) setSiteInfo(res.data); }).catch(() => {});
    if (!userInfo) fetchUserInfo().catch(() => {});
  }, []);

  // 窄屏使用抽屉菜单；宽屏恢复用户手动展开
  useEffect(() => {
    let autoCollapsed = false;
    const sync = () => {
      const state = useAppStore.getState();
      const narrow = shouldAutoCollapseSidebar(window.innerWidth);
      setIsNarrow(narrow);
      if (narrow) {
        if (!state.collapsed) {
          state.toggleCollapse();
          autoCollapsed = true;
        }
      } else {
        setMobileOpen(false);
        if (autoCollapsed) {
          if (state.collapsed) state.toggleCollapse();
          autoCollapsed = false;
        }
      }
    };
    sync();
    window.addEventListener('resize', sync);
    return () => window.removeEventListener('resize', sync);
  }, []);

  // 路由切换后关闭抽屉
  useEffect(() => {
    setMobileOpen(false);
  }, [location.pathname]);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div className="flex h-screen overflow-hidden bg-background text-foreground">
      {/* 侧边栏：宽屏常驻，窄屏为抽屉 */}
      {!isNarrow && (
        <aside className={cn(
          'flex flex-col border-r bg-sidebar-background transition-all duration-200',
          collapsed ? 'w-16' : 'w-60'
        )}>
          <div className="flex h-14 items-center justify-center border-b px-4">
            {!collapsed && <span className="text-lg font-semibold truncate">{siteName}</span>}
          </div>
          <nav className="flex-1 overflow-y-auto py-2">
            <SidebarItems menus={menus} collapsed={collapsed} currentPath={location.pathname} onNavigate={(p) => navigate(p)} />
          </nav>
        </aside>
      )}
      {isNarrow && mobileOpen && (
        <div className="fixed inset-0 z-40 flex">
          <div className="absolute inset-0 bg-black/40" onClick={() => setMobileOpen(false)} />
          <aside className="relative z-50 flex w-60 flex-col border-r bg-sidebar-background shadow-lg">
            <div className="flex h-14 items-center justify-between border-b px-4">
              <span className="text-lg font-semibold truncate">{siteName}</span>
              <Button variant="ghost" size="icon" onClick={() => setMobileOpen(false)}>
                <PanelLeftClose className="h-4 w-4" />
              </Button>
            </div>
            <nav className="flex-1 overflow-y-auto py-2">
              <SidebarItems menus={menus} collapsed={false} currentPath={location.pathname} onNavigate={(p) => navigate(p)} />
            </nav>
          </aside>
        </div>
      )}

      {/* 主区域 */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* 顶栏 */}
        <header className="flex h-14 items-center gap-2 border-b px-4">
          <Button variant="ghost" size="icon" onClick={() => (isNarrow ? setMobileOpen(true) : toggleCollapse())}>
            {!isNarrow && !collapsed ? <PanelLeftClose className="h-4 w-4" /> : <PanelLeft className="h-4 w-4" />}
          </Button>

          <nav className="flex items-center gap-1 text-sm text-muted-foreground flex-1">
            <Link to="/" className="hover:text-foreground">首页</Link>
          </nav>

          <Button variant="ghost" size="icon" onClick={toggleDark}>
            {darkMode ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
          </Button>

          {/* 用户菜单 */}
          <div className="relative">
            <Button variant="ghost" size="sm" className="gap-2" onClick={() => setUserMenuOpen(!userMenuOpen)}>
              <User className="h-4 w-4" />
              <span className="max-w-24 truncate">{userInfo?.displayName ?? userInfo?.name ?? '用户'}</span>
            </Button>
            {userMenuOpen && (
              <div className="absolute right-0 top-full mt-1 z-50 w-40 rounded-md border bg-popover p-1 shadow-md">
                <button className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-accent" onClick={handleLogout}>
                  <LogOut className="h-4 w-4" />退出登录
                </button>
              </div>
            )}
          </div>
        </header>

        {/* 内容 */}
        <main className="flex-1 overflow-auto p-2 md:p-4">
          {tabs.length > 1 ? (
            <div className="mb-3 flex flex-wrap items-center gap-2">
              {tabs.map((t) => (
                <span
                  key={t.id}
                  className={`inline-flex items-center gap-1 rounded-full border px-3 py-1 text-sm ${
                    t.id === activeTabId ? 'border-primary bg-primary text-primary-foreground' : 'bg-card'
                  }`}
                >
                  <button type="button" onClick={() => activateTab(t)}>{t.title}</button>
                  {t.closable ? (
                    <button type="button" className="text-xs" onClick={() => closeTabAndNavigate(t.id)}>×</button>
                  ) : null}
                </span>
              ))}
              <Button variant="ghost" size="sm" onClick={closeAll}>关闭全部</Button>
            </div>
          ) : null}
          <Outlet />
        </main>
      </div>
    </div>
  );
}

/** 递归侧边栏 */
function SidebarItems({
  menus, collapsed, currentPath, onNavigate, depth = 0,
}: {
  menus: CubeMenuItem[];
  collapsed: boolean;
  currentPath: string;
  onNavigate: (url: string) => void;
  depth?: number;
}) {
  const [open, setOpen] = useState<Record<number, boolean>>({});

  return (
    <ul className="space-y-0.5">
      {menus.filter((m) => m.visible !== false).map((m) => {
        const hasChildren = m.children?.length > 0;
        const path = m.url?.startsWith('/') ? m.url : `/${m.url}`;
        const isActive = currentPath === path;

        return (
          <li key={m.id}>
            <button
              className={cn(
                'flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm transition-colors hover:bg-sidebar-accent',
                isActive && 'bg-sidebar-accent text-sidebar-accent-foreground font-medium',
                depth > 0 && 'pl-' + (3 + depth * 4)
              )}
              style={depth > 0 ? { paddingLeft: `${0.75 + depth}rem` } : undefined}
              onClick={() => {
                if (hasChildren) setOpen((p) => ({ ...p, [m.id]: !p[m.id] }));
                else onNavigate(path);
              }}
            >
              <span className="flex-1 truncate text-left">{collapsed ? '' : m.displayName}</span>
              {hasChildren && !collapsed && (
                open[m.id] ? <ChevronDown className="h-3 w-3" /> : <ChevronRight className="h-3 w-3" />
              )}
            </button>
            {hasChildren && open[m.id] && (
              <SidebarItems menus={m.children} collapsed={collapsed} currentPath={currentPath} onNavigate={onNavigate} depth={depth + 1} />
            )}
          </li>
        );
      })}
    </ul>
  );
}
