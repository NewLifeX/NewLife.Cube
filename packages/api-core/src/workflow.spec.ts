import { describe, expect, it } from 'vitest';
import { createWorkflowApi } from './api';

/** 捕获请求配置的假 RequestFn */
function captureApi() {
  const calls: Array<{ url?: string; method?: string; params?: unknown; data?: unknown }> = [];
  const request = (async (cfg: any) => {
    calls.push({ url: cfg.url, method: cfg.method, params: cfg.params, data: cfg.data });
    return { code: 0 };
  }) as any;
  return { api: createWorkflowApi(request), calls };
}

describe('createWorkflowApi /Cube/Workflow URL（OSC-26090347f1）', () => {
  it('meta / definitions / publish', async () => {
    const { api, calls } = captureApi();
    await api.meta();
    await api.definitions({ typePath: 'Admin/User' });
    await api.createDefinition({ typePath: 'Admin/User', name: 'x' });
    await api.updateDefinition(3, { name: 'y' });
    await api.publishDefinition(3);

    expect(calls.map((c) => `${c.method} ${c.url}`)).toEqual([
      'get /Cube/Workflow/Meta',
      'get /Cube/Workflow/Definitions',
      'post /Cube/Workflow/Definitions',
      'put /Cube/Workflow/Definitions/3',
      'post /Cube/Workflow/Definitions/3/Publish',
    ]);
  });

  it('instances and tasks paths', async () => {
    const { api, calls } = captureApi();
    await api.start({ typePath: 'Admin/User', keys: ['1', '2'], definitionId: 3, comment: '批' });
    await api.instance(9);
    await api.withdraw(9);
    await api.cancel(9);
    await api.jump(9, { targetNodeId: 'n2' });
    await api.approve(7, { comment: 'ok' });
    await api.reject(7, { comment: 'no' });
    await api.addSign(7, { before: true, to: { kind: 'users', users: [5] } });
    await api.transfer(7, { to: { kind: 'users', users: [6] } });
    await api.cc(7, { to: { kind: 'users', users: [8] } });
    await api.rollback(7, { targetNodeId: 'n1' });

    expect(calls.map((c) => `${c.method} ${c.url}`)).toEqual([
      'post /Cube/Workflow/Instances',
      'get /Cube/Workflow/Instances/9',
      'post /Cube/Workflow/Instances/9/Withdraw',
      'post /Cube/Workflow/Instances/9/Cancel',
      'post /Cube/Workflow/Instances/9/Jump',
      'post /Cube/Workflow/Tasks/7/Approve',
      'post /Cube/Workflow/Tasks/7/Reject',
      'post /Cube/Workflow/Tasks/7/AddSign',
      'post /Cube/Workflow/Tasks/7/Transfer',
      'post /Cube/Workflow/Tasks/7/Cc',
      'post /Cube/Workflow/Tasks/7/Rollback',
    ]);
  });

  it('batch / todo / phrases / patch', async () => {
    const { api, calls } = captureApi();
    await api.batchApprove({ ids: [1, 2], comment: 'ok' });
    await api.todo({ pageSize: 20 });
    await api.started();
    await api.done();
    await api.phrases();
    await api.savePhrases(['同意']);
    await api.patchEntity('Admin/User', '42', { Remark: 'x' });

    expect(calls.map((c) => `${c.method} ${c.url}`)).toEqual([
      'post /Cube/Workflow/Tasks/BatchApprove',
      'get /Cube/Workflow/Todo',
      'get /Cube/Workflow/Started',
      'get /Cube/Workflow/Done',
      'get /Cube/Workflow/Phrases',
      'put /Cube/Workflow/Phrases',
      // typePath 含 / 移入 query，避免路由段断裂
      'post /Cube/Workflow/Entities/42/Patch',
    ]);
    expect(calls[6].params).toEqual({ typePath: 'Admin/User' });
  });
});
