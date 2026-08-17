import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { clearTenantSession, resolveTenantHeader } from './tenantHeader';

const store = new Map<string, string>();

describe('resolveTenantHeader', () => {
  beforeEach(() => {
    store.clear();
    vi.stubGlobal('sessionStorage', {
      getItem: (k: string) => (store.has(k) ? store.get(k)! : null),
      setItem: (k: string, v: string) => {
        store.set(k, String(v));
      },
      removeItem: (k: string) => {
        store.delete(k);
      },
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('adds header when code present', () => {
    expect(resolveTenantHeader('acme')).toEqual({ 'X-Tenant': 'acme' });
  });

  it('empty for platform / blank', () => {
    expect(resolveTenantHeader('')).toEqual({});
    expect(resolveTenantHeader(null)).toEqual({});
    expect(resolveTenantHeader(undefined)).toEqual({});
  });

  it('skips header when multi-tenant disabled', () => {
    sessionStorage.setItem('cube.tenant.enabled', '0');
    expect(resolveTenantHeader('acme')).toEqual({});
  });

  it('clearTenantSession removes code so header stops', () => {
    sessionStorage.setItem('cube.tenant.code', 'acme');
    sessionStorage.setItem('cube.tenant.enabled', '1');
    expect(resolveTenantHeader(sessionStorage.getItem('cube.tenant.code'))).toEqual({
      'X-Tenant': 'acme',
    });
    clearTenantSession();
    expect(sessionStorage.getItem('cube.tenant.code')).toBeNull();
    expect(sessionStorage.getItem('cube.tenant.enabled')).toBeNull();
    expect(resolveTenantHeader(sessionStorage.getItem('cube.tenant.code'))).toEqual({});
  });
});
