import { describe, expect, it } from 'vitest';
import { parseInboxTarget } from './inboxTarget';

describe('parseInboxTarget', () => {
  it('parses Area/Controller#id', () => {
    expect(parseInboxTarget('Admin/User#12')).toEqual({ path: '/Admin/User', id: '12' });
  });

  it('rejects zero and multi-segment paths', () => {
    expect(parseInboxTarget('Admin/User#0')).toBeNull();
    expect(parseInboxTarget('Admin/User/Extra#1')).toBeNull();
  });

  it('rejects traversal, query, and http', () => {
    expect(parseInboxTarget('Admin/../User#1')).toBeNull();
    expect(parseInboxTarget('Admin/User?x#1')).toBeNull();
    expect(parseInboxTarget('http://x#1')).toBeNull();
  });
});
