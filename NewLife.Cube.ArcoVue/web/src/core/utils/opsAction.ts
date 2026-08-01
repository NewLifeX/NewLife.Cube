export type OpsAction = 'detail' | 'edit' | 'delete';

/** 按权限拼操作列文案（与 ListTable 展示一致） */
export function buildOpsParts(flags: {
  canViewDetail?: boolean;
  canEdit?: boolean;
  canDelete?: boolean;
}): OpsAction[] {
  const parts: OpsAction[] = [];
  if (flags.canViewDetail) parts.push('detail');
  if (flags.canEdit) parts.push('edit');
  if (flags.canDelete) parts.push('delete');
  return parts;
}

export function formatOpsLabel(parts: OpsAction[]): string {
  const map: Record<OpsAction, string> = {
    detail: '详情',
    edit: '编辑',
    delete: '删除',
  };
  return parts.length ? parts.map((p) => map[p]).join(' · ') : '-';
}

/**
 * 在操作列内按横向比例命中动作（canvas 文本列无独立节点）。
 * ratio: 0~1，相对单元格左缘。
 */
export function resolveOpsActionByRatio(
  ratio: number,
  flags: { canViewDetail?: boolean; canEdit?: boolean; canDelete?: boolean },
): OpsAction | null {
  const parts = buildOpsParts(flags);
  if (!parts.length) return null;
  if (parts.length === 1) return parts[0];
  const r = Number.isFinite(ratio) ? Math.min(1, Math.max(0, ratio)) : 0;
  // 右边界落在最后一段
  const idx = Math.min(parts.length - 1, Math.floor(r * parts.length));
  return parts[idx];
}
