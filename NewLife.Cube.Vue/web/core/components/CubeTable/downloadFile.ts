/**
 * 文件下载触发（DOM 基础设施，不含业务判断）
 *
 * 引擎侧只负责拿到 blob 与文件名（`ctx.pendingBlob`），浏览器动作留在这里，
 * 这样引擎保持无 DOM 依赖、可单测；本文件也不掺任何业务规则。
 */

/** 触发浏览器下载并回收 ObjectURL */
export function saveBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
