import { Auth, checkAuth } from '@newlifex/page-utils';

export interface DbDiffColumn {
  name: string;
  columnName: string;
  dataType: string;
}

export interface DbDiffTable {
  name: string;
  tableName: string;
  displayName: string;
  hasEntityModel: boolean;
  columns: DbDiffColumn[];
}

export interface DbDiffRow extends DbDiffColumn {
  name: string;
  tableName: string;
  displayName: string;
  hasEntityModel: boolean;
}

/** 数据库页动作权限；未配置菜单权限时保持现有页面的开发友好策略。 */
export function getDbActionPermissions(perms: Record<string, string> | null | undefined) {
  const keys = Object.keys(perms ?? {}).length;
  return {
    canInspect: keys === 0 || checkAuth(perms ?? {}, Auth.VIEW),
    canCompact: keys === 0 || checkAuth(perms ?? {}, Auth.EDIT),
  };
}

/** 将按表分组的模型差异展开为抽屉表格行。 */
export function flattenDiff(tables: DbDiffTable[]): DbDiffRow[] {
  return tables.flatMap((table) =>
    table.columns.map((column) => ({
      ...column,
      name: table.name,
      tableName: table.tableName,
      displayName: table.displayName,
      hasEntityModel: table.hasEntityModel,
    })),
  );
}
