/**
 * LOV 选项加载 Hook
 *
 * 统一加载值集选项：
 * - 有 dataSource → 直接使用数据字典
 * - lovCode 为 ENUM → 从 Meta 接口取 options
 * - lovCode 为 LIST → 从 ListData 取前 100 条（valueField/labelField 由 Meta 下发）
 */
import { useEffect, useState } from 'react';
import { fetchLovMeta, fetchLovListData } from '@/api/lov';
import { resolveLovType } from '@/types/lov';

export interface LovOption {
  value: string;
  label: string;
}

/**
 * 加载值集选项
 *
 * @param lovCode 值集编码（可选）
 * @param dataSource 数据字典（可选，优先）
 * @returns 选项列表与加载状态
 */
export function useLovOptions(lovCode?: string, dataSource?: Record<string, string>) {
  const [options, setOptions] = useState<LovOption[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    // 数据字典优先
    if (dataSource && Object.keys(dataSource).length > 0) {
      setOptions(Object.entries(dataSource).map(([value, label]) => ({ value, label })));
      return;
    }
    if (!lovCode) {
      setOptions([]);
      return;
    }

    let cancelled = false;
    setLoading(true);
    (async () => {
      try {
        const type = resolveLovType(lovCode);
        if (type === 'ENUM') {
          const metas = await fetchLovMeta(lovCode);
          const item = metas.find((m) => m.type === 'ENUM');
          if (item && !cancelled) {
            setOptions(item.options.map((o) => ({ value: o.value, label: o.label })));
          }
        } else {
          // LIST 型：先取 meta 拿 valueField/labelField，再取前 100 条
          const metas = await fetchLovMeta(lovCode);
          const meta = metas.find((m) => m.type === 'LIST');
          const vf = meta?.valueField ?? 'id';
          const lf = meta?.labelField ?? 'name';
          const res = await fetchLovListData<Record<string, unknown>>({
            lovCode,
            pageNum: 1,
            pageSize: 100,
          });
          if (!cancelled) {
            setOptions(
              res.data.map((row) => ({
                value: String(row[vf] ?? ''),
                label: String(row[lf] ?? ''),
              })),
            );
          }
        }
      } catch {
        // 加载失败静默，保留空选项
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [lovCode, dataSource]);

  return { options, loading };
}

export default useLovOptions;
