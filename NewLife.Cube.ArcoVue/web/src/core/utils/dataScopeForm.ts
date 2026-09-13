import { normalizeIamTypePath } from './rolePermission';
import { getValueByKey } from './url';

/** 自定义数据范围（XCode DataScopes.自定义 = 4） */
export const CUSTOM_DATA_SCOPE = 4;

/**
 * 是否「自定义」数据范围。兼容数值枚举、数字字符串与中英文枚举名。
 * 后端 Role.DataScope 为枚举，JSON 既可能下发数值也可能下发名称。
 */
export function isCustomDataScope(value: unknown): boolean {
  if (value == null) return false;
  if (typeof value === 'number') return value === CUSTOM_DATA_SCOPE;

  const text = String(value).trim();
  if (!text) return false;
  if (/^\d+$/.test(text)) return Number(text) === CUSTOM_DATA_SCOPE;

  return text === '自定义' || text.toLowerCase() === 'custom';
}

/** 当前模型是否为自定义数据范围（数据部门列表仅在此时有意义） */
export function shouldShowDataDepartmentIds(
  model: Record<string, unknown> | null | undefined,
): boolean {
  if (!model) return false;
  return isCustomDataScope(getValueByKey(model, 'dataScope') ?? getValueByKey(model, 'DataScope'));
}

/**
 * 表单字段是否可见（OSC-2608273d95）。
 * 仅对角色表单的 DataDepartmentIds 生效：数据范围为自定义才显示；其它字段、其它模块一律可见。
 * 判定只读，不依赖角色名。
 */
export function shouldShowDataScopeField(input: {
  typePath: unknown;
  model: Record<string, unknown> | null | undefined;
  fieldName: unknown;
}): boolean {
  if (String(input.fieldName ?? '').toLowerCase() !== 'datadepartmentids') return true;
  if (normalizeIamTypePath(input.typePath) !== 'admin/role') return true;
  return shouldShowDataDepartmentIds(input.model);
}
