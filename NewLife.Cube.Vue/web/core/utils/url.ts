import qs from 'query-string';

/**
 * 翻转字符串首字母大小写。
 * PascalCase 转 camelCase（如 `CreateUserID` → `createUserID`），
 * camelCase 转 PascalCase（如 `createUserID` → `CreateUserID`）。
 * 用于容错后端 JSON 序列化时大小写不匹配的情况。
 */
export function toPascalAndCamel(str: string): string {
  if (!str) return str;
  return str.charAt(0).toUpperCase() === str.charAt(0)
    ? str.charAt(0).toLowerCase() + str.slice(1)
    : str.charAt(0).toUpperCase() + str.slice(1);
}

/**
 * 将短横线命名转为大驼峰命名
 * 示例: my-device-name → MyDeviceName
 */
export function toPascalCase(str: string): string {
  if (!str) return str;
  return str
    .split('-')
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join('');
}

/**
 * 将 PascalCase/camelCase 转为短横线风格
 * 例: MyDeviceName → my-device-name
 */
export function toKebabCase(str: string) {
  return str
    .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
    .replace(/([A-Z])([A-Z][a-z])/g, '$1-$2')
    .toLowerCase();
}

/**
 * 将后端返回的菜单 URL 路径转换为短横线风格
 * 示例:
 *   /IoT/Device/Product → /iot/device/product
 *   /EMS/EnergyReport → /ems/energy-report
 *
 * @param url 后端返回的原始 URL
 * @returns 短横线风格的 URL
 */
export function normalizeMenuUrl(url: string): string {
  if (!url || typeof url !== 'string') return url;

  // 分离路径和查询参数
  const [path, query] = url.split('?');

  // 转换路径段为短横线风格
  const normalizedPath = path
    .split('/')
    .filter(Boolean)
    .map((segment) => toKebabCase(segment))
    .join('/');

  // 重新拼接路径（保留前导斜杠）
  const result = '/' + normalizedPath;

  // 附加查询参数
  return query ? `${result}?${query}` : result;
}

/**
 * 根据路由路径获取 api 前缀
 * 示例：/device/device-profile → /Device/DeviceProfile
 */
export function routeToApiPrefix(path: string): string {
  return '/' + path.split('/').filter(Boolean).map(toPascalCase).join('/');
}

/**
 * 从数据对象中取值，先尝试 `data[key]` 直接获取，
 * 取不到时通过 `toPascalAndCamel(key)` 翻转首字母再取一次，
 * 如果 key 是全大写字母（如 ID、UUID），再尝试全小写（id、uuid），
 * 容错后端 JSON 字段名大小写不匹配问题。
 */
export function getValueByKey(data: Record<string, unknown>, key: string): unknown {
  if (key in data) return data[key];
  // 翻转首字母再试（容错 PascalCase ↔ camelCase）
  const flipped = toPascalAndCamel(key);
  if (flipped !== key && flipped in data) return data[flipped];
  // 如果 key 是全大写，转成全小写再试（如 ID → id, UUID → uuid）
  if (key === key.toUpperCase() && key !== key.toLowerCase()) {
    const lowerKey = key.toLowerCase();
    if (lowerKey in data) return data[lowerKey];
  }
  // 如果 key 是全小写且包含字母，试试全大写（如 id → ID）
  if (key === key.toLowerCase() && /[a-z]/.test(key)) {
    const upperKey = key.toUpperCase();
    if (upperKey in data) return data[upperKey];
  }
  return undefined;
}

export function isUrl(path: string) {
  /* eslint no-useless-escape:0 */
  const reg =
    /(((^https?:(?:\/\/)?)(?:[-;:&=\+\$,\w]+@)?[A-Za-z0-9.-]+|(?:www.|[-;:&=\+\$,\w]+@)[A-Za-z0-9.-]+)(:[\d]+)?((?:\/[\+~%\/.\w-_]*)?\??(?:[-\+=&;%@.\w_]*)#?(?:[\w]*))?)$/g;
  return reg.test(path);
}

/**
 * 生成带Get参数的URL
 * @param {String} url      原来的url
 * @param {Object} params   get 参数
 */
export function generateUrlWithGetParam(url: string, params: {}) {
  let newUrl = url;
  if (params && Object.keys(params).length >= 1) {
    const newParams = params; // filterNullValueObject
    if (Object.keys(newParams).length >= 1) {
      newUrl += `${url.indexOf('?') >= 0 ? '&' : '?'}${qs.stringify(newParams)}`;
    }
  }
  return newUrl;
}

/**
 * 得到get请求后面的参数部分,并去掉参数值为空的
 * @param param
 * @returns {String}
 */
export function getUrlParam(param: { [x: string]: any; }) {
  let on = true;
  let result = '';
  for (const item in param) {
    if (on) {
      on = false;
      if (param[item] || param[item] === 0 || param[item] === false) {
        result = `?${item}=${param[item]}`;
      } else {
        result = '?';
      }
    } else if (param[item] || param[item] === 0 || param[item] === false) {
      result = `${result}&${item}=${param[item]}`;
    }
  }
  return result;
}
