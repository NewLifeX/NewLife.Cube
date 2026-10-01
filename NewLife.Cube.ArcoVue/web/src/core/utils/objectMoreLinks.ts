import { normalizeMenuUrl } from './url';

export interface ObjectMoreLink {
  title: string;
  path: string;
}

export interface MenuUrlNode {
  url?: string | null;
  visible?: boolean;
  children?: MenuUrlNode[];
}

const MORE_LINKS: ObjectMoreLink[] = [
  { title: '短信设置', path: '/Admin/SmsConfig' },
  { title: '邮件设置', path: '/Admin/MailConfig' },
  { title: 'OAuth 设置', path: '/Admin/OAuthConfig' },
  { title: '访问规则', path: '/Admin/AccessRule' },
];

function normPath(url: string): string {
  const path = normalizeMenuUrl(url, 'pascal').split('?')[0] ?? '';
  return path.replace(/^\/+|\/+$/g, '').toLowerCase();
}

/** 展平菜单（含 visible=false）后，只保留 url 命中的固定项。 */
export function buildObjectMoreLinks(menus: MenuUrlNode[]): ObjectMoreLink[] {
  const urls = new Set<string>();
  const walk = (items: MenuUrlNode[]) => {
    for (const item of items ?? []) {
      if (item.url) urls.add(normPath(item.url));
      if (item.children?.length) walk(item.children);
    }
  };
  walk(menus ?? []);
  return MORE_LINKS.filter((link) => urls.has(normPath(link.path)));
}
