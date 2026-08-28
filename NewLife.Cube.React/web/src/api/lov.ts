/**
 * LOV 值集系统 API 请求封装（对齐 Vue 皮肤 core/utils/lov-api.ts）
 *
 * 提供 LovSelect 所需的两个后端接口调用：
 *   - Meta：获取值集元数据（枚举选项 / 列表配置）
 *   - ListData：获取列表型值集的数据（代理查询）
 *
 * 所有请求走全局 api.client（axios），自动携带认证 Token 与 /api 前缀。
 */
import { api } from '@/api';
import type { LovMetaItem, LovListDataRequest, LovListDataResponse } from '@/types/lov';

/**
 * 获取值集元数据。支持逗号分隔多个 lovCode，枚举型内联 options。
 *
 * @param lovCode 值集编码，可逗号分隔传多个
 * @returns 值集元数据数组
 *
 * @example
 * const meta = await fetchLovMeta('Enum.ProcessCard.Status');
 * // meta[0].type === 'ENUM'
 * // meta[0].options => [{ value: '0', label: '草稿' }, ...]
 */
export async function fetchLovMeta(lovCode: string): Promise<LovMetaItem[]> {
  const res = await api.client.get('/Admin/Lov/Meta', { params: { lovCode } });
  const body = res.data as { data?: LovMetaItem[] };
  return body.data ?? [];
}

/**
 * 获取列表型值集的代理查询数据。
 *
 * @param requestParams 查询参数
 * @returns 列表数据响应
 */
export async function fetchLovListData<T = Record<string, unknown>>(
  requestParams: LovListDataRequest,
): Promise<LovListDataResponse<T>> {
  const res = await api.client.post('/Admin/Lov/ListData', requestParams);
  const body = res.data as { data?: T[]; total?: number };
  return {
    data: body.data ?? [],
    total: body.total ?? 0,
  };
}
