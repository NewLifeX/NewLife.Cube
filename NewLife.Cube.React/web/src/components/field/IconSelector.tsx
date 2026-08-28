/**
 * 图标选择器（菜单图标字段专用）
 *
 * 图标值统一使用 **Element Plus 图标名**（如 Monitor / User / Tools）——这是数据库当前跨皮肤标准
 * （Vue/ElementUI 皮肤直接按名渲染），React 皮肤经 utils/icon.resolveIcon 映射为 antd 图标。
 * 避免各自皮肤存各自命名导致菜单在其它皮肤下图标失效。
 */
import { Select } from 'antd';
import {
  AppstoreOutlined,
  BarChartOutlined,
  BellOutlined,
  CloudOutlined,
  CloudServerOutlined,
  ControlOutlined,
  DashboardOutlined,
  DatabaseOutlined,
  DesktopOutlined,
  DownloadOutlined,
  EditOutlined,
  EyeOutlined,
  FieldTimeOutlined,
  FileTextOutlined,
  FolderOutlined,
  HomeOutlined,
  IdcardOutlined,
  KeyOutlined,
  LineChartOutlined,
  LockOutlined,
  MenuOutlined,
  PhoneOutlined,
  PictureOutlined,
  PlusCircleOutlined,
  ProfileOutlined,
  ReloadOutlined,
  SafetyOutlined,
  SaveOutlined,
  SearchOutlined,
  SettingOutlined,
  ShareAltOutlined,
  ShoppingCartOutlined,
  StarOutlined,
  TableOutlined,
  TeamOutlined,
  ToolOutlined,
  TrophyOutlined,
  UnlockOutlined,
  UploadOutlined,
  UserOutlined,
} from '@ant-design/icons';

/** 选项：value 为 Element Plus 图标名（跨皮肤标准），node 为 antd 渲染 */
const ICONS: { value: string; label: string; node: React.ReactNode }[] = [
  { value: 'Monitor', label: '显示器（系统）', node: <DesktopOutlined /> },
  { value: 'User', label: '用户', node: <UserOutlined /> },
  { value: 'UserFilled', label: '用户填充', node: <UserOutlined /> },
  { value: 'Avatar', label: '头像/身份', node: <IdcardOutlined /> },
  { value: 'Team', label: '团队', node: <TeamOutlined /> },
  { value: 'Setting', label: '设置', node: <SettingOutlined /> },
  { value: 'Tools', label: '工具', node: <ToolOutlined /> },
  { value: 'Dashboard', label: '仪表盘', node: <DashboardOutlined /> },
  { value: 'DataBoard', label: '数据面板', node: <DashboardOutlined /> },
  { value: 'Odometer', label: '仪表盘/计量', node: <DashboardOutlined /> },
  { value: 'DataLine', label: '数据曲线', node: <LineChartOutlined /> },
  { value: 'DataAnalysis', label: '数据分析', node: <BarChartOutlined /> },
  { value: 'Table', label: '表格', node: <TableOutlined /> },
  { value: 'Document', label: '文档', node: <FileTextOutlined /> },
  { value: 'Folder', label: '文件夹', node: <FolderOutlined /> },
  { value: 'Home', label: '首页', node: <HomeOutlined /> },
  { value: 'Grid', label: '应用网格', node: <AppstoreOutlined /> },
  { value: 'CirclePlus', label: '新增/添加', node: <PlusCircleOutlined /> },
  { value: 'Operation', label: '操作/管理', node: <ControlOutlined /> },
  { value: 'Menu', label: '菜单', node: <MenuOutlined /> },
  { value: 'Safety', label: '安全', node: <SafetyOutlined /> },
  { value: 'Bell', label: '通知', node: <BellOutlined /> },
  { value: 'Database', label: '数据库', node: <DatabaseOutlined /> },
  { value: 'Server', label: '服务器', node: <CloudServerOutlined /> },
  { value: 'Cloud', label: '云', node: <CloudOutlined /> },
  { value: 'Key', label: '密钥', node: <KeyOutlined /> },
  { value: 'Lock', label: '锁定', node: <LockOutlined /> },
  { value: 'Unlock', label: '解锁', node: <UnlockOutlined /> },
  { value: 'Timer', label: '计时/日志', node: <FieldTimeOutlined /> },
  { value: 'Clock', label: '时钟', node: <FieldTimeOutlined /> },
  { value: 'Calendar', label: '日历', node: <FieldTimeOutlined /> },
  { value: 'Star', label: '星标', node: <StarOutlined /> },
  { value: 'Trophy', label: '奖杯', node: <TrophyOutlined /> },
  { value: 'ShoppingCart', label: '购物车', node: <ShoppingCartOutlined /> },
  { value: 'Phone', label: '电话', node: <PhoneOutlined /> },
  { value: 'Picture', label: '图片', node: <PictureOutlined /> },
  { value: 'Search', label: '搜索', node: <SearchOutlined /> },
  { value: 'Download', label: '下载', node: <DownloadOutlined /> },
  { value: 'Upload', label: '上传', node: <UploadOutlined /> },
  { value: 'Refresh', label: '刷新', node: <ReloadOutlined /> },
  { value: 'Edit', label: '编辑', node: <EditOutlined /> },
  { value: 'View', label: '查看', node: <EyeOutlined /> },
  { value: 'Share', label: '分享', node: <ShareAltOutlined /> },
  { value: 'Tickets', label: '工单', node: <ProfileOutlined /> },
  { value: 'Save', label: '保存', node: <SaveOutlined /> },
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
