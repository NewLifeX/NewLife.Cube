/**
 * 页面种类探测纯函数（OSC-2608139feb）。
 *
 * DynamicPage 按 typePath 探测后端契约种类：
 * - home：Admin/Index 短路（主页仪表盘，不请求任何探测）
 * - custom：Admin/Db、Admin/File 短路（专用页，GetPage/对象双探必然失败）
 * - entity：GetPage 返回有效实体元数据 → DefaultList
 * - object：GetPage 失败且 GetFields 为数组、GET body 为对象且非分页形 → DefaultObject
 * - unknown：全部失败 → a-empty
 */

export type PageKind = 'home' | 'custom' | 'entity' | 'object' | 'unknown';

/** 探测注入点：便于单测替代真实网络请求 */
export interface PageKindProbes {
  /** GetPage 探测（失败抛错或返回非法形） */
  getPage(typePath: string): Promise<unknown>;
  /** Object 双探：GetFields 原始值与 GET type 原始值（均未解包） */
  getObjectProbe(typePath: string): Promise<{ fields: unknown; body: unknown }>;
}

const HOME_TYPES: ReadonlySet<string> = new Set(['admin/index']);
const CUSTOM_TYPES: ReadonlySet<string> = new Set(['admin/db', 'admin/file']);

function normalizePath(typePath: string): string {
  return typePath.replace(/^\/+/, '').replace(/\/+$/, '').toLowerCase();
}

/** 是否为纯对象（非数组、非 null） */
function isPlainObjectLike(v: unknown): v is Record<string, unknown> {
  return !!v && typeof v === 'object' && !Array.isArray(v);
}

/**
 * 有效实体元数据判定（design §2.2 真值表）：
 * 非 null 对象且下列至少一项成立：list/search/addForm 为数组，或 setting 为对象。
 * 字符串 / 数组根 / `code`+`message` 且无 data 结构 → 假。
 */
export function isValidEntityPageMeta(meta: unknown): boolean {
  if (!isPlainObjectLike(meta)) return false;
  return (
    Array.isArray(meta.list) ||
    Array.isArray(meta.search) ||
    Array.isArray(meta.addForm) ||
    isPlainObjectLike(meta.setting)
  );
}

/** 分页列表形：`{ data: any[], page: object }`（GET 实体控制器） */
function isPagedListShape(body: unknown): boolean {
  if (!isPlainObjectLike(body)) return false;
  const data = body.data ?? body;
  return Array.isArray(data) && isPlainObjectLike(body.page);
}

/** 从可能被 `{data}` 包裹的值中解包 */
function unwrap(v: unknown): unknown {
  if (isPlainObjectLike(v) && 'data' in v) return (v as Record<string, unknown>).data;
  return v;
}

/**
 * 探测页面种类（design §2.2 真值表 + 本号新增 custom 行）。
 * @param typePath 类型路径，如 Admin/User
 * @param probes 注入的网络探测
 */
export async function detectPageKind(typePath: string, probes: PageKindProbes): Promise<PageKind> {
  const p = normalizePath(typePath);
  if (HOME_TYPES.has(p)) return 'home';
  if (CUSTOM_TYPES.has(p)) return 'custom';

  let page: unknown = null;
  try {
    page = await probes.getPage(typePath);
  } catch {
    page = null;
  }
  if (isValidEntityPageMeta(unwrap(page))) return 'entity';

  try {
    const { fields, body } = await probes.getObjectProbe(typePath);
    const f = unwrap(fields);
    const b = unwrap(body);
    if (Array.isArray(f) && isPlainObjectLike(b) && !isPagedListShape(b)) return 'object';
  } catch {
    /* ignore */
  }

  return 'unknown';
}
