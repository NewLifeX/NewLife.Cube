/**
 * Log.Remark 字段变更解析（OSC-260819e483 P4）。
 *
 * XCode `LogProvider.WriteLog(action, entity)` 在 action 为 Update/修改 时已写出
 * `Field=old -> new` 文法（主键 `ID=12`；脏字段 `Name=张三 -> 李四`；pass/password 两侧清空）。
 * 本模块解析该文法为字段表，供历史 Tab 渲染新旧值；失败（逗号在值内、截断、Insert 快照、
 * 自动化 JSON）返回 null，调用方回落 historyRemark 原文。
 * 禁止 JSON.parse 整段 Remark（自动化日志除外，由 historyRemark 处理）。
 */

/** 单条字段变更 */
export interface RemarkDiff {
  /** 字段名（原始大小写） */
  field: string;
  /** 显示名（字段元数据；无则回退字段名） */
  displayName: string;
  /** 旧值 */
  oldValue: string;
  /** 新值 */
  newValue: string;
}

function escapeRegExp(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

/**
 * 解析 Log.Remark 中 Update/修改/Edit 的字段变更。
 *
 * 规则（OSC-260819e483 P4）：
 * - 用字段名锚定 `Field=` 或 `,Field=`（忽略大小写）。`(^|,)` 前缀保证独立字段匹配，
 *   短名不会误吞长字段内部（如 `Name=` 不会匹配 `DisplayName=`）。
 * - 每个锚点段（到下一锚点或行尾）内拆 ` -> `；至少一条带箭头才返回数组。
 * - 值内逗号（old/new 含逗号，说明段被逗号截断错乱）或整体无法解析 → null，调用方回落 historyRemark。
 *
 * @param remark Log.Remark 原文
 * @param fields GetPage 字段元数据（name/displayName），作锚点
 * @returns 字段变更列表；无法解析为 null
 */
export function parseRemarkDiff(
  remark: string,
  fields: { name: string; displayName?: string }[],
): RemarkDiff[] | null {
  if (!remark) return null;

  const names = fields.filter((f) => f.name && f.name.length > 0);
  if (names.length === 0) return null;

  // 收集锚点（匹配起点 + 取值起点），长字段名优先处理保证无歧义（独立匹配，排序仅为稳定）
  const anchors: { matchStart: number; start: number; end: number; field: string; displayName: string }[] = [];
  for (const f of [...names].sort((a, b) => b.name.length - a.name.length)) {
    const rx = new RegExp(`(^|,)${escapeRegExp(f.name)}=`, 'gi');
    let m: RegExpExecArray | null;
    while ((m = rx.exec(remark)) !== null) {
      anchors.push({
        matchStart: m.index,
        start: m.index + m[0].length,
        end: remark.length,
        field: f.name,
        displayName: f.displayName || f.name,
      });
    }
  }
  if (anchors.length === 0) return null;
  anchors.sort((a, b) => a.start - b.start);
  // 每段结束 = 下一锚点匹配起点（去掉分隔逗号）
  for (let i = 0; i < anchors.length; i++) {
    anchors[i].end = i + 1 < anchors.length ? anchors[i + 1].matchStart : remark.length;
  }

  const diffs: RemarkDiff[] = [];
  for (const a of anchors) {
    const seg = remark.substring(a.start, a.end).replace(/,\s*$/, '');
    const arrow = seg.indexOf(' -> ');
    if (arrow < 0) continue; // 主键/Insert 快照/无变化
    const oldValue = seg.substring(0, arrow).trim();
    const newValue = seg.substring(arrow + 4).trim();
    // 值内逗号（段被截断错乱）→ 整体回退原文，避免展示错乱
    if (oldValue.includes(',') || newValue.includes(',')) return null;
    diffs.push({ field: a.field, displayName: a.displayName, oldValue, newValue });
  }

  return diffs.length > 0 ? diffs : null;
}
