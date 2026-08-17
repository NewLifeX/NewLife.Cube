import { Message } from '@arco-design/web-vue';
import { resolveUrl } from '@cube/page-utils';
import type { Router } from 'vue-router';
import cubeApi from '@/api';
import type { OpsCustomLink } from '@/core/utils/opsAction';

function isExternalOrBlank(target?: string, url?: string): boolean {
  if (target === '_blank') return true;
  if (!url) return false;
  return /^https?:\/\//i.test(url) || url.startsWith('//');
}

/** 站内路径：去掉 origin，保留 path+query+hash */
export function toSpaPath(url: string): string {
  try {
    if (/^https?:\/\//i.test(url) || url.startsWith('//')) {
      const u = new URL(url, window.location.origin);
      if (u.origin === window.location.origin) {
        return `${u.pathname}${u.search}${u.hash}`;
      }
      return url;
    }
  } catch {
    /* fall through */
  }
  return url.startsWith('/') ? url : `/${url}`;
}

/** 解析行级 Url 模板；空结果返回 null */
export function resolveRowUrl(
  template: string,
  row: Record<string, unknown>,
): string | null {
  const resolved = resolveUrl(template, row).trim();
  return resolved || null;
}

export async function navigateResolvedUrl(
  url: string,
  target: string | undefined,
  router: Router,
): Promise<void> {
  if (isExternalOrBlank(target, url)) {
    window.open(url, '_blank', 'noopener,noreferrer');
    return;
  }
  const path = toSpaPath(url);
  if (/^https?:\/\//i.test(path) || path.startsWith('//')) {
    window.open(path, '_blank', 'noopener,noreferrer');
    return;
  }
  try {
    await router.push(path);
  } catch {
    window.location.assign(path);
  }
}

/** dataAction 非空：GET 解析后的 Url（经典 data-action 多为 GET 链），成功 resolve */
export async function requestDataAction(url: string): Promise<void> {
  const path = toSpaPath(url);
  // 实体动作常挂在 /api 下；已是绝对/协议相对则原样请求
  const requestUrl =
    /^https?:\/\//i.test(path) || path.startsWith('//') || path.startsWith('/api/')
      ? path
      : path.startsWith('/')
        ? `/api${path}`
        : path;
  const res = await cubeApi.client.request({
    url: requestUrl,
    method: 'GET',
  });
  const body = res?.data as { code?: number; message?: string } | undefined;
  if (body && typeof body.code === 'number' && body.code !== 0) {
    throw new Error(body.message || `动作失败(${body.code})`);
  }
}

export async function runOpsCustomLink(options: {
  link: OpsCustomLink;
  row: Record<string, unknown>;
  router: Router;
  onDone?: () => void | Promise<void>;
}): Promise<void> {
  const { link, row, router, onDone } = options;
  const url = resolveRowUrl(link.url, row);
  if (!url) {
    Message.warning('链接地址无效');
    return;
  }
  if (link.dataAction?.trim()) {
    try {
      await requestDataAction(url);
      Message.success('操作成功');
      await onDone?.();
    } catch (e) {
      Message.error(e instanceof Error ? e.message : '操作失败');
    }
    return;
  }
  await navigateResolvedUrl(url, link.target, router);
}

/** 单元格挂链接：同导航语义 */
export async function runCellFieldLink(options: {
  urlTemplate: string;
  target: string | undefined;
  row: Record<string, unknown>;
  router: Router;
}): Promise<void> {
  const url = resolveRowUrl(options.urlTemplate, options.row);
  if (!url) {
    Message.warning('链接地址无效');
    return;
  }
  await navigateResolvedUrl(url, options.target, options.router);
}
