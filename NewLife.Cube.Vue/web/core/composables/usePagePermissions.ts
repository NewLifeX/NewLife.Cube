import { computed, type ComputedRef } from 'vue';
import type { CubeEngineContext } from '../engine/types';
import { useUserStore } from '../stores/user';
import { canUseAction, parsePermissionMap } from '../utils/permission';

export interface PagePermissions {
  /** 新增动作：页面能力由调用方再与 pageSetting.enableAdd 合并 */
  canAdd: ComputedRef<boolean>;
  canUpdate: ComputedRef<boolean>;
  canDelete: ComputedRef<boolean>;
  canExport: ComputedRef<boolean>;
}

/**
 * 从 `/meta` 的静态权限要求 + 当前用户 `permission` 串计算页面动作权限。
 *
 * 组件单测通常没有 Pinia；此时返回“不额外限制”，保持既有组件行为，
 * 页面级能力（enableAdd / isReadOnly）仍由调用方控制。
 */
export function usePagePermissions(ctx: CubeEngineContext): PagePermissions {
  let userStore: ReturnType<typeof useUserStore> | null = null;
  try {
    userStore = useUserStore();
  } catch {
    userStore = null;
  }

  const permissions = computed(() => parsePermissionMap(userStore?.userInfo?.permission));
  const setting = computed(() => ctx.pageSetting.value);
  const isSystemAdmin = () => userStore?.userInfo?.isSystemAdmin === true;

  const can = (key: 'add' | 'update' | 'delete') =>
    computed(() =>
      canUseAction(
        permissions.value,
        setting.value.menuId,
        setting.value.requiredPermissions?.[key],
        isSystemAdmin(),
      ),
    );

  return {
    canAdd: can('add'),
    canUpdate: can('update'),
    canDelete: can('delete'),
    // 导出动作当前沿用 Detail 权限，后端已校验；这里不额外限制
    canExport: computed(() => true),
  };
}
