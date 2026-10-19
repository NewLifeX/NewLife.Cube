/**
 * 工作台部件数据辅助函数
 *
 * 后端 Widget 数据是弱类型（匿名对象/字典/字符串），页面按卡片类型做最小归一化。
 */

/** 任意值转为可展示文本。对象转 JSON，HTML 标签剥离，空值显示 - */
export function formatCell(value: unknown): string {
  if (value === null || value === undefined || value === '') return '-';
  if (typeof value === 'string') {
    const text = value.replace(/<[^>]+>/g, '').replace(/&nbsp;/g, ' ').trim();
    return text || '-';
  }
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

/**
 * 从后端 KV 值中提取链接
 *
 * SysInfo 等部件允许返回 `<a href="/Admin/Index/Main">查看完整服务器信息 »</a>`，
 * 前端不直接 v-html（避免把服务端字符串当 HTML 注入），只解析出 href/text 后用路由链接渲染。
 * @param value 原始值
 * @returns 链接信息；不是链接时返回 null
 */
export function extractLink(value: unknown): { href: string; text: string } | null {
  if (typeof value !== 'string') return null;

  const matched = value.match(/<a\s+[^>]*href=["']([^"']+)["'][^>]*>([\s\S]*?)<\/a>/i);
  if (!matched) return null;

  const href = matched[1].trim();
  const text = formatCell(matched[2]);
  if (!href || text === '-') return null;

  return { href, text };
}

/** 角色字段可能是逗号分隔字符串或数组，统一为数组 */
export function normalizeRoles(value: string | string[] | undefined | null): string[] {
  if (!value) return [];
  if (Array.isArray(value)) return value.filter(Boolean);

  return value
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean);
}

/** 是否为站外链接 */
export function isExternalLink(href: string): boolean {
  return /^https?:\/\//i.test(href) || href.startsWith('//');
}

/** 把未知值收窄为普通对象。数组/原始值返回空对象，避免模板里访问属性报错 */
export function asRecord(value: unknown): Record<string, unknown> {
  if (value && typeof value === 'object' && !Array.isArray(value)) return value as Record<string, unknown>;
  return {};
}

/** KPI 合法配色。后端 Color 属性只允许这些语义色 */
const KPI_COLORS = new Set(['blue', 'green', 'cyan', 'orange', 'red', 'purple', 'grey']);

/** 归一化 KPI 配色，未知值回退蓝色，避免把后端字符串直接拼进 class */
export function normalizeKpiColor(color?: string | null): string {
  const value = (color || '').trim().toLowerCase();
  return KPI_COLORS.has(value) ? value : 'blue';
}

/** 归一化 12 栅格宽度。缺省 6，越界收敛到 1-12 */
export function normalizeCols(cols?: number | null): number {
  if (!Number.isFinite(cols)) return 6;
  return Math.min(12, Math.max(1, Math.round(cols as number)));
}
