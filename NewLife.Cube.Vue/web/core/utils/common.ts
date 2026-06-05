import notification from '../components/Notification.ts';

/**
 * 生成指定长度的随机十六进制字符串
 */
export function getRandomHex(length: number): string {
  const bytes = new Uint8Array(Math.ceil(length / 2));
  window.crypto.getRandomValues(bytes);
  return Array.from(bytes)
    .map((byte) => byte.toString(16).padStart(2, '0'))
    .join('')
    .substring(0, length);
}

export function getResponse(
  response:
    | {
        failed: boolean;
        detailsMessage: string;
        message: string;
        description: unknown;
        code: unknown;
        requestMessage: string | number | boolean | null | undefined;
        content: unknown;
        type: unknown;
      }
    | {
        code: number;
        message?: string | null;
        data?: unknown;
        traceId?: string | null;
        page?: {
          pageIndex: number;
          pageSize: number;
          totalCount: number;
          longTotalCount: string;
        } | null;
        stat?: Record<string, unknown> | null;
      }
    | null
    | undefined,
  errorCallback?: (arg0: unknown) => void,
) {
  if (!response) {
    return response;
  }

  // 处理新的标准API响应格式
  if ('code' in response && typeof response.code === 'number') {
    const apiResponse = response as {
      code: number;
      message?: string | null;
      data?: unknown;
      traceId?: string | null;
    };

    // 只输出 traceId 到控制台，不处理业务逻辑
    if (apiResponse.traceId) {
      console.log('TraceId:', apiResponse.traceId);
    }

    // 不在这里处理成功/失败逻辑，让 API 包装器来处理
    return response;
  }

  // 处理旧的响应格式（保持兼容性）
  if ('failed' in response && response.failed === true) {
    if (errorCallback) {
      errorCallback(response);
    } else {
      const msg = {
        message: response.detailsMessage || response.message || '操作失败',
      };
      switch (response.type) {
        case 'info':
          notification.info(msg);
          break;
        case 'warn':
          notification.warning(msg);
          break;
        case 'error':
        default:
          notification.error(msg);
          break;
      }
    }
  }

  return response;
}

/**
 * 根据给定的键路径从对象中获取数据
 * @param obj 源数据对象
 * @param key 键路径，可以是简单字符串或点分隔的路径，如 "data.items.list"
 * @param defaultValue 如果找不到值时返回的默认值
 * @returns 找到的值或默认值
 */
export function getDataByKey<T = unknown>(
  obj: object,
  key: string,
  defaultValue?: T,
): T | undefined | null {
  if (obj === null || obj === undefined) {
    return defaultValue;
  }

  // 支持点分隔的键路径
  const keys = key.split('.');
  let result: unknown | Record<string, unknown | Record<string, unknown>> = obj;

  for (const k of keys) {
    if (result === null || result === undefined) {
      return defaultValue;
    }

    if (typeof result !== 'object') {
      return defaultValue;
    } else {
      // 使用类型断言确保TypeScript知道result是一个可索引的对象
      const resultObj = result as Record<string, unknown>;
      if (!(k in resultObj)) {
        return defaultValue;
      } else {
        result = resultObj[k];
      }
    }
  }

  return result === undefined || result === null ? defaultValue : (result as T);
}

/**
 * 处理响应数据
 * @param item 原始数据项
 * @param dataKey 数据键路径
 * @returns 处理后的对象数组
 */
export function generateResponseData(item: unknown, dataKey?: string): object[] {
  if (item) {
    if (Array.isArray(item)) {
      return item;
    }
    if (typeof item === 'object') {
      if (dataKey) {
        const result = getDataByKey<unknown>(item as Record<string, unknown>, dataKey);
        if (result === undefined || result === null) {
          return [item];
        }
        if (Array.isArray(result)) {
          return result;
        }
        if (typeof result === 'object') {
          return [result as object];
        }
      } else {
        return [item];
      }
    }
  }
  return [];
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function isPromise(obj: any): boolean {
  return (
    !!obj &&
    (typeof obj === 'object' || typeof obj === 'function') &&
    typeof obj.then === 'function'
  );
}
