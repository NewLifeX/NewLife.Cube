/**
 * DefaultRole：后端存角色 Name；候选优先用 GetFields 物化的 dataSource。
 * 若旧后端未下发字典，则按启用角色列表兜底（Name→Name）。
 */
import cubeApi from '@/api';
import type { FieldMeta } from '@/core/types/field';

function isDefaultRoleField(name?: string): boolean {
  return (name || '').toLowerCase() === 'defaultrole';
}

function hasDataSource(field: FieldMeta): boolean {
  return !!(field.dataSource && Object.keys(field.dataSource).length);
}

/** 将当前已保存值并入候选项，避免下拉看不到历史配置 */
export function ensureCurrentRoleOption(
  field: FieldMeta,
  currentValue?: unknown,
): void {
  if (!field.dataSource) field.dataSource = {};
  const cur = currentValue == null ? '' : String(currentValue).trim();
  if (!cur) return;
  if (!(cur in field.dataSource)) field.dataSource[cur] = cur;
}

/**
 * 为 DefaultRole 填充可选角色。
 * @param fields Object 表单字段
 * @param model 当前配置对象（读取已保存 DefaultRole）
 */
export async function enrichDefaultRoleField(
  fields: FieldMeta[],
  model?: Record<string, unknown> | null,
): Promise<void> {
  const field = fields.find((f) => isDefaultRoleField(f.name));
  if (!field) return;

  const current =
    model?.DefaultRole ?? model?.defaultRole ?? undefined;

  if (hasDataSource(field)) {
    ensureCurrentRoleOption(field, current);
    return;
  }

  try {
    const res = await cubeApi.page.getList<Record<string, unknown>>('/Admin/Role', {
      pageIndex: 0,
      pageSize: 500,
      enable: true,
    });
    const rows = (res.data ?? []) as Record<string, unknown>[];
    const ds: Record<string, string> = {};
    for (const row of rows) {
      const enable = row.enable ?? row.Enable;
      if (enable === false || enable === 0 || enable === 'false') continue;
      const name = String(row.name ?? row.Name ?? '').trim();
      if (!name) continue;
      ds[name] = name;
    }
    field.dataSource = ds;
    field.itemType = field.itemType || 'singleSelect';
    ensureCurrentRoleOption(field, current);
  } catch {
    /* 无角色读权限时保持文本框 */
  }
}
