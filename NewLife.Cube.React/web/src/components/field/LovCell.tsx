/**
 * LOV 单元格：值集编码 + 值 → 显示标签
 */
import { useLovOptions } from '@/hooks/useLovOptions';

export interface LovCellProps {
  value?: unknown;
  lovCode?: string;
  dataSource?: Record<string, string>;
}

export default function LovCell({ value, lovCode, dataSource }: LovCellProps) {
  const { options } = useLovOptions(lovCode, dataSource);
  if (value == null || value === '') return <span>-</span>;

  const raw = String(value);
  // 多选（逗号分隔）逐个翻译
  if (raw.includes(',')) {
    const parts = raw.split(',').filter(Boolean);
    return (
      <span>
        {parts.map((p, i) => {
          const opt = options.find((o) => o.value === p);
          return (
            <span key={i}>
              {i > 0 && '、'}
              {opt?.label ?? p}
            </span>
          );
        })}
      </span>
    );
  }
  const opt = options.find((o) => o.value === raw);
  return <span>{opt?.label ?? raw}</span>;
}
