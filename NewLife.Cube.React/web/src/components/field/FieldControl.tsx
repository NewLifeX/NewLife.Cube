/**
 * 字段控件渲染器（表单/编辑控件）
 *
 * 依据 utils/fieldControl.resolveControl 解析的控件类型，渲染对应 AntD 控件。
 * 本组件是表单页 / 表单弹窗共用的唯一控件渲染入口。
 */
import dayjs from 'dayjs';
import { ColorPicker, DatePicker, Input, InputNumber, Switch, TimePicker } from 'antd';
import LovSelect from './LovSelect';
import Uploader from './Uploader';
import JsonEditor from './JsonEditor';
import IconSelector from './IconSelector';
import RichEditor from './RichEditor';
import { resolveControl, isFullWidthControl, resolveNumberPrecision } from '@/utils/fieldControl';
import type { FieldMeta } from '@/types/field';

export interface FieldControlProps {
  /** 字段元数据 */
  field: FieldMeta;
  /** 当前值 */
  value?: unknown;
  /** 值变更回调 */
  onChange?: (value: unknown) => void;
  /** 实体路径前缀（上传需要，如 '/Admin/User'） */
  apiPrefix?: string;
  /** 主记录主键（上传需要，0=新增） */
  recordId?: number | string;
  /** 是否禁用 */
  disabled?: boolean;
}

/** 布尔值归一（后端可能返回 'true'/'false' 字符串） */
function toBool(v: unknown): boolean {
  return v === true || v === 'true' || v === 1 || v === '1';
}

export default function FieldControl({ field, value, onChange, apiPrefix, recordId, disabled }: FieldControlProps) {
  const control = resolveControl(field);
  const placeholder = field.description || field.displayName;
  const commonProps = { disabled };

  const renderByControl = () => {
    switch (control) {
      case 'input':
      case 'email':
      case 'tel':
      case 'url': {
        const typeMap = { email: 'email', tel: 'tel', url: 'url' } as const;
        return (
          <Input
            {...commonProps}
            type={control === 'input' ? 'text' : typeMap[control as 'email' | 'tel' | 'url']}
            value={(value as string) ?? ''}
            onChange={(e) => onChange?.(e.target.value)}
            placeholder={placeholder}
            allowClear
          />
        );
      }
      case 'textarea':
        return (
          <Input.TextArea
            {...commonProps}
            value={(value as string) ?? ''}
            onChange={(e) => onChange?.(e.target.value)}
            placeholder={placeholder}
            rows={3}
          />
        );
      case 'inputNumber':
        return (
          <InputNumber
            {...commonProps}
            value={typeof value === 'number' ? value : value != null && value !== '' ? Number(value) : undefined}
            onChange={(v) => onChange?.(v)}
            precision={resolveNumberPrecision(field)}
            placeholder={placeholder}
            style={{ width: '100%' }}
          />
        );
      case 'switch':
        return <Switch {...commonProps} checked={toBool(value)} onChange={(v) => onChange?.(v)} />;
      case 'datePicker':
        return (
          <DatePicker
            {...commonProps}
            showTime
            value={value ? dayjs(value as string) : null}
            onChange={(d) => onChange?.(d ? d.toISOString() : null)}
            style={{ width: '100%' }}
          />
        );
      case 'timePicker':
        return (
          <TimePicker
            {...commonProps}
            value={value ? dayjs(value as string) : null}
            onChange={(d) => onChange?.(d ? d.format('HH:mm:ss') : null)}
            style={{ width: '100%' }}
          />
        );
      case 'lov':
        return (
          <LovSelect
            {...commonProps}
            value={value as string}
            onChange={(v) => onChange?.(v)}
            lovCode={field.lovCode}
            dataSource={field.dataSource}
            placeholder={placeholder}
          />
        );
      case 'lovMulti':
        return (
          <LovSelect
            {...commonProps}
            value={value as string[]}
            onChange={(v) => onChange?.(v)}
            lovCode={field.lovCode}
            dataSource={field.dataSource}
            multiple
            placeholder={placeholder}
          />
        );
      case 'image':
        return (
          <Uploader
            {...commonProps}
            value={value as string}
            onChange={(v) => onChange?.(v)}
            type={apiPrefix}
            recordId={recordId}
            image
            placeholder={placeholder}
          />
        );
      case 'upload':
        return (
          <Uploader
            {...commonProps}
            value={value as string}
            onChange={(v) => onChange?.(v)}
            type={apiPrefix}
            recordId={recordId}
            placeholder={placeholder}
          />
        );
      case 'json':
        return <JsonEditor {...commonProps} value={value as string} onChange={(v) => onChange?.(v)} placeholder={placeholder} />;
      case 'richHtml':
        return <RichEditor {...commonProps} value={value as string} onChange={(v) => onChange?.(v)} placeholder={placeholder} />;
      case 'richMarkdown':
        return <RichEditor {...commonProps} markdown value={value as string} onChange={(v) => onChange?.(v)} placeholder={placeholder} />;
      case 'color':
        return (
          <ColorPicker
            {...commonProps}
            value={(value as string) || undefined}
            onChange={(_, hex) => onChange?.(hex)}
            showText
          />
        );
      case 'icon':
        return <IconSelector {...commonProps} value={value as string} onChange={(v) => onChange?.(v)} placeholder={placeholder} />;
      case 'readonly':
      default:
        return <span>{String(value ?? '')}</span>;
    }
  };

  return <div style={isFullWidthControl(control) ? { width: '100%' } : undefined}>{renderByControl()}</div>;
}
