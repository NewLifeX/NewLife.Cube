/** GetMapConfig 响应解析。系统级单服务商：高德amap / 百度baidu / 腾讯tencent；未配置时 provider 为 null。 */

export type MapProviderKind = 'amap' | 'baidu' | 'tencent';

export interface MapSetting {
  /** 服务商；null=系统未配置地图 */
  provider: MapProviderKind | null;
  /** JS API Key */
  key: string;
  /** 自定义脚本地址（可选覆盖服务商默认加载地址） */
  scriptUrl: string;
}

export const DEFAULT_MAP_CONFIG: MapSetting = {
  provider: null,
  key: '',
  scriptUrl: '',
};

const VALID_PROVIDERS: readonly MapProviderKind[] = ['amap', 'baidu', 'tencent'];

function asRecord(v: unknown): Record<string, unknown> | null {
  if (v && typeof v === 'object' && !Array.isArray(v)) return v as Record<string, unknown>;
  return null;
}

function hasMapKey(obj: Record<string, unknown>): boolean {
  return Object.keys(obj).some((k) => {
    const n = k.toLowerCase();
    return n === 'provider' || n === 'mapprovider' || n === 'key' || n === 'mapkey' || n === 'scripturl' || n === 'mapscripturl';
  });
}

function unwrapMapPayload(raw: unknown): Record<string, unknown> {
  const seen = new Set<unknown>();
  let cur: unknown = raw;
  for (let i = 0; i < 4; i++) {
    const rec = asRecord(cur);
    if (!rec || seen.has(rec)) break;
    seen.add(rec);
    if (hasMapKey(rec)) return rec;
    if ('data' in rec) {
      cur = rec.data;
      continue;
    }
    return rec;
  }
  return asRecord(cur) ?? {};
}

function pickIgnoreCase(obj: Record<string, unknown>, name: string): unknown {
  const hit = Object.keys(obj).find((k) => k.toLowerCase() === name.toLowerCase());
  return hit !== undefined ? obj[hit] : undefined;
}

function asText(v: unknown): string {
  return typeof v === 'string' ? v.trim() : '';
}

/**
 * 从 Axios / ApiResponse / 裸对象中读地图服务商配置。
 * 兼容 Provider / provider / MapProvider（FastJson 首字母小写）。
 * provider 或 key 任一缺失时视为未配置（provider=null）。
 */
export function parseMapConfig(raw: unknown): MapSetting {
  const obj = unwrapMapPayload(raw);
  const providerRaw = (
    asText(pickIgnoreCase(obj, 'Provider')) || asText(pickIgnoreCase(obj, 'MapProvider'))
  ).toLowerCase();
  const provider = (VALID_PROVIDERS as readonly string[]).includes(providerRaw)
    ? (providerRaw as MapProviderKind)
    : null;
  const key = asText(pickIgnoreCase(obj, 'Key')) || asText(pickIgnoreCase(obj, 'MapKey'));
  const scriptUrl = asText(pickIgnoreCase(obj, 'ScriptUrl')) || asText(pickIgnoreCase(obj, 'MapScriptUrl'));
  if (!provider || !key) return { ...DEFAULT_MAP_CONFIG, scriptUrl };
  return { provider, key, scriptUrl };
}
