import { describe, expect, it } from 'vitest';
import { buildAccountLinks } from './accountLinks';

describe('buildAccountLinks', () => {
  it('disables every link except user stat when userId is missing', () => {
    const links = buildAccountLinks(undefined);
    const stat = links.find((l) => l.path === '/Admin/UserStat');
    expect(stat?.disabled).toBe(false);
    expect(stat?.query).toBeUndefined();
    for (const link of links) {
      if (link.path === '/Admin/UserStat') continue;
      expect(link.disabled).toBe(true);
    }
  });

  it('puts userId=3 on the token link', () => {
    const token = buildAccountLinks(3).find((l) => l.path === '/Admin/UserToken');
    expect(token?.disabled).toBe(false);
    expect(token?.query).toEqual({ userId: '3' });
  });

  it('rejects non positive integers', () => {
    expect(buildAccountLinks(0).find((l) => l.path === '/Admin/Log')?.disabled).toBe(true);
    expect(buildAccountLinks(1.5).find((l) => l.path === '/Admin/Log')?.disabled).toBe(true);
    expect(buildAccountLinks(Number.NaN).find((l) => l.path === '/Admin/Log')?.disabled).toBe(true);
  });
});
