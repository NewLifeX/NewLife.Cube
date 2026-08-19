import { describe, expect, it, vi } from 'vitest';
import { createCommentApi, createPageApi, createProfileApi, createAutomationApi } from './api';
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

describe('createAutomationApi', () => {
  it('list hits GET /Cube/Automation with typePath', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: [] });
    const api = createAutomationApi(request);
    await api.list({ typePath: 'Admin/User', enable: true, triggerKind: 'button' });
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Cube/Automation',
        method: 'get',
        params: { typePath: 'Admin/User', enable: true, triggerKind: 'button' },
      }),
    );
  });

  it('create posts filter+actions body', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: {} });
    const api = createAutomationApi(request);
    const body = {
      typePath: 'Admin/User',
      name: '新增通知',
      triggerKind: 'insert',
      filter: { logic: 'all', conditions: [] },
      actions: [{ type: 'notify', data: { channel: 'InApp' } }],
    };
    await api.create(body);
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Cube/Automation',
        method: 'post',
        data: body,
      }),
    );
  });

  it('update puts /Cube/Automation/Update and falls back to POST on 405', async () => {
    const ok = { code: 0, data: { id: 3 } };
    const request = vi.fn()
      .mockRejectedValueOnce({ isAxiosError: true, response: { status: 405 } })
      .mockResolvedValueOnce(ok);
    const api = createAutomationApi(request);
    const body = { id: 3, typePath: 'Admin/User', name: '改名', triggerKind: 'insert', version: 1 };
    const result = await api.update(body);
    expect(result).toBe(ok);
    expect(request).toHaveBeenNthCalledWith(1, expect.objectContaining({
      url: '/Cube/Automation/Update',
      method: 'put',
      data: body,
    }));
    expect(request).toHaveBeenNthCalledWith(2, expect.objectContaining({
      url: '/Cube/Automation/Update',
      method: 'post',
      data: body,
    }));
  });

  it('run posts POST /Cube/Automation/Run', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: { runId: 1 } });
    const api = createAutomationApi(request);
    await api.run({ automationId: 8, recordKey: '12' });
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Cube/Automation/Run',
        method: 'post',
        data: { automationId: 8, recordKey: '12' },
      }),
    );
  });

    it('runs hits GET /Cube/Automation/Runs', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: [] });
    const api = createAutomationApi(request);
    await api.runs({ typePath: 'Admin/User', recordKey: 7 });
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Cube/Automation/Runs',
        method: 'get',
        params: { typePath: 'Admin/User', recordKey: 7 },
      }),
    );
  });

  it('entities hits GET /Cube/Automation/Entities', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: [] });
    const api = createAutomationApi(request);
    await api.entities('insert');
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Cube/Automation/Entities',
        method: 'get',
        params: { permission: 'insert' },
      }),
    );
  });

  it('recipients hits GET /Cube/Automation/Recipients', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: [] });
    const api = createAutomationApi(request);
    await api.recipients({ kind: 'role', key: 'admin' });
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Cube/Automation/Recipients',
        method: 'get',
        params: { kind: 'role', key: 'admin' },
      }),
    );
  });

  it('inbox / unread / markRead hit Inbox routes', async () => {
    const request = vi.fn().mockResolvedValue({ code: 0, data: {} });
    const api = createAutomationApi(request);
    await api.inbox({ pageIndex: 1, unread: true });
    await api.inboxUnreadCount();
    await api.markInboxRead({ all: true });
    expect(request).toHaveBeenNthCalledWith(1, expect.objectContaining({
      url: '/Cube/Automation/Inbox',
      method: 'get',
      params: { pageIndex: 1, unread: true },
    }));
    expect(request).toHaveBeenNthCalledWith(2, expect.objectContaining({
      url: '/Cube/Automation/Inbox/UnreadCount',
      method: 'get',
    }));
    expect(request).toHaveBeenNthCalledWith(3, expect.objectContaining({
      url: '/Cube/Automation/Inbox/Read',
      method: 'post',
      data: { all: true },
    }));
  });
});

describe('createPageApi', () => {
  it('getObject hits GET {type} without pagination params (OSC-2608139feb)', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: { Debug: false } });
    const api = createPageApi(request);
    await api.getObject('/Admin/Cube');
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Admin/Cube',
        method: 'get',
      }),
    );
  });

  it('home dashboard APIs hit /Admin/Index/* (OSC-2608139feb)', async () => {
    const request = vi.fn().mockResolvedValue({ code: 0, data: {} });
    const api = createPageApi(request);
    await api.getIndexMain();
    expect(request).toHaveBeenCalledWith(expect.objectContaining({ url: '/Admin/Index/Main', method: 'get' }));
    await api.getServerVarList();
    expect(request).toHaveBeenCalledWith(expect.objectContaining({ url: '/Admin/Index/ServerVarList', method: 'get' }));
    await api.getProcessList('All');
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({ url: '/Admin/Index/ProcessList', method: 'get', params: { model: 'All' } }),
    );
    await api.getAssemblyList();
    expect(request).toHaveBeenCalledWith(expect.objectContaining({ url: '/Admin/Index/AssemblyList', method: 'get' }));
    await api.memoryFree();
    expect(request).toHaveBeenCalledWith(expect.objectContaining({ url: '/Admin/Index/MemoryFree', method: 'get' }));
    await api.restart();
    expect(request).toHaveBeenCalledWith(expect.objectContaining({ url: '/Admin/Index/Restart', method: 'post' }));
  });

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

  it('patchFields hits PATCH /Admin/User with {id,values}', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: { ok: 1, fail: 0, errors: [] } });
    const api = createPageApi(request);
    const result = await api.patchFields('/Admin/User', { id: '7', values: { Name: 'x' } });
    expect(result.data.ok).toBe(1);
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Admin/User',
        method: 'patch',
        data: { id: '7', values: { Name: 'x' } },
      }),
    );
  });

  it('batchUpdateFields hits POST /Admin/User/BatchUpdateFields with {keys,field,value}', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: { ok: 2, fail: 1, errors: [{ id: '9', message: '数据不存在' }] } });
    const api = createPageApi(request);
    const result = await api.batchUpdateFields('/Admin/User', { keys: '7,8,9', field: 'Name', value: 'x' });
    expect(result.data.ok).toBe(2);
    expect(result.data.errors[0].id).toBe('9');
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Admin/User/BatchUpdateFields',
        method: 'post',
        data: { keys: '7,8,9', field: 'Name', value: 'x' },
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

describe('createUserApi auth extensions', () => {
  it('listBinds hits GET /Auth/Binds', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: { providers: [] } });
    const { createUserApi } = await import('./api');
    const api = createUserApi(request);
    await api.listBinds();
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({ url: '/Auth/Binds', method: 'get' }),
    );
  });

  it('listTenants hits GET /Auth/Tenants', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: { currentId: 0, items: [] } });
    const { createUserApi } = await import('./api');
    const api = createUserApi(request);
    await api.listTenants();
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({ url: '/Auth/Tenants', method: 'get' }),
    );
  });

  it('switchTenant posts tenantId', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: { currentId: 2, items: [] } });
    const { createUserApi } = await import('./api');
    const api = createUserApi(request);
    await api.switchTenant(2);
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Auth/SwitchTenant',
        method: 'post',
        data: { tenantId: 2 },
      }),
    );
  });

  it('mfaActivate passes code as query param（后端 [ApiController] 简单类型推断 [FromQuery]，非 body）', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: { backupCodes: ['1234567890'] } });
    const { createUserApi } = await import('./api');
    const api = createUserApi(request);
    await api.mfaActivate('123456');
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Mfa/Activate',
        method: 'post',
        params: { code: '123456' },
      }),
    );
  });

  it('mfaDisable passes code as query param', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: undefined });
    const { createUserApi } = await import('./api');
    const api = createUserApi(request);
    await api.mfaDisable('654321');
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Mfa/Disable',
        method: 'post',
        params: { code: '654321' },
      }),
    );
  });

  it('mfaVerify posts body with mfaToken + code（复杂类型走 body）', async () => {
    const request = vi.fn().mockResolvedValueOnce({ code: 0, data: { accessToken: 'a', refreshToken: 'r' } });
    const { createUserApi } = await import('./api');
    const api = createUserApi(request);
    await api.mfaVerify({ mfaToken: 'tok', code: '123456' });
    expect(request).toHaveBeenCalledWith(
      expect.objectContaining({
        url: '/Mfa/Verify',
        method: 'post',
        data: { mfaToken: 'tok', code: '123456' },
      }),
    );
  });

  it('mfaSetup / mfaStatus hit GET endpoints', async () => {
    const request = vi.fn().mockResolvedValue({ code: 0, data: {} });
    const { createUserApi } = await import('./api');
    const api = createUserApi(request);
    await api.mfaSetup();
    expect(request).toHaveBeenNthCalledWith(1, expect.objectContaining({ url: '/Mfa/Setup', method: 'get' }));
    await api.mfaStatus();
    expect(request).toHaveBeenNthCalledWith(2, expect.objectContaining({ url: '/Mfa/Status', method: 'get' }));
  });
});
