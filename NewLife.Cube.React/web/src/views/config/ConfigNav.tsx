/**
 * 配置中心导航（仿 MVC 版 _Object_Nav.cshtml）
 *
 * 魔方后台的配置控制器（基本设置/系统设置/星尘设置/数据中间件/魔方设置 + 更多配置：
 * 短信/邮件/OAuth/访问规则）在左侧菜单中全部隐藏，避免左侧菜单过多。
 * 进入任一配置页后，通过本组件在页面顶部切换各配置页。
 *
 * - 核心配置：Segmented 分段控件（5 项）
 * - 更多配置：Dropdown 下拉（短信/邮件/OAuth/访问规则）
 * - 隐藏配置页不在菜单树中（后端 MenuTree 过滤 Visible=false），
 *   由 findConfigCenter 按静态清单匹配，供路由/标题/面包屑复用。
 */
import { useMemo } from 'react';
import { Button, Dropdown, Segmented } from 'antd';
import { DownOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';

/** 配置页路径 → 显示名 */
export interface ConfigNavItem {
  /** 路由路径，如 /Admin/Core */
  path: string;
  /** 显示名，如 基本设置 */
  label: string;
}

/** 核心配置（对齐 MVC _Object_Nav.cshtml 主导航） */
export const CONFIG_NAV: ConfigNavItem[] = [
  { path: '/Admin/Core', label: '基本设置' },
  { path: '/Admin/Sys', label: '系统设置' },
  { path: '/Admin/Star', label: '星尘设置' },
  { path: '/Admin/XCode', label: '数据中间件' },
  { path: '/Admin/Cube', label: '魔方设置' },
];

/** 更多配置（对齐 MVC _Object_Nav.cshtml 更多设置下拉） */
export const MORE_NAV: ConfigNavItem[] = [
  { path: '/Admin/SmsConfig', label: '短信设置' },
  { path: '/Admin/MailConfig', label: '邮件设置' },
  { path: '/Admin/OAuthConfig', label: 'OAuth设置' },
  { path: '/Admin/AccessRule', label: '访问规则' },
];

/** 全部配置项 */
export const ALL_CONFIG_NAV: ConfigNavItem[] = [...CONFIG_NAV, ...MORE_NAV];

/**
 * 按路径查找配置项（精确或前缀匹配）
 *
 * 规则：`pathname === item.path` 或 `pathname.startsWith(item.path + '/')`，
 * 避免 /Admin/Cube 误匹配 /Admin/CubeXxx 之类的路径。
 *
 * @param path 路由路径（pathname，不含 query）
 * @returns 命中的配置项，未命中返回 undefined
 */
export function findConfigCenter(path: string): ConfigNavItem | undefined {
  const lower = path.toLowerCase();
  return ALL_CONFIG_NAV.find((item) => {
    const p = item.path.toLowerCase();
    return lower === p || lower.startsWith(p + '/');
  });
}

/** 判断是否为配置中心路径 */
export function isConfigCenterPath(path: string): boolean {
  return !!findConfigCenter(path);
}

/** 解析配置页标题（未命中返回 fallback） */
export function resolveConfigTitle(path: string, fallback = ''): string {
  return findConfigCenter(path)?.label || fallback;
}

export interface ConfigNavProps {
  /** 当前路由路径（pathname） */
  currentPath?: string;
}

/** 配置中心切换器：Segmented 核心配置 + Dropdown 更多配置 */
export default function ConfigNav({ currentPath }: ConfigNavProps) {
  const navigate = useNavigate();
  // 当前配置项：精确或前缀匹配
  const current = useMemo(() => findConfigCenter(currentPath || ''), [currentPath]);
  const inMore = !!current && MORE_NAV.some((i) => i.path === current.path);

  const segments = CONFIG_NAV.map((item) => ({ value: item.path, label: item.label }));
  // 未命中核心配置（更多配置或未知路径）时用哨兵值，避免 Segmented 默认选中第一项
  const mainValue = current && !inMore ? current.path : '__none__';
  const moreMenu = {
    items: MORE_NAV.map((item) => ({ key: item.path, label: item.label })),
    onClick: ({ key }: { key: string }) => navigate(key),
  };

  return (
    <div className="cube-config-nav">
      <Segmented
        options={segments}
        value={mainValue}
        onChange={(value) => navigate(String(value))}
      />
      <Dropdown menu={moreMenu} placement="bottomRight">
        <Button type="text" size="small" className={`cube-config-nav-more${inMore ? ' active' : ''}`}>
          更多配置 <DownOutlined />
        </Button>
      </Dropdown>
    </div>
  );
}
