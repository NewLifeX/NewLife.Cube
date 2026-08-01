import { describe, expect, it } from 'vitest';
import {
  createDevProxy,
  DEV_PROXY_AREA_PATTERN,
  DEV_PROXY_PREFIXES,
  DEV_PROXY_TARGET,
  shouldBypassToSpa,
} from './devProxy';

describe('createDevProxy', () => {
  it('includes /Auth and /Mfa for login and MFA pathways', () => {
    expect(DEV_PROXY_PREFIXES).toContain('/Auth');
    expect(DEV_PROXY_PREFIXES).toContain('/Mfa');
  });

  it('maps every fixed prefix to the Cube backend target', () => {
    const proxy = createDevProxy();
    for (const prefix of DEV_PROXY_PREFIXES) {
      expect(proxy[prefix].target).toBe(DEV_PROXY_TARGET);
      expect(proxy[prefix].changeOrigin).toBe(true);
      expect(typeof proxy[prefix].bypass).toBe('function');
    }
  });

  it('proxies PascalCase business areas like /School/', () => {
    const proxy = createDevProxy();
    expect(proxy[DEV_PROXY_AREA_PATTERN]).toBeTruthy();
    expect(proxy[DEV_PROXY_AREA_PATTERN].target).toBe(DEV_PROXY_TARGET);
    expect(new RegExp(DEV_PROXY_AREA_PATTERN).test('/School/Class/GetPage')).toBe(true);
    expect(new RegExp(DEV_PROXY_AREA_PATTERN).test('/admin/user')).toBe(false);
  });
});

describe('shouldBypassToSpa', () => {
  it('bypasses browser HTML navigation to SPA', () => {
    expect(
      shouldBypassToSpa({ headers: { accept: 'text/html,application/xhtml+xml' } }),
    ).toBe('/index.html');
  });

  it('does not bypass typical XHR/fetch Accept', () => {
    expect(
      shouldBypassToSpa({
        headers: { accept: 'application/json, text/plain, */*' },
      }),
    ).toBeUndefined();
  });
});
