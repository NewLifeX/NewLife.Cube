/**
 * 搜索栏（动态搜索控件）
 *
 * 依据 resolveSearchControl 渲染 text / numberRange / dateRange / datetimeRange /
 * timeRange / lov / lovMulti / switch / fileExists 控件。
 * 布局：条件与操作按钮（搜索/重置/展开）同一 flex-wrap 容器，按钮落在最后一行右侧，
 * 行满时自动换行并靠右对齐（对齐主流 QueryFilter 与规范 §7.2）。
 * 搜索参数约定（对齐 Vue）：
 * - 文本/值集/开关 → formData[field]
 * - 数值/时间范围 → formData[`${field}_min`] / formData[`${field}_max`]
 * - 日期/日期时间范围 → formData[field] = [start, end]
 */
import { useEffect, useRef, useState } from 'react';
import { Button, DatePicker, Input, InputNumber, Select, Space, Switch, TimePicker } from 'antd';
import { DownOutlined, UpOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import LovSelect from '@/components/field/LovSelect';
import { resolveSearchControl } from '@/utils/fieldControl';
import { toFieldMeta } from '@/types/field';
import type { FieldMapping } from '@newlifex/field-mapping';
import type { SearchControlType } from '@/types/field';

export interface SearchBarProps {
  fields: FieldMapping[];
  onSearch: (params: Record<string, unknown>) => void;
  onReset: () => void;
}

/** 范围类控件（占 2 格，避免被压缩） */
const RANGE_CONTROLS: SearchControlType[] = ['numberRange', 'dateRange', 'datetimeRange', 'timeRange'];

/** 最小格子宽度，与 entity.css 中 .cube-search-grid 的 minmax 保持一致 */
const MIN_CELL = 200;

/** 折叠时最多显示的行数（规范 §7.2：条件多时折叠，避免挤压表格） */
const MAX_ROWS = 2;

/** 是否范围类控件 */
function isRangeControl(control: SearchControlType): boolean {
  return RANGE_CONTROLS.includes(control);
}

/** 单字段搜索控件 */
function SearchControl({
  field,
  control,
  value,
  onChange,
}: {
  field: FieldMapping;
  control: SearchControlType;
  value: unknown;
  onChange: (value: unknown) => void;
}) {
  const meta = toFieldMeta(field.field);
  const placeholder = `请输入${meta.displayName || meta.name}`;
  const label = meta.displayName || meta.name;

  switch (control) {
    case 'text':
      return (
        <Input
          allowClear
          placeholder={placeholder}
          value={(value as string) ?? ''}
          onChange={(e) => onChange(e.target.value || undefined)}
        />
      );
    case 'numberRange': {
      const range = Array.isArray(value) ? (value as [number | null, number | null]) : [null, null];
      return (
        <Space.Compact style={{ width: '100%' }}>
          <InputNumber
            placeholder="最小值"
            value={range[0]}
            onChange={(v) => onChange([v, range[1]])}
            style={{ width: '50%' }}
          />
          <InputNumber
            placeholder="最大值"
            value={range[1]}
            onChange={(v) => onChange([range[0], v])}
            style={{ width: '50%' }}
          />
        </Space.Compact>
      );
    }
    case 'dateRange': {
      const range = Array.isArray(value) ? (value as [string, string]) : null;
      return (
        <DatePicker.RangePicker
          value={range ? [dayjs(range[0]), dayjs(range[1])] : null}
          onChange={(dates) => onChange(dates ? [dates[0]?.format('YYYY-MM-DD'), dates[1]?.format('YYYY-MM-DD')] : undefined)}
          style={{ width: '100%' }}
        />
      );
    }
    case 'datetimeRange': {
      const range = Array.isArray(value) ? (value as [string, string]) : null;
      return (
        <DatePicker.RangePicker
          showTime
          value={range ? [dayjs(range[0]), dayjs(range[1])] : null}
          onChange={(dates) =>
            onChange(dates ? [dates[0]?.toISOString(), dates[1]?.toISOString()] : undefined)
          }
          style={{ width: '100%' }}
        />
      );
    }
    case 'timeRange': {
      const range = Array.isArray(value) ? (value as [string, string]) : [null, null];
      return (
        <Space.Compact>
          <TimePicker
            placeholder="起"
            value={range[0] ? dayjs(range[0], 'HH:mm:ss') : null}
            onChange={(d) => onChange([d ? d.format('HH:mm:ss') : null, range[1]])}
            style={{ width: '100%' }}
          />
          <TimePicker
            placeholder="止"
            value={range[1] ? dayjs(range[1], 'HH:mm:ss') : null}
            onChange={(d) => onChange([range[0], d ? d.format('HH:mm:ss') : null])}
            style={{ width: '100%' }}
          />
        </Space.Compact>
      );
    }
    case 'lov':
      return (
        <LovSelect
          lovCode={meta.lovCode}
          dataSource={meta.dataSource}
          placeholder={`请选择${label}`}
          value={value as string}
          onChange={onChange}
        />
      );
    case 'lovMulti':
      return (
        <LovSelect
          lovCode={meta.lovCode}
          dataSource={meta.dataSource}
          multiple
          placeholder={`请选择${label}`}
          value={value as string[]}
          onChange={onChange}
        />
      );
    case 'switch':
      return (
        <Select
          allowClear
          placeholder="全部"
          value={value == null ? undefined : value === true || value === 'true' ? '1' : '0'}
          onChange={(v) => onChange(v == null ? undefined : v === '1')}
          options={[
            { value: '1', label: '是' },
            { value: '0', label: '否' },
          ]}
          style={{ width: '100%' }}
        />
      );
    case 'fileExists':
      return (
        <Select
          allowClear
          placeholder="全部"
          value={value == null ? undefined : value === true || value === '1' ? '1' : '0'}
          onChange={(v) => onChange(v == null ? undefined : v === '1')}
          options={[
            { value: '1', label: '有附件' },
            { value: '0', label: '无附件' },
          ]}
          style={{ width: '100%' }}
        />
      );
    default:
      return <Input allowClear placeholder={placeholder} value={(value as string) ?? ''} onChange={(e) => onChange(e.target.value || undefined)} />;
  }
}

export default function SearchBar({ fields, onSearch, onReset }: SearchBarProps) {
  const [formData, setFormData] = useState<Record<string, unknown>>({});
  const [expanded, setExpanded] = useState(false);
  const [visibleCount, setVisibleCount] = useState(0);
  const gridRef = useRef<HTMLDivElement>(null);
  const actionsRef = useRef<HTMLDivElement>(null);

  // 字段变化时重置
  useEffect(() => {
    setFormData({});
    setExpanded(false);
  }, [fields]);

  // 折叠可见数：按容器宽度估算列数，折叠时最多显示 MAX_ROWS 行（范围控件占 2 格）
  useEffect(() => {
    const el = gridRef.current;
    if (!el) return;

    const calc = () => {
      const width = el.clientWidth;
      // 预留操作按钮区宽度，让「搜索/重置/展开」尽量留在最后一行右侧
      const actionsWidth = actionsRef.current?.offsetWidth ?? 0;
      const cols = Math.max(1, Math.floor((width - actionsWidth) / MIN_CELL));
      let cells = 0;
      let count = 0;
      for (const f of fields) {
        const control = resolveSearchControl(toFieldMeta(f.field));
        const need = cols > 1 && isRangeControl(control) ? 2 : 1;
        if (cells + need > cols * MAX_ROWS) break;
        cells += need;
        count++;
      }
      setVisibleCount(count);
    };

    calc();
    if (typeof ResizeObserver !== 'undefined') {
      const ro = new ResizeObserver(calc);
      ro.observe(el);
      return () => ro.disconnect();
    }
    return undefined;
  }, [fields]);

  const setValue = (key: string, value: unknown) => {
    setFormData((prev) => ({ ...prev, [key]: value }));
  };

  const handleSearch = () => {
    // 字段名 → 搜索控件类型（多选 LOV 需转逗号串提交，后端 SplitAsInt 解析）
    const controlOf = new Map(fields.map((f) => [f.field.name, resolveSearchControl(toFieldMeta(f.field))]));
    // 清理空值
    const params: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(formData)) {
      if (v === '' || v === null || v === undefined) continue;
      if (Array.isArray(v)) {
        const nonNull = (v as unknown[]).filter((x) => x !== null && x !== undefined);
        if (!nonNull.length) continue;
        // lovMulti 多选（角色/部门等）：逗号分隔字符串，如 roleID=1,2；范围类保持数组
        params[k] = controlOf.get(k) === 'lovMulti' ? nonNull.join(',') : nonNull;
        continue;
      }
      params[k] = v;
    }
    onSearch(params);
  };

  const handleReset = () => {
    setFormData({});
    onReset();
  };

  if (!fields.length) return null;

  const visibleFields = expanded ? fields : fields.slice(0, visibleCount);
  const canCollapse = fields.length > visibleCount;

  return (
    <div className="cube-search-panel">
      <div ref={gridRef} className="cube-search-fields">
        {visibleFields.map((field) => {
          const meta = toFieldMeta(field.field);
          const control = resolveSearchControl(meta);
          const wide = isRangeControl(control);
          return (
            <div
              key={field.field.name}
              className={wide ? 'cube-search-item cube-search-item--wide' : 'cube-search-item'}
            >
              <span className="cube-search-label">{meta.displayName || meta.name}</span>
              <div className="cube-search-control">
                <SearchControl
                  field={field}
                  control={control}
                  value={formData[field.field.name]}
                  onChange={(v) => setValue(field.field.name, v)}
                />
              </div>
            </div>
          );
        })}
        {/* 操作按钮与条件同行，落在最后一行右侧；行满时自动换行并靠右对齐 */}
        <div ref={actionsRef} className="cube-search-actions">
          <Button type="primary" onClick={handleSearch}>
            搜索
          </Button>
          <Button onClick={handleReset}>重置</Button>
          {canCollapse && (
            <Button type="link" size="small" onClick={() => setExpanded((v) => !v)}>
              {expanded ? (
                <>
                  收起 <UpOutlined />
                </>
              ) : (
                <>
                  展开 <DownOutlined />
                </>
              )}
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
