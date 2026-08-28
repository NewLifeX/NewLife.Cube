/**
 * 搜索栏（动态搜索控件）
 *
 * 依据 resolveSearchControl 渲染 text / numberRange / dateRange / datetimeRange /
 * timeRange / lov / lovMulti / switch / fileExists 控件。
 * 搜索参数约定（对齐 Vue）：
 * - 文本/值集/开关 → formData[field]
 * - 数值/时间范围 → formData[`${field}_min`] / formData[`${field}_max`]
 * - 日期/日期时间范围 → formData[field] = [start, end]
 */
import { useEffect, useState } from 'react';
import { Button, DatePicker, Input, InputNumber, Select, Space, Switch, TimePicker } from 'antd';
import dayjs from 'dayjs';
import LovSelect from '@/components/field/LovSelect';
import { resolveSearchControl } from '@/utils/fieldControl';
import { toFieldMeta } from '@/types/field';
import type { FieldMapping } from '@cube/field-mapping';
import type { SearchControlType } from '@/types/field';

export interface SearchBarProps {
  fields: FieldMapping[];
  onSearch: (params: Record<string, unknown>) => void;
  onReset: () => void;
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

  // 字段变化时重置
  useEffect(() => {
    setFormData({});
  }, [fields]);

  const setValue = (key: string, value: unknown) => {
    setFormData((prev) => ({ ...prev, [key]: value }));
  };

  const handleSearch = () => {
    // 清理空值
    const params: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(formData)) {
      if (v === '' || v === null || v === undefined) continue;
      if (Array.isArray(v)) {
        const nonNull = (v as unknown[]).filter((x) => x !== null && x !== undefined);
        if (nonNull.length) params[k] = nonNull;
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

  return (
    <div className="cube-search-panel">
      <div className="cube-search-grid">
        {fields.map((field) => {
          const meta = toFieldMeta(field.field);
          const control = resolveSearchControl(meta);
          return (
            <div key={field.field.name} className="cube-search-item">
              <span className="cube-search-label">{meta.displayName || meta.name}</span>
              <SearchControl
                field={field}
                control={control}
                value={formData[field.field.name]}
                onChange={(v) => setValue(field.field.name, v)}
              />
            </div>
          );
        })}
      </div>
      <div className="cube-search-actions">
        <Button type="primary" onClick={handleSearch}>
          搜索
        </Button>
        <Button onClick={handleReset}>重置</Button>
      </div>
    </div>
  );
}
