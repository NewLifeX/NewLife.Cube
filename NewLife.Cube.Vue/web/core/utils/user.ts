export const getCurrentUser = () => {
  const userInfo = window.store.state.value.userInfo;
  return userInfo;
};

/**
 * 获取当前租户信息
 */
export function getCurrentTenant() {
  const state = window.getDvaApp?.()?._store.getState();
  const _state$user = state.user,
    user = _state$user === void 0 ? {} : _state$user;
  const _user$currentUser = user.currentUser,
    currentUser = _user$currentUser === void 0 ? {} : _user$currentUser;
  const tenantId = currentUser.tenantId,
    tenantName = currentUser.tenantName,
    tenantNum = currentUser.tenantNum;
  return {
    tenantId,
    tenantName,
    tenantNum,
  };
}

/**
 * 获取当前用户角色租户id
 */
export function getCurrentTenantId() {
  return getCurrentTenant().tenantId;
}

/**
 * 获取当前用户角色租户id
 */
export function getCurrentOrganizationId() {
  let _getCurrentTenant;
  return (_getCurrentTenant = getCurrentTenant()) === null || _getCurrentTenant === void 0
    ? void 0
    : _getCurrentTenant.tenantId;
  // return getCurrentUser()?.organizationId;
}

export function isTenantRoleLevel() {
  const _getCurrentRole = getCurrentRole(),
    level = _getCurrentRole.level;
  return level !== 'site';
}

/**
 * 获取当前用户所属租户 ID
 */
export function getUserOrganizationId() {
  return getCurrentUser().organizationId;
}

/**
 * 获取当前登录用户id
 */
export function getCurrentUserId() {
  return getCurrentUser().id;
}
export function isEqualOrganization() {
  const _getCurrentRole2 = getCurrentRole(),
    level = _getCurrentRole2.level;
  return level === 'organization';
}
export function getCurrentRole(dvaState: any = undefined) {
  const state = dvaState || window.getDvaApp?.()?._store.getState();
  const _state$user2 = state.user,
    user = _state$user2 === void 0 ? {} : _state$user2;
  const _user$currentUser2 = user.currentUser,
    currentUser = _user$currentUser2 === void 0 ? {} : _user$currentUser2;
  const currentRoleId = currentUser.currentRoleId,
    currentRoleName = currentUser.currentRoleName,
    currentRoleLevel = currentUser.currentRoleLevel,
    currentRoleCode = currentUser.currentRoleCode;
  return {
    id: currentRoleId,
    name: currentRoleName,
    level: currentRoleLevel,
    code: currentRoleCode,
  };
}
if (process.env.NODE_ENV === 'development') {
  window.getCurrentOrganizationId = getCurrentOrganizationId;
}

/**
 * 获取系统当前语言
 * @export
 * @returns
 */
export function getCurrentLanguage() {
  const state = window.getDvaApp?.()?._store.getState();
  const _state$user3 = state.user,
    user = _state$user3 === void 0 ? {} : _state$user3;
  const _user$currentUser3 = user.currentUser,
    currentUser = _user$currentUser3 === void 0 ? {} : _user$currentUser3;
  const language = currentUser.language;
  return language;
}

/**
 * 获取平台版本API
 */
export function getPlatformVersionApi(api: any) {
  const tenantId = getCurrentOrganizationId();
  const isTenantLevel = isTenantRoleLevel();
  return isTenantLevel ? `${tenantId}/${api}` : `${api}`;
}
