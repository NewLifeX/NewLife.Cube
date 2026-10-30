/**
 * 深度合并对象
 * @param target 目标对象
 * @param source 源对象
 * @returns 合并后的对象
 */
export function deepMerge<T extends object = object>(target: T, source: any): T {
  const result: Record<string, any> = { ...target };

  if (isObject(target) && isObject(source)) {
    for (const key in source) {
      // 仅当两侧都是普通对象时才递归合并；target 侧是函数/非对象时（如 defaultConfig
      // 的 getUserInfoAxiosConfig 函数被项目对象覆盖），直接以 source 值覆盖。
      // 否则 deepMerge(函数, 对象) 会因 {...函数}={} 且 isObject(函数)=false 退化为空对象，两边数据全丢。
      if (isObject(source[key]) && isObject(result[key])) {
        result[key] = deepMerge(result[key], source[key]);
      } else {
        Object.assign(result, { [key]: source[key] });
      }
    }
  }

  return result as T;
}

/**
 * 判断是否为对象
 * @param item 待检查项
 * @returns 是否为对象
 */
export function isObject(item: any): boolean {
  return item && typeof item === 'object' && !Array.isArray(item);
}