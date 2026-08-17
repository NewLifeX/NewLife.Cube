/** 动态 favicon 标记，便于清空时只移除我们创建的节点 */
const FAVICON_ATTR = 'data-cube-favicon';

/**
 * 将浏览器页签图标设为指定 URL；空地址时移除本模块创建的 link。
 */
export function applyDocumentFavicon(href: string | null | undefined): void {
  if (typeof document === 'undefined') return;
  const url = (href ?? '').trim();
  const existing = document.querySelectorAll(`link[${FAVICON_ATTR}]`);

  if (!url) {
    existing.forEach((el) => el.remove());
    return;
  }

  let link = document.querySelector<HTMLLinkElement>(`link[${FAVICON_ATTR}]`);
  if (!link) {
    link = document.createElement('link');
    link.setAttribute(FAVICON_ATTR, '1');
    link.rel = 'icon';
    document.head.appendChild(link);
  }
  if (link.href !== url && link.getAttribute('href') !== url) {
    link.href = url;
  }
}
