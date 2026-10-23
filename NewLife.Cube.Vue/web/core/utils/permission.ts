/**
 * 页面动作权限解析。
 *
 * `/meta` 只下发静态的 menuId + requiredPermissions；实际权限来自登录用户的
 * `permission` 串（格式：`menuId#flags,menuId#flags`）。这样 `/meta` 可以安全缓存，
 * 角色权限变化不会读到旧的有效权限。
 */

/** 解析用户权限串为菜单编号 → 权限位 */
export function parsePermissionMap(raw?: string | null): Map<number, number> {
  const map = new Map<number, number>();
  if (!raw) return map;

  for (const part of raw.split(',')) {
    const [id, flags] = part.split('#');
    const menuId = Number(id);
    if (!Number.isFinite(menuId) || menuId <= 0) continue;
    const value = Number(flags ?? 0);
    map.set(menuId, Number.isFinite(value) ? value : 0);
  }
  return map;
}

/**
 * 判断当前用户是否具备页面动作权限。
 *
 * - 没有菜单或没有声明权限要求：放行（最终仍由后端 `[EntityAuthorize]` 拒绝）；
 * - 系统管理员：放行；
 * - 用户权限串中没有该菜单：按现有“未配置权限的菜单默认放行”口径放行。
 */
export function canUseAction(
  permissions: Map<number, number>,
  menuId: number | undefined,
  required: number | undefined,
  isSystemAdmin = false,
): boolean {
  if (!required || menuId == null) return true;
  if (isSystemAdmin) return true;
  const flags = permissions.get(menuId);
  if (flags == null) return true;
  return (flags & required) === required;
}
