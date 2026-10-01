import type { FieldMeta } from '@/core/types/field';
import { isCascaderField, resolveListControl } from '@/core/utils/fieldControl';
import { getValueByKey } from '@/core/utils/url';
import { missingLovValues } from './useListQuery';

/** 同一值集上还缺标签的字段与取值 */
export interface MissingLovGroup {
  code: string;
  values: string[];
  fields: FieldMeta[];
}

function pushValue(values: string[], value: string) {
  if (values.some((item) => item.toLowerCase() === value.toLowerCase())) return;
  values.push(value);
}

/**
 * 抽屉里还缺标签的 LOV 字段，按值集分组。
 * 入选条件与列表 hydrateLovLabels 相同：有 lovCode，且列表控件为 lov。级联与枚举不进组。
 * dataSource 里已经有的值不再请求。
 */
export function groupMissingLov(
  fields: FieldMeta[],
  row: Record<string, unknown>,
): MissingLovGroup[] {
  const groups = new Map<string, MissingLovGroup>();
  for (const field of fields) {
    const code = (field.lovCode || '').trim();
    if (!code || code.toLowerCase().startsWith('enum.')) continue;
    if (isCascaderField(field)) continue;
    if (resolveListControl(field) !== 'lov') continue;
    const raw = getValueByKey(row, field.name);
    if (raw == null || raw === '') continue;
    const missing = missingLovValues([String(raw)], undefined, field.dataSource);
    if (!missing.length) continue;
    let group = groups.get(code);
    if (!group) {
      group = { code, values: [], fields: [] };
      groups.set(code, group);
    }
    group.fields.push(field);
    for (const value of missing) pushValue(group.values, value);
  }
  return [...groups.values()];
}
