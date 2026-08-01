import { checkAuth, Auth, type AuthCode } from '@cube/page-utils';
import type { PageSetting } from '@cube/api-core';

export interface CrudFlags {
  canAdd: boolean;
  canEdit: boolean;
  canDelete: boolean;
  canExport: boolean;
  canImport: boolean;
}

/**
 * 权限门闩：数字 Auth 码 + pageSetting。
 * 无权限配置时允许（开发友好）；有配置则严格 checkAuth。
 */
export function resolveCrudFlags(
  perms: Record<string, string> | null | undefined,
  setting: PageSetting | null | undefined,
): CrudFlags {
  const p = perms ?? {};
  const noPermConfig = Object.keys(p).length === 0;
  const allow = (code: AuthCode) => noPermConfig || checkAuth(p, code);
  const readOnly = setting?.isReadOnly === true;

  return {
    canAdd: allow(Auth.ADD) && setting?.enableAdd !== false && !readOnly,
    canEdit: allow(Auth.EDIT) && !readOnly,
    canDelete: allow(Auth.DELETE) && !readOnly,
    canExport: allow(Auth.EXPORT),
    canImport: allow(Auth.IMPORT),
  };
}

export { Auth, checkAuth };
