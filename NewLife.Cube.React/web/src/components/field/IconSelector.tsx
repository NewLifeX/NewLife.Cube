/**
 * 图标选择器（从常见 antd 图标中选择）
 */
import { Select } from 'antd';
import {
  AppstoreOutlined,
  BarChartOutlined,
  BellOutlined,
  CloudOutlined,
  DashboardOutlined,
  DatabaseOutlined,
  FileTextOutlined,
  FolderOutlined,
  HomeOutlined,
  KeyOutlined,
  MenuOutlined,
  SafetyOutlined,
  SettingOutlined,
  TableOutlined,
  TeamOutlined,
  ToolOutlined,
  UserOutlined,
} from '@ant-design/icons';

const ICONS: { value: string; label: string; node: React.ReactNode }[] = [
  { value: 'user', label: '用户', node: <UserOutlined /> },
  { value: 'team', label: '团队', node: <TeamOutlined /> },
  { value: 'setting', label: '设置', node: <SettingOutlined /> },
  { value: 'dashboard', label: '仪表盘', node: <DashboardOutlined /> },
  { value: 'table', label: '表格', node: <TableOutlined /> },
  { value: 'file', label: '文件', node: <FileTextOutlined /> },
  { value: 'home', label: '首页', node: <HomeOutlined /> },
  { value: 'appstore', label: '应用', node: <AppstoreOutlined /> },
  { value: 'safety', label: '安全', node: <SafetyOutlined /> },
  { value: 'bell', label: '通知', node: <BellOutlined /> },
  { value: 'database', label: '数据库', node: <DatabaseOutlined /> },
  { value: 'tool', label: '工具', node: <ToolOutlined /> },
  { value: 'chart', label: '图表', node: <BarChartOutlined /> },
  { value: 'folder', label: '文件夹', node: <FolderOutlined /> },
  { value: 'cloud', label: '云', node: <CloudOutlined /> },
  { value: 'key', label: '密钥', node: <KeyOutlined /> },
  { value: 'menu', label: '菜单', node: <MenuOutlined /> },
];

export interface IconSelectorProps {
  value?: string;
  onChange?: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
}

export default function IconSelector({ value, onChange, placeholder, disabled }: IconSelectorProps) {
  return (
    <Select
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      disabled={disabled}
      allowClear
      showSearch
      optionFilterProp="label"
      options={ICONS.map((i) => ({
        value: i.value,
        label: (
          <span>
            {i.node} {i.label}
          </span>
        ),
      }))}
      style={{ width: '100%' }}
    />
  );
}
