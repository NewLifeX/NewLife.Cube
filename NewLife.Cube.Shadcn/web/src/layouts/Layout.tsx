import { useState, useEffect } from 'react';
import { Outlet, useNavigate, useLocation, Link } from 'react-router-dom';
import {
  PanelLeftClose, PanelLeft, Sun, Moon, ChevronDown, ChevronRight, LogOut, User,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import api from '@/api';
import type { MenuItem as CubeMenuItem } from '@cube/api-core';

export default function Layout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { collapsed, toggleCollapse, darkMode, toggleDark, siteName, setSiteInfo } = useAppStore();
  const { userInfo, fetchUserInfo, logout } = useUserStore();

  const [menus, setMenus] = useState<CubeMenuItem[]>([]);
  const [userMenuOpen, setUserMenuOpen] = useState(false);

  useEffect(() => {
    if (!api.tokenManager.getToken()) { navigate('/login'); return; }
    api.menu.getMenuTree().then((res) => setMenus(res.data ?? [])).catch(() => {});
    api.user.getSiteInfo().then((res) => { if (res.data) setSiteInfo(res.data); }).catch(() => {});
    if (!userInfo) fetchUserInfo().catch(() => {});
  }, []);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div className="flex h-screen overflow-hidden bg-background text-foreground">
      {/* 侧边栏 */}
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

      {/* 主区域 */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* 顶栏 */}
        <header className="flex h-14 items-center gap-2 border-b px-4">
          <Button variant="ghost" size="icon" onClick={toggleCollapse}>
            {collapsed ? <PanelLeft className="h-4 w-4" /> : <PanelLeftClose className="h-4 w-4" />}
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
        <main className="flex-1 overflow-auto p-4">
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
