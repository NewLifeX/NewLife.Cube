/**
 * 实体详情弹窗（只读展示 detailFields）
 *
 * 供列表页操作列「查看」使用：从后端 Detail 接口拉取单条数据，按 detailFields
 * 元数据渲染字段名 + 格式化值。详情接口失败时用列表行数据兜底。
 * 对应皮肤设计规范 §7.9：只读控制器或无可编辑权限时提供「查看」。
 */
import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { Descriptions, Modal, Tabs, Tag } from 'antd';
import { api } from '@/api';
import LovCell from '@/components/field/LovCell';
import { getValueByKey } from '@/utils/url';
import { groupByCategory, hasCategory, resolveListControl } from '@/utils/fieldControl';
import { toFieldMeta, type FieldMeta } from '@/types/field';
import type { FieldMapping } from '@cube/field-mapping';

export interface DetailDialogProps {
  open: boolean;
  /** 实体路径前缀，如 /Admin/User */
  apiPrefix: string;
  /** 主键值 */
  id?: number | string | null;
  /** 详情字段元数据 */
  fields: FieldMapping[];
  /** 回退行数据（详情接口失败时直接用列表行渲染） */
  row?: Record<string, unknown> | null;
  onClose: () => void;
}

/** 格式化日期：dateOnly 输出 yyyy-MM-dd，否则到时分秒 */
function formatDateTime(v: unknown, dateOnly = false): string {
  const s = String(v);
  const d = new Date(s.includes('T') ? s : s.replace(' ', 'T'));
  if (isNaN(d.getTime())) return s;
  const pad = (n: number) => String(n).padStart(2, '0');
  const base = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  return dateOnly ? base : `${base} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

/** 按字段类型格式化详情值 */
function formatValue(field: FieldMapping, value: unknown): ReactNode {
  const meta = toFieldMeta(field.field);
  const control = resolveListControl(meta);
  if (value === null || value === undefined || value === '') return '-';

  switch (control) {
    case 'boolean':
      return (
        <Tag color={value === true || value === 'true' || value === 1 ? 'success' : 'default'}>
          {value === true || value === 'true' || value === 1 ? '是' : '否'}
        </Tag>
      );
    case 'date': {
      const dateOnly = (meta.itemType || '').toLowerCase() === 'date';
      return <span>{formatDateTime(value, dateOnly)}</span>;
    }
    case 'time':
      return <span>{formatDateTime(value)}</span>;
    case 'lov':
      return <LovCell value={value} lovCode={meta.lovCode} dataSource={meta.dataSource} />;
    case 'image':
      return value ? <img src={String(value)} alt="" style={{ maxWidth: 120, maxHeight: 120, borderRadius: 8 }} /> : '-';
    case 'file':
      return value ? (
        <a href={String(value)} target="_blank" rel="noreferrer">
          查看文件
        </a>
      ) : (
        '-'
      );
    case 'html':
      return <div dangerouslySetInnerHTML={{ __html: String(value) }} />;
    default:
      return <span style={{ wordBreak: 'break-all' }}>{String(value)}</span>;
  }
}

export default function DetailDialog({ open, apiPrefix, id, fields, row, onClose }: DetailDialogProps) {
  const [detail, setDetail] = useState<Record<string, unknown> | null>(null);

  // 打开时拉取详情，失败用行数据兜底
  useEffect(() => {
    if (!open || id == null) {
      setDetail(null);
      return;
    }
    let cancelled = false;
    api.page
      .getDetail<Record<string, unknown>>(apiPrefix, String(id))
      .then((res) => {
        if (cancelled) return;
        setDetail(res.data ?? {});
      })
      .catch(() => {
        if (cancelled) return;
        setDetail(row ?? {});
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, apiPrefix, id]);

  const data = detail ?? row ?? {};

  // 详情字段元数据（与 FieldMapping 按 name 一一对应）
  const metas = useMemo(() => fields.map((f) => toFieldMeta(f.field)), [fields]);
  const mappingByName = useMemo(() => new Map(fields.map((f) => [f.field.name, f])), [fields]);

  // 分组：字段带 Category 才按分类 Tabs 分组，否则平铺（字段少的详情一屏展示）
  const groups = useMemo(() => {
    if (!hasCategory(metas)) return null;
    return groupByCategory(metas);
  }, [metas]);

  /** 渲染一组详情描述（分组内 / 平铺共用） */
  const renderDescriptions = (list: FieldMeta[]) => (
    <Descriptions
      column={1}
      bordered
      size="small"
      style={{ marginTop: 16 }}
      items={list.map((meta) => {
        const mapping = mappingByName.get(meta.name)!;
        return {
          key: meta.name,
          label: meta.displayName || meta.name,
          children: formatValue(mapping, getValueByKey(data, meta.name)),
        };
      })}
    />
  );

  return (
    <Modal
      open={open}
      title="详情"
      onCancel={onClose}
      footer={null}
      width={680}
      destroyOnHidden
      // 分组时限制弹窗高度，避免分组标签 + 单组详情把弹窗撑得过高
      styles={groups ? { body: { maxHeight: '60vh', overflowY: 'auto' } } : undefined}
    >
      {groups ? (
        <Tabs
          items={groups.map((g) => ({
            key: g.category,
            label: g.category,
            children: renderDescriptions(g.fields),
          }))}
        />
      ) : (
        renderDescriptions(metas)
      )}
    </Modal>
  );
}
