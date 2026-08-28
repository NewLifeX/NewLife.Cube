/**
 * 列表表格内容（动态列渲染）
 *
 * 每列按 resolveListControl 解析渲染类型：
 * 链接(resolveUrl)/布尔标签/日期/数值/图片缩略图/颜色色块/图标/LOV 翻译/JSON 摘要等。
 */
import { Table, Tag, Tooltip, Button } from 'antd';
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
  canEdit?: boolean;
  canDelete?: boolean;
  selectable?: boolean;
  selectedKeys?: React.Key[];
  onSelectChange?: (keys: React.Key[]) => void;
  onEdit?: (row: Record<string, unknown>) => void;
  onDelete?: (row: Record<string, unknown>) => void;
  onSortChange?: (sort?: string, desc?: boolean) => void;
  onChange?: (pagination: TablePaginationConfig) => void;
}

/** 日期格式化 */
function formatDate(v: unknown): string {
  if (!v) return '';
  if (v instanceof Date) return v.toLocaleString();
  const s = String(v);
  // 兼容 ISO 与 "2026-08-28 12:00:00"
  const d = new Date(s.includes('T') ? s : s.replace(' ', 'T'));
  return isNaN(d.getTime()) ? s : d.toLocaleString('zh-CN', { hour12: false });
}

/** 渲染单元格 */
function CellRenderer({ field, value }: { field: FieldMapping; value: unknown }) {
  const meta = toFieldMeta(field.field);
  const control = resolveListControl(meta);
  const raw = value;

  switch (control) {
    case 'boolean':
      return <Tag color={raw === true || raw === 'true' || raw === 1 ? 'success' : 'default'}>{raw === true || raw === 'true' || raw === 1 ? '是' : '否'}</Tag>;
    case 'date':
    case 'time':
      return <span>{formatDate(raw)}</span>;
    case 'number':
      return <span style={{ fontVariantNumeric: 'tabular-nums' }}>{String(raw ?? '')}</span>;
    case 'color':
      return raw ? (
        <Tooltip title={String(raw)}>
          <span style={{ display: 'inline-block', width: 18, height: 18, borderRadius: 4, background: String(raw), border: '1px solid #ddd', verticalAlign: 'middle' }} />
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
  canEdit = true,
  canDelete = true,
  selectable = true,
  selectedKeys,
  onSelectChange,
  onEdit,
  onDelete,
  onSortChange,
  onChange,
}: TableContentProps) {
  const columns: ColumnsType<Record<string, unknown>> = fields.map((field) => {
    const meta = toFieldMeta(field.field);
    const control = resolveListControl(meta);

    // 列宽：主键窄、图片/文件/颜色/图标/布尔窄
    let width: number | undefined;
    if (meta.primaryKey) width = 90;
    else if (control === 'image' || control === 'file') width = 100;
    else if (control === 'color' || control === 'icon') width = 90;
    else if (control === 'boolean') width = 90;

    // 对齐
    const align = control === 'number' ? 'right' : control === 'boolean' || control === 'color' || control === 'icon' || control === 'image' ? 'center' : 'left';

    return {
      title: meta.displayName || meta.name,
      dataIndex: meta.name,
      key: meta.name,
      width,
      align: align as 'left' | 'center' | 'right',
      ellipsis: true,
      sorter: onSortChange ? true : false,
      render: (_: unknown, row: Record<string, unknown>) => (
        <CellRenderer field={field} value={getValueByKey(row, meta.name)} />
      ),
    };
  });

  // 操作列
  if (canEdit || canDelete) {
    columns.push({
      title: '操作',
      key: '__ops',
      width: 140,
      fixed: 'right',
      render: (_, row) => (
        <span>
          {canEdit && (
            <Button type="link" size="small" onClick={() => onEdit?.(row)}>
              编辑
            </Button>
          )}
          {canDelete && (
            <Button type="link" size="small" danger onClick={() => onDelete?.(row)}>
              删除
            </Button>
          )}
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
