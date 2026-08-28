/**
 * 图标工具：后端菜单图标字符串 → antd 图标组件
 *
 * 后端菜单图标为 FontAwesome 风格字符串（如 fa-table / fa-user）。
 * 这里做常见映射，未命中返回默认图标。
 */
import type { ReactNode } from 'react';
import {
  TableOutlined,
  UserOutlined,
  TeamOutlined,
  SettingOutlined,
  DashboardOutlined,
  FileTextOutlined,
  LogoutOutlined,
  HomeOutlined,
  AppstoreOutlined,
  SafetyOutlined,
  BellOutlined,
  DatabaseOutlined,
  ToolOutlined,
  BarChartOutlined,
  FolderOutlined,
  CloudOutlined,
  KeyOutlined,
  MenuOutlined,
  QuestionCircleOutlined,
} from '@ant-design/icons';

const ICON_MAP: Record<string, ReactNode> = {
  table: <TableOutlined />,
  'fa-table': <TableOutlined />,
  user: <UserOutlined />,
  'fa-user': <UserOutlined />,
  team: <TeamOutlined />,
  'fa-users': <TeamOutlined />,
  setting: <SettingOutlined />,
  'fa-cog': <SettingOutlined />,
  dashboard: <DashboardOutlined />,
  'fa-dashboard': <DashboardOutlined />,
  file: <FileTextOutlined />,
  'fa-file': <FileTextOutlined />,
  home: <HomeOutlined />,
  'fa-home': <HomeOutlined />,
  appstore: <AppstoreOutlined />,
  'fa-th-large': <AppstoreOutlined />,
  safety: <SafetyOutlined />,
  'fa-shield': <SafetyOutlined />,
  bell: <BellOutlined />,
  'fa-bell': <BellOutlined />,
  database: <DatabaseOutlined />,
  'fa-database': <DatabaseOutlined />,
  tool: <ToolOutlined />,
  'fa-wrench': <ToolOutlined />,
  chart: <BarChartOutlined />,
  'fa-bar-chart': <BarChartOutlined />,
  folder: <FolderOutlined />,
  'fa-folder': <FolderOutlined />,
  cloud: <CloudOutlined />,
  'fa-cloud': <CloudOutlined />,
  key: <KeyOutlined />,
  'fa-key': <KeyOutlined />,
  menu: <MenuOutlined />,
  'fa-list': <MenuOutlined />,
};

/**
 * 将后端图标字符串转为 antd 图标节点
 *
 * @param icon 图标字符串（如 fa-table / user）
 * @returns 图标节点，未命中返回默认图标
 */
export function resolveIcon(icon?: string): ReactNode {
  if (!icon) return <MenuOutlined />;
  const key = icon.trim().toLowerCase();
  return ICON_MAP[key] ?? <QuestionCircleOutlined />;
}

export default resolveIcon;
