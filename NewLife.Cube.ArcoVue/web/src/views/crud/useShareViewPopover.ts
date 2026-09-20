import { ref, watch, type Ref } from 'vue';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { formatDateTime } from '@/core/utils/datetime';

export type ShareExpireKey = '1h' | '1d' | '7d' | 'long' | 'custom';

export type ShareKind = 'page' | 'workbench';

export interface ShareResult {
  token: string;
  expire?: string;
  path?: string;
  url?: string;
}

/** 长期 = 1 年 */
export const SHARE_LONG_SECONDS = 365 * 24 * 3600;

export function resolveExpireSeconds(key: ShareExpireKey, customDays: number): number {
  if (key === '1h') return 3600;
  if (key === '1d') return 86400;
  if (key === '7d') return 604800;
  if (key === 'long') return SHARE_LONG_SECONDS;
  const days = Math.max(1, Math.min(365, Math.floor(customDays || 1)));
  return days * 86400;
}

export function normalizeShareData(data: unknown): ShareResult | null {
  if (!data || typeof data !== 'object') return null;
  const r = data as Record<string, unknown>;
  const token = String(r.token ?? r.Token ?? '');
  if (token) {
    return {
      token,
      expire: (r.expire ?? r.Expire) as string | undefined,
      path: (r.path ?? r.Path) as string | undefined,
      url: (r.url ?? r.Url) as string | undefined,
    };
  }
  const nested = r.data ?? r.Data;
  if (nested && nested !== data && typeof nested === 'object') return normalizeShareData(nested);
  return null;
}

/** 构建可打开的分享 URL（embed=1 隐藏壳层导航） */
export function buildSharePageUrl(typePath: string, viewId: string, token: string): string {
  const path = '/' + typePath.replace(/^\/+/, '');
  const q = new URLSearchParams();
  if (viewId) q.set('viewId', viewId);
  q.set('embed', '1');
  q.set('token', token);
  return `${window.location.origin}${path}?${q.toString()}`;
}

export function useShareViewPopover(
  getTypePath: () => string,
  getViewId: () => string,
  visible: Ref<boolean>,
  emitVisible: (v: boolean) => void,
  getKind: () => ShareKind = () => 'page',
  getSlug: () => string = () => '',
) {
  const creating = ref(false);
  const shareUrl = ref('');
  const expireText = ref('');
  const generateError = ref('');
  const expireKey = ref<ShareExpireKey>('1d');
  const customDays = ref(7);
  let genSeq = 0;

  function reset() {
    shareUrl.value = '';
    expireText.value = '';
    generateError.value = '';
  }

  async function createLink(expireSeconds: number) {
    creating.value = true;
    generateError.value = '';
    try {
      const typePath = getTypePath();
      const kind = getKind();
      const shareApi = cubeApi.workbench?.share;
      if (kind === 'workbench' && typeof shareApi !== 'function') {
        throw new Error('工作台分享接口不可用，请重新编译前端');
      }
      const res =
        kind === 'workbench'
          ? await shareApi({ slug: getSlug() || undefined, expireSeconds })
          : await cubeApi.page.share(typePath, { viewId: getViewId(), expireSeconds });
      const data = normalizeShareData(res?.data) || normalizeShareData(res);
      if (!data?.token) throw new Error('分享接口未返回令牌');
      const viewId = kind === 'workbench' ? '' : getViewId();
      shareUrl.value = data.url || buildSharePageUrl(typePath, viewId, data.token);
      expireText.value = data.expire ? formatDateTime(data.expire) || String(data.expire) : '';
    } catch (e) {
      shareUrl.value = '';
      expireText.value = '';
      throw new Error(formatApiError(e) || '生成分享链接失败');
    } finally {
      creating.value = false;
    }
  }

  async function regenerate() {
    const seq = ++genSeq;
    try {
      await createLink(resolveExpireSeconds(expireKey.value, customDays.value));
      if (seq !== genSeq) return;
    } catch (e) {
      if (seq !== genSeq) return;
      const msg = e instanceof Error ? e.message : '生成失败';
      generateError.value = msg;
      Message.error(msg);
    }
  }

  function onVisibleChange(v: boolean) {
    emitVisible(v);
    if (!v) return;
    expireKey.value = '1d';
    customDays.value = 7;
    reset();
    void regenerate();
  }

  /** 打开弹层或变更有效期时立即生成链接 */
  watch(
    () => (visible.value ? `${expireKey.value}:${customDays.value}` : ''),
    (key) => {
      if (!key) return;
      void regenerate();
    },
  );

  async function copyLink(): Promise<boolean> {
    const text = shareUrl.value;
    if (!text) return false;
    try {
      if (navigator.clipboard?.writeText) {
        await navigator.clipboard.writeText(text);
        return true;
      }
    } catch {
      /* fall through */
    }
    try {
      const ta = document.createElement('textarea');
      ta.value = text;
      ta.setAttribute('readonly', '');
      ta.style.position = 'fixed';
      ta.style.left = '-9999px';
      document.body.appendChild(ta);
      ta.select();
      const ok = document.execCommand('copy');
      document.body.removeChild(ta);
      return ok;
    } catch {
      return false;
    }
  }

  async function onCopy() {
    const ok = await copyLink();
    if (ok) Message.success('已复制分享链接');
    else Message.error('复制失败，请手动选择链接');
  }

  return {
    creating,
    shareUrl,
    expireText,
    generateError,
    expireKey,
    customDays,
    onVisibleChange,
    onCopy,
    reset,
    createLink,
    copyLink,
  };
}
