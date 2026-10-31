/**
 * 地区级联的框架无关控制器（各皮肤共用同一份状态机）
 *
 * 后端接口口径（与 MVC `_Area3` / Vue `AreaSelect` 一致）：
 * - `GET /Cube/AreaChilds?id={parentId}` 懒加载子级，`id=0` 取省；
 * - `GET /Cube/AreaParents?id={id}&isContainSelf=true` 回显从省到自身的路径。
 *
 * 皮肤只需：把 `state.options` 渲染成 `targetLevel` 个下拉，选中后调用 `selectLevel`，
 * 再按下拉最后一层的值提交（`state.value`），即与 MVC 的 city-picker 行为一致。
 */

/** 地区下拉项 */
export interface AreaOption {
  /** 地区编号 */
  id: number;
  /** 地区名称 */
  name: string;
}

/** 地区级联快照（皮肤把它塞进自己的响应式状态） */
export interface AreaCascadeState {
  /** 每一级的可选项，长度等于目标层级 */
  options: AreaOption[][];
  /** 已选路径（省 → 市 → 区 → 街道），长度不超过目标层级 */
  path: number[];
  /** 当前提交值：路径最后一层；未选任何层级时为 null */
  value: number | null;
}

/** 地区子级接口路径 */
export const AREA_CHILDREN_PATH = '/Cube/AreaChilds';

/** 地区父级路径接口路径 */
export const AREA_PARENTS_PATH = '/Cube/AreaParents';

/** 归一化 `/Cube/AreaChilds` 响应为下拉项（兼容 {data:[...]}、{items:[...]} 与裸数组） */
export function normalizeAreaOptions(raw: unknown): AreaOption[] {
  const list = extractList(raw);
  const options: AreaOption[] = [];
  for (const item of list) {
    if (!item || typeof item !== 'object') continue;
    const row = item as Record<string, unknown>;
    const id = Number(row.id ?? row.ID ?? row.value);
    if (!Number.isFinite(id) || id <= 0) continue;
    const name = String(row.name ?? row.displayName ?? row.label ?? id);
    options.push({ id, name });
  }
  return options;
}

/** 归一化 `/Cube/AreaParents` 响应为 id 路径（从省到自身；缺失层级时按响应顺序） */
export function normalizeAreaPath(raw: unknown): number[] {
  const list = extractList(raw);
  const path: number[] = [];
  for (const item of list) {
    if (!item || typeof item !== 'object') continue;
    const row = item as Record<string, unknown>;
    const id = Number(row.id ?? row.ID ?? row.value);
    if (!Number.isFinite(id) || id <= 0) continue;
    path.push(id);
  }
  return path;
}

/** 从常见响应包装里取出数组 */
function extractList(raw: unknown): unknown[] {
  if (Array.isArray(raw)) return raw;
  if (!raw || typeof raw !== 'object') return [];
  const wrapper = raw as Record<string, unknown>;
  for (const key of ['data', 'items', 'list', 'rows', 'result']) {
    const value = wrapper[key];
    if (Array.isArray(value)) return value;
  }
  return [];
}

/** 级联依赖：由皮肤提供自身的 HTTP 客户端 */
export interface AreaCascadeDeps {
  /** 加载某父级的子级地区；parentId=0 取省 */
  loadChildren: (parentId: number) => Promise<AreaOption[]>;
  /** 按地区编号回显完整路径 */
  loadPath: (id: number) => Promise<number[]>;
}

/**
 * 地区级联控制器。
 *
 * 构造后先 `await init(初始值)`，之后每次下拉变更 `await selectLevel(级, 编号)`，
 * 用返回的快照刷新界面；提交时取 `snapshot.value`。
 *
 * @example
 * const cascade = new AreaCascade({ loadChildren, loadPath }, 3);
 * await cascade.init(110101);
 * await cascade.selectLevel(1, 110100);
 * cascade.snapshot.value; // 提交值
 */
export class AreaCascade {
  private readonly _deps: AreaCascadeDeps;
  private readonly _level: number;
  private _options: AreaOption[][] = [];
  private _path: number[] = [];

  /**
   * @param deps 加载依赖
   * @param targetLevel 目标层级：1 省 / 2 市 / 3 区 / 4 街道（ItemType=area 默认 3）
   */
  constructor(deps: AreaCascadeDeps, targetLevel = 3) {
    this._deps = deps;
    this._level = Math.min(4, Math.max(1, Math.trunc(targetLevel) || 3));
    this._options = Array.from({ length: this._level }, () => []);
  }

  /** 当前快照（浅拷贝，皮肤可直接 diff） */
  get snapshot(): AreaCascadeState {
    return {
      options: this._options.map((x) => x.slice()),
      path: this._path.slice(),
      value: this._path.length ? this._path[this._path.length - 1] : null,
    };
  }

  /**
   * 按已有值回显：加载每一级选项并定位到该值。
   *
   * @param id 当前地区编号；0/null 表示未选
   */
  async init(id?: number | null): Promise<AreaCascadeState> {
    this._path = [];
    this._options = Array.from({ length: this._level }, () => []);

    const value = Number(id ?? 0);
    if (Number.isFinite(value) && value > 0) {
      const path = await this._deps.loadPath(value);
      this._path = path.slice(0, this._level);
    }

    await this._reload(0);
    return this.snapshot;
  }

  /**
   * 选择某一级：清空其下级并重新加载。
   *
   * @param level 层级（0 起）
   * @param id 地区编号；null 表示清空该级及以下
   */
  async selectLevel(level: number, id: number | null): Promise<AreaCascadeState> {
    if (level < 0 || level >= this._level) return this.snapshot;

    const next = Number(id ?? 0);
    this._path = this._path.slice(0, level);
    if (Number.isFinite(next) && next > 0) this._path.push(next);

    await this._reload(level);
    return this.snapshot;
  }

  /** 重新加载 from 层及以下的可选项（上级未选则下级为空） */
  private async _reload(from: number): Promise<void> {
    for (let i = from; i < this._level; i++) {
      const parent = i === 0 ? 0 : this._path[i - 1] ?? 0;
      if (i > 0 && !parent) {
        this._options[i] = [];
        continue;
      }
      this._options[i] = await this._deps.loadChildren(parent);
    }
  }
}
