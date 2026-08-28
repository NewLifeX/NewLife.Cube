/**
 * LOV 表格弹窗选择器（CMP-1 增强项 E1）
 *
 * 适用于列表型值集（LIST）数据量大时的表格浏览选择，对齐 Vue 皮肤 LovSelectTable：
 * 入口为只读展示框，点击打开 Modal，内置搜索 + 分页表格；单选行点击即选并回填，多选勾选后确定回填。
 *
 * 数据流：fetchLovMeta（取 valueField/labelField/tableColumns/searchFields）→
 * fetchLovListData（分页 + 搜索参数代理查询）。refLovCode 列值由值集翻译（ENUM 取 options，LIST 拉映射）。
 */
import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Input, Modal, Table, Typography } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { fetchLovMeta, fetchLovListData } from '@/api/lov';
import { resolveLovType } from '@/types/lov';
import type { LovListMeta, LovSearchField } from '@/types/lov';

export interface LovSelectTableProps {
  /** 当前值（单选为单值，多选为数组或逗号分隔字符串） */
  value?: string | string[] | number | null;
  onChange?: (value: string | string[] | undefined) => void;
  /** 值集编码（List.xxx） */
  lovCode?: string;
  /** 是否多选 */
  multiple?: boolean;
  placeholder?: string;
  disabled?: boolean;
  style?: React.CSSProperties;
}

/**
 * 将已存储值归一为 string[]（兼容数组 / 数字 / 逗号分隔字符串）
 *
 * @param val 原始值
 * @returns 字符串数组
 *
 * @example
 * toValueArray('1,2')   // => ['1', '2']
 * toValueArray([1, 2])  // => ['1', '2']
 * toValueArray(undefined) // => []
 */
export function toValueArray(val?: string | Array<string | number> | number | null): string[] {
  if (val == null) return [];
  if (Array.isArray(val)) return val.map(String);
  if (typeof val === 'number') return [String(val)];
  return String(val)
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s !== '');
}

/** 值集翻译单元格：异步解析 refLovCode 列的显示文本 */
function LovLabel({ code, value: v }: { code: string; value: unknown }) {
  const [label, setLabel] = useState<string | null>(null);
  const str = v == null ? '' : String(v);
  useEffect(() => {
    let cancelled = false;
    if (!str) {
      setLabel('-');
      return;
    }
    (async () => {
      try {
        const type = resolveLovType(code);
        const metas = await fetchLovMeta(code);
        if (type === 'ENUM') {
          const item = metas.find((m) => m.type === 'ENUM');
          const map = new Map((item?.options ?? []).map((o) => [String(o.value), o.label]));
          if (!cancelled) setLabel(map.get(str) ?? str);
        } else {
          const m = metas.find((x) => x.type === 'LIST') as LovListMeta | undefined;
          const res = await fetchLovListData<Record<string, unknown>>({
            lovCode: code,
            params: {},
            pageNum: 1,
            pageSize: 9999,
          });
          const vf = m?.valueField ?? 'id';
          const lf = m?.labelField ?? 'name';
          const hit = res.data.find((r) => String(r[vf]) === str);
          if (!cancelled) setLabel(hit ? String(hit[lf] ?? str) : str);
        }
      } catch {
        if (!cancelled) setLabel(str);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [code, str]);
  return <span>{label ?? str}</span>;
}

export default function LovSelectTable({
  value,
  onChange,
  lovCode,
  multiple = false,
  placeholder,
  disabled,
  style,
}: LovSelectTableProps) {
  const [open, setOpen] = useState(false);
  const [meta, setMeta] = useState<LovListMeta | null>(null);
  const [rows, setRows] = useState<Record<string, unknown>[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [search, setSearch] = useState<Record<string, string>>({});
  const [selectedKeys, setSelectedKeys] = useState<React.Key[]>([]);

  const vf = meta?.valueField ?? 'id';
  const lf = meta?.labelField ?? 'name';
  const searchFields = meta?.searchFields ?? [];

  // 入口展示：当前值 → 标签文本（本地缓存 / 原值兜底）
  const display = useMemo(() => {
    const vals = toValueArray(value);
    // 简化：展示原值；已选标签在打开弹窗后由表格回填，这里不额外请求
    return vals.join('、') || '';
  }, [value]);

  // 打开时加载 meta
  useEffect(() => {
    if (!open || !lovCode) return;
    let cancelled = false;
    setMeta(null);
    fetchLovMeta(lovCode)
      .then((metas) => {
        const m = metas.find((x) => x.type === 'LIST') as LovListMeta | undefined;
        if (m && !cancelled) setMeta(m);
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [open, lovCode]);

  // 分页 + 搜索加载表格数据
  const load = useCallback(() => {
    if (!lovCode) return;
    setLoading(true);
    fetchLovListData<Record<string, unknown>>({ lovCode, params: search, pageNum: page, pageSize })
      .then((res) => {
        setRows(res.data);
        setTotal(res.total);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [lovCode, search, page, pageSize]);

  useEffect(() => {
    if (open) load();
  }, [open, load]);

  // 打开时回显已选
  useEffect(() => {
    if (!open) return;
    const vals = toValueArray(value);
    if (multiple) setSelectedKeys(vals);
  }, [open, value, multiple]);

  // 单选行点击即选并关闭
  const handleRowClick = (row: Record<string, unknown>) => {
    if (multiple) return;
    const key = String(row[vf] ?? '');
    if (key) {
      onChange?.(key);
      setOpen(false);
    }
  };

  // 多选确定
  const handleConfirm = () => {
    onChange?.(selectedKeys.map(String));
    setOpen(false);
  };

  // 列定义：单选时左侧单选框列 + 元数据列
  const columns: ColumnsType<Record<string, unknown>> = useMemo(() => {
    const cols: ColumnsType<Record<string, unknown>> = [];
    if (!multiple) {
      cols.push({
        title: '',
        key: '__radio',
        width: 48,
        align: 'center',
        render: (_v, row) => (
          <Button
            type="text"
            shape="circle"
            size="small"
            icon={
              <span
                style={{
                  display: 'inline-block',
                  width: 14,
                  height: 14,
                  borderRadius: '50%',
                  border: '1px solid #d9d9d9',
                  background: selectedKeys.includes(String(row[vf])) ? '#1677ff' : '#fff',
                }}
              />
            }
            onClick={(e) => {
              e.stopPropagation();
              handleRowClick(row);
            }}
          />
        ),
      });
    }
    for (const col of meta?.tableColumns ?? []) {
      cols.push({
        title: col.title,
        dataIndex: col.field,
        width: col.width || undefined,
        align: (col.align as 'left' | 'center' | 'right') || 'left',
        render: (v: unknown) => (col.refLovCode ? <LovLabel code={col.refLovCode} value={v} /> : String(v ?? '-')),
      });
    }
    return cols;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [meta, multiple, selectedKeys, vf]);

  return (
    <>
      <Input
        readOnly
        value={display}
        placeholder={placeholder}
        disabled={disabled}
        onClick={() => !disabled && setOpen(true)}
        suffix={<SearchOutlined style={{ color: 'rgba(0,0,0,0.45)' }} />}
        style={{ width: '100%', cursor: disabled ? 'not-allowed' : 'pointer', ...style }}
      />
      <Modal
        open={open}
        title={`选择${meta?.name ?? ''}`}
        width={680}
        onCancel={() => setOpen(false)}
        footer={
          multiple ? (
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography.Text type="secondary">已选 {selectedKeys.length} 项</Typography.Text>
              <div>
                <Button onClick={() => setOpen(false)}>取消</Button>
                <Button type="primary" style={{ marginLeft: 8 }} onClick={handleConfirm}>
                  确定
                </Button>
              </div>
            </div>
          ) : null
        }
      >
        {/* 搜索栏：按元数据 searchFields 渲染（统一 Input 降级） */}
        {searchFields.length > 0 && (
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginBottom: 12 }}>
            {searchFields.map((f: LovSearchField) => (
              <Input
                key={f.field}
                size="small"
                placeholder={f.title}
                style={{ width: 160 }}
                value={search[f.field] ?? ''}
                onChange={(e) => setSearch((prev) => ({ ...prev, [f.field]: e.target.value }))}
              />
            ))}
            <Button size="small" type="primary" onClick={() => setPage(1)}>
              搜索
            </Button>
            <Button
              size="small"
              onClick={() => {
                setSearch({});
                setPage(1);
              }}
            >
              重置
            </Button>
          </div>
        )}
        {/* 数据表格 */}
        <Table
          rowKey={(row) => String(row[vf] ?? '')}
          size="small"
          loading={loading}
          columns={columns}
          dataSource={rows}
          rowSelection={
            multiple
              ? {
                  selectedRowKeys: selectedKeys,
                  preserveSelectedRowKeys: true,
                  onChange: (keys) => setSelectedKeys(keys),
                }
              : undefined
          }
          onRow={(row) => ({
            onClick: () => handleRowClick(row),
            style: { cursor: multiple ? 'default' : 'pointer' },
          })}
          pagination={
            meta?.listConfig?.pageable === false
              ? false
              : {
                  current: page,
                  pageSize,
                  total,
                  showSizeChanger: true,
                  showTotal: (t) => `共 ${t} 条`,
                  pageSizeOptions: [10, 20, 50, 100],
                  onChange: (p, ps) => {
                    setPage(p);
                    setPageSize(ps);
                  },
                }
          }
        />
      </Modal>
    </>
  );
}
