/**
 * 列表 LOV 翻译（值集编码 → 显示标签）
 *
 * 标签真源只有一个：`LovSelect/lovStore` 的会话级缓存（不走已废弃的 BatchLabel 端点，
 * 也不在引擎里再建一份缓存 —— 引擎 `deps.lookup` 默认不启用正是同一理由）。
 *
 * 为什么还要本地存一份 reactive 副本：`lovStore.labelCache` 是普通 `Map`，不响应式，
 * 异步解析完成后表格不会自己重渲染。但**读取时必须先认共享缓存**——
 * 它可能已被别处播种（最常见：搜索区同一个值集的 `LovSelect` 在挂载时就拉过 meta）。
 * 早先的写法把「共享缓存已命中」当成「不用再处理」直接跳过，结果本地副本始终是空的，
 * 单元格一直显示码值（`1` 而不是 `启用`）。现在已命中就抄进副本，读时也回退查共享缓存。
 *
 * ⚠️ 比 `ListTableContent.vue:136-159` 多一步 `getMeta()`，那是修另一处真 bug：
 * `resolveColumnLabels()` 的 ENUM 快路径只读 `getCachedMeta()`、自己不拉 meta
 * （`core/components/LovSelect/lovStore.ts:202-206`）。列表页若没有同值集的 LovSelect 预热，
 * 枚举列会永远停在码值上 —— 旧 Section 组件同样中招。
 */
import { reactive, watch } from 'vue';
import { getValueByKey } from '../../utils/url';
import { getColumnLabel, getMeta, resolveColumnLabels } from '../LovSelect/lovStore';
import { normalizeMulti } from './cubeTable.logic';
import type { CubeColumnView } from './types';

export function useCubeTableLov(
  columns: () => CubeColumnView[],
  rows: () => Record<string, unknown>[],
) {
  /** `lovCode:value` → 标签（响应式副本，负责驱动单元格重渲染） */
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

      const pending: string[] = [];
      for (const value of values) {
        const cached = getColumnLabel(lovCode, value);
        // 共享缓存已经有标签就抄过来；只有「缓存返回原值」才需要异步解析
        if (cached !== value) labels[`${lovCode}:${value}`] = cached;
        else pending.push(value);
      }
      if (!pending.length) continue;

      tasks.push(
        (async () => {
          try {
            await getMeta(lovCode);
          } catch (err) {
            // 拉不到 meta 就保留原始码值继续显示，但在控制台留痕：
            // 否则「这一列为什么显示 1 不显示 启用」没有任何可查线索
            console.warn(`[CubeTable] Failed to load metadata of value set ${lovCode}:`, err);
          }
          for (const value of pending) {
            labels[`${lovCode}:${value}`] = await resolveColumnLabels(lovCode, value);
          }
        })(),
      );
    }
    if (tasks.length) await Promise.all(tasks);
  }

  watch([columns, rows], load, { immediate: true });

  /** 读标签：本地副本优先 → 回退共享缓存 → 都没有才显示原始值 */
  function lovLabel(lovCode: string, value: unknown): string {
    const raw = String(value ?? '');
    const local = labels[`${lovCode}:${raw}`];
    if (local !== undefined && local !== raw) return local;
    const cached = getColumnLabel(lovCode, raw);
    return cached !== raw ? cached : local ?? raw;
  }

  return { labels, load, lovLabel };
}
