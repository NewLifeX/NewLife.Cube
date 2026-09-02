/**
 * 命名工作台纯函数（OSC-260902ef43）。
 * 供 useWorkbench / menuRoutes 等复用；slug 白名单与后端 WorkbenchNamedStore.IsValidSlug 一致。
 */

/** slug 白名单：小写字母开头，仅小写字母/数字/中划线，长度 ≤32 */
export function isValidNamedSlug(slug: string): boolean {
  return /^[a-z][a-z0-9-]{0,31}$/.test(slug);
}

/** 4 位随机小写 hex */
export function randomSlugSuffix(len = 4): string {
  let s = '';
  for (let i = 0; i < len; i++) s += Math.floor(Math.random() * 16).toString(16);
  return s;
}

/** 发布对话框默认 slug：wb-<4 位 hex> */
export function defaultPublishSlug(): string {
  return 'wb-' + randomSlugSuffix();
}

export type NamedMenuAction =
  | { action: 'publish' }
  | { action: 'rename' }
  | { action: 'delete' }
  | { action: 'default' }
  | { action: 'switch'; slug: string };

/**
 * ▾ 菜单项 value → 动作。识别 __publish/__rename/__delete/__default/__named:{slug}；
 * 未知返回 null（调用方忽略）。
 */
export function parseNamedMenuValue(value: string): NamedMenuAction | null {
  if (value === '__publish') return { action: 'publish' };
  if (value === '__rename') return { action: 'rename' };
  if (value === '__delete') return { action: 'delete' };
  if (value === '__default') return { action: 'default' };
  if (value.startsWith('__named:')) {
    const slug = value.slice('__named:'.length);
    if (isValidNamedSlug(slug)) return { action: 'switch', slug };
  }
  return null;
}

/** 工作台页路由路径：空 slug → /home；有 slug → /Workbench/{slug} */
export function workbenchRoutePath(slug: string): string {
  return slug ? `/Workbench/${slug}` : '/home';
}
