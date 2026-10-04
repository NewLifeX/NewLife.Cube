import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const api = vi.hoisted(() => ({
  listTenants: vi.fn(),
  switchTenant: vi.fn(),
}));

vi.mock('@/api', () => ({
  default: { user: { listTenants: api.listTenants, switchTenant: api.switchTenant } },
}));

import { useTenantStore } from './tenant';

describe('tenant store loading', () => {
  beforeEach(() => {
    const values = new Map<string, string>();
    vi.stubGlobal('sessionStorage', {
      getItem: (key: string) => values.get(key) ?? null,
      setItem: (key: string, value: string) => values.set(key, value),
      removeItem: (key: string) => values.delete(key),
    });
    setActivePinia(createPinia());
    api.listTenants.mockReset();
    api.switchTenant.mockReset();
  });

  it('deduplicates concurrent loads, caches success, and reloads after switching', async () => {
    api.listTenants
      .mockResolvedValueOnce({ data: { enableTenant: true, currentId: 1, currentCode: 'north', items: [] } })
      .mockResolvedValueOnce({ data: { enableTenant: true, currentId: 2, currentCode: 'south', items: [] } })
      .mockResolvedValueOnce({ data: { enableTenant: true, currentId: 0, currentCode: '', items: [] } });
    api.switchTenant.mockResolvedValue({ data: true });
    const store = useTenantStore();

    await Promise.all([store.load(), store.load()]);
    await store.load();
    expect(api.listTenants).toHaveBeenCalledOnce();

    await store.switchTo(2);
    expect(api.listTenants).toHaveBeenCalledTimes(2);
    expect(store.currentCode).toBe('south');

    store.clear();
    await store.load();
    expect(api.listTenants).toHaveBeenCalledTimes(3);
  });

  it('force=true abandons in-flight and applies the newer response', async () => {
    let resolveFirst!: (value: unknown) => void;
    const first = new Promise((resolve) => {
      resolveFirst = resolve;
    });
    api.listTenants
      .mockImplementationOnce(() => first)
      .mockResolvedValueOnce({
        data: { enableTenant: true, currentId: 2, currentCode: 'south', items: [{ id: 2, code: 'south', name: '南' }] },
      });
    const store = useTenantStore();

    const p1 = store.load();
    const p2 = store.load(true);
    expect(api.listTenants).toHaveBeenCalledTimes(2);

    resolveFirst({
      data: { enableTenant: true, currentId: 1, currentCode: 'north', items: [{ id: 1, code: 'north', name: '北' }] },
    });
    await Promise.all([p1, p2]);
    expect(store.currentCode).toBe('south');
  });
});
