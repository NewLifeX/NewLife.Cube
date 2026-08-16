export type OpsAction = 'detail' | 'edit' | 'delete';

/** 操作动作显示名（customLayout 独立链接渲染） */
export const OPS_ACTION_LABELS: Record<OpsAction, string> = {
  detail: '详情',
  edit: '编辑',
  delete: '删除',
};

/** 操作列额外按钮（自动化 button 规则） */
export interface OpsAutomationButton {
  id: number | string;
  name: string;
}

/** 操作链接配色（Arco 语义 token）：detail/edit=主色、delete=警示色、其余系统自定义=链接色 */
export interface OpsLinkColor {
  /** 正常态 Arco token 名（--primary-6 / --danger-6 / --link-6） */
  token: string;
  /** hover 态 Arco token 名 */
  hoverToken: string;
  /** 正常态兜底三元组（无浏览器/SSR 时） */
  fallback: string;
  /** hover 态兜底三元组 */
  hoverFallback: string;
}

export const OPS_ACTION_COLORS: Record<string, OpsLinkColor> = {
  detail: {
    token: '--primary-6',
    hoverToken: '--primary-5',
    fallback: '22, 93, 255',
    hoverFallback: '20, 86, 240',
  },
  edit: {
    token: '--primary-6',
    hoverToken: '--primary-5',
    fallback: '22, 93, 255',
    hoverFallback: '20, 86, 240',
  },
  delete: {
    token: '--danger-6',
    hoverToken: '--danger-5',
    fallback: '245, 63, 63',
    hoverFallback: '249, 141, 141',
  },
};

/** 其余系统自定义操作默认链接色 */
export const OPS_LINK_COLOR: OpsLinkColor = {
  token: '--link-6',
  hoverToken: '--link-5',
  fallback: '22, 93, 255',
  hoverFallback: '69, 149, 255',
};

/** 按动作取链接配色；未登记的自定义操作回落链接色 */
export function opsActionColor(action: OpsAction | string): OpsLinkColor {
  return OPS_ACTION_COLORS[action] ?? OPS_LINK_COLOR;
}

export function opsAutoKey(id: number | string): string {
  return `auto:${id}`;
}

/** 按权限拼操作动作清单（与 ListTable customLayout 渲染一致） */
export function buildOpsParts(flags: {
  canViewDetail?: boolean;
  canEdit?: boolean;
  canDelete?: boolean;
  automationButtons?: OpsAutomationButton[];
}): (OpsAction | string)[] {
  const parts: (OpsAction | string)[] = [];
  if (flags.canViewDetail) parts.push('detail');
  if (flags.canEdit) parts.push('edit');
  if (flags.canDelete) parts.push('delete');
  const extra = (flags.automationButtons ?? []).slice(0, 3);
  for (const b of extra) parts.push(opsAutoKey(b.id));
  return parts;
}
