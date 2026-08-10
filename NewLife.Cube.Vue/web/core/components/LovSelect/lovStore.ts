/**
 * LovSelect 内部内存态 store（非 pinia）。
 *
 * 设计目标（用户 2026-08-09 明确要求）：
 *  1. 在 lovSelect 内部新建「内存态」store，统一管理 LOV 所有请求；
 *  2. 按 key 缓存配置（meta）/ 列表数据（listData），无 key 再请求，避免重复网络往返；
 *  3. 去掉独立的「批量翻译 label」请求端点（原 fetchBatchLabel）；列（配置了 refLovCode / lovCode）
 *     的原始值翻译改为「按值集码取配置数据（options / listData）后本地映射 label」，与请求层解耦；
 *  4. 列表值集选择数据后，必须回显其 textField（labelField）对应的值——已选行统一登记到
 *     labelCache，回显时优先命中，缺失则回退请求该值集 listData 兜底，确保正常回显。
 *
 * 实现要点：
 *  - 模块级单例（与 pinia 无关），在 SSR / 多实例下共享同一份内存缓存，符合「内存态」语义。
 *  - 所有请求方法均为「幂等缓存」：相同 key 仅首次触发真实请求，后续读缓存；调用方可并发调用，
 *    由 inFlight 合并，避免重复请求。
 *  - 不直接依赖 Vue 响应式（缓存为普通 Map），组件侧用 computed/ref 包裹即可获得响应式。
 */

import {
  fetchLovMeta,
  fetchLovListData,
  fetchLovListDataDirect,
  shouldDirectRequest,
} from '@newlifex/cube-vue/core/utils/lov-api';
import type {
  LovMetaItem,
  LovEnumMeta,
  LovListMeta,
  LovListDataResponse,
} from '@newlifex/cube-vue/core/types/lov';

/** 列翻译所需的最小 meta 视图（只需 value/label 字段定义） */
interface LabelFieldMeta {
  valueField: string;
  labelField: string;
}

// ── 内存缓存（模块级单例）──
/** meta 缓存：lovCode → LovMetaItem（含 ENUM options / LIST 配置） */
const metaCache = new Map<string, LovMetaItem>();
/** meta 进行中的请求：lovCode → Promise（并发合并） */
const metaInflight = new Map<string, Promise<LovMetaItem>>();

/**
 * 已选行 / 列表行 的 label 缓存：键 `${lovCode}:${value}` → 显示文本。
 * 覆盖三类来源，按写入优先级（后者不覆盖前者）作为兜底链：
 *  1) 选择时登记（registerSelectedRow / registerRows）：最精确，命中即可回显已选 textField；
 *  2) 列表自身行数据（listData）：把"加载过的列表行"变成天然翻译表；
 *  3) 缓存未命中时按需请求 listData 兜底（resolveSelectedLabel 触发），避免显示原始 ID。
 */
const labelCache = new Map<string, string>();
/** 已登记的行数据：lovCode → Map(value → row)（供兜底请求与回显复用） */
const rowCache = new Map<string, Map<string, Record<string, unknown>>>();
/** 某一 lovCode 的 listData 是否已登记（避免无限兜底请求） */
const listLoaded = new Map<string, boolean>();

/** listData 进行中的请求：lovCode → Promise（并发合并） */
const listInflight = new Map<string, Promise<LovListDataResponse>>();

/** 强制刷新标记：set 后下一次 getMeta/getListData 绕过缓存重新请求 */
const staleKeys = new Set<string>();

// ── meta ──────────────────────────────────────────────────

/**
 * 按 lovCode 获取值集元数据（带缓存）。
 * 同一 lovCode 仅请求一次；并发调用合并到同一 Promise。
 */
export async function getMeta(lovCode: string): Promise<LovMetaItem & { inlineEnums?: Record<string, LovEnumOption[]> }> {
  const hit = metaCache.get(lovCode);
  if (hit && !staleKeys.has(lovCode)) return hit as LovMetaItem & { inlineEnums?: Record<string, LovEnumOption[]> };
  const inflight = metaInflight.get(lovCode);
  if (inflight) return inflight;
  const p = (async () => {
    try {
      const res = await fetchLovMeta(lovCode);
      const item = res.meta?.[0];
      if (!item) throw new Error(`LovSelect: 值集 ${lovCode} 无元数据`);
      // 枚举内联选项也并入 labelCache（ENUM 列翻译走这条，不再发 BatchLabel）
      if (item.type === 'ENUM') {
        for (const opt of (item as LovEnumMeta).options || []) {
          labelCache.set(`${lovCode}:${opt.value}`, opt.label);
        }
      }
      // 携带 inlineEnums（LIST 类型 meta 随附的轻量值→标签映射），供 loadMeta 正常路径消费（写入 translateCache 作关闭态回显兜底）
      const inlineEnums = res.inlineEnums || null;
      const result = { ...item, inlineEnums: inlineEnums ?? undefined } as LovMetaItem & { inlineEnums?: Record<string, LovEnumOption[]> };
      metaCache.set(lovCode, result);
      staleKeys.delete(lovCode);
      return result;
    } finally {
      metaInflight.delete(lovCode);
    }
  })();
  metaInflight.set(lovCode, p);
  return p;
}

/** 同步读已缓存的 meta（未加载返回 null，不触发请求） */
export function getCachedMeta(lovCode: string): LovMetaItem | null {
  return metaCache.get(lovCode) ?? null;
}

// ── listData ──────────────────────────────────────────────

/**
 * 按 lovCode 拉取列表数据（带缓存），用于「列翻译 / 已选回显兜底」。
 * 仅 LIST 类型需要；调用前建议先 getMeta 确认类型。
 */
export async function getListData(lovCode: string): Promise<LovListDataResponse> {
  const hit = listLoaded.get(lovCode);
  const cachedRows = rowCache.get(lovCode);
  if (hit && cachedRows && !staleKeys.has(lovCode)) {
    return { data: [...cachedRows.values()], total: cachedRows.size };
  }
  const inflight = listInflight.get(lovCode);
  if (inflight) return inflight;
  const p = (async () => {
    try {
      const meta = await getMeta(lovCode);
      if (meta.type !== 'LIST') return { data: [], total: 0 };
      const listMeta = meta as LovListMeta;
      const config = listMeta.listConfig;
      const direct = shouldDirectRequest(config);
      const result = direct
        ? await fetchLovListDataDirect(config!, { lovCode, params: {}, pageNum: 1, pageSize: 9999 })
        : await fetchLovListData({ lovCode, params: {}, pageNum: 1, pageSize: 9999 });
      registerRows(lovCode, result.data || []);
      staleKeys.delete(lovCode);
      return result;
    } finally {
      listInflight.delete(lovCode);
    }
  })();
  listInflight.set(lovCode, p);
  return p;
}

// ── 行登记（回显核心）─────────────────────────────────────

/**
 * 登记一批行数据（键 `${lovCode}:${value}`），同时写入 labelCache 与 rowCache。
 * 不覆盖已存在的精确翻译（选择登记的优先级最高），仅填空缺。
 */
export function registerRows(lovCode: string, rows: Array<Record<string, unknown>>, fieldMeta?: LabelFieldMeta) {
  const meta = getCachedMeta(lovCode);
  const valueField = fieldMeta?.valueField ?? (meta?.type === 'LIST' ? (meta as LovListMeta).valueField || 'id' : 'id');
  const labelField =
    fieldMeta?.labelField ?? (meta?.type === 'LIST' ? (meta as LovListMeta).labelField || 'name' : 'name');

  let map = rowCache.get(lovCode);
  if (!map) {
    map = new Map();
    rowCache.set(lovCode, map);
  }
  for (const row of rows) {
    const v = row[valueField];
    const l = row[labelField];
    if (v == null || l == null) continue;
    const key = `${lovCode}:${v}`;
    map.set(String(v), row);
    if (!labelCache.has(key)) labelCache.set(key, String(l));
  }
  listLoaded.set(lovCode, true);
}

/** 登记单行（选择时调用，最高优先级，覆盖已有翻译以确保回显精确） */
export function registerSelectedRow(lovCode: string, row: Record<string, unknown>, fieldMeta?: LabelFieldMeta) {
  const meta = getCachedMeta(lovCode);
  const valueField = fieldMeta?.valueField ?? (meta?.type === 'LIST' ? (meta as LovListMeta).valueField || 'id' : 'id');
  const labelField =
    fieldMeta?.labelField ?? (meta?.type === 'LIST' ? (meta as LovListMeta).labelField || 'name' : 'name');
  const v = row[valueField];
  const l = row[labelField];
  if (v == null || l == null) return;
  const key = `${lovCode}:${v}`;
  let map = rowCache.get(lovCode);
  if (!map) {
    map = new Map();
    rowCache.set(lovCode, map);
  }
  map.set(String(v), row);
  // 选择登记强制覆盖（用户刚选中的行，label 必然最准确）
  labelCache.set(key, String(l));
}

// ── 翻译解析 ──────────────────────────────────────────────

/**
 * 解析「配置了值集码」的列翻译：给定 lovCode 与原始值，返回显示文本数组（支持多值逗号/分号分隔）。
 * 解耦逻辑：
 *  - ENUM：直接读已缓存的 options（labelCache 已播种），无需请求；
 *  - LIST：先查 labelCache（含已选/已加载行），缺失则触发 getListData 兜底拉取整表映射。
 * 不阻塞主渲染：缺失时先返回原始值，异步补全后由调用方响应式刷新。
 */
export async function resolveColumnLabels(lovCode: string, raw: unknown): Promise<string> {
  const values = normalizeMulti(raw);
  if (values.length === 0) return '';
  const meta = getCachedMeta(lovCode);
  // ENUM 直接本地映射
  if (meta?.type === 'ENUM') {
    return values.map((v) => labelCache.get(`${lovCode}:${v}`) ?? v).join('、');
  }
  // LIST：先查缓存，缺失再整体拉列表兜底
  const missing = values.filter((v) => !labelCache.has(`${lovCode}:${v}`));
  if (missing.length > 0) {
    try {
      await getListData(lovCode);
    } catch {
      /* 兜底请求失败时保留原始值 */
    }
  }
  return values.map((v) => labelCache.get(`${lovCode}:${v}`) ?? v).join('、');
}

/** 同步取列翻译（不触发请求，仅读缓存；缺失返回原始值，供初次渲染） */
export function getColumnLabel(lovCode: string, value: string): string {
  return labelCache.get(`${lovCode}:${value}`) ?? value;
}

/**
 * 回显「已选值」的 label（LIST 外层只读框核心兜底）。
 * 优先级：labelCache（含选择登记 + 已加载行）→ 兜底请求 listData（仅一次）→ 原始值。
 */
export async function resolveSelectedLabel(lovCode: string, value: string | number | undefined): Promise<string> {
  if (value == null) return '';
  const key = `${lovCode}:${value}`;
  const cached = labelCache.get(key);
  if (cached) return cached;
  // 兜底：拉整表映射一次（listLoaded 标记避免重复请求）
  try {
    await getListData(lovCode);
  } catch {
    /* 失败保留原始值 */
  }
  return labelCache.get(key) ?? String(value);
}

/** 同步读已选回显 label（不触发请求，供 watch 立即回显；缺失异步补全） */
export function getSelectedLabel(lovCode: string, value: string | number | undefined): string {
  if (value == null) return '';
  return labelCache.get(`${lovCode}:${value}`) ?? String(value);
}

// ── 工具 ──────────────────────────────────────────────────

/** 把单值/多值（逗号、分号、JSON 数组字符串）归一为字符串数组 */
function normalizeMulti(raw: unknown): string[] {
  if (raw == null) return [];
  if (Array.isArray(raw)) return raw.map(String).filter(Boolean);
  const s = String(raw).trim();
  if (!s) return [];
  if (s.startsWith('[')) {
    try {
      const arr = JSON.parse(s);
      if (Array.isArray(arr)) return arr.map(String).filter(Boolean);
    } catch {
      /* ignore */
    }
  }
  return s
    .split(/[,;]/)
    .map((x) => x.trim())
    .filter(Boolean);
}

/** 强制刷新缓存（清除指定 lovCode 或全量；下次请求重新拉取） */
export function invalidateLov(lovCode?: string) {
  if (!lovCode) {
    metaCache.clear();
    labelCache.clear();
    rowCache.clear();
    listLoaded.clear();
    staleKeys.clear();
    return;
  }
  metaCache.delete(lovCode);
  rowCache.delete(lovCode);
  listLoaded.delete(lovCode);
  staleKeys.add(lovCode);
}
