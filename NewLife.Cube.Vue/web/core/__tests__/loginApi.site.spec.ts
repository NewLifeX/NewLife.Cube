import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getSite: vi.fn(),
  getLoginConfig: vi.fn(),
}));

vi.mock('../utils/request', () => ({
  user: { getLoginConfig: mocks.getLoginConfig },
  site: { getSite: mocks.getSite },
}));

import { fetchLoginConfig, fetchSiteConfig } from '../utils/loginApi';

beforeEach(() => {
  mocks.getSite.mockReset();
  mocks.getLoginConfig.mockReset();
});

describe('fetchSiteConfig', () => {
  it('把租户标识透传给站点接口', async () => {
    mocks.getSite.mockResolvedValueOnce({
      code: 0,
      data: { name: 'Acme', tenantCode: 'acme' },
    });

    const res = await fetchSiteConfig('acme');

    expect(mocks.getSite).toHaveBeenCalledWith('acme');
    expect(res.code).toBe(0);
    expect(res.data?.name).toBe('Acme');
  });

  it('未指定租户时不带租户参数', async () => {
    mocks.getSite.mockResolvedValueOnce({ code: 0, data: { name: 'Cube' } });

    await fetchSiteConfig();

    expect(mocks.getSite).toHaveBeenCalledWith(undefined);
  });

  it('站点接口异常时向上抛出，交由调用方统一处理', async () => {
    mocks.getSite.mockRejectedValueOnce(new Error('network error'));

    await expect(fetchSiteConfig()).rejects.toThrow('network error');
  });
});

describe('fetchLoginConfig', () => {
  it('把租户标识透传给登录配置接口', async () => {
    mocks.getLoginConfig.mockResolvedValueOnce({
      code: 0,
      data: { enableTenant: true, code: 'acme' },
    });

    const res = await fetchLoginConfig('acme');

    expect(mocks.getLoginConfig).toHaveBeenCalledWith('acme');
    expect(res.data?.code).toBe('acme');
  });

  it('未指定租户时不带租户参数', async () => {
    mocks.getLoginConfig.mockResolvedValueOnce({ code: 0, data: { enableTenant: false } });

    await fetchLoginConfig();

    expect(mocks.getLoginConfig).toHaveBeenCalledWith(undefined);
  });
});
