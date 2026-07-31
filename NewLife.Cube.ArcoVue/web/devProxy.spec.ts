import { describe, expect, it } from 'vitest';
import { createDevProxy, DEV_PROXY_PREFIXES, DEV_PROXY_TARGET } from './devProxy';

describe('createDevProxy', () => {
  it('includes /Auth and /Mfa for login and MFA pathways', () => {
    expect(DEV_PROXY_PREFIXES).toContain('/Auth');
    expect(DEV_PROXY_PREFIXES).toContain('/Mfa');
  });

  it('maps every prefix to the Cube backend target', () => {
    const proxy = createDevProxy();
    for (const prefix of DEV_PROXY_PREFIXES) {
      expect(proxy[prefix]).toEqual({
        target: DEV_PROXY_TARGET,
        changeOrigin: true,
      });
    }
  });
});
