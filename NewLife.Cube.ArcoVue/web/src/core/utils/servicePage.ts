/**
 * 顶层服务控制器页（Auth/Sso/Mfa/AI/Automation/CubeController）。
 * 菜单可能挂在任意 Area 下（如 CubeDemo `[AreaBase("vTest1")]` → /vTest1/Auth），
 * 它们没有 GetPage / Object 契约，不能当实体或对象页探测。
 */

export type ServiceLeaf = 'auth' | 'sso' | 'mfa' | 'oauth' | 'ai' | 'automation' | 'cube';

const SERVICE_LEAVES: ReadonlySet<string> = new Set([
  'auth',
  'sso',
  'mfa',
  'oauth',
  'ai',
  'automation',
]);

function normalizePath(typePath: string): string {
  return typePath.replace(/^\/+/, '').replace(/\/+$/, '').toLowerCase();
}

function pathSegments(typePath: string): string[] {
  return normalizePath(typePath).split('/').filter(Boolean);
}

/**
 * 是否为服务控制器菜单路径。
 * Admin/Cube 是对象设置页，不走本短路；/Cube、/vTest1/Cube 才是魔方数据接口。
 */
export function isServiceControllerPath(typePath: string): boolean {
  const segs = pathSegments(typePath);
  if (!segs.length) return false;
  const leaf = segs[segs.length - 1];
  if (SERVICE_LEAVES.has(leaf)) return true;
  if (leaf === 'cube' && segs[0] !== 'admin') {
    return segs.length === 1 || segs.length === 2;
  }
  return false;
}

/** 归一末段控制器名；非服务路径返回 null */
export function parseServiceLeaf(typePath: string): ServiceLeaf | null {
  if (!isServiceControllerPath(typePath)) return null;
  const segs = pathSegments(typePath);
  const leaf = segs[segs.length - 1];
  return leaf as ServiceLeaf;
}

export interface ServicePageLink {
  label: string;
  path: string;
}

export interface ServicePageGuide {
  title: string;
  summary: string;
  links: ServicePageLink[];
  endpoints: string[];
}

const GUIDES: Record<ServiceLeaf, ServicePageGuide> = {
  auth: {
    title: '认证',
    summary: '统一认证入口（/Auth/*），为 SPA 提供登录、刷新令牌、注册等接口，不是实体列表页。登录后请到账号中心管理资料与会话。',
    links: [
      { label: '账号中心', path: '/account' },
      { label: '登录页', path: '/login' },
    ],
    endpoints: ['POST /Auth/Login', 'POST /Auth/Refresh', 'GET /Auth/Info', 'POST /Auth/Register'],
  },
  sso: {
    title: '单点登录',
    summary: 'OAuth2 / SSO 服务端与客户端接口（/Sso/*）。绑定与解绑在账号中心完成，不在本页做实体 CRUD。',
    links: [{ label: '账号绑定', path: '/account?tab=bind' }],
    endpoints: ['GET /Sso/Login', 'GET /Sso/Authorize', 'POST /Sso/Token', 'GET /Sso/UserInfo'],
  },
  mfa: {
    title: '多因素认证',
    summary: 'TOTP 开通、激活、禁用与登录二步验证（/Mfa/*）。请在账号安全页操作，无需单独的实体页。',
    links: [{ label: '账号安全', path: '/account?tab=security' }],
    endpoints: ['GET /Mfa/Setup', 'POST /Mfa/Activate', 'POST /Mfa/Disable', 'POST /Mfa/Verify'],
  },
  oauth: {
    title: 'OAuth',
    summary: 'OAuth 回调与令牌相关接口。第三方绑定请到账号中心。',
    links: [{ label: '账号绑定', path: '/account?tab=bind' }],
    endpoints: ['GET /OAuth/Callback'],
  },
  ai: {
    title: 'AI',
    summary: '全局 AI 对话入口为 /Ai/AiChat（SSE）。各业务页通过右下角浮窗使用，不按实体列表打开。',
    links: [{ label: '返回首页', path: '/home' }],
    endpoints: ['POST /Ai/AiChat', 'GET /Cube/GetAiConfig'],
  },
  automation: {
    title: '实体自动化',
    summary: '自动化流程 API（/Cube/Automation）。规则挂在具体实体上，请打开用户、角色等实体列表，使用工具栏「自动化」。',
    links: [
      { label: '用户', path: '/Admin/User' },
      { label: '返回首页', path: '/home' },
    ],
    endpoints: ['GET /Cube/Automation', 'POST /Cube/Automation', 'GET /Cube/Automation/Inbox'],
  },
  cube: {
    title: '魔方数据接口',
    summary: '向前端提供菜单、地区、查找、呈现配置等常用接口（/Cube/{action}），不是 Cube 区域实体表，也不是「魔方设置」。',
    links: [
      { label: '魔方设置', path: '/Admin/Cube' },
      { label: '返回首页', path: '/home' },
    ],
    endpoints: [
      'GET /Cube/Info',
      'GET /Cube/MenuTree',
      'GET /Cube/GetArea',
      'GET /Cube/Lookup',
      'GET /Cube/UserProfile',
    ],
  },
};

export function resolveServicePageGuide(typePath: string): ServicePageGuide | null {
  const leaf = parseServiceLeaf(typePath);
  return leaf ? GUIDES[leaf] : null;
}
