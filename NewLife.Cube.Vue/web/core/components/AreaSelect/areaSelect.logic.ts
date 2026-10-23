/**
 * 地区级联纯函数（AreaSelect 组件使用）
 *
 * 后端接口：
 * - `GET /Cube/AreaChilds?id=` 返回直接子级；
 * - `GET /Cube/AreaParents?id=&isContainSelf=true` 返回从根到当前节点的路径。
 * 两处都可能是 ApiResponse 信封或裸数组，这里统一归一。
 */

/** 后端地区节点（归一后） */
export interface AreaNode {
  /** 地区编号 */
  id: number;
  /** 地区名称 */
  name: string;
  /** 层级。省=1、市=2、区=3、街道=4 */
  level: number;
}

/** el-cascader 选项。索引签名用于满足 CascaderOption 约束 */
export interface AreaOption {
  [key: string]: unknown;
  /** 地区编号 */
  value: number;
  /** 地区名称 */
  label: string;
  /** 层级 */
  level: number;
  /** 是否叶子节点（达到目标层级） */
  leaf: boolean;
}

/** 从 ApiResponse / 裸数组归一地区节点，过滤无效项 */
export function normalizeAreaResponse(response: unknown): AreaNode[] {
  const body = (response as { data?: unknown } | null)?.data ?? response;
  if (!Array.isArray(body)) return [];

  const nodes: AreaNode[] = [];
  for (const item of body) {
    if (!item || typeof item !== 'object') continue;
    const record = item as Record<string, unknown>;
    const id = Number(record.id ?? record.ID ?? 0);
    const name = String(record.name ?? record.Name ?? '').trim();
    if (id <= 0 || !name) continue;
    nodes.push({ id, name, level: Number(record.level ?? record.Level ?? 0) });
  }
  return nodes;
}

/** 地区节点 → 级联选项；达到目标层级即叶子 */
export function toAreaOptions(nodes: AreaNode[], targetLevel: number): AreaOption[] {
  return nodes.map((node) => ({
    value: node.id,
    label: node.name,
    level: node.level,
    leaf: node.level >= targetLevel,
  }));
}

/** 父级链 → 级联路径（按层级升序） */
export function areaPathIds(nodes: AreaNode[]): number[] {
  return nodes
    .filter((node) => node.id > 0)
    .sort((a, b) => a.level - b.level)
    .map((node) => node.id);
}
