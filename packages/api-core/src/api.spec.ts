import { describe, expect, it, vi } from 'vitest';
import { createProfileApi } from './api';
import type { ApiResponse, ViewProfileModel } from './types';

vi.mock('axios', () => ({
  isAxiosError: (error: unknown) => !!(error as { isAxiosError?: boolean })?.isAxiosError,
}));

describe('createProfileApi', () => {
  it('falls back to POST when PUT /Cube/ViewProfile returns 405', async () => {
    const ok: ApiResponse<ViewProfileModel> = {
      code: 0,
      data: { id: 1, typePath: 'Admin/User', activeViewId: 'v-card' },
    };

    const request = vi
      .fn()
      .mockRejectedValueOnce({
        isAxiosError: true,
        response: { status: 405 },
      })
      .mockResolvedValueOnce(ok);

    const api = createProfileApi(request);
    const payload = { typePath: 'Admin/User', activeViewId: 'v-card' };
    const result = await api.putViewProfile(payload);

    expect(result).toBe(ok);
    expect(request).toHaveBeenCalledTimes(2);
    expect(request).toHaveBeenNthCalledWith(
      1,
      expect.objectContaining({
        url: '/Cube/ViewProfile',
        method: 'put',
        data: payload,
      }),
    );
    expect(request).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({
        url: '/Cube/ViewProfile',
        method: 'post',
        data: payload,
      }),
    );
  });
});
