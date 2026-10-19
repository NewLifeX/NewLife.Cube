/**
 * 列表 LOV 翻译（值集编码 → 显示标签）
 *
 * 口径与 `core/views/components/ListTableContent.vue:112-178` 一致：
 * 统一由 `LovSelect/lovStore` 的会话级缓存承担，**不再走 BatchLabel 端点**，
 * 也不再新建第二份 LOV 缓存（引擎侧 `deps.lookup` 默认不启用正是同一理由）。
 *
 * 渲染策略：先用同步 `getColumnLabel` 出已缓存的值，未命中的再异步 `resolveColumnLabels`
 * 回填 —— 这样首屏不闪空、也不阻塞表格渲染。
 */
import { reactive, watch } from 'vue';
import { getValueByKey } from '../../utils/url';
import { getColumnLabel, resolveColumnLabels } from '../LovSelect/lovStore';
import { normalizeMulti } from './cubeTable.logic';
import type { CubeColumnView } from './types';

export function useCubeTableLov(
  columns: () => CubeColumnView[],
  rows: () => Record<string, unknown>[],
) {
  /** `lovCode:value` → 标签 */
  const labels = reactive<Record<string, string>>({});

  async function load(): Promise<void> {
    const tasks: Promise<void>[] = [];
    for (const column of columns()) {
      if (column.control !== 'lov' || !column.field.lovCode) continue;
      const lovCode = column.field.lovCode;
      const values = new Set<string>();
      for (const row of rows()) {
        for (const value of normalizeMulti(getValueByKey(row, column.key))) values.add(value);
      }
      // 缓存未命中（getColumnLabel 原样返回传入值）的才需要异步解析
      const uncached = [...values].filter((v) => getColumnLabel(lovCode, v) === v);
      for (const value of uncached) {
        tasks.push(
          (async () => {
            labels[`${lovCode}:${value}`] = await resolveColumnLabels(lovCode, value);
          })(),
        );
      }
    }
    if (tasks.length) await Promise.all(tasks);
  }

  watch([columns, rows], load, { immediate: true });

  function lovLabel(lovCode: string, value: unknown): string {
    return labels[`${lovCode}:${String(value)}`] ?? String(value ?? '');
  }

  return { labels, load, lovLabel };
}
