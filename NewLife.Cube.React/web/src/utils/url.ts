/**
 * URL 与取值工具（对齐 Vue 皮肤 core/utils/url.ts）
 */

/** 字符串转 PascalCase */
export function toPascalCase(str: string): string {
  if (!str) return '';
  return str
    .split(/[-_/\s]+/)
    .filter(Boolean)
    .map((s) => s.charAt(0).toUpperCase() + s.slice(1))
    .join('');
}

/** 字符串转小驼峰 */
export function toCamelCase(str: string): string {
  if (!str) return '';
  const s = str.replace(/[-_](\w)/g, (_, c: string) => (c ? c.toUpperCase() : ''));
  return s.charAt(0).toLowerCase() + s.slice(1);
}

/** 翻转首字母大小写（PascalCase ↔ camelCase） */
export function toPascalAndCamel(str: string): string {
  if (!str) return '';
  return str.charAt(0).toUpperCase() + str.slice(1);
}

/**
 * 路由路径 → API 前缀
 *
 * 示例：/device/device-profile → /Device/DeviceProfile
 * （实体接口由 api-core 自动补 /api 前缀，这里只返回实体路径前缀）
 */
export function routeToApiPrefix(path: string): string {
  return path
    .split('/')
    .filter(Boolean)
    .map(toPascalCase)
    .join('/');
}

/**
 * 从数据对象中取值：先试 data[key]，再翻转首字母（PascalCase ↔ camelCase），
 * 全大写 key（如 ID）转全小写再试，容错后端 JSON 字段名大小写不匹配。
 */
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

/**
 * URL 变量替换：将 `/path/{Id}` 替换为 `/path/123`
 *
 * @param url 含变量占位符的 URL 模板
 * @param row 数据行对象
 */
export function resolveUrl(url: string, row: Record<string, unknown>): string {
  return url.replace(/\{(\w+)\}/g, (_, key: string) => {
    const val = getValueByKey(row, key);
    return val !== undefined && val !== null ? encodeURIComponent(String(val)) : '';
  });
}

export default { toPascalCase, toCamelCase, routeToApiPrefix, getValueByKey, resolveUrl };
