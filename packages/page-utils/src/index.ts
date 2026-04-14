/**
 * @cube/page-utils — 魔方前端页面工具函数集
 *
 * 零框架依赖，提供 URL 变量替换、权限检查、菜单查找、导出格式常量等通用工具。
 * 所有皮肤包共享此模块，避免重复实现。
 */

// ======================== 类型 ========================

/** 菜单树节点（最小必要字段） */
export interface MenuNode {
  url?: string;
  permissions?: Record<string, string>;
  children?: MenuNode[];
}

/** 权限码常量（2 的幂） */
export const Auth = {
  /** 查看 */
  VIEW: 1,
  /** 新增 */
  ADD: 2,
  /** 编辑 */
  EDIT: 4,
  /** 删除 */
  DELETE: 8,
  /** 导出 */
  EXPORT: 16,
  /** 导入 */
  IMPORT: 32,
} as const;

export type AuthCode = (typeof Auth)[keyof typeof Auth];

/** 导出格式定义 */
export interface ExportFormat {
  /** 后端 format 参数值 */
  key: string;
  /** 显示标签 */
  label: string;
}

/** 预定义导出格式列表 */
export const EXPORT_FORMATS: ExportFormat[] = [
  { key: 'Excel', label: '导出 Excel' },
  { key: 'Csv', label: '导出 CSV' },
  { key: 'Json', label: '导出 JSON' },
  { key: 'Xml', label: '导出 XML' },
  { key: 'ExcelTemplate', label: '导出模板' },
];

// ======================== 工具函数 ========================

/**
 * URL 变量替换：将 `/path/{Id}` 替换为 `/path/123`
 *
 * @param url - 含变量占位符的 URL 模板
 * @param row - 数据行对象
 * @returns 替换后的 URL
 *
 * @example
 * resolveUrl('/Admin/User/Detail?id={Id}', { Id: 42 })
 * // => '/Admin/User/Detail?id=42'
 */
export function resolveUrl(url: string, row: Record<string, unknown>): string {
  return url.replace(/\{(\w+)\}/g, (_, key: string) => {
    const val = row[key] ?? row[toCamelCase(key)];
    return val !== undefined && val !== null ? encodeURIComponent(String(val)) : '';
  });
}

/**
 * 字符串转小驼峰
 *
 * @example toCamelCase("User_Name") → "userName"
 * @example toCamelCase("UserName") → "userName"
 */
export function toCamelCase(str: string): string {
  if (!str) return '';
  const s = str.replace(/[-_](\w)/g, (_, c: string) => (c ? c.toUpperCase() : ''));
  return s.charAt(0).toLowerCase() + s.slice(1);
}

/**
 * 检查当前权限映射中是否包含指定权限码
 *
 * @param perms - 菜单的 permissions 对象 `{ "2": "添加", "4": "编辑" }`
 * @param auth - 要检查的权限码（Auth.ADD / Auth.EDIT 等）
 * @returns 是否具备该权限
 */
export function checkAuth(perms: Record<string, string>, auth: AuthCode | number): boolean {
  return String(auth) in perms;
}

/**
 * 递归查找菜单树中 URL 匹配的节点
 *
 * @param menus - 菜单树根节点数组
 * @param path - 当前路径（如 `/Admin/User`）
 * @returns 匹配的菜单节点，或 undefined
 */
export function findMenu<T extends MenuNode>(menus: T[], path: string): T | undefined {
  for (const m of menus) {
    if (m.url && path.toLowerCase().endsWith(m.url.toLowerCase())) return m;
    if (m.children?.length) {
      const found = findMenu(m.children as T[], path);
      if (found) return found as T;
    }
  }
  return undefined;
}

/**
 * 获取指定路径的菜单权限映射
 *
 * @param menus - 菜单树
 * @param path - 当前路径
 * @returns 权限码到权限名的映射
 */
export function getMenuPermission<T extends MenuNode>(menus: T[], path: string): Record<string, string> {
  const item = findMenu(menus, path);
  return item?.permissions ?? {};
}

/**
 * 构造统一导出下载 URL
 *
 * @param type - 路径前缀，如 `/Admin/User`
 * @param format - 导出格式，如 `Excel`、`Csv`
 * @param baseUrl - API 基础路径（可选）
 * @returns 完整导出 URL
 */
export function buildExportUrl(type: string, format: string, baseUrl?: string): string {
  const base = baseUrl ?? '';
  return `${base}${type}/ExportFile?format=${encodeURIComponent(format)}`;
}

/**
 * 分解二进制权限值为各权限码数组
 *
 * @param value - 权限值（如 23 = 1+2+4+16）
 * @returns 各权限码数组 [1, 2, 4, 16]
 */
export function decomposePowerOfTwo(value: number): number[] {
  const result: number[] = [];
  let bit = 1;
  while (bit <= value) {
    if (value & bit) result.push(bit);
    bit <<= 1;
  }
  return result;
}
