export function getLocalIcons() {
  const svgIcons = import.meta.glob('/src/assets/svg-icon/*.svg');

  const keys = Object.keys(svgIcons)
    .map(item => item.split('/').at(-1)?.replace('.svg', '') || '')
    .filter(Boolean);

  return keys;
}

/**
 * 图标值 → Iconify 名称
 *
 * 兼容三类菜单图标值：iconify 原名（ep:user）、Element Plus 图标名（User）、FontAwesome 类名（fa-user）。
 */
export function resolveIconName(value: unknown) {
  if (typeof value !== 'string') return '';
  const text = value.trim();
  if (!text) return '';
  if (text.includes(':')) return text;
  if (/^fa[\s-]/i.test(text)) return `fa:${text.replace(/^fa[\s-]+/i, '')}`;
  const kebab = text.replace(/([a-z0-9])([A-Z])/g, '$1-$2').replace(/[\s_]+/g, '-').toLowerCase();
  return `ep:${kebab}`;
}
