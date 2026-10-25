/**
 * 统一响应处理工具类
 * 处理API返回的标准格式数据
 */
import notification from '../components/Notification';
import i18n from '../i18n';

/** 工具函数不在组件上下文内，直接走全局 i18n 实例 */
function t(key: string): string {
  return i18n.global.t(key);
}

/**
 * 标准API响应接口
 */
export interface ApiResponse<T = unknown> {
  /** 响应状态码，0或200表示成功 */
  code: number;
  /** 响应消息 */
  message?: string | null;
  /** 响应数据 */
  data?: T;
  /** 链路追踪ID */
  traceId?: string | null;
  /** 分页信息 */
  page?: {
    pageIndex: number;
    pageSize: number;
    totalCount: number;
    longTotalCount: string;
  } | null;
  /** 统计信息 */
  stat?: Record<string, unknown> | null;
}

/**
 * 分页请求参数接口
 */
export interface PageParams {
  pageIndex?: number;
  pageSize?: number;
}

/**
 * 标准化分页请求参数
 * @param params 原始参数
 * @returns 标准化后的参数
 */
export function normalizePageParams<T extends Record<string, unknown>>(params: T & PageParams): T & Required<PageParams> {
  return {
    ...params,
    pageIndex: params.pageIndex || 1,
    pageSize: params.pageSize || 10,
  };
}

/**
 * 处理API响应
 * @param response API响应数据
 * @param options 处理选项
 * @returns 处理结果
 */
export function handleApiResponse<T = unknown>(
  response: unknown,
  options: {
    /** 是否显示成功提示，默认为false（查询操作通常不显示成功提示） */
    showSuccessMessage?: boolean;
    /** 是否显示错误提示，默认为true */
    showErrorMessage?: boolean;
    /** 自定义成功消息 */
    successMessage?: string;
    /** 自定义错误消息 */
    errorMessage?: string;
  } = {}
): {
  success: boolean;
  data: T | null;
  message: string | null;
  page: ApiResponse['page'];
  stat: ApiResponse['stat'];
} {
  const {
    showSuccessMessage = false, // 默认不显示成功提示
    showErrorMessage = true,
    successMessage,
    errorMessage
  } = options;

  console.log('=== handleApiResponse start ===');
  console.log('response:', response);
  console.log('response type:', typeof response);
  console.log('response has code field:', response && typeof response === 'object' && 'code' in response);

  // 检查响应是否为有效对象
  if (!response || typeof response !== 'object') {
    console.log('❌ invalid response');
    const errorMsg = errorMessage || t('response.invalidData');
    if (showErrorMessage) {
      notification.error({ message: errorMsg });
    }
    return {
      success: false,
      data: null,
      message: errorMsg,
      page: null,
      stat: null,
    };
  }

  const responseObj = response as Record<string, unknown>;
  console.log('normalized response keys:', Object.keys(responseObj));
  console.log('responseObj.code raw:', responseObj.code);
  console.log('responseObj.code type:', typeof responseObj.code);

  // 1. 检查 code 字段并判断成功状态
  const code = responseObj.code as number;
  const isSuccess = code === 0 || code === 200;

  console.log('status check:', {
    code: code,
    codeType: typeof code,
    codeValue: responseObj.code,
    isCodeZero: code === 0,
    isCode200: code === 200,
    isSuccess: isSuccess
  });

  // 2. 处理 message 字段（可能为 null 或空字符串）
  const message = (responseObj.message as string) || null;

  // 3. 处理 traceId 字段（可能为 null、空字符串或不存在）
  const traceId = responseObj.traceId as string;
  if (traceId && traceId.trim() !== '') {
    console.log('TraceId:', traceId);
  }

  // 4. 处理 page 字段（可能为 null、空对象或不存在）
  let pageInfo: ApiResponse['page'] = null;
  if (responseObj.page && typeof responseObj.page === 'object') {
    const page = responseObj.page as Record<string, unknown>;
    // 检查是否为有效的分页对象（包含必要字段）
    if (typeof page.pageIndex === 'number' && typeof page.pageSize === 'number') {
      pageInfo = {
        pageIndex: page.pageIndex,
        pageSize: page.pageSize,
        totalCount: (page.totalCount as number) || 0,
        longTotalCount: (page.longTotalCount as string) || '0',
      };
    }
  }

  // 5. 处理 stat 字段（可能为 null、空对象或不存在）
  let statInfo: ApiResponse['stat'] = null;
  if (responseObj.stat && typeof responseObj.stat === 'object') {
    const stat = responseObj.stat as Record<string, unknown>;
    // 只有当 stat 对象有内容时才保留
    if (Object.keys(stat).length > 0) {
      statInfo = stat;
    }
  }

  // 6. 处理 data 字段（可能是对象、数组、基本类型或 null）
  const data = responseObj.data as T;

  console.log('processed data:', {
    success: isSuccess,
    data: data,
    message: message,
    page: pageInfo,
    stat: statInfo
  });

  if (isSuccess) {
    // 成功处理
    console.log('✅ success branch');

    // 显示成功提示（如果 message 不为空且配置了显示成功提示）
    const displayMessage = successMessage || message;
    if (showSuccessMessage && displayMessage) {
      console.log('show success message:', displayMessage);
      notification.success({ message: displayMessage });
    }

    const result = {
      success: true,
      data: data || null,
      message: message,
      page: pageInfo,
      stat: statInfo,
    };
    console.log('✅ returned success result:', result);
    return result;
  } else {
    // 错误处理
    console.log('❌ error branch');

    // 显示错误提示
    const displayMessage = errorMessage || message || t('response.operationFailed');
    if (showErrorMessage) {
      console.log('show error message:', displayMessage);
      notification.error({ message: displayMessage });
    }

    const result = {
      success: false,
      data: null,
      message: message,
      page: pageInfo,
      stat: statInfo,
    };
    console.log('❌ returned error result:', result);
    return result;
  }
}

/**
 * 创建API请求包装器
 * 自动处理响应并返回标准化结果
 */
export function createApiWrapper<T = unknown, P = unknown>(
  apiCall: (params: P) => Promise<ApiResponse<T>>,
  options?: {
    showSuccessMessage?: boolean;
    showErrorMessage?: boolean;
    successMessage?: string;
    errorMessage?: string;
  }
) {
  return async (params: P) => {
    try {
      console.log('=== createApiWrapper call start ===');
      console.log('params:', params);

      const response = await apiCall(params);

      console.log('=== createApiWrapper got response ===');
      console.log('apiCall response:', response);
      console.log('response type:', typeof response);
      console.log('response is array:', Array.isArray(response));
      console.log('response has code field:', response && typeof response === 'object' && 'code' in response);

      return handleApiResponse(response, {
        showSuccessMessage: false, // 默认不显示成功提示
        showErrorMessage: true,    // 默认显示错误提示
        ...options,               // 用户配置覆盖默认值
      });
    } catch (error) {
      // 网络错误或其他异常
      console.error('API call failed:', error);

      if (options?.showErrorMessage !== false) {
        const message = options?.errorMessage || t('response.networkErrorRetry');
        notification.error({ message });
      }

      return {
        success: false,
        data: null,
        message: t('response.networkError'),
        page: null,
        stat: null,
      };
    }
  };
}

/**
 * 分页API请求包装器
 * 专门处理分页请求，自动标准化分页参数
 */
export function createPageApiWrapper<T = unknown, P = Record<string, unknown>>(
  apiCall: (params: P & Required<PageParams>) => Promise<ApiResponse<T>>,
  options?: {
    showSuccessMessage?: boolean;
    showErrorMessage?: boolean;
    successMessage?: string;
    errorMessage?: string;
  }
) {
  return async (params: P & PageParams) => {
    const normalizedParams = {
      ...params,
      pageIndex: params.pageIndex || 1,
      pageSize: params.pageSize || 10,
    } as P & Required<PageParams>;
    return createApiWrapper(apiCall, options)(normalizedParams);
  };
}

/**
 * 提取响应数据
 * 从API响应中提取数据部分，如果失败则返回null
 * @param response API响应
 * @returns 数据或null
 */
export function extractResponseData<T>(response: ApiResponse<T>): T | null {
  const result = handleApiResponse(response, { showSuccessMessage: false, showErrorMessage: false });
  return result.success ? (result.data as T) : null;
}

/**
 * 提取分页数据
 * 从API响应中提取分页信息
 * @param response API响应
 * @returns 分页信息或null
 */
export function extractPageInfo(response: ApiResponse): ApiResponse['page'] {
  return response.page || null;
}

/**
 * 检查响应是否成功
 * @param response API响应
 * @returns 是否成功
 */
export function isApiSuccess(response: ApiResponse): boolean {
  return response.code === 0 || response.code === 200;
}

/**
 * 解包请求层返回的业务体
 *
 * `request` 已把 axios 响应解包为业务体 `ApiResponse`（`{code,data,...}`），
 * 因此页面取数应优先用 `data`；同时兼容少数直接返回裸数组/裸对象的接口。
 * @param payload 请求层返回值
 * @returns 业务数据；无法识别时返回 undefined
 */
export function unwrapApiData<T = unknown>(payload: unknown): T | undefined {
  if (payload === null || payload === undefined) return undefined;
  if (Array.isArray(payload)) return payload as T;

  if (typeof payload === 'object') {
    const obj = payload as Record<string, unknown>;
    if ('code' in obj && 'data' in obj) {
      const data = obj.data;
      return data === null || data === undefined ? undefined : (data as T);
    }
  }

  return payload as T;
}

// 导出所有工具函数和类型，方便其他模块使用
export * from './response';
