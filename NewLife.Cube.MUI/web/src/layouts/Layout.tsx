import { useState, useEffect } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import {
  AppBar, Box, Drawer, IconButton, List, ListItemButton, ListItemIcon,
  ListItemText, Toolbar, Typography, Menu, MenuItem, Avatar, Collapse,
  Divider, Switch, FormControlLabel, Breadcrumbs, Link,
} from '@mui/material';
import {
  Menu as MenuIcon, ChevronLeft, ExpandLess, ExpandMore,
  DarkMode, LightMode, AccountCircle,
} from '@mui/icons-material';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { shouldAutoCollapseSidebar } from '@newlifex/page-logic';
import api from '@/api';
import type { MenuItem as CubeMenuItem } from '@newlifex/api-core';

const DRAWER_WIDTH = 240;

export default function Layout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { collapsed, toggleCollapse, darkMode, toggleDark, siteName, setSiteInfo } = useAppStore();
  const { userInfo, fetchUserInfo, logout } = useUserStore();

  const [menus, setMenus] = useState<CubeMenuItem[]>([]);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [isNarrow, setIsNarrow] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  // 检查登录
  useEffect(() => {
    if (!api.tokenManager.getToken()) {
      navigate('/login');
      return;
    }
    // 加载菜单
    api.menu.getMenuTree().then((res) => setMenus(res.data ?? [])).catch(() => {});
    // 加载站点信息
    // 站点品牌由匿名接口 /Cube/Site 提供；LoginConfig 只含登录能力
    api.site.getSite().then((res) => { if (res.data) setSiteInfo(res.data); }).catch(() => {});
    // 加载用户信息
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
    setAnchorEl(null);
    await logout();
    navigate('/login');
  };

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      {/* 侧边栏：宽屏常驻，窄屏为抽屉 */}
      <Drawer
        variant={isNarrow ? 'temporary' : 'permanent'}
        open={isNarrow ? mobileOpen : true}
        onClose={() => setMobileOpen(false)}
        sx={{
          width: isNarrow ? DRAWER_WIDTH : collapsed ? 64 : DRAWER_WIDTH,
          flexShrink: 0,
          '& .MuiDrawer-paper': {
            width: isNarrow ? DRAWER_WIDTH : collapsed ? 64 : DRAWER_WIDTH,
            transition: 'width 0.2s',
            overflowX: 'hidden',
          },
        }}
      >
        <Toolbar sx={{ justifyContent: 'center' }}>
          {(!collapsed || isNarrow) && <Typography variant="h6" noWrap>{siteName}</Typography>}
        </Toolbar>
        <Divider />
        <List component="nav" dense>
          <SidebarItems
            menus={menus}
            collapsed={isNarrow ? false : collapsed}
            onNavigate={(url) => navigate(url)}
            currentPath={location.pathname}
          />
        </List>
      </Drawer>

      {/* 主区域 */}
      <Box sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <AppBar position="sticky" color="default" elevation={1}>
          <Toolbar variant="dense">
            <IconButton edge="start" onClick={() => (isNarrow ? setMobileOpen(true) : toggleCollapse())} sx={{ mr: 1 }}>
              {!isNarrow && !collapsed ? <ChevronLeft /> : <MenuIcon />}
            </IconButton>

            <Breadcrumbs sx={{ flexGrow: 1 }}>
              <Link underline="hover" color="inherit" onClick={() => navigate('/')}>首页</Link>
            </Breadcrumbs>

            <FormControlLabel
              control={<Switch checked={darkMode} onChange={toggleDark} size="small" />}
              label={darkMode ? <DarkMode fontSize="small" /> : <LightMode fontSize="small" />}
            />

            <IconButton onClick={(e) => setAnchorEl(e.currentTarget)}>
              {userInfo?.avatar ? <Avatar src={userInfo.avatar} sx={{ width: 28, height: 28 }} /> : <AccountCircle />}
            </IconButton>
            <Menu anchorEl={anchorEl} open={!!anchorEl} onClose={() => setAnchorEl(null)}>
              <MenuItem disabled>{userInfo?.displayName ?? userInfo?.name ?? '用户'}</MenuItem>
              <Divider />
              <MenuItem onClick={handleLogout}>退出登录</MenuItem>
            </Menu>
          </Toolbar>
        </AppBar>

        <Box sx={{ p: 2, flexGrow: 1, overflow: 'auto' }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  );
}

/** 递归侧边栏菜单 */
function SidebarItems({
  menus, collapsed, onNavigate, currentPath, depth = 0,
}: {
  menus: CubeMenuItem[];
  collapsed: boolean;
  onNavigate: (url: string) => void;
  currentPath: string;
  depth?: number;
}) {
  const [open, setOpen] = useState<Record<number, boolean>>({});

  return (
    <>
      {menus.filter((m) => m.visible !== false).map((m) => {
        const hasChildren = m.children?.length > 0;
        const path = m.url?.startsWith('/') ? m.url : `/${m.url}`;
        const isActive = currentPath === path;

        return (
          <div key={m.id}>
            <ListItemButton
              selected={isActive}
              sx={{ pl: 2 + depth * 2 }}
              onClick={() => {
                if (hasChildren) {
                  setOpen((prev) => ({ ...prev, [m.id]: !prev[m.id] }));
                } else {
                  onNavigate(path);
                }
              }}
            >
              <ListItemText primary={collapsed ? '' : m.displayName} />
              {hasChildren && !collapsed && (open[m.id] ? <ExpandLess /> : <ExpandMore />)}
            </ListItemButton>
            {hasChildren && (
              <Collapse in={open[m.id]} timeout="auto" unmountOnExit>
                <SidebarItems menus={m.children} collapsed={collapsed} onNavigate={onNavigate} currentPath={currentPath} depth={depth + 1} />
              </Collapse>
            )}
          </div>
        );
      })}
    </>
  );
}
