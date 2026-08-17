/**
 * 登录后落地页：优先 query.redirect；否则 StartPage。
 * 常见 MVC 路径按映射表转到 SPA；未知 /Admin/* 动作页回落实体列表或 /home。
 */
import { toPascalCase } from '@/core/utils/url';

/** 精确 / 规范化后的 MVC → SPA 映射（键小写、无尾斜杠） */
const START_PAGE_EXACT: Record<string, string> = {
  '/admin/user/info': '/home',
  '/admin/index': '/home',
  '/admin/index/index': '/home',
  '/admin/index/main': '/home',
  '/admin/home': '/home',
  '/home/index': '/home',
  '/': '/home',
};

const MVC_ACTION = new Set(['info', 'detail', 'edit', 'add', 'delete', 'index', 'main']);

function normalizePathKey(raw: string): string {
  let p = raw.trim().replace(/\\/g, '/');
  if (p.startsWith('~/')) p = p.slice(1);
  const q = p.indexOf('?');
  if (q >= 0) p = p.slice(0, q);
  if (!p.startsWith('/')) p = `/${p}`;
  // 去尾斜杠（保留根）
  if (p.length > 1 && p.endsWith('/')) p = p.slice(0, -1);
  return p;
}

/** 将路径段 PascalCase：admin/user → /Admin/User */
export function toSpaPath(raw: string): string {
  const key = normalizePathKey(raw);
  const segs = key.split('/').filter(Boolean);
  if (!segs.length) return '/home';
  return `/${segs.map((s) => toPascalCase(s)).join('/')}`;
}

/**
 * 将魔方 StartPage（常为 MVC）映射为 ArcoVue 路由。
 * 无匹配时：去掉 Info/Detail 等动作段后保留 Area/Controller；仍无效则 /home。
 */
export function mapStartPageToSpa(start: string): string {
  const key = normalizePathKey(start).toLowerCase();
  if (START_PAGE_EXACT[key]) return START_PAGE_EXACT[key];

  // cshtml / IndexController 等无效
  if (key.includes('.cshtml') || key.includes('indexcontroller')) return '/home';

  const orig = normalizePathKey(start);
  const segs = orig.split('/').filter(Boolean);
  if (!segs.length) return '/home';

  // 非 Admin 区：保留原路径（如 /object/Cube、/home）
  if (segs[0].toLowerCase() !== 'admin') return orig;

  // /Admin/User/Info → 已在 EXACT；其它 /Admin/Controller/Action → /Admin/Controller
  const last = segs[segs.length - 1].toLowerCase();
  let keep = segs;
  if (segs.length >= 3 && MVC_ACTION.has(last)) {
    keep = segs.slice(0, -1);
  } else if (segs.length === 2 && last === 'index') {
    return '/home';
  }

  if (keep.length < 2) return '/home';

  return toSpaPath('/' + keep.join('/'));
}

export function resolveStartPage(
  cfg: { startPage?: string | null; StartPage?: string | null } | null | undefined,
  redirect?: string | null,
): string {
  const q = (redirect || '').trim();
  if (q.startsWith('/') && !q.startsWith('//')) return q;

  const start = (cfg?.startPage || cfg?.StartPage || '').trim();
  if (!start.startsWith('/') && !start.startsWith('~/')) return '/home';
  if (start.startsWith('//')) return '/home';

  return mapStartPageToSpa(start);
}
