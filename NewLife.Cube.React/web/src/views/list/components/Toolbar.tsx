/**
 * 列表工具栏（规范 §7.1/§7.6/§7.8）
 *
 * - 左侧：新增（primary）、删除选中（选中行时显示）、刷新（图标）
 * - 右侧：表格/图表视图切换（Segmented，§7.6）、高级菜单（§7.8 必须项）
 * 高级菜单按权限（canExport/canImport/canDelete）驱动；分享/备份/还原等按需项后续补充。
 */
import { Button, Dropdown, Popconfirm, Segmented, Tooltip } from 'antd';
import type { MenuProps } from 'antd';
import { DeleteOutlined, DownOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons';
import { EXPORT_FORMATS } from '@newlifex/page-utils';

/** 列表视图模式：表格 / 图表 */
export type ListViewMode = 'table' | 'chart';

export interface ToolbarProps {
  canAdd?: boolean;
  canDelete?: boolean;
  canExport?: boolean;
  canImport?: boolean;
  /** 当前页是否有图表数据能力（控制 表格/图表 视图切换显示） */
  canChart?: boolean;
  /** 当前视图 */
  view?: ListViewMode;
  /** 已选中行数（>0 时显示「删除选中」） */
  selectedCount?: number;
  onNew?: () => void;
  /** 删除选中行 */
  onDelete?: () => void;
  /** 删除当前查询的全部数据（高级菜单） */
  onDeleteAll?: () => void;
  onExport?: (format: string) => void;
  onImport?: () => void;
  onViewChange?: (view: ListViewMode) => void;
  onRefresh?: () => void;
}

export default function Toolbar({
  canAdd = true,
  canDelete = true,
  canExport = true,
  canImport = true,
  canChart = false,
  view = 'table',
  selectedCount = 0,
  onNew,
  onDelete,
  onDeleteAll,
  onExport,
  onImport,
  onViewChange,
  onRefresh,
}: ToolbarProps) {
  // 高级菜单：导出（全部格式）→ 导入 → 删除全部，按权限驱动
  const advItems: MenuProps['items'] = [];
  if (canExport) {
    advItems.push(
      ...EXPORT_FORMATS.map((f) => ({
        key: `export-${f.key}`,
        label: f.label,
        onClick: () => onExport?.(f.key),
      })),
    );
  }
  if (canImport) {
    advItems.push({ key: 'import', label: '导入 Excel/Json/Zip', onClick: () => onImport?.() });
  }
  if (canDelete) {
    advItems.push({ type: 'divider' });
    advItems.push({ key: 'deleteAll', label: '删除全部', danger: true, onClick: () => onDeleteAll?.() });
  }

  return (
    <div className="cube-toolbar">
      <div className="cube-toolbar-main">
        {canAdd && (
          <Button type="primary" icon={<PlusOutlined />} onClick={onNew}>
            新增
          </Button>
        )}
        {canDelete && selectedCount > 0 && (
          <Popconfirm
            title={`确定删除选中的 ${selectedCount} 条数据吗？`}
            onConfirm={onDelete}
            okText="删除"
            okButtonProps={{ danger: true }}
          >
            <Button danger icon={<DeleteOutlined />}>
              删除选中 ({selectedCount})
            </Button>
          </Popconfirm>
        )}
        <Tooltip title="刷新">
          <Button aria-label="刷新" icon={<ReloadOutlined />} onClick={onRefresh} />
        </Tooltip>
      </div>
      <div className="cube-toolbar-side">
        {canChart && (
          <Segmented
            size="small"
            value={view}
            options={[
              { label: '表格', value: 'table' },
              { label: '图表', value: 'chart' },
            ]}
            onChange={(v) => onViewChange?.(v as ListViewMode)}
          />
        )}
        {advItems.length > 0 && (
          <Dropdown menu={{ items: advItems }} placement="bottomRight">
            <Button icon={<DownOutlined />}>高级</Button>
          </Dropdown>
        )}
      </div>
    </div>
  );
}
