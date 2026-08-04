/**
 * 日期时间工具：壁钟时间（wall-clock）解析，避免时区漂移。
 *
 * NewLife.Cube 后端 JSON 序列化 DateTime 为字符串（如 "2026-08-02T03:04:05"），
 * 可能不带时区标识；若用 `new Date(v)` 解析带 'Z' 的 UTC 串再 `getHours()`，
 * 显示会被换算成本地时区，造成列表/详情显示偏移。这里统一按“壁钟时间”解析：
 * 取字符串里的年月日时分秒原样展示，不做时区换算。
 */

export interface WallClock {
  y: number;
  m: number; // 1-12
  d: number; // 1-31
  h: number; // 0-23
  mi: number; // 0-59
  s: number; // 0-59
}

const PAD = (n: number) => String(n).padStart(2, '0');

/**
 * 把任意日期时间值解析为壁钟时间分量。
 * - 字符串：按 ISO / 常见格式正则抽取年月日时分秒，忽略时区标识（Z / +08:00）。
 * - Date：取其本地分量（后端返回的无时区串已被 new Date 当作本地解析，分量即壁钟）。
 * - 数字：视为时间戳毫秒。
 * 无效输入返回 null。
 */
export function parseWallClock(v: unknown): WallClock | null {
  if (v == null || v === '') return null;

  if (v instanceof Date) {
    if (Number.isNaN(v.getTime())) return null;
    return {
      y: v.getFullYear(),
      m: v.getMonth() + 1,
      d: v.getDate(),
      h: v.getHours(),
      mi: v.getMinutes(),
      s: v.getSeconds(),
    };
  }

  if (typeof v === 'number') {
    if (!Number.isFinite(v)) return null;
    const d = new Date(v);
    return {
      y: d.getFullYear(),
      m: d.getMonth() + 1,
      d: d.getDate(),
      h: d.getHours(),
      mi: d.getMinutes(),
      s: d.getSeconds(),
    };
  }

  if (typeof v !== 'string') return null;
  const s = v.trim();
  if (!s) return null;

  // 仅取第一个时间片段，避免 “2026-08-02 03:04:05 / 备注含斜杠” 被切坏
  const head = s.split(/[\/\\]/)[0] ?? s;
  // YYYY-MM-DD 或 YYYY/MM/DD，可选 THH:mm:ss 或 空格HH:mm:ss，可带 .fff 与时区
  const m = head.match(
    /^(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})(?:[ T](\d{1,2}):(\d{1,2})(?::(\d{1,2}))?)?/,
  );
  if (!m) return null;

  const y = Number(m[1]);
  const mo = Number(m[2]);
  const d = Number(m[3]);
  if (mo < 1 || mo > 12 || d < 1 || d > 31) return null;

  return {
    y,
    m: mo,
    d,
    h: m[4] ? Number(m[4]) : 0,
    mi: m[5] ? Number(m[5]) : 0,
    s: m[6] ? Number(m[6]) : 0,
  };
}

/** YYYY-MM-DD；无效/空输入返回 '-' */
export function formatDate(v: unknown): string {
  const w = parseWallClock(v);
  if (!w) return '-';
  return `${w.y}-${PAD(w.m)}-${PAD(w.d)}`;
}

/** HH:mm:ss；无效/空输入返回 '-' */
export function formatTime(v: unknown): string {
  const w = parseWallClock(v);
  if (!w) return '-';
  return `${PAD(w.h)}:${PAD(w.mi)}:${PAD(w.s)}`;
}

/** YYYY-MM-DD HH:mm:ss；无效/空输入返回 '-' */
export function formatDateTime(v: unknown): string {
  const w = parseWallClock(v);
  if (!w) return '-';
  return `${w.y}-${PAD(w.m)}-${PAD(w.d)} ${PAD(w.h)}:${PAD(w.mi)}:${PAD(w.s)}`;
}

export type DateKind = 'date' | 'datetime' | 'time';

/**
 * 推断字段的日期种类：
 * - itemType 显式声明优先（date/datetime/time）。
 * - TimeSpan → time。
 * - DateTime → datetime（默认含时分秒）。
 * - 其它（含无元数据）→ datetime 作为安全默认。
 */
export function inferDateKind(field: {
  typeName?: string;
  itemType?: string;
}): DateKind {
  const it = (field.itemType ?? '').trim().toLowerCase();
  if (it === 'date') return 'date';
  if (it === 'time') return 'time';
  if (it === 'datetime') return 'datetime';
  const tn = (field.typeName ?? '').trim();
  if (tn === 'TimeSpan') return 'time';
  return 'datetime';
}

/**
 * 把后端返回的日期时间值归一为 Arco picker 的字符串值。
 * 去掉时区标识，返回 naive 本地字符串，避免 picker 内部 dayjs 按时区换算。
 * - date → YYYY-MM-DD
 * - time → HH:mm:ss
 * - datetime → YYYY-MM-DD HH:mm:ss
 */
export function toPickerValue(v: unknown, kind: DateKind): string | undefined {
  const w = parseWallClock(v);
  if (!w) return undefined;
  if (kind === 'date') return `${w.y}-${PAD(w.m)}-${PAD(w.d)}`;
  if (kind === 'time') return `${PAD(w.h)}:${PAD(w.mi)}:${PAD(w.s)}`;
  return `${w.y}-${PAD(w.m)}-${PAD(w.d)} ${PAD(w.h)}:${PAD(w.mi)}:${PAD(w.s)}`;
}

/** 把 picker 输出的字符串转为提交给后端的字符串（naive，无时区标识）。 */
export function fromPickerValue(v: unknown, kind: DateKind): string | undefined {
  if (v == null || v === '') return undefined;
  const s = String(v);
  if (kind === 'date') return s.length >= 10 ? s.slice(0, 10) : s;
  if (kind === 'time') return s.length >= 8 ? s.slice(0, 8) : s;
  // datetime：Arco 可能返回 'YYYY-MM-DD HH:mm:ss' 或 'YYYY-MM-DDTHH:mm:ss'
  return s.replace('T', ' ').slice(0, 19);
}
