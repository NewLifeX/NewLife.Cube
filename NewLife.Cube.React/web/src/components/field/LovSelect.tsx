/**
 * LOV 值集选择组件（单选 / 多选）
 *
 * 选项来源优先级：dataSource（数据字典）→ lovCode（Meta/ListData 接口）。
 * 多选提交时由 serializeSubmitModel 统一转为逗号分隔字符串。
 */
import { Select } from 'antd';
import { useLovOptions } from '@/hooks/useLovOptions';

export interface LovSelectProps {
  value?: string | string[] | number | number[] | null;
  onChange?: (value: string | string[] | undefined) => void;
  lovCode?: string;
  dataSource?: Record<string, string>;
  multiple?: boolean;
  placeholder?: string;
  disabled?: boolean;
  allowClear?: boolean;
  style?: React.CSSProperties;
}

export default function LovSelect({
  value,
  onChange,
  lovCode,
  dataSource,
  multiple = false,
  placeholder,
  disabled,
  allowClear = true,
  style,
}: LovSelectProps) {
  const { options, loading } = useLovOptions(lovCode, dataSource);

  // 后端 value 可能是数字，统一转字符串比较；展示时保持原值
  const normalized = Array.isArray(value)
    ? value.map((v) => String(v))
    : value != null
      ? String(value)
      : undefined;

  return (
    <Select
      mode={multiple ? 'multiple' : undefined}
      value={normalized}
      onChange={(val) => onChange?.(val as string | string[] | undefined)}
      options={options}
      loading={loading}
      placeholder={placeholder}
      disabled={disabled}
      allowClear={allowClear}
      showSearch
      optionFilterProp="label"
      style={{ width: '100%', ...style }}
    />
  );
}
