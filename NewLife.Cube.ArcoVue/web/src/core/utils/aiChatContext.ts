import type { PageKind } from './pageKind';

export type AiChatPage = 'list' | 'form' | 'detail' | 'object' | 'home' | 'custom';

/** 当前页 AI 上下文（appStore；消息列表不放 store） */
export interface AiRuntimeContext {
  page: AiChatPage;
  mode: 'add' | 'edit';
  id: number;
  typePath: string;
  queryB64: string;
  applyFill?: (values: Record<string, unknown>) => void;
}

export function emptyAiRuntimeContext(): AiRuntimeContext {
  return { page: 'home', mode: 'add', id: 0, typePath: '', queryB64: '' };
}

/** 路由 path → area/controller：`Admin/User` → Admin + User；单段 `Home` → 空 area */
export function parseAreaController(path: string): { area: string; controller: string } {
  const segs = String(path || '')
    .replace(/^\/+/, '')
    .split('/')
    .filter(Boolean);
  if (!segs.length) return { area: '', controller: '' };
  if (segs.length === 1) return { area: '', controller: segs[0] };
  return { area: segs.slice(0, -1).join('/'), controller: segs[segs.length - 1] };
}

/** 列表 Search 参数 JSON → Base64；失败变空串，不阻断发送 */
export function encodeQueryB64(params: Record<string, unknown> | undefined | null): string {
  try {
    if (!params || typeof params !== 'object') return '';
    const json = JSON.stringify(params);
    if (!json || json === '{}') return '';
    return btoa(unescape(encodeURIComponent(json)));
  } catch {
    return '';
  }
}

/** detectPageKind + 抽屉模式 → AiChatRequest.page */
export function mapPageKindToAiPage(
  kind: PageKind,
  drawer?: 'add' | 'edit' | 'detail' | null,
): AiChatPage {
  if (kind === 'home') return 'home';
  if (kind === 'custom' || kind === 'unknown') return 'custom';
  if (kind === 'object') return 'object';
  if (drawer === 'add' || drawer === 'edit') return 'form';
  if (drawer === 'detail') return 'detail';
  return 'list';
}
