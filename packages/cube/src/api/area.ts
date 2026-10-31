/** 地区接口（/Cube/Area*，服务路由，不带 /api 前缀） */
import type { AxiosRequestConfig } from 'axios';
import type { ApiResponse } from './types';
import { AreaCascade, normalizeAreaOptions, normalizeAreaPath, AREA_CHILDREN_PATH, AREA_PARENTS_PATH } from '../field/area';

/** 请求函数签名（与 createRequest 返回一致） */
type RequestFn = <T>(config: AxiosRequestConfig) => Promise<ApiResponse<T>>;

/** 地区 API：供 ItemType=area/area1~4 的级联选择使用 */
export function createAreaApi(request: RequestFn) {
  return {
    /**
     * 地区子级。
     *
     * @param parentId 父级编号；0 取省
     */
    getChildren: (parentId = 0) =>
      request<unknown>({ url: AREA_CHILDREN_PATH, method: 'get', params: { id: parentId } }),

    /**
     * 地区父级路径（从省到自身）。
     *
     * @param id 地区编号
     */
    getPath: (id: number) =>
      request<unknown>({ url: AREA_PARENTS_PATH, method: 'get', params: { id, isContainSelf: true } }),
  };
}

/** 地区 API 的返回类型 */
export type AreaApi = ReturnType<typeof createAreaApi>;

/**
 * 用某个 API 客户端创建地区级联控制器（各皮肤同口径）。
 *
 * @param api 地区 API
 * @param targetLevel 目标层级 1~4（`area` 默认三级）
 * @example
 * const cascade = createAreaCascade(api.area, 3);
 * await cascade.init(row.areaId);
 */
export function createAreaCascade(api: AreaApi, targetLevel = 3): AreaCascade {
  return new AreaCascade(
    {
      loadChildren: async (parentId: number) => {
        const res = await api.getChildren(parentId);
        return normalizeAreaOptions(res?.data);
      },
      loadPath: async (id: number) => {
        const res = await api.getPath(id);
        return normalizeAreaPath(res?.data);
      },
    },
    targetLevel,
  );
}
