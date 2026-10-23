import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const { getSite } = vi.hoisted(() => ({ getSite: vi.fn() }));

vi.mock('../utils/request', () => ({ site: { getSite } }));

import { useSiteStore } from '../stores/site';

describe('site store — 站点品牌共享缓存', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('首次加载后缓存，多个布局复用同一份配置', async () => {
    getSite.mockResolvedValue({ code: 0, data: { name: 'Acme', logo: '/logo.png' } });
    const store = useSiteStore();

    await store.load();
    await store.load();

    expect(getSite).toHaveBeenCalledTimes(1);
    expect(store.name).toBe('Acme');
    expect(store.logo).toBe('/logo.png');
  });

  it('强制刷新时重新请求', async () => {
    getSite
      .mockResolvedValueOnce({ code: 0, data: { name: 'Acme' } })
      .mockResolvedValueOnce({ code: 0, data: { name: 'Acme 2' } });
    const store = useSiteStore();

    await store.load();
    await store.load(undefined, true);

    expect(getSite).toHaveBeenCalledTimes(2);
    expect(store.name).toBe('Acme 2');
  });

  it('接口失败时保持空配置，不抛出异常', async () => {
    getSite.mockRejectedValue(new Error('network'));
    const store = useSiteStore();

    await expect(store.load()).resolves.toBeNull();
    expect(store.loading).toBe(false);
  });
});
