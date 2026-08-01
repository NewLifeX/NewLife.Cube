import { ApiError } from '@cube/api-core';

/** 从保存/业务异常中提取可读消息（含字段级错误） */
export function formatApiError(err: unknown, fallback = '操作失败'): string {
  if (err instanceof ApiError) {
    const fieldMsg = err.fieldErrors?.map((e) => e.message).filter(Boolean).join('；');
    return fieldMsg || err.message || fallback;
  }
  if (err && typeof err === 'object') {
    const anyErr = err as {
      message?: string;
      response?: { data?: { message?: string; fieldErrors?: { message?: string }[] } };
    };
    const data = anyErr.response?.data;
    const fieldMsg = data?.fieldErrors?.map((e) => e.message).filter(Boolean).join('；');
    if (fieldMsg) return fieldMsg;
    if (data?.message) return data.message;
    if (anyErr.message && anyErr.message !== 'Network Error') return anyErr.message;
  }
  return fallback;
}
