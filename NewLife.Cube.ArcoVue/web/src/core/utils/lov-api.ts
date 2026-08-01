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

const metaCache = new Map<string, Promise<LovMetaResponse>>();

/** 带缓存的 Meta 拉取，避免同页反复请求 */
export async function fetchLovMeta(lovCode: string): Promise<LovMetaResponse> {
  const key = lovCode.trim();
  const hit = metaCache.get(key);
  if (hit) return hit;
  const p = (async () => {
    const res = await fetch(`/Admin/Lov/Meta?lovCode=${encodeURIComponent(key)}`, {
      headers: authHeaders(),
    });
    if (!res.ok) throw new Error(`LOV Meta failed: ${res.status}`);
    const body = await res.json();
    const payload = body?.meta ? body : (body?.data ?? body);
    return {
      meta: payload?.meta ?? [],
      inlineEnums: payload?.inlineEnums ?? null,
    } as LovMetaResponse;
  })();
  metaCache.set(key, p);
  try {
    return await p;
  } catch (e) {
    metaCache.delete(key);
    throw e;
  }
}

/** 将 Enum.* 值集 options 灌入 field.dataSource（一次 Meta，列表徽章/表单下拉共用） */
export async function enrichFieldsWithEnumDataSource(
  fields: { lovCode?: string; dataSource?: Record<string, string> }[],
): Promise<void> {
  const codes = [
    ...new Set(
      fields
        .filter((f) => f.lovCode?.startsWith('Enum.') && !(f.dataSource && Object.keys(f.dataSource).length))
        .map((f) => f.lovCode!),
    ),
  ];
  if (!codes.length) return;
  try {
    const meta = await fetchLovMeta(codes.join(','));
    for (const f of fields) {
      if (!f.lovCode?.startsWith('Enum.') || (f.dataSource && Object.keys(f.dataSource).length)) continue;
      const item = meta.meta?.find((m) => m.lovCode === f.lovCode);
      const opts =
        (item?.type === 'ENUM' ? item.options : undefined) ??
        meta.inlineEnums?.[f.lovCode] ??
        [];
      if (!opts.length) continue;
      const ds: Record<string, string> = {};
      for (const o of opts) ds[String(o.value)] = o.label;
      f.dataSource = ds;
    }
  } catch {
    /* ignore */
  }
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
