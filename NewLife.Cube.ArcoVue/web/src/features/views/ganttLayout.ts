import { getValueByKey } from '@/core/utils/url';
import type { GanttMapping } from '@/core/utils/viewMapping';

/** 像素抖动阈值：1px 滚动条/亚像素变化不触发重建 */
export const GANTT_LAYOUT_EPS = 2;

export function nearlySame(a: number, b: number, eps = GANTT_LAYOUT_EPS): boolean {
  return Math.abs(a - b) < eps;
}

/** mapping 身份：不含 tableWidth（拖拽宽度上报不得整表重建） */
export function ganttMappingSignature(m: GanttMapping | null | undefined): string {
  if (!m) return '';
  return [
    m.titleField,
    m.plannedStartField,
    m.plannedEndField,
    m.actualStartField ?? '',
    m.actualEndField ?? '',
    m.barColor ?? '',
  ].join('\0');
}

/** 记录身份：id + 计划/实际日期 + 标题；同内容新数组不重建 */
export function ganttRecordsSignature(
  records: Record<string, unknown>[],
  mapping: GanttMapping | null | undefined,
  rowKey: string,
): string {
  if (!mapping || !records.length) return String(records.length);
  const { titleField, plannedStartField, plannedEndField, actualStartField, actualEndField } =
    mapping;
  return records
    .map((row, idx) => {
      const id = getValueByKey(row, rowKey);
      return [
        id == null || id === '' ? idx : id,
        getValueByKey(row, titleField) ?? '',
        getValueByKey(row, plannedStartField) ?? '',
        getValueByKey(row, plannedEndField) ?? '',
        actualStartField ? (getValueByKey(row, actualStartField) ?? '') : '',
        actualEndField ? (getValueByKey(row, actualEndField) ?? '') : '',
      ].join('\t');
    })
    .join('\n');
}
