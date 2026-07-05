/**
 * LOV 值集系统 API 请求封装
 *
 * 提供 LovSelect / LovSelectTable 组件所需的三个后端接口调用：
 *   - Meta：获取值集元数据（枚举选项 / 列表配置）
 *   - ListData：获取列表型值集的数据（代理查询）
 *   - BatchLabel：批量翻译值（value → label）
 *
 * 所有请求走 cube-front 全局 Axios 实例（request），自动携带认证 Token。
 */

import request from 'cube-front/core/utils/request';
import type {
  LovMetaResponse,
  LovListDataRequest,
  LovListDataResponse,
  LovBatchLabelRequest,
  LovBatchLabelResponse,
} from 'cube-front/core/types/lov';

/**
 * 获取值集元数据。支持逗号分隔多个 lovCode，枚举型内联 options。
 *
 * @param lovCode 值集编码，可逗号分隔传多个
 * @returns 值集元数据响应
 *
 * @example
 * ```ts
 * const meta = await fetchLovMeta('Enum.ProcessCard.Status');
 * // meta.meta[0].type === 'ENUM'
 * // meta.meta[0].options => [{ value: '0', label: '草稿' }, ...]
 * ```
 */
export async function fetchLovMeta(lovCode: string): Promise<LovMetaResponse> {
  const res = await request.get('/Admin/Lov/Meta', { params: { lovCode } });
  return res.data;
}

/**
 * 获取列表型值集的代理查询数据。
 *
 * @param requestParams 查询参数
 * @returns 列表数据响应
 *
 * @example
 * ```ts
 * const data = await fetchLovListData({
 *   lovCode: 'List.User',
 *   params: { name: '张' },
 *   pageNum: 1,
 *   pageSize: 20,
 * });
 * ```
 */
export async function fetchLovListData<T = Record<string, unknown>>(
  requestParams: LovListDataRequest,
): Promise<LovListDataResponse<T>> {
  const res = await request.post('/Admin/Lov/ListData', requestParams);
  // 后端返回结构：{ data: [...], total: number }
  const body = res.data;
  return {
    data: body.data ?? [],
    total: body.total ?? 0,
  };
}

/**
 * 批量翻译值集原始值为显示文本。
 *
 * @param requestParams 包含 lovCode 和 values 数组
 * @returns value → label 映射字典
 *
 * @example
 * ```ts
 * const labels = await fetchBatchLabel({ lovCode: 'Enum.Status', values: ['0', '1', '2'] });
 * // labels => { '0': '草稿', '1': '试模中', '2': '试模合格待审批' }
 * ```
 */
export async function fetchBatchLabel(
  requestParams: LovBatchLabelRequest,
): Promise<LovBatchLabelResponse> {
  const res = await request.post('/Admin/Lov/BatchLabel', requestParams);
  return res.data;
}

/**
 * 解析值集编码的前缀，判断类型。
 *
 * @example
 * ```ts
 * resolveLovType('Enum.ProcessCard.Status') // => 'ENUM'
 * resolveLovType('List.User')              // => 'LIST'
 * ```
 */
export function resolveLovType(lovCode: string): 'ENUM' | 'LIST' | null {
  const prefix = lovCode.split('.')[0];
  if (prefix === 'Enum') return 'ENUM';
  if (prefix === 'List') return 'LIST';
  return null;
}
