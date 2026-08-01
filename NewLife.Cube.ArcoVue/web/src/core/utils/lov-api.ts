import cubeApi from '@/api';
import type {
  LovBatchLabelResponse,
  LovListDataRequest,
  LovListDataResponse,
  LovMetaResponse,
} from '../types/lov';

function authHeaders(json = false): Record<string, string> {
  const token = cubeApi.tokenManager.getToken();
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (json) headers['Content-Type'] = 'application/json';
  if (token) headers.Authorization = `Bearer ${token}`;
  return headers;
}

export async function fetchLovMeta(lovCode: string): Promise<LovMetaResponse> {
  const res = await fetch(`/Admin/Lov/Meta?lovCode=${encodeURIComponent(lovCode)}`, {
    headers: authHeaders(),
  });
  if (!res.ok) throw new Error(`LOV Meta failed: ${res.status}`);
  const body = await res.json();
  const payload = body?.meta ? body : (body?.data ?? body);
  return {
    meta: payload?.meta ?? [],
    inlineEnums: payload?.inlineEnums ?? null,
  };
}

export async function fetchLovListData<T = Record<string, unknown>>(
  requestParams: LovListDataRequest,
): Promise<LovListDataResponse<T>> {
  const res = await fetch('/Admin/Lov/ListData', {
    method: 'POST',
    headers: authHeaders(true),
    body: JSON.stringify(requestParams),
  });
  if (!res.ok) throw new Error(`LOV ListData failed: ${res.status}`);
  const body = await res.json();
  if (Array.isArray(body?.data) && typeof body?.total === 'number') {
    return { data: body.data, total: body.total };
  }
  const inner = body?.data ?? body;
  return {
    data: (inner?.data ?? (Array.isArray(inner) ? inner : [])) as T[],
    total: Number(inner?.total ?? body?.total ?? 0),
  };
}

export async function fetchBatchLabel(params: {
  lovCode: string;
  values: string[];
}): Promise<LovBatchLabelResponse> {
  const res = await fetch('/Admin/Lov/BatchLabel', {
    method: 'POST',
    headers: authHeaders(true),
    body: JSON.stringify(params),
  });
  if (!res.ok) throw new Error(`LOV BatchLabel failed: ${res.status}`);
  const body = await res.json();
  return (body?.data ?? body) as LovBatchLabelResponse;
}

export function resolveLovType(lovCode: string): 'ENUM' | 'LIST' | null {
  const prefix = lovCode.split('.')[0];
  if (prefix === 'Enum') return 'ENUM';
  if (prefix === 'List') return 'LIST';
  return null;
}
