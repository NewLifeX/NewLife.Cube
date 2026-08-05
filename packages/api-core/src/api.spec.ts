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

  it('getChartData without params keeps original URL and no params', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: [] });
    const api = createPageApi(request);
    const result = await api.getChartData('/Admin/User');
    expect(result.data).toEqual([]);
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Admin/User/GetChartData',
        method: 'get',
        params: undefined,
      }),
    );
  });

  it('getChartData with search params passes them through', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: [] });
    const api = createPageApi(request);
    const params = { Name: 'abc', Status: ['1', '2'], Enable: false };
    await api.getChartData('/Admin/User', params);
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Admin/User/GetChartData',
        method: 'get',
        params,
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

  it('getViewProfileTemplate hits GET /Cube/ViewProfileTemplate with typePath', async () => {
    const ok: ApiResponse<ViewProfileModel> = {
      code: 0,
      data: { typePath: 'Admin/User', viewsJson: '[]' },
    };
    const request = vi.fn().mockResolvedValueOnce(ok);
    const api = createProfileApi(request);
    const result = await api.getViewProfileTemplate('Admin/User');
    expect(result).toBe(ok);
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Cube/ViewProfileTemplate',
        method: 'get',
        params: { typePath: 'Admin/User' },
      }),
    );
  });

  it('putViewProfileTemplate falls back to POST when PUT returns 405', async () => {
    const ok: ApiResponse<ViewProfileModel> = {
      code: 0,
      data: { typePath: 'Admin/User', viewsJson: '[{"id":"default","name":"默认","view":"table"}]' },
    };
    const request = vi
      .fn()
      .mockRejectedValueOnce({ isAxiosError: true, response: { status: 405 } })
      .mockResolvedValueOnce(ok);
    const api = createProfileApi(request);
    const payload = { typePath: 'Admin/User', viewsJson: '[{"id":"default","name":"默认","view":"table"}]' };
    const result = await api.putViewProfileTemplate(payload);
    expect(result).toBe(ok);
    expect(request).toHaveBeenCalledTimes(2);
    expect(request).toHaveBeenNthCalledWith(
      1,
      expect.objectContaining({ url: '/Cube/ViewProfileTemplate', method: 'put', data: payload }),
    );
    expect(request).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({ url: '/Cube/ViewProfileTemplate', method: 'post', data: payload }),
    );
  });

  it('deleteViewProfileTemplate hits DELETE /Cube/ViewProfileTemplate with typePath', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: undefined });
    const api = createProfileApi(request);
    await api.deleteViewProfileTemplate('Admin/User');
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Cube/ViewProfileTemplate',
        method: 'delete',
        params: { typePath: 'Admin/User' },
      }),
    );
  });
});
