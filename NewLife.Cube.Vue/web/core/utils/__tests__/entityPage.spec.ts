/**
 * 实体页探测测试
 *
 * 覆盖「无菜单页面回退动态页」的判定：路由 → /meta 地址推导、响应形态解析。
 * 不发起网络请求（probeEntityPage 的网络分支由 E2E 覆盖）。
 *
 * 运行：pnpm test:unit core/utils/__tests__/entityPage.spec.ts
 */
import { describe, expect, it } from 'vitest';
import { buildMetaUrl, parseEntityPageProbe } from '../entityPage';

describe('entityPage 实体页探测', () => {
  it('应该由路由推导元数据地址 当路径为区域/控制器时', () => {
    expect(buildMetaUrl('/Admin/Tenant')).toBe('/api/Admin/Tenant/meta');
    expect(buildMetaUrl('/Cube/CronJob')).toBe('/api/Cube/CronJob/meta');
  });

  it('应该忽略尾部斜杠 当路由带尾斜杠时', () => {
    expect(buildMetaUrl('/Admin/Tenant/')).toBe('/api/Admin/Tenant/meta');
  });

  it('应该判定实体页存在并取显示名 当返回成功元数据时', () => {
    const probe = parseEntityPageProbe(200, {
      code: 0,
      data: { entity: { name: 'Tenant', displayName: '租户' }, form: [] },
    });

    expect(probe.exists).toBe(true);
    expect(probe.displayName).toBe('租户');
  });

  it('应该判定实体页存在 当成功码为 200 但没有显示名时', () => {
    const probe = parseEntityPageProbe(200, { code: 200, data: {} });

    expect(probe.exists).toBe(true);
    expect(probe.displayName).toBeUndefined();
  });

  it('应该判定不存在 当 HTTP 404 时', () => {
    expect(parseEntityPageProbe(404, { code: 0, data: {} }).exists).toBe(false);
  });

  it('应该判定不存在 当响应体为空或非对象时', () => {
    expect(parseEntityPageProbe(200, '').exists).toBe(false);
    expect(parseEntityPageProbe(200, null).exists).toBe(false);
    expect(parseEntityPageProbe(200, 'Not Found').exists).toBe(false);
  });

  it('应该判定不存在 当返回业务错误信封时', () => {
    // 纯 API 控制器（无实体元数据）的 /meta 常返回参数缺失等业务错误
    expect(parseEntityPageProbe(200, { code: -2, message: 'The lovCode field is required.' }).exists).toBe(false);
  });
});
