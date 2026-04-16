/** 魔方标准 API 响应结构 */
export interface ApiResponse<T = unknown> {
  /** 状态码，0 表示成功 */
  code: number;
  /** 数据 */
  data: T;
  /** 消息 */
  message?: string;
  /** 分页信息 */
  page?: PageInfo;
  /** 统计行数据 */
  stat?: Record<string, unknown>;
}

/** 分页信息 */
export interface PageInfo {
  /** 页码（从 0 开始） */
  pageIndex: number;
  /** 每页大小 */
  pageSize: number;
  /** 总记录数 */
  totalCount: number;
  /** 长整型总数（字符串） */
  longTotalCount?: string;
}

/** 分页查询参数 */
export interface PageParams {
  /** 页码（从 0 开始） */
  pageIndex?: number;
  /** 每页大小 */
  pageSize?: number;
  /** 排序字段 */
  sort?: string;
  /** 是否降序 */
  desc?: boolean;
  /** 其它搜索参数 */
  [key: string]: unknown;
}

/** 字段元数据（对应后端 DataField） */
export interface DataField {
  /** 字段名 */
  name: string;
  /** 显示名称 */
  displayName?: string;
  /** 说明 */
  description?: string;
  /** 分类（表单分组） */
  category?: string;
  /** .NET 类型名（Int32/String/Boolean/DateTime 等） */
  typeName?: string;
  /** UI 控件类型（mail/mobile/image/file 等特殊字段） */
  itemType?: string;
  /** 长度 */
  length?: number;
  /** 精度 */
  precision?: number;
  /** 小数位 */
  scale?: number;
  /** 是否主键 */
  primaryKey?: boolean;
  /** 是否允许空 */
  nullable?: boolean;
  /** 是否只读 */
  readOnly?: boolean;
  /** 是否必填 */
  required?: boolean;
  /** 是否可见 */
  visible?: boolean;
  /** 映射字段名 */
  mapField?: string;
  /** 链接 URL（支持变量替换 {Id} 等） */
  url?: string;
  /** 链接目标 */
  target?: string;
  /** 数据动作（AJAX POST） */
  dataAction?: string;
  /** 表头文字 */
  header?: string;
  /** 最大宽度 */
  maxWidth?: number;
  /** 文本对齐 */
  textAlign?: string;
  /** 数据字典（键值对列表） */
  dataSource?: Record<string, string>;
}

/** 页面设置（GetPage.data.setting） */
export interface PageSetting {
  navView?: string;
  enableNavbar?: boolean;
  enableToolbar?: boolean;
  enableAdd?: boolean;
  enableKey?: boolean;
  enableSelect?: boolean;
  enableFooter?: boolean;
  isReadOnly?: boolean;
  enableTableDoubleClick?: boolean;
  orderByKey?: boolean;
  doubleDelete?: boolean;
}

/** GetPage 聚合元数据 */
export interface PageMeta {
  /** 新结构：页面设置 */
  setting?: PageSetting;
  /** 兼容旧结构 */
  pageSetting?: PageSetting;

  /** 新结构：扁平字段集合 */
  list?: DataField[];
  addForm?: DataField[];
  editForm?: DataField[];
  detail?: DataField[];
  search?: DataField[];

  /** 兼容旧结构：fields 嵌套 */
  fields?: {
    list?: DataField[];
    form?: {
      addForm?: DataField[];
      editForm?: DataField[];
      detail?: DataField[];
    };
    search?: DataField[];
  };
}

/** GetFields 的 kind 参数枚举 */
export enum FieldKind {
  /** 列表字段 */
  List = 1,
  /** 详情字段 */
  Detail = 2,
  /** 新增字段 */
  Add = 3,
  /** 编辑字段 */
  Edit = 4,
  /** 搜索字段 */
  Search = 5,
}

/** 用户信息 */
export interface UserInfo {
  id: number;
  name: string;
  displayName: string;
  sex?: string;
  mail?: string;
  mobile?: string;
  avatar?: string;
  roleID?: number;
  roleName?: string;
  online?: boolean;
  enable?: boolean;
  logins?: number;
  lastLogin?: string;
  lastLoginIP?: string;
  permission?: string;
  remark?: string;
}

/** 登录返回 */
export interface LoginResult {
  /** 访问令牌 */
  accessToken: string;
  /** 刷新令牌 */
  refreshToken?: string;
  /** 过期时间（秒） */
  expireIn?: number;
}

/** OAuth 提供商 */
export interface OAuthProvider {
  name: string;
  logo?: string;
  nickName?: string;
}

/** 登录配置 */
export interface LoginConfig {
  displayName?: string;
  logo?: string;
  allowLogin?: boolean;
  allowRegister?: boolean;
  enableSms?: boolean;
  enableMail?: boolean;
  enableSmsRegister?: boolean;
  enableMailRegister?: boolean;
  loginTip?: string;
  providers?: OAuthProvider[];
}

/** 站点信息 */
export interface SiteInfo {
  displayName?: string;
  copyright?: string;
  registration?: string;
  loginTip?: string;
  logo?: string;
  /** 登录页 Logo，空则使用皮肤内置默认 */
  loginLogo?: string;
  /** 登录页左侧背景图，空则使用皮肤内置默认 */
  loginBackground?: string;
}

/** 注册参数 */
export enum RegisterCategory {
  /** 用户名密码注册 */
  Password = 0,
  /** 手机验证码注册 */
  Phone = 1,
  /** 邮箱验证码注册 */
  Email = 2,
  /** OAuth 回跳后绑定注册 */
  OAuthBind = 3,
}

/** 注册参数 */
export interface RegisterModel {
  registerCategory?: RegisterCategory;
  username?: string;
  email?: string;
  mobile?: string;
  password: string;
  confirmPassword?: string;
  password2?: string;
  code?: string;
  oauthToken?: string;
}

/** OAuth 回跳待注册信息 */
export interface OAuthPendingInfo {
  provider?: string;
  username?: string;
  email?: string;
  mobile?: string;
  avatar?: string;
}

/** 重置密码参数（忘记密码流程） */
export interface ResetPasswordModel {
  /** 手机号或邮箱 */
  username: string;
  /** 短信/邮件验证码 */
  code: string;
  /** 新密码 */
  newPassword: string;
  /** 确认密码 */
  confirmPassword: string;
}

/**
 * Challenge-Response 安全登录挑战响应
 *
 * 调用 GET /Auth/Challenge 返回此对象。
 * 前端用 publicKey 以 RSA-OAEP/SHA-256 加密原始密码，
 * 再携带 challengeId + 加密密文提交 POST /Auth/Login。
 */
export interface ChallengeResult {
  /** 挑战标识，登录时原样传回 LoginModel.challengeId 字段 */
  challengeId: string;
  /** PEM(SPKI) 格式 RSA 公钥，用于 Web Crypto importKey('spki', ...) */
  publicKey: string;
}

/** 菜单树节点 */
export interface MenuItem {
  id: number;
  name: string;
  displayName: string;
  parentID: number;
  url: string;
  icon?: string;
  visible: boolean;
  newWindow?: boolean;
  permissions?: Record<string, string>;
  children: MenuItem[];
}

/** 权限码常量（2 的幂） */
export const Auth = {
  /** 查看 */
  VIEW: 1,
  /** 新增 */
  ADD: 2,
  /** 编辑 */
  EDIT: 4,
  /** 删除 */
  DELETE: 8,
  /** 导出 */
  EXPORT: 16,
  /** 导入 */
  IMPORT: 32,
} as const;
