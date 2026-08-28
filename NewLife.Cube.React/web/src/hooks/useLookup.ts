/**
 * 数据字典查询 Hook（/Cube/Lookup）
 *
 * 批量翻译 / 下拉选项：codes 逗号分隔，返回 code → options 映射。
 */
import { useEffect, useState } from 'react';
import { api } from '@/api';

export interface DictOption {
  value: string;
  label: string;
}

/**
 * 加载数据字典
 *
 * @param codes 字典编码（逗号分隔）
 * @returns code → 选项列表
 *
 * @example
 * ```ts
 * const dicts = useLookup('sex,status');
 * dicts['sex'] // => [{value:'1',label:'男'}, ...]
 * ```
 */
export function useLookup(codes?: string): Record<string, DictOption[]> {
  const [dicts, setDicts] = useState<Record<string, DictOption[]>>({});

  useEffect(() => {
    if (!codes) {
      setDicts({});
      return;
    }
    let cancelled = false;
    api.page
      .lookup(codes)
      .then((res) => {
        if (cancelled) return;
        const map: Record<string, DictOption[]> = {};
        const data = res.data as Record<string, Array<Record<string, unknown>>>;
        for (const [k, items] of Object.entries(data ?? {})) {
          map[k] = (items ?? []).map((it) => ({
            value: String(it.value ?? it.Value ?? ''),
            label: String(it.label ?? it.Text ?? ''),
          }));
        }
        setDicts(map);
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [codes]);

  return dicts;
}

export default useLookup;
