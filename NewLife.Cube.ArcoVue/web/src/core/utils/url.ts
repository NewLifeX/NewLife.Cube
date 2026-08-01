/** PascalCase ↔ camelCase 首字母翻转 */
export function toPascalAndCamel(str: string): string {
  if (!str) return str;
  return str.charAt(0).toUpperCase() === str.charAt(0)
    ? str.charAt(0).toLowerCase() + str.slice(1)
    : str.charAt(0).toUpperCase() + str.slice(1);
}

/** my-device → MyDevice */
export function toPascalCase(str: string): string {
  if (!str) return str;
  return str
    .split('-')
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join('');
}

/** MyDeviceName → my-device-name */
export function toKebabCase(str: string): string {
  return str
    .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
    .replace(/([A-Z])([A-Z][a-z])/g, '$1-$2')
    .toLowerCase();
}

export type RouteNamingStyle = 'pascal' | 'kebab';

/** 规范化菜单 URL（默认保持 Pascal 段） */
export function normalizeMenuUrl(url: string, style: RouteNamingStyle = 'pascal'): string {
  if (!url || typeof url !== 'string') return url;
  const [path, query] = url.split('?');
  const normalizedPath = path
    .split('/')
    .filter(Boolean)
    .map((segment) => (style === 'kebab' ? toKebabCase(segment) : segment))
    .join('/');
  const result = '/' + normalizedPath;
  return query ? `${result}?${query}` : result;
}

/** /admin/user → /Admin/User */
export function routeToApiPrefix(path: string): string {
  return (
    '/' +
    path
      .split('/')
      .filter(Boolean)
      .map(toPascalCase)
      .join('/')
  );
}

/** 容错取值（大小写） */
export function getValueByKey(data: Record<string, unknown>, key: string): unknown {
  if (key in data) return data[key];
  const flipped = toPascalAndCamel(key);
  if (flipped !== key && flipped in data) return data[flipped];
  if (key === key.toUpperCase() && key !== key.toLowerCase()) {
    const lowerKey = key.toLowerCase();
    if (lowerKey in data) return data[lowerKey];
  }
  if (key === key.toLowerCase() && /[a-z]/.test(key)) {
    const upperKey = key.toUpperCase();
    if (upperKey in data) return data[upperKey];
  }
  return undefined;
}
