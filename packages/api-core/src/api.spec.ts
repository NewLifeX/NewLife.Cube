import { describe, expect, it, vi } from 'vitest';
import { createCommentApi, createPageApi, createProfileApi } from './api';
import type { ApiResponse, EntityCommentModel, ViewProfileModel } from './types';

vi.mock('axios', () => ({
  isAxiosError: (error: unknown) => !!(error as { isAxiosError?: boolean })?.isAxiosError,
}));

describe('createCommentApi', () => {
  it('getList hits GET /Cube/EntityComment with category+linkId', async () => {
    const ok: ApiResponse<EntityCommentModel[]> = { code: 0, data: [{ id: 1, content: 'hi' }] };
    const request = vi.fn().mockResolvedValueOnce(ok);
    const api = createCommentApi(request);
    const result = await api.getList({ category: 'Admin/User', linkId: 7 });
    expect(result).toBe(ok);
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Cube/EntityComment',
        method: 'get',
        params: { category: 'Admin/User', linkId: 7 },
      }),
    );
  });

  it('post sends parentId for reply', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: {} });
    const api = createCommentApi(request);
    await api.post({ category: 'Admin/User', linkId: 7, content: '回复', parentId: 3 });
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Cube/EntityComment',
        method: 'post',
        data: { category: 'Admin/User', linkId: 7, content: '回复', parentId: 3 },
      }),
    );
  });

  it('remove hits DELETE /Cube/EntityComment?id=', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: undefined });
    const api = createCommentApi(request);
    await api.remove(9);
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Cube/EntityComment',
        method: 'delete',
        params: { id: 9 },
      }),
    );
  });
});

describe('createPageApi', () => {
  it('enableSelect hits GET /Admin/User/EnableSelect with keys', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: {} });
    const api = createPageApi(request);
    await api.enableSelect('/Admin/User', [7]);
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Admin/User/EnableSelect',
        method: 'get',
        params: { keys: '7' },
      }),
    );
  });

  it('disableSelect hits GET /Admin/User/DisableSelect with keys', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: {} });
    const api = createPageApi(request);
    await api.disableSelect('/Admin/User', [7, 8]);
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Admin/User/DisableSelect',
        method: 'get',
        params: { keys: '7,8' },
      }),
    );
  });
});

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
