import { describe, expect, it } from 'vitest';
import {
  formatInboxBadgeCount,
  INBOX_BADGE_MAX,
  parseInboxUnreadCount,
  resolveInboxTotal,
} from './inboxBadge';

describe('formatInboxBadgeCount', () => {
  it('hides badge when unread is 0 or invalid', () => {
    expect(formatInboxBadgeCount(0)).toBe(0);
    expect(formatInboxBadgeCount(-1)).toBe(0);
    expect(formatInboxBadgeCount(undefined)).toBe(0);
    expect(formatInboxBadgeCount('x')).toBe(0);
  });

  it('shows exact count up to the max', () => {
    expect(formatInboxBadgeCount(1)).toBe(1);
    expect(formatInboxBadgeCount(INBOX_BADGE_MAX)).toBe(10);
  });

  it('shows 10+ when unread exceeds 10', () => {
    expect(formatInboxBadgeCount(11)).toBe('10+');
    expect(formatInboxBadgeCount(99)).toBe('10+');
  });
});

describe('parseInboxUnreadCount', () => {
  it('reads count from data object', () => {
    expect(parseInboxUnreadCount({ count: 3 })).toBe(3);
    expect(parseInboxUnreadCount({ Count: 12 })).toBe(12);
    expect(parseInboxUnreadCount(7)).toBe(7);
  });

  it('returns 0 when missing', () => {
    expect(parseInboxUnreadCount(undefined)).toBe(0);
    expect(parseInboxUnreadCount({})).toBe(0);
    expect(parseInboxUnreadCount({ count: 0 })).toBe(0);
  });
});

describe('resolveInboxTotal', () => {
  it('uses page.totalCount when positive', () => {
    expect(resolveInboxTotal({ totalCount: 6 }, 3)).toBe(6);
  });

  it('falls back to list length when totalCount is 0', () => {
    expect(resolveInboxTotal({ totalCount: 0 }, 3)).toBe(3);
    expect(resolveInboxTotal(undefined, 3)).toBe(3);
  });
});
