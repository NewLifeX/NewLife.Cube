/** 站内信 Target：仅 Area/Controller#正整数 */
const TARGET_RE = /^([A-Za-z][A-Za-z0-9]*)\/([A-Za-z][A-Za-z0-9]*)#([1-9][0-9]*)$/;

export type InboxTargetRoute = { path: string; id: string };

/**
 * 解析 Inbox `target`。拒绝含 `..`、空格、`?`、`http` 或主键为 0 的字符串。
 */
export function parseInboxTarget(raw: string | null | undefined): InboxTargetRoute | null {
  if (!raw) return null;
  const s = raw.trim();
  if (!s) return null;
  if (s.includes('..') || s.includes(' ') || s.includes('?') || /https?:/i.test(s)) {
    return null;
  }
  const m = TARGET_RE.exec(s);
  if (!m) return null;
  return { path: `/${m[1]}/${m[2]}`, id: m[3] };
}
