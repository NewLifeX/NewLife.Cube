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

/** 容错赋值（大小写）：与 getValueByKey 对称，找到实际键后写入；找不到则按原键写入 */
export function setValueByKey(data: Record<string, unknown>, key: string, value: unknown): void {
  if (key in data) {
    data[key] = value;
    return;
  }
  const flipped = toPascalAndCamel(key);
  if (flipped !== key && flipped in data) {
    data[flipped] = value;
    return;
  }
  if (key === key.toUpperCase() && key !== key.toLowerCase()) {
    const lowerKey = key.toLowerCase();
    if (lowerKey in data) {
      data[lowerKey] = value;
      return;
    }
  }
  if (key === key.toLowerCase() && /[a-z]/.test(key)) {
    const upperKey = key.toUpperCase();
    if (upperKey in data) {
      data[upperKey] = value;
      return;
    }
  }
  data[key] = value;
}

/**
 * 按字段元数据把数据键归一化到 FieldMeta.name（PascalCase）。
 * GetPage 返回字段名为 PascalCase，而 GetList/Detail 返回数据为 camelCase；
 * 编辑表单用 model[field.name] 直接索引，若不归一化将取不到值导致内容为空。
 */
export function normalizeKeysByFields(
  data: Record<string, unknown>,
  fields: { name: string }[],
): Record<string, unknown> {
  const out: Record<string, unknown> = {};
  for (const f of fields) {
    const v = getValueByKey(data, f.name);
    if (v !== undefined) out[f.name] = v;
  }
  return out;
}
