/** 看板「未分组」列 key，与 bucketKanban 一致 */
export const KANBAN_UNGROUPED_KEY = '__ungrouped__';

export type KanbanDrop = 'noop' | 'refuse' | 'move';

/** 分组字段在编辑分区、且非只读时才允许拖。返回编辑字段名（大小写以元数据为准） */
export function kanbanGroupDragMeta(
  editFields: { name?: string; readOnly?: boolean; required?: boolean }[],
  groupField: string,
): { field: string; required: boolean } | null {
  if (!groupField) return null;
  const field = editFields.find((item) => (item.name || '').toLowerCase() === groupField.toLowerCase());
  if (!field?.name || field.readOnly) return null;
  return { field: field.name, required: !!field.required };
}

/**
 * 卡片能否拖。迷你看板与无编辑权不可拖。
 * 审批中仅当行上可写名单包含分组字段。
 */
export function kanbanCardDraggable(opts: {
  compact: boolean;
  canDragGroup: boolean;
  groupField: string;
  locked: boolean;
  writable: string[];
}): boolean {
  if (opts.compact || !opts.canDragGroup || !opts.groupField) return false;
  if (!opts.locked) return true;
  const name = opts.groupField.toLowerCase();
  return opts.writable.some((item) => item.toLowerCase() === name);
}

/** 同列不写库；必填字段不能放到「未分组」 */
export function kanbanDropAllowed(fromKey: string, toKey: string, required: boolean): KanbanDrop {
  if (!toKey || fromKey === toKey) return 'noop';
  if (toKey === KANBAN_UNGROUPED_KEY && required) return 'refuse';
  return 'move';
}

/** 布尔列提交 true/false；未分组提交 null；其余原样提交列 key，由后端转换类型 */
export function kanbanPatchValue(typeName: string | undefined, columnKey: string): unknown {
  if (columnKey === KANBAN_UNGROUPED_KEY) return null;
  if (typeName === 'Boolean') {
    const key = columnKey.toLowerCase();
    return key === 'true' || key === '1';
  }
  return columnKey;
}

/** 从按钮或操作区按下时不开始拖，避免挡住详情/编辑/删除 */
export function dragStartedFromControl(target: EventTarget | null): boolean {
  const el = target as { closest?: (sel: string) => unknown } | null;
  return !!el?.closest?.('button, a, .record-card-actions');
}
