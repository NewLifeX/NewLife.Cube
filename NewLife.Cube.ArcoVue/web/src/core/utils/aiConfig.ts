/** GetAiConfig 响应解析。FastJson CamelCase 把 AISwitch 写成 aISwitch，须忽略大小写。 */

export interface AiAssistantConfig {
  enabled: boolean;
  primary: string;
  secondary: string;
}

export const DEFAULT_AI_CONFIG: AiAssistantConfig = {
  enabled: false,
  primary: '#2ecc71',
  secondary: '#1e8e3e',
};

function asRecord(v: unknown): Record<string, unknown> | null {
  if (v && typeof v === 'object' && !Array.isArray(v)) return v as Record<string, unknown>;
  return null;
}

function hasAiKey(obj: Record<string, unknown>): boolean {
  return Object.keys(obj).some((k) => {
    const n = k.toLowerCase();
    return n === 'aiswitch' || n === 'aiprimarycolor' || n === 'aisecondarycolor';
  });
}

function unwrapAiPayload(raw: unknown): Record<string, unknown> {
  const seen = new Set<unknown>();
  let cur: unknown = raw;
  for (let i = 0; i < 4; i++) {
    const rec = asRecord(cur);
    if (!rec || seen.has(rec)) break;
    seen.add(rec);
    if (hasAiKey(rec)) return rec;
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

function asEnabled(v: unknown): boolean {
  return v === true || v === 1 || v === 'true' || v === 'True' || v === 'TRUE';
}

function asColor(v: unknown, fallback: string): string {
  return typeof v === 'string' && v.trim() ? v : fallback;
}

/**
 * 从 Axios / ApiResponse / 裸对象中读 AI 开关与配色。
 * 兼容 AISwitch / aiSwitch / aISwitch（FastJson 首字母小写）。
 */
export function parseAiConfig(raw: unknown): AiAssistantConfig {
  const obj = unwrapAiPayload(raw);
  return {
    enabled: asEnabled(pickIgnoreCase(obj, 'AISwitch')),
    primary: asColor(pickIgnoreCase(obj, 'AIPrimaryColor'), DEFAULT_AI_CONFIG.primary),
    secondary: asColor(pickIgnoreCase(obj, 'AISecondaryColor'), DEFAULT_AI_CONFIG.secondary),
  };
}
