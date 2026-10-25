/**
 * 响应解包工具测试
 *
 * `request` 已把 axios 响应解包为业务体 ApiResponse，页面取数必须走 `data`；
 * unwrapApiData 负责这条约定，并兼容少数返回裸数组/裸对象的接口。
 *
 * 运行：pnpm test:unit core/utils/__tests__/response.spec.ts
 */
import { describe, expect, it } from 'vitest';
import { unwrapApiData } from '../response';

describe('unwrapApiData 业务体解包', () => {
  it('应该取出 data 当返回标准 ApiResponse 时', () => {
    expect(unwrapApiData({ code: 0, data: { system: '/api/Admin/Index/Main' } })).toEqual({
      system: '/api/Admin/Index/Main',
    });
  });

  it('应该原样返回裸数组 当接口直接返回数组时', () => {
    expect(unwrapApiData([{ name: 'a' }])).toEqual([{ name: 'a' }]);
  });

  it('应该原样返回裸对象 当响应不是信封时', () => {
    expect(unwrapApiData({ count: 1 })).toEqual({ count: 1 });
  });

  it('应该返回 undefined 当响应为空或 data 为空时', () => {
    expect(unwrapApiData(undefined)).toBeUndefined();
    expect(unwrapApiData(null)).toBeUndefined();
    expect(unwrapApiData({ code: 0, data: null })).toBeUndefined();
  });
});
