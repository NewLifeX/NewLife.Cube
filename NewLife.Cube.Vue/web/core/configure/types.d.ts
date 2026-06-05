import type { AxiosRequestConfig } from 'axios';

// 基础配置
export interface BaseConfig {
  title: string;
  logo?: string;
  footer?: string;
  env?: 'dev' | 'production' | 'test';
}

// 菜单相关配置
export interface MenuConfig {
  getMenuAxiosConfig:
  | AxiosRequestConfig
  | (() => AxiosRequestConfig)
  | (() => Promise<AxiosRequestConfig>);
  isMenuTree: boolean;
  dataKey: string;
  idField: string;
  parentField: string;
  nameField: string;
  pathField: string;
  titleField: string;
  iconField: string;
  sortField: string;
  childrenField: string;
}

// 用户相关配置
export interface UserConfig {
  getUserInfoAxiosConfig:
  | AxiosRequestConfig
  | (() => AxiosRequestConfig)
  | (() => Promise<AxiosRequestConfig>);
}

// UI相关配置
export interface UIConfig {
  layout: {
    header: {
      show: boolean;
      fixed: boolean;
      theme: 'light' | 'dark';
      height?: number;
    };
    sider?: {
      show?: boolean;
      collapsible?: boolean;
      defaultCollapsed?: boolean;
      width?: number;
      collapsedWidth?: number;
      theme?: 'light' | 'dark';
    };
    footer?: {
      show?: boolean;
      fixed?: boolean;
    };
  };
  theme?: {
    primaryColor?: string;
    linkColor?: string;
    successColor?: string;
    warningColor?: string;
    errorColor?: string;
    font?: {
      baseSize?: number;
      family?: string;
    };
  };
}

// API相关配置
export interface RequestConfig {
  /** 基础URL */
  baseUrl: string;
  /** 请求超时时间 */
  timeout?: number;
  /** 额外请求头 */
  additionalRequestHeader?: Recore<string, string> | (() => Recore<string, string>);
  /** 响应拦截器 */
  responseIntercept?: (response: AxiosResponse) => void;
}

// 认证相关配置
export interface AuthConfig {
  tokenKey: string;
  oauthUrl: string;
  redirectUrl?: string;
  pageTitle?: string;
  background?: string;
  logoutAxiosConfig?:
  | AxiosRequestConfig
  | (() => AxiosRequestConfig)
  | (() => Promise<AxiosRequestConfig>);
  reLoginParams?: {
    titleIntlCode?: string;
    titleIntlDefault?: string;
    messageIntlCode?: string;
    messageIntlDefault?: string;
    okTextIntlCode?: string;
    okTextIntlDefault?: string;
    cancelTextIntlCode?: string;
    cancelTextIntlDefault?: string;
    loginPageUrl?: string;
    cancelText?: string;
    onModalShow?: () => void;
    onOk?: () => void;
    onCancel?: () => void;
    noticeMethod?: Array<() => void>;
    isShow?: boolean;
    isUseCustomizeModal?: boolean;
    customizeModalProps?: Recore<string, unknown>;
  };
}

// 总配置
export interface CubeFrontConfig {
  base: BaseConfig;
  ui: UIConfig;
  request: RequestConfig;
  auth: AuthConfig;
  menu: MenuConfig;
  user: UserConfig;
}

// 环境配置类型 - 所有字段都是可选的深度部分类型
export type EnvConfig = {
  base?: Partial<BaseConfig>;
  ui?: Partial<UIConfig>;
  request?: Partial<RequestConfig>;
  auth?: Partial<AuthConfig>;
  menu?: Partial<MenuConfig>;
  user?: Partial<UserConfig>;
};
