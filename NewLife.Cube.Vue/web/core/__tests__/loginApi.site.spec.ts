import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getSite: vi.fn(),
  getLoginConfig: vi.fn(),
}));

vi.mock('../utils/request', () => ({
  user: { getLoginConfig: mocks.getLoginConfig },
  site: { getSite: mocks.getSite },
}));

import { fetchSiteConfig } from '../utils/loginApi';

describe('fetchSiteConfig', () => {
  beforeEach(() => {
    mocks.getSite.mockReset();
  });

  it('把租户标识透传给站点接口', async () => {
    mocks.getSite.mockResolvedValue({
      code: 0,
      data: { name: 'Acme', tenantCode: 'acme' },
    });

    const res = await fetchSiteConfig('acme');

    expect(mocks.getSite).toHaveBeenCalledWith('acme');
    expect(res.data?.name).toBe('Acme');
  });

  it('未指定租户时不传参数', async () => {
    mocks.getSite.mockResolvedValue({ code: 0, data: { name: 'Cube' } });

    await fetchSiteConfig();

    expect(mocks.getSite).toHaveBeenCalledWith(undefined);
  });
});
