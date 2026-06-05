/**
 * 深度合并对象
 * @param target 目标对象
 * @param source 源对象
 * @returns 合并后的对象
 */
export function deepMerge<T extends object = object>(target: T, source: any): T {
  const result = { ...target };

  if (isObject(target) && isObject(source)) {
    for (const key in source) {
      if (isObject(source[key])) {
        if (!result[key]) {
          Object.assign(result, { [key]: {} });
        }
        result[key] = deepMerge(result[key], source[key]);
      } else {
        Object.assign(result, { [key]: source[key] });
      }
    }
  }

  return result;
}

/**
 * 判断是否为对象
 * @param item 待检查项
 * @returns 是否为对象
 */
export function isObject(item: any): boolean {
  return item && typeof item === 'object' && !Array.isArray(item);
}