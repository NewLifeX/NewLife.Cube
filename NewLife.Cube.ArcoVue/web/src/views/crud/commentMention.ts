/**
 * 评论提及纯函数（OSC-260926c2b8）。
 *
 * 提及只负责发站内信：选人结果写入正文里的 `@显示名`，ID 另经 `mentionUserIds` 提交（≤20）。
 * 后端已跳过自己/禁用/重复项；前端仍先去掉自己并截断，保证提交体稳定。
 * 不新增评论表列、不做富文本；刷新评论后不还原提及标签。
 */

/** 已选提及条目 */
export interface MentionEntry {
  id: number;
  name: string;
}

/** 候选用户（搜索 /Admin/User 的行） */
export interface MentionUser {
  id: number;
  name: string;
}

/** 单条评论最多提及人数（与后端一致） */
export const MENTION_MAX = 20;

/** 构建提交 ID 数组：去重、去掉自己、最多 20 */
export function buildMentionIds(entries: readonly MentionEntry[], selfId?: number | null): number[] {
  const out: number[] = [];
  for (const e of entries) {
    if (e?.id == null || e.id <= 0) continue;
    if (selfId != null && e.id === selfId) continue;
    if (out.includes(e.id)) continue;

    out.push(e.id);
    if (out.length >= MENTION_MAX) break;
  }
  return out;
}

/** 挑选用户加入列表：已选、自己或已满 20 时返回原列表副本（不修改入参） */
export function addMentionEntry(
  entries: readonly MentionEntry[],
  user: MentionUser,
  selfId?: number | null,
): MentionEntry[] {
  const list = [...entries];
  if (user?.id == null || user.id <= 0) return list;
  if (selfId != null && user.id === selfId) return list;
  if (list.some((e) => e.id === user.id)) return list;
  if (list.length >= MENTION_MAX) return list;

  list.push({ id: user.id, name: user.name });
  return list;
}

/** 移除提及：从列表去掉该 ID，并从正文删除第一次出现的 `@显示名`（连同紧随的一个空白） */
export function removeMentionEntry(
  entries: readonly MentionEntry[],
  id: number,
  content: string,
  name: string,
): { entries: MentionEntry[]; content: string } {
  const list = entries.filter((e) => e.id !== id);

  const token = `@${name}`;
  const idx = content.indexOf(token);
  if (idx < 0) return { entries: list, content };

  const rest = content.slice(idx + token.length).replace(/^\s/, '');
  return { entries: list, content: content.slice(0, idx) + rest };
}

/** 在光标处插入 `@显示名 `（pos 越界或缺失时追加到末尾）；返回新正文与新光标位置 */
export function insertMentionText(
  content: string,
  name: string,
  pos?: number | null,
): { content: string; cursor: number } {
  const at = pos == null || pos < 0 || pos > content.length ? content.length : pos;
  const token = `@${name} `;

  return { content: content.slice(0, at) + token + content.slice(at), cursor: at + token.length };
}
