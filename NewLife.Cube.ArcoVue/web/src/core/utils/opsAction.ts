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

/** GetPage 合成 Url / dataAction 自定义链接（OSC-2608178bdb） */
export interface OpsCustomLink {
  name: string;
  label: string;
  url: string;
  target?: string;
  dataAction?: string;
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

export function opsLinkKey(name: string): string {
  return `link:${name}`;
}

export function isOpsLinkKey(action: string): boolean {
  return action.startsWith('link:');
}

export function parseOpsLinkKey(action: string): string | null {
  return isOpsLinkKey(action) ? action.slice(5) : null;
}

/** 按权限拼操作动作清单（与 ListTable customLayout 渲染一致） */
export function buildOpsParts(flags: {
  canViewDetail?: boolean;
  canEdit?: boolean;
  canDelete?: boolean;
  automationButtons?: OpsAutomationButton[];
}): (OpsAction | string)[] {
  return buildOpsPartsWithLinks({ ...flags, opsLinks: [] }).parts;
}

/**
 * 含自定义链接的操作列拼装：详情→编辑→删除→自定义直出≤N→自动化≤3；
 * 超出直出的自定义进 overflowLinks，parts 末尾追加 `more`。
 */
export function buildOpsPartsWithLinks(flags: {
  canViewDetail?: boolean;
  canEdit?: boolean;
  canDelete?: boolean;
  automationButtons?: OpsAutomationButton[];
  opsLinks?: OpsCustomLink[];
  inlineMax?: number;
}): {
  parts: (OpsAction | string)[];
  overflowLinks: OpsCustomLink[];
  linkByKey: Record<string, OpsCustomLink>;
} {
  const parts: (OpsAction | string)[] = [];
  if (flags.canViewDetail) parts.push('detail');
  if (flags.canEdit) parts.push('edit');
  if (flags.canDelete) parts.push('delete');

  const links = flags.opsLinks ?? [];
  const max = flags.inlineMax ?? 2;
  const inline = links.slice(0, max);
  const overflowLinks = links.slice(max);
  const linkByKey: Record<string, OpsCustomLink> = {};
  for (const l of links) {
    linkByKey[opsLinkKey(l.name)] = l;
  }
  for (const l of inline) {
    parts.push(opsLinkKey(l.name));
  }

  const extra = (flags.automationButtons ?? []).slice(0, 3);
  for (const b of extra) parts.push(opsAutoKey(b.id));

  if (overflowLinks.length) parts.push('more');

  return { parts, overflowLinks, linkByKey };
}
