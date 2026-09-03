import cubeApi from '@/api';

/** 接收人类别（与 WorkflowRecipient.kind / 后端 to schema 对齐） */
export type RecipientKind = 'users' | 'roles' | 'departments';

/** 接收人候选项 */
export interface RecipientOption {
  id: number;
  name: string;
  displayName: string;
}

/** 归一任意来源接收人行（Automation/实体列表 键兼容） */
export function normalizeRecipient(row: Record<string, unknown>): RecipientOption | null {
  const id = Number(row.id ?? row.Id ?? row.iD ?? 0);
  if (!Number.isFinite(id) || id <= 0) return null;
  const name = String(row.name ?? row.Name ?? '');
  const displayName = String(
    row.displayName ?? row.DisplayName ?? row.fullName ?? row.FullName ?? (name || id),
  );
  return { id, name, displayName };
}

const KIND_TO_API = { users: 'user', roles: 'role', departments: 'department' } as const;
const KIND_TO_ENTITY = {
  users: '/Admin/User',
  roles: '/Admin/Role',
  departments: '/Admin/Department',
} as const;

/**
 * 搜索接收人：优先 /Cube/Automation/Recipients（用户/角色/部门），
 * 空结果回退对应实体列表（与自动化动作卡片同策略）。
 */
export async function searchRecipients(
  kind: RecipientKind,
  keyword = '',
  limit = 50,
): Promise<RecipientOption[]> {
  const tasks = [
    (async () => {
      try {
        const res = await cubeApi.automation.recipients({
          kind: KIND_TO_API[kind],
          key: keyword || undefined,
        });
        return (res.data ?? [])
          .map((r) => normalizeRecipient(r as unknown as Record<string, unknown>))
          .filter((x): x is RecipientOption => x !== null);
      } catch {
        return [];
      }
    })(),
    (async () => {
      try {
        const type = KIND_TO_ENTITY[kind];
        const res = await cubeApi.page.getList(type, {
          pageIndex: 0,
          pageSize: limit,
          ...(keyword ? { q: keyword } : {}),
        });
        return (res.data ?? [])
          .map((r) => normalizeRecipient(r as unknown as Record<string, unknown>))
          .filter((x): x is RecipientOption => x !== null);
      } catch {
        return [];
      }
    })(),
  ];
  const [fromApi, fromEntity] = await Promise.all(tasks);
  // 两侧都有时取更长的一份；接口优先
  return fromApi.length >= fromEntity.length && fromApi.length > 0
    ? fromApi
    : fromEntity.length > 0
      ? fromEntity
      : fromApi;
}
