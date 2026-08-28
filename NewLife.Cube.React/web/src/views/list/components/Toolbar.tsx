/**
 * 列表工具栏（新增/删除/导出/导入/图表/刷新，权限控制）
 */
import { Button, Dropdown, Popconfirm, Space, Tooltip } from 'antd';
import {
  DeleteOutlined,
  DownloadOutlined,
  FundOutlined,
  ImportOutlined,
  PlusOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import { EXPORT_FORMATS } from '@cube/page-utils';

export interface ToolbarProps {
  canAdd?: boolean;
  canDelete?: boolean;
  canExport?: boolean;
  canImport?: boolean;
  selectedCount?: number;
  onNew?: () => void;
  onDelete?: () => void;
  onExport?: (format: string) => void;
  onImport?: () => void;
  onChart?: () => void;
  onRefresh?: () => void;
}

export default function Toolbar({
  canAdd = true,
  canDelete = true,
  canExport = true,
  canImport = true,
  selectedCount = 0,
  onNew,
  onDelete,
  onExport,
  onImport,
  onChart,
  onRefresh,
}: ToolbarProps) {
  const exportItems = EXPORT_FORMATS.map((f) => ({
    key: f.key,
    label: f.label,
    onClick: () => onExport?.(f.key),
  }));

  return (
    <Space wrap>
      {canAdd && (
        <Button type="primary" icon={<PlusOutlined />} onClick={onNew}>
          新增
        </Button>
      )}
      {canDelete && (
        <Popconfirm
          title={selectedCount > 0 ? `确定删除选中的 ${selectedCount} 条数据吗？` : '确定删除吗？'}
          onConfirm={onDelete}
          okText="删除"
          okButtonProps={{ danger: true }}
          disabled={selectedCount === 0}
        >
          <Button danger icon={<DeleteOutlined />} disabled={selectedCount === 0}>
            删除{selectedCount > 0 ? ` (${selectedCount})` : ''}
          </Button>
        </Popconfirm>
      )}
      {canExport && (
        <Dropdown menu={{ items: exportItems }} disabled={!canExport}>
          <Button icon={<DownloadOutlined />}>导出</Button>
        </Dropdown>
      )}
      {canImport && (
        <Tooltip title="导入 Excel/CSV 文件">
          <Button icon={<ImportOutlined />} onClick={onImport}>
            导入
          </Button>
        </Tooltip>
      )}
      <Button icon={<FundOutlined />} onClick={onChart}>
        图表
      </Button>
      <Button icon={<ReloadOutlined />} onClick={onRefresh}>
        刷新
      </Button>
    </Space>
  );
}
