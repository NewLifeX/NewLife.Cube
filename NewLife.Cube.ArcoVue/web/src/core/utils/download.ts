/**
 * 浏览器端 Blob 下载帮助（OSC-2608139feb）。
 *
 * 后端返回文件流接口（File/Download、Db/Download）经 axios responseType=blob
 * 拿到 Blob 后，由本函数触发本地保存。
 */

/** 触发浏览器下载 Blob 为本地文件 */
export function saveBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

/**
 * 从请求结果解 Blob（兼容直接 Blob 与 AxiosResponse.data 两种形态）。
 * 响应拦截器 unwrapResponse=false 时返回完整 AxiosResponse；
 * 且仅 octet-stream/arraybuffer 透传，其它 content-type（如 application/xml）不会自动解包。
 */
export function blobOf(res: unknown): Blob | null {
  if (res instanceof Blob) return res;
  if (res && typeof res === 'object' && 'data' in res) {
    const data = (res as { data?: unknown }).data;
    if (data instanceof Blob) return data;
  }
  return null;
}

/** 从响应 Content-Disposition 提取文件名；失败回落给定默认名 */
export function filenameOf(disposition: string | undefined, fallback: string): string {
  if (!disposition) return fallback;
  const m = /filename\*?=(?:UTF-8'')?["']?([^"';\r\n]+)["']?/i.exec(disposition);
  if (!m) return fallback;
  try {
    return decodeURIComponent(m[1].trim());
  } catch {
    return m[1].trim() || fallback;
  }
}
