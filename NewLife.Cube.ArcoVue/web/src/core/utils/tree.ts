/** 检测列表是否为树（任一行含 children 数组） */
export function detectTreeData(rows: Record<string, unknown>[]): boolean {
  return rows.some((r) => Array.isArray(r.children));
}

/** 路径启发式：菜单/部门/地区类实体倾向树 */
export function preferTreeByType(typePath: string): boolean {
  const lower = typePath.toLowerCase();
  return (
    lower.endsWith('/menu') ||
    lower.endsWith('/department') ||
    lower.endsWith('/area') ||
    lower.includes('tree')
  );
}
