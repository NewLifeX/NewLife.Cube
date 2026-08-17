import { ApiError } from '@cube/api-core';
import { buildStarTraceUrl } from './starTrace';

/** 可选：从应用状态注入 StarWeb，避免 apiError 直接依赖 Pinia */
let starWebResolver: (() => string | null | undefined) | null = null;

export function setStarWebResolver(fn: (() => string | null | undefined) | null) {
  starWebResolver = fn;
}

/** 从保存/业务异常中提取可读消息（含字段级错误；有 StarWeb 时附追踪链接） */
export function formatApiError(err: unknown, fallback = '操作失败'): string {
  let msg = fallback;
  let traceId: string | undefined;

  if (err instanceof ApiError) {
    const fieldMsg = err.fieldErrors?.map((e) => e.message).filter(Boolean).join('；');
    msg = fieldMsg || err.message || fallback;
    traceId = err.response?.traceId;
  } else if (err && typeof err === 'object') {
    const anyErr = err as {
      message?: string;
      response?: {
        data?: { message?: string; fieldErrors?: { message?: string }[]; traceId?: string };
        traceId?: string;
      };
    };
    const data = anyErr.response?.data;
    const fieldMsg = data?.fieldErrors?.map((e) => e.message).filter(Boolean).join('；');
    if (fieldMsg) msg = fieldMsg;
    else if (data?.message) msg = data.message;
    else if (anyErr.message && anyErr.message !== 'Network Error') msg = anyErr.message;
    traceId = data?.traceId || anyErr.response?.traceId;
  }

  const star = starWebResolver?.();
  const url = buildStarTraceUrl(star, traceId);
  if (url) return `${msg}（追踪 ${url}）`;
  return msg;
}
