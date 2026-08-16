import { describe, expect, it } from 'vitest';
import { resolveTenantHeader } from './tenantHeader';

describe('resolveTenantHeader', () => {
  it('adds header when code present', () => {
    expect(resolveTenantHeader('acme')).toEqual({ 'X-Tenant': 'acme' });
  });

  it('empty for platform / blank', () => {
    expect(resolveTenantHeader('')).toEqual({});
    expect(resolveTenantHeader(null)).toEqual({});
    expect(resolveTenantHeader(undefined)).toEqual({});
  });
});
