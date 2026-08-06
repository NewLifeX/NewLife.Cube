/**
 * LOV 值集系统 API 请求封装
 *
 * 提供 LovSelect / LovSelectTable 组件所需的三个后端接口调用：
 *   - Meta：获取值集元数据（枚举选项 / 列表配置）
 *   - ListData：获取列表型值集的数据（代理查询）
 *   - BatchLabel：批量翻译值（value → label）
 *
 * 所有请求走 @newlifex/cube-vue 全局 Axios 实例（request），自动携带认证 Token。
 */

import request from '@newlifex/cube-vue/core/utils/request';
import type {
  LovMetaResponse,
  LovListConfig,
  LovListDataRequest,
  LovListDataResponse,
  LovBatchLabelRequest,
  LovBatchLabelResponse,
} from '@newlifex/cube-vue/core/types/lov';

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

/**
 * 解析 JSON 路径表达式（如 data.records），从响应对象中提取子节点。
 *
 * @param root 响应根对象
 * @param path 点号分隔的路径，空字符串/空值返回根
 */
function resolveJsonPath(root: Record<string, unknown>, path: string | null | undefined): unknown {
  if (!path) return root;
  const parts = path.split('.');
  let current: unknown = root;
  for (const part of parts) {
    if (current && typeof current === 'object' && !Array.isArray(current)) {
      current = (current as Record<string, unknown>)[part];
    } else {
      return undefined;
    }
  }
  return current;
}

/**
 * 根据列表型值集配置，由前端「直接请求」数据源（非代理）。
 * 适用于同源/同应用接口（requestUrl 以 / 开头）或显式关闭代理的场景。
 * 自动合并搜索参数、分页参数（pageNumField/pageSizeField）与固定参数，并按 dataPath/totalPath 解析响应。
 *
 * @param config 列表型值集数据源配置（含 requestUrl / method / pageable / dataPath 等）
 * @param requestParams 查询参数（lovCode、搜索条件、分页）
 * @returns 列表数据响应 { data, total }
 */
export async function fetchLovListDataDirect(
  config: LovListConfig,
  requestParams: LovListDataRequest,
): Promise<LovListDataResponse> {
  const method = (config.method || 'GET').toUpperCase();
  const params: Record<string, unknown> = { ...(requestParams.params || {}) };
  const fixedParams: Record<string, string> = config.fixedParams || {};

  // 分页参数
  if (config.pageable) {
    if ((requestParams.pageNum ?? 0) > 0 && config.pageNumField) params[config.pageNumField] = requestParams.pageNum;
    if ((requestParams.pageSize ?? 0) > 0 && config.pageSizeField) params[config.pageSizeField] = requestParams.pageSize;
  }
  // 固定参数（不覆盖已存在的搜索参数）
  for (const [k, v] of Object.entries(fixedParams)) {
    if (!(k in params)) params[k] = v;
  }

  let res;
  if (method === 'GET') {
    res = await request.get(config.requestUrl, { params });
  } else {
    res = await request.post(config.requestUrl, params);
  }

  const body = (res.data ?? res) as Record<string, unknown>;
  const data = (resolveJsonPath(body, config.dataPath) as unknown[]) || [];
  let total = 0;
  if (config.pageable && config.totalPath) {
    const t = resolveJsonPath(body, config.totalPath);
    total = typeof t === 'number' ? t : Number(t) || 0;
  }
  return { data: data as Record<string, unknown>[], total };
}

/**
 * 判断列表型值集是否应「前端直连」数据源：
 * 1) requestUrl 以 / 开头（同源同应用接口）—— 不论 proxyRequest 为何值，一律直连、禁止代理；
 * 2) 其余情况按 proxyRequest 决定：false/未设置 → 直连；true → 走后端 /Admin/Lov/ListData 代理。
 */
export function shouldDirectRequest(config: LovListConfig | null | undefined): boolean {
  if (!config) return false;
  if (config.requestUrl.startsWith('/')) return true;
  return !config.proxyRequest;
}
