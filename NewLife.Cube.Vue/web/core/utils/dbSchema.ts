/**
 * 数据库管理页数据结构与归一化
 *
 * 后端 `DbController` 返回 `ApiResponse` 包裹的数组（数据库列表 / 表结构 / 实体结构 / 模型差异），
 * 这里统一解包并兜底成稳定形状，页面只负责渲染。
 */

/** 数据库条目 */
export interface DbItemView {
  name: string;
  type: string;
  version: string;
  driver: string;
  driverVersion: string;
  entities: number;
  tables: number;
  backups: number;
}

/** 列结构 */
export interface DbColumnView {
  name: string;
  columnName: string;
  displayName: string;
  type: string;
  length: number;
  precision: number;
  scale: number;
  primaryKey: boolean;
  identity: boolean;
  nullable: boolean;
  unique: boolean;
  description: string;
}

/** 表/实体结构 */
export interface DbSchemaTableView {
  name: string;
  tableName: string;
  displayName: string;
  count: number;
  columns: DbColumnView[];
}

/** 模型差异条目 */
export interface DbDiffView {
  name: string;
  tableName: string;
  displayName: string;
  hasEntityModel: boolean;
  extraColumns: string[];
  xmlFragment: string;
}

/** 解包 ApiResponse/裸数组，统一成数组 */
function toArray(payload: unknown): Record<string, unknown>[] {
  if (Array.isArray(payload)) return payload as Record<string, unknown>[];
  if (payload && typeof payload === 'object') {
    const data = (payload as Record<string, unknown>).data;
    if (Array.isArray(data)) return data as Record<string, unknown>[];
  }
  return [];
}

const str = (v: unknown) => (v === null || v === undefined ? '' : String(v));
const num = (v: unknown) => {
  const n = Number(v);
  return Number.isFinite(n) ? n : 0;
};
const bool = (v: unknown) => v === true || v === 'true' || v === 1;

/**
 * 归一化数据库列表
 * @param payload 接口返回值
 * @returns 数据库条目集合
 */
export function normalizeDbList(payload: unknown): DbItemView[] {
  return toArray(payload).map((e) => ({
    name: str(e.name),
    type: str(e.type),
    version: str(e.version),
    driver: str(e.driver),
    driverVersion: str(e.driverVersion),
    entities: num(e.entities),
    tables: num(e.tables),
    backups: num(e.backups),
  }));
}

/**
 * 归一化表/实体结构列表
 * @param payload 接口返回值
 * @returns 结构列表
 */
export function normalizeSchemaList(payload: unknown): DbSchemaTableView[] {
  return toArray(payload).map((e) => {
    const columns = Array.isArray(e.columns) ? (e.columns as Record<string, unknown>[]) : [];
    return {
      name: str(e.name),
      tableName: str(e.tableName),
      displayName: str(e.displayName),
      count: num(e.count),
      columns: columns.map((c) => ({
        name: str(c.name),
        columnName: str(c.columnName),
        displayName: str(c.displayName),
        type: str(c.type),
        length: num(c.length),
        precision: num(c.precision),
        scale: num(c.scale),
        primaryKey: bool(c.primaryKey),
        identity: bool(c.identity),
        nullable: bool(c.nullable),
        unique: bool(c.unique),
        description: str(c.description),
      })),
    };
  });
}

/**
 * 归一化模型差异列表
 * @param payload 接口返回值
 * @returns 差异列表
 */
export function normalizeDiffList(payload: unknown): DbDiffView[] {
  return toArray(payload).map((e) => ({
    name: str(e.name),
    tableName: str(e.tableName),
    displayName: str(e.displayName),
    hasEntityModel: bool(e.hasEntityModel),
    extraColumns: Array.isArray(e.extraColumns) ? (e.extraColumns as unknown[]).map(str) : [],
    xmlFragment: str(e.xmlFragment),
  }));
}

/**
 * 列键标识：自增 > 主键 > 唯一索引，与 MVC 表结构页的 AI/PK/UQ 口径一致
 * @param col 列结构
 * @returns `AI` / `PK` / `UQ` / 空串
 */
export function columnFlag(col: Pick<DbColumnView, 'identity' | 'primaryKey' | 'unique'>): string {
  if (col.identity) return 'AI';
  if (col.primaryKey) return 'PK';
  if (col.unique) return 'UQ';
  return '';
}

/**
 * 精度展示：仅有精度或小数位时输出 `(precision, scale)`
 * @param col 列结构
 * @returns 文本或空串
 */
export function precisionText(col: Pick<DbColumnView, 'precision' | 'scale'>): string {
  return col.precision > 0 || col.scale > 0 ? `(${col.precision}, ${col.scale})` : '';
}
