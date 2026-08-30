/**
 * 列表表格内容（动态列渲染）
 *
 * 每列按 resolveListControl 解析渲染类型：
 * 链接(resolveUrl)/布尔标签/日期/数值/图片缩略图/颜色色块/图标/LOV 翻译/JSON 摘要等。
 */
import { Table, Tag, Tooltip, Button, Popconfirm } from 'antd';
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table';
import type { FieldMapping } from '@cube/field-mapping';
import { resolveListControl } from '@/utils/fieldControl';
import { toFieldMeta } from '@/types/field';
import { getValueByKey, resolveUrl } from '@/utils/url';
import { resolveIcon } from '@/utils/icon';
import LovCell from '@/components/field/LovCell';
import type { ListControlType } from '@/types/field';

export interface TableContentProps {
  fields: FieldMapping[];
  data: Record<string, unknown>[];
  loading?: boolean;
  pkField?: string;
  canView?: boolean;
  canEdit?: boolean;
  canDelete?: boolean;
  /** 软删除字段名（如 Deleted/IsDeleted），存在时已删除行操作列显示「恢复」 */
  softDeleteField?: string;
  selectable?: boolean;
  selectedKeys?: React.Key[];
  onSelectChange?: (keys: React.Key[]) => void;
  onView?: (row: Record<string, unknown>) => void;
  onEdit?: (row: Record<string, unknown>) => void;
  onDelete?: (row: Record<string, unknown>) => void;
  onRestore?: (row: Record<string, unknown>) => void;
  onSortChange?: (sort?: string, desc?: boolean) => void;
  /** 当前排序列名（配合 onSortChange 受控显示排序指示，仅排序列显示箭头） */
  sortField?: string;
  /** 当前是否降序 */
  sortDesc?: boolean;
  onChange?: (pagination: TablePaginationConfig) => void;
}

/** 日期格式化。dateOnly 输出 yyyy-MM-dd，否则 yyyy-MM-dd HH:mm:ss（规范 §7.4：日期时间到时分秒） */
function formatDate(v: unknown, dateOnly = false): string {
  if (!v) return '';
  let d: Date;
  if (v instanceof Date) {
    d = v;
  } else {
    const s = String(v);
    d = new Date(s.includes('T') ? s : s.replace(' ', 'T'));
    if (isNaN(d.getTime())) return s;
  }
  const pad = (n: number) => String(n).padStart(2, '0');
  const base = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  return dateOnly ? base : `${base} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

/** 渲染单元格 */
function CellRenderer({ field, value }: { field: FieldMapping; value: unknown }) {
  const meta = toFieldMeta(field.field);
  const control = resolveListControl(meta);
  const raw = value;

  switch (control) {
    case 'boolean':
      return <Tag color={raw === true || raw === 'true' || raw === 1 ? 'success' : 'default'}>{raw === true || raw === 'true' || raw === 1 ? '是' : '否'}</Tag>;
    case 'date': {
      // 纯日期字段（ItemType=date）只显示日期，其余显示完整时间
      const dateOnly = (meta.itemType || '').toLowerCase() === 'date';
      return <span>{formatDate(raw, dateOnly)}</span>;
    }
    case 'time':
      return <span>{formatDate(raw)}</span>;
    case 'number':
      return <span style={{ fontVariantNumeric: 'tabular-nums' }}>{String(raw ?? '')}</span>;
    case 'color':
      return raw ? (
        <Tooltip title={String(raw)}>
          <span style={{ display: 'inline-block', width: 18, height: 18, borderRadius: 4, background: String(raw), border: '1px solid var(--cube-border-soft)', verticalAlign: 'middle' }} />
        </Tooltip>
      ) : (
        '-'
      );
    case 'icon':
      return raw ? <span style={{ fontSize: 16 }}>{String(raw)}</span> : '-';
    case 'image':
      return raw ? <img src={String(raw)} alt="" style={{ width: 40, height: 40, objectFit: 'cover', borderRadius: 4 }} /> : '-';
    case 'json':
      return <span style={{ fontFamily: 'monospace', fontSize: 12 }}>{(raw ? String(raw) : '').slice(0, 60)}</span>;
    case 'html':
      return <span style={{ maxWidth: 200, display: 'inline-block', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{(raw ? String(raw) : '').replace(/<[^>]+>/g, '').slice(0, 60)}</span>;
    case 'lov':
      return <LovCell value={raw} lovCode={meta.lovCode} dataSource={meta.dataSource} />;
    case 'file':
      return raw ? (
        <a href={String(raw)} target="_blank" rel="noreferrer">
          查看文件
        </a>
      ) : (
        '-'
      );
    case 'readonly':
    case 'text':
    default: {
      // 链接字段（支持 {Id} 变量替换）
      if (meta.url && raw != null) {
        const href = resolveUrl(meta.url, { [meta.name]: raw, ...Object.fromEntries(Object.entries((raw as object) ?? {})) });
        return (
          <a href={href} target={meta.target || '_blank'} rel="noreferrer">
            {String(raw)}
          </a>
        );
      }
      return <span>{String(raw ?? '')}</span>;
    }
  }
}

export default function TableContent({
  fields,
  data,
  loading,
  pkField = 'id',
  canView = false,
  canEdit = true,
  canDelete = true,
  softDeleteField,
  selectable = true,
  selectedKeys,
  onSelectChange,
  onView,
  onEdit,
  onDelete,
  onRestore,
  onSortChange,
  sortField,
  sortDesc,
  onChange,
}: TableContentProps) {
  const columns: ColumnsType<Record<string, unknown>> = fields.map((field) => {
    const meta = toFieldMeta(field.field);
    const control = resolveListControl(meta);

    // 列宽规范（皮肤设计规范 §7.5）：数字/枚举 ≥80、时间 ≥140、字符串自适应。
    // 主键(Id)列初始宽 52px 且居中（规范 §7.4：主键/ID 居中对齐）：数据少时列窄无大块留白；
    // 长 Id（雪花）由 max-content auto 布局按内容撑开（配合主键列不省略、不排序）。
    // 列多时表格横向滚动，不把各列挤压到无法阅读
    let width: number | undefined;
    if (meta.primaryKey) width = 52;
    else if (control === 'image' || control === 'file') width = 100;
    else if (control === 'color' || control === 'icon') width = 90;
    else if (control === 'boolean') width = 90;
    else if (control === 'number') width = 100;
    else if (control === 'lov') width = 100;
    else if (control === 'date') width = (meta.itemType || '').toLowerCase() === 'date' ? 100 : 140;
    else if (control === 'time') width = 140;

    // 对齐：主键列居中（规范 §7.4），数字右对齐，其余按类型
    const align = meta.primaryKey
      ? 'center'
      : control === 'number'
        ? 'right'
        : control === 'boolean' || control === 'color' || control === 'icon' || control === 'image'
          ? 'center'
          : 'left';

    return {
      title: meta.displayName || meta.name,
      dataIndex: meta.name,
      key: meta.name,
      width,
      align: align as 'left' | 'center' | 'right',
      // 主键列不参与排序 + 不省略：避免表头排序图标撑宽列、ellipsis 约束 auto 布局下按内容自适应；
      // 配合主键列 64px 初始宽，数据少时列窄、雪花 ID 位数多时列宽
      ellipsis: meta.primaryKey ? false : true,
      sorter: onSortChange && !meta.primaryKey ? true : false,
      // 排序箭头仅当前排序列显示（受控）：其余列 sortOrder 置 null 复位，配合 CSS 隐藏默认箭头
      sortOrder: meta.name === sortField ? (sortDesc ? 'descend' : 'ascend') : null,
      render: (_: unknown, row: Record<string, unknown>) => (
        <CellRenderer field={field} value={getValueByKey(row, meta.name)} />
      ),
    };
  });

  // 操作列：查看（只读/无编辑权限时）+ 编辑 + 删除（软删除行显示「恢复」）
  const isSoftDeleted = (row: Record<string, unknown>) => {
    if (!softDeleteField) return false;
    const v = row[softDeleteField];
    return v === true || v === 'true' || v === 1;
  };

  if (canView || canEdit || canDelete) {
    // 操作列宽度按可用操作数自适应：每按钮约 48px + 单元格 16px 内边距，
    // 仅「查看」时收窄避免大块留白，多操作时撑开容纳按钮
    const opsCount = (canView ? 1 : 0) + (canEdit ? 1 : 0) + (canDelete ? 1 : 0);
    columns.push({
      title: '操作',
      key: '__ops',
      width: 16 + 48 * opsCount,
      fixed: 'right',
      render: (_, row) => (
        <span>
          {canView && (
            <Button type="link" size="small" onClick={() => onView?.(row)}>
              查看
            </Button>
          )}
          {canEdit && (
            <Button type="link" size="small" onClick={() => onEdit?.(row)}>
              编辑
            </Button>
          )}
          {canDelete &&
            (isSoftDeleted(row) ? (
              <Button type="link" size="small" onClick={() => onRestore?.(row)}>
                恢复
              </Button>
            ) : (
              <Popconfirm
                title="确定删除该行数据吗？"
                okText="删除"
                okButtonProps={{ danger: true }}
                onConfirm={() => onDelete?.(row)}
              >
                <Button type="link" size="small" danger>
                  删除
                </Button>
              </Popconfirm>
            ))}
        </span>
      ),
    });
  }

  return (
    <Table<Record<string, unknown>>
      rowKey={(row) => String(getValueByKey(row, pkField) ?? JSON.stringify(row))}
      columns={columns}
      dataSource={data}
      loading={loading}
      size="middle"
      scroll={{ x: 'max-content' }}
      rowSelection={
        selectable
          ? {
              selectedRowKeys: selectedKeys,
              onChange: onSelectChange,
            }
          : undefined
      }
      pagination={false}
      onChange={(pagination, _filters, sorter) => {
        if (onSortChange && sorter && !Array.isArray(sorter) && sorter.order) {
          const fieldName = String(sorter.field ?? sorter.columnKey ?? '');
          onSortChange(fieldName, sorter.order === 'descend');
        } else if (onSortChange) {
          onSortChange(undefined, false);
        }
        onChange?.(pagination);
      }}
    />
  );
}
