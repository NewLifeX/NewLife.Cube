/**
 * 后端服务控制器识别
 *
 * 魔方 WebAPI 版路由约定：
 * - 实体/后台控制器位于 {area}，固定带 /api 前缀，如 /api/Admin/User/Index
 * - 服务控制器（Auth/Sso/Mfa/OAuth/CubeController）不带 /api 前缀，如 /Auth/Login、/Sso/Login、/Cube/MenuTree
 * - 注意 Cube 区域实体控制器（/Cube/App、/Cube/CronJob 等）仍带 /api 前缀
 */

/** 服务控制器前缀（后端路由无 /api） */
const SERVICE_PREFIXES = ['/Auth/', '/Sso/', '/Mfa/', '/OAuth/'];

/** CubeController 服务动作（区别于 Cube 区域实体控制器，实体走 /api） */
const CUBE_SERVICE_ACTIONS = new Set([
  'Info',
  'Apis',
  'UserSearch',
  'DepartmentSearch',
  'GetArea',
  'AreaChilds',
  'AreaParents',
  'AreaAllParents',
  'Lookup',
  'SaveLayout',
  'GetAiConfig',
  'GetPageConfig',
  'SetPageConfig',
  'MenuTree',
  'Image',
  'File',
  'Setting',
  // CubeController 呈现/评论（无 /api；区域实体如 /api/Cube/App 仍带前缀）
  'UserProfile',
  'ViewProfile',
  'ViewProfileTemplate',
  'EntityComment',
  // 独立 AutomationController（[Route("Cube/Automation")]，无 /api）
  'Automation',
]);

/**
 * 由实体 baseURL 派生服务接口 baseURL
 *
 * 实体接口带 /api 路径前缀，服务接口（Auth/Cube/Sso/Mfa）不带：
 * - `http://host:7116/api` → `http://host:7116`（跨域部署，保留协议+主机+端口）
 * - `/api` 或 `''` → `''`（同域部署）
 *
 * @param baseUrl 实体 baseURL（如 '/api' 或 'http://host/api'）
 * @returns 服务接口 baseURL
 */
export function getServiceBaseUrl(baseUrl?: string): string {
  return (baseUrl ?? '').replace(/\/+$/, '').replace(/\/api$/i, '');
}

/**
 * 解析最终请求地址：合并 baseUrl（主机，可选带 /api 前缀）与相对 url，
 * 按「baseUrl 是否含 /api」与「url 是否为服务接口」统一去重 / 补 /api。
 *
 * 规则：
 * - baseUrl 含 /api（如 http://host:5000/api）：实体请求保留 /api、服务接口去掉 /api；
 *   url 自身若已带 /api 则去重，避免得到 http://host/api/api/...。
 * - baseUrl 不含 /api（如 http://host:5000，cube-vue 推荐只传纯主机）：
 *   服务接口不补前缀；非服务接口已带 /api 的不重复补、缺 /api 则补。
 * - 绝对地址（含协议或 //）原样返回。
 *
 * @param baseUrl 基础地址（如 'http://host:5000' 或 'http://host:5000/api'）
 * @param url 相对请求路径
 * @returns 完整请求地址
 */
export function resolveRequestUrl(baseUrl: string, url: string): string {
  // 绝对地址（含协议或 //）原样返回
  if (!url || /^(?:https?:)?\/\//i.test(url)) return url;
  const base = (baseUrl ?? '').replace(/\/+$/, '');
  const baseNoApi = base.replace(/\/api$/i, '');
  if (isServiceApiPath(url)) {
    return `${baseNoApi}${url}`;
  }
  const path = url.startsWith('/api') ? url.slice(4) : url;
  return `${baseNoApi}/api${path}`;
}

/**
 * 判断请求路径是否为服务接口（后端路由不带 /api 前缀）
 *
 * 用于前端请求层决定是否拼接 /api 前缀：服务接口直接请求，实体接口由 baseURL 提供 /api。
 *
 * @param url 请求路径，如 '/Auth/Login'、'/Cube/MenuTree'、'/Admin/User/Index'
 * @returns true 表示服务接口，无需拼接 /api 前缀
 */
export function isServiceApiPath(url: string): boolean {
  if (!url || /^(?:https?:)?\/\//i.test(url)) return false;

  if (SERVICE_PREFIXES.some((p) => url.startsWith(p))) return true;

  // Cube 区域实体与 CubeController 服务共用 /Cube 前缀，按动作名区分
  if (url.startsWith('/Cube/')) {
    const seg = (url.split('/')[2] ?? '').split(/[?/]/)[0];
    return CUBE_SERVICE_ACTIONS.has(seg);
  }

  return false;
}
