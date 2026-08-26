/** 顶栏站内通知未读徽标：超过该数显示 `${max}+` */
export const INBOX_BADGE_MAX = 10;

function toNonNegativeInt(value: unknown): number {
  const n = Math.floor(Number(value));
  if (!Number.isFinite(n) || n < 0) return 0;
  return n;
}

/**
 * 未读数为 0 时返回 0（徽标隐藏）；1..max 返回数字；超过 max 返回 `${max}+`。
 */
export function formatInboxBadgeCount(unread: unknown, max = INBOX_BADGE_MAX): number | string {
  const n = toNonNegativeInt(unread);
  if (n <= 0) return 0;
  if (n > max) return `${max}+`;
  return n;
}

/** UnreadCount 接口：`data.count`，兼容直接数字 / PascalCase */
export function parseInboxUnreadCount(data: unknown): number {
  if (data == null) return 0;
  if (typeof data === 'number' || typeof data === 'string') return toNonNegativeInt(data);
  if (typeof data !== 'object') return 0;
  const o = data as Record<string, unknown>;
  return toNonNegativeInt(o.count ?? o.Count);
}

/**
 * 抽屉「共 n 条」：优先用分页总数；totalCount 为 0/缺失时回退到本页条数
 * （后端未开 RetrieveTotalCount 时 TotalCount 恒为 0）。
 */
export function resolveInboxTotal(
  page: { totalCount?: number; TotalCount?: number } | undefined,
  listLength: number,
): number {
  const n = toNonNegativeInt(page?.totalCount ?? page?.TotalCount);
  if (n > 0) return n;
  return toNonNegativeInt(listLength);
}
