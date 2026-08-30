/**
 * 配置类型定义（对齐 Vue 皮肤 configure/types.d.ts，按 React 皮肤需要精简）
 */

/** 站点基础配置 */
export interface BaseConfig {
  /** 系统名称 */
  title: string;
  /** Logo 地址（空则用皮肤内置默认） */
  logo?: string;
  /** 页脚版权 */
  footer?: string;
  /** 环境 */
  env?: 'dev' | 'production' | 'test';
}

/** 请求配置 */
export interface RequestConfig {
  /** API 基础地址（可含 /api 前缀或纯主机），api-core 按 resolveRequestUrl 规则统一拼接 */
  baseUrl: string;
  /** 请求超时（毫秒） */
  timeout?: number;
  /** Token 存储方式 */
  tokenStorage?: 'cookie' | 'localStorage';
  /** Authorization 头前缀（本后端要求 'Bearer '） */
  tokenHeaderPrefix?: string;
  /** 附加请求头（静态对象或返回对象的函数，如多租户 X-Tenant） */
  additionalRequestHeaders?: Record<string, string> | (() => Record<string, string>);
}

/** 认证配置 */
export interface AuthConfig {
  /** 登录页路径 */
  loginPageUrl: string;
  /** 登录后回跳 URL 的 query 参数名 */
  redirectKey: string;
}

/** 布局配置 */
export interface LayoutConfig {
  header: {
    show: boolean;
    fixed: boolean;
    theme: 'light' | 'dark';
    height?: number;
  };
  sider: {
    show: boolean;
    collapsible: boolean;
    defaultCollapsed: boolean;
    width: number;
    collapsedWidth: number;
    theme: 'light' | 'dark';
  };
  footer: {
    show: boolean;
    fixed: boolean;
  };
}

/** UI 配置 */
export interface UIConfig {
  layout: LayoutConfig;
}

/** 菜单配置（后端菜单字段映射） */
export interface MenuConfig {
  idField: string;
  parentField: string;
  nameField: string;
  pathField: string;
  titleField: string;
  iconField: string;
  sortField: string;
  childrenField: string;
  /** 可见性字段名，该字段值为 false 时菜单项及其子树不显示 */
  visibleField?: string;
}

/** 总配置 */
export interface CubeConfig {
  base: BaseConfig;
  request: RequestConfig;
  auth: AuthConfig;
  ui: UIConfig;
  menu: MenuConfig;
  theme: {
    /** 默认明暗模式 */
    defaultMode: 'light' | 'dark';
    /** 默认主色（antd token） */
    primaryColor: string;
  };
}

/** 环境配置类型（所有字段可选） */
export type EnvConfig = {
  base?: Partial<BaseConfig>;
  request?: Partial<RequestConfig>;
  auth?: Partial<AuthConfig>;
  ui?: Partial<UIConfig>;
  menu?: Partial<MenuConfig>;
  theme?: Partial<CubeConfig['theme']>;
};
