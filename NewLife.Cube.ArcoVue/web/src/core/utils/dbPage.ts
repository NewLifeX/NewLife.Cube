import { Auth, checkAuth } from '@newlifex/page-utils';

/** 数据库页动作权限；未配置菜单权限时保持现有页面的开发友好策略。 */
export function getDbActionPermissions(perms: Record<string, string> | null | undefined) {
  const keys = Object.keys(perms ?? {}).length;
  return {
    canInspect: keys === 0 || checkAuth(perms ?? {}, Auth.VIEW),
    canCompact: keys === 0 || checkAuth(perms ?? {}, Auth.EDIT),
  };
}

/** 名称/备注拆分：Description 以首个「。」分隔，前半为名称（不含句号），后半为备注；无描述回落技术名。 */
export function splitDbDescription(name: string, description?: string): { name: string; remark: string } {
  const text = (description ?? '').trim();
  if (!text) return { name, remark: '' };

  const index = text.indexOf('。');
  if (index < 0) return { name: text, remark: '' };

  return { name: text.slice(0, index), remark: text.slice(index + 1).trim() };
}
