/** 字段级验证错误 */
export interface FieldError {
  /** 字段名（与前端表单字段对应） */
  field: string;
  /** 错误消息，例如"XX字段不可以为空" */
  message: string;
}

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
  /** 跟踪编号 */
  traceId?: string;
  /** 字段级验证错误列表（新增/编辑/删除时校验失败返回） */
  fieldErrors?: FieldError[];
}

/** 包含完整 API 响应的业务错误，用于 try-catch 中提取字段级错误 */
export class ApiError<T = unknown> extends Error {
  /** API 响应码 */
  code: number;
  /** 字段级验证错误 */
  fieldErrors?: FieldError[];
  /** 完整响应数据 */
  response: ApiResponse<T>;

  constructor(response: ApiResponse<T>) {
    super(response.message ?? `API error: ${response.code}`);
    this.name = 'ApiError';
    this.code = response.code ?? -1;
    this.fieldErrors = response.fieldErrors;
    this.response = response;
  }
}

/** 分页信息 */
export interface PageInfo {
  /** 页码（从 1 开始） */
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
  /** 页码（从 1 开始，默认 1；第一页可不传） */
  pageIndex?: number;
  /** 每页大小（默认 20，恰为 20 可不传） */
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
  /** 取值字段。列表单元格优先取该字段值，为空时回退到本字段（如名称列优先显示昵称：Name.valueField=DisplayName） */
  valueField?: string;
  /** 链接 URL（支持变量替换 {Id} 等） */
  url?: string;
  /** 链接目标 */
  target?: string;
  /** 链接文字。列表单元格有 url 时优先显示该文字（如「追踪」），为空回退显示字段值 */
  text?: string;
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
  /** 值集编码（枚举 / singleSelect / multipleSelect 由后端静态构造下发，前端绝不硬编码） */
  lovCode?: string;
  /** 是否多选（multipleSelect 为 true；亦可由前端在映射时推导） */
  multiple?: boolean;
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
  /** 全部可用列表字段（应用用户列配置前，供列设置面板） */
  allList?: DataField[];
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
  /** 邮箱已验证（安全中心展示验证状态） */
  mailVerified?: boolean;
  /** 手机已验证（安全中心展示验证状态） */
  mobileVerified?: boolean;
  /** 生日（用户中心编辑资料展示） */
  birthday?: string;
  logins?: number;
  lastLogin?: string;
  lastLoginIP?: string;
  /** 注册时间 */
  registerTime?: string;
  permission?: string;
  remark?: string;

  // === 多租户 ===
  /** 是否启用多租户。false 时隐藏租户相关 UI */
  enableTenant?: boolean;
  /** 当前租户编号。0=管理后台，>0=租户；未开启多租户恒为 0 */
  tenantId?: number;
  /** 当前租户编码。管理后台或未开启多租户为空 */
  tenantCode?: string;
  /** 当前租户名称。管理后台或未开启多租户为空 */
  tenantName?: string;
  /** 租户模式。0=未设置，1=管理后台，2=租户 */
  tenantMode?: number;
  /** 是否系统管理员。可进入管理后台，可切换任意租户 */
  isSystemAdmin?: boolean;
  /** 当前用户所属有效租户列表（租户切换器数据源） */
  tenants?: TenantItem[];
}

/** 租户切换器选项 */
export interface TenantItem {
  /** 租户编号 */
  id: number;
  /** 租户编码 */
  code?: string;
  /** 租户名称 */
  name?: string;
}

/** 登录返回 */
export interface LoginResult {
  /** 访问令牌 */
  accessToken: string;
  /** 刷新令牌 */
  refreshToken?: string;
  /** 过期时间（秒） */
  expireIn?: number;
  /** 是否待激活。true 表示注册成功但需先激活邮箱/手机才能登录（此时 accessToken 为空） */
  pendingActivation?: boolean;
  /** 已发送激活的渠道列表。mail/sms */
  channels?: string[];
  /** 对应渠道的脱敏目标 */
  targets?: string[];

  // ---- 后端字段别名（由 client.ts normalizeLoginResult 统一归一化，请勿直接读取） ----
  /** @internal 后端 snake_case 兼容字段，请使用 accessToken */
  access_token?: string;
  /** @internal 后端 PascalCase 兼容字段，请使用 accessToken */
  Token?: string;
  /** @internal 后端 snake_case 兼容字段，请使用 refreshToken */
  refresh_token?: string;
  /** @internal 后端 PascalCase 兼容字段，请使用 refreshToken */
  RefreshToken?: string;
  /** @internal 后端 snake_case 兼容字段，请使用 expireIn */
  expire_in?: number;
  /** @internal 后端 PascalCase 兼容字段，请使用 expireIn */
  ExpireIn?: number;
}

/** OAuth 提供商 */
export interface OAuthProvider {
  name: string;
  logo?: string;
  nickName?: string;
}

/** 登录配置 */
/** 登录能力配置 */
export interface LoginAbility {
  /** 允许密码登录 */
  password?: boolean;
  /** 允许短信验证码登录 */
  sms?: boolean;
  /** 允许邮箱验证码登录 */
  mail?: boolean;
  /** 登录时需要图片验证码 */
  captcha?: boolean;
}

/** 注册能力配置 */
export interface RegisterAbility {
  /** 是否允许注册 */
  enabled?: boolean;
  /** 允许用户名密码注册 */
  password?: boolean;
  /** 允许手机验证码注册 */
  sms?: boolean;
  /** 允许邮箱验证码注册 */
  mail?: boolean;
  /** 注册时需要图片验证码 */
  captcha?: boolean;
  /** 需要邮箱验证。注册后必须激活邮箱才能登录，注册表单须强制填写邮箱 */
  requireMailVerify?: boolean;
  /** 需要手机验证。注册后必须激活手机才能登录，注册表单须强制填写手机 */
  requireMobileVerify?: boolean;
}

/** 安全策略配置 */
export interface SecurityConfig {
  /** 是否要求 Challenge-Response 加密传输密码 */
  challengeRequired?: boolean;
  /** 是否开放 MFA 功能 */
  mfaAvailable?: boolean;
  /** 是否启用密码复杂度校验。false 时登录页仅要求密码非空，不做复杂度校验 */
  passwordComplexity?: boolean;
  /** 密码强度正则。* 表示无限制，客户端可用于校验提示 */
  passwordStrength?: string;
}

/** 登录配置（新版嵌套结构，v2 起） */
export interface LoginConfig {
  /** 是否启用多租户。前端据此控制租户相关 UI（切换器/租户字段）显隐 */
  enableTenant?: boolean;
  /** 租户 Code，有租户时非空 */
  code?: string;
  /** 系统名称（租户级优先） */
  name?: string;
  /** 版权信息，服务端已替换 {now:yyyy} 等变量，前端直接渲染（支持 HTML） */
  copyright?: string;
  /** 备案号 */
  registration?: string;
  /** Logo图标地址，空则使用皮肤内置默认 */
  logo?: string;
  /** 登录提示 */
  loginTip?: string;
  /** 登录页 Logo，空则使用皮肤内置默认 */
  loginLogo?: string;
  /** 登录页背景图，空则使用皮肤内置默认 */
  loginBackground?: string;
  /** 登录能力配置 */
  login?: LoginAbility;
  /** 注册能力配置 */
  register?: RegisterAbility;
  /** OAuth 提供商列表 */
  oauth?: OAuthProvider[];
  /** 安全策略 */
  security?: SecurityConfig;

  // === 兼容旧版平铺字段（v1），请勿在新代码中使用 ===
  /** @deprecated 使用 name */
  displayName?: string;
  /** @deprecated 使用 login.password */
  allowLogin?: boolean;
  /** @deprecated 使用 register.enabled */
  allowRegister?: boolean;
  /** @deprecated 使用 login.sms */
  enableSms?: boolean;
  /** @deprecated 使用 login.mail */
  enableMail?: boolean;
  /** @deprecated 使用 login.sms */
  enableSmsRegister?: boolean;
  /** @deprecated 使用 login.mail */
  enableMailRegister?: boolean;
  /** @deprecated 使用 oauth */
  providers?: OAuthProvider[];
}

/**
 * 统一认证分类，适用于登录与注册接口的 category 字段
 *
 * 与后端 AuthCategory 枚举对应：Password=0, Mobile=1, Mail=2, OAuth=3
 */
export type AuthCategory = '' | 'mobile' | 'mail' | 'oauth';

/** 注册参数 */
export interface RegisterModel {
  category?: AuthCategory;
  username?: string;
  email?: string;
  mobile?: string;
  password: string;
  confirmPassword?: string;
  password2?: string;
  code?: string;
  oauthToken?: string;
  /** 多租户：OAuth 应用 AppId，请求时转为 X-App-Id 头，按 OAuth 配置定位租户（优先于 tenantCode） */
  appId?: string;
  /** 多租户：租户编码，请求时转为 X-Tenant 头，按编码查询租户 */
  tenantCode?: string;
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
  /** 挑战码标识 */
  challengeId?: string;
}

/** 激活参数。邮箱/手机验证码激活 */
export interface ActivateModel {
  /** 渠道。mail/sms */
  channel: string;
  /** 邮箱或手机号 */
  account: string;
  /** 验证码 */
  code: string;
}

/** 验证联系方式参数。安全中心验证/更换邮箱或手机 */
export interface VerifyContactModel {
  /** 渠道。mail/sms */
  channel: string;
  /** 新邮箱或手机号 */
  account: string;
  /** 验证码（经 sendCode action=bind 发送） */
  code: string;
}

/** 联系方式验证状态 */
export interface VerifyStatus {
  /** 邮箱已验证 */
  mailVerified: boolean;
  /** 手机已验证 */
  mobileVerified: boolean;
}

/** 第三方绑定记录（对应后端 UserConnect，GET /Admin/User/Binds） */
export interface UserBind {
  /** 编号 */
  id?: number;
  /** 平台标识（OpenWeixin/Microsoft/Github 等，与 OAuthPlatform.name 对应） */
  provider?: string;
  /** 第三方昵称 */
  nickName?: string;
  /** 第三方头像 */
  avatar?: string;
  /** 是否启用（解绑后为 false） */
  enable?: boolean;
  /** 绑定时间 */
  createTime?: string;
}

/** 可绑定的第三方平台（对应后端 OAuthConfig） */
export interface OAuthPlatform {
  /** 编号 */
  id?: number;
  /** 平台标识（绑定/解绑 URL 使用） */
  name?: string;
  /** 平台显示名 */
  nickName?: string;
  /** 平台 Logo */
  logo?: string;
  /** 是否启用 */
  enable?: boolean;
  /** 是否可见 */
  visible?: boolean;
}

/** 第三方授权绑定数据（GET /Admin/User/Binds 返回） */
export interface BindsResult {
  /** 用户名 */
  name?: string;
  /** 已绑定记录 */
  connects?: UserBind[];
  /** 可选的第三方平台 */
  oAuthItems?: OAuthPlatform[];
}

/** 修改密码（POST /Admin/User/ChangePassword） */
export interface ChangePasswordModel {
  /** 原密码 */
  oldPassword: string;
  /** 新密码 */
  newPassword: string;
  /** 确认新密码 */
  newPassword2: string;
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

/** 图片验证码结果（GET /Auth/Captcha） */
export interface CaptchaResult {
  /** 验证码 ID，登录/注册时原样传回 captchaId 字段 */
  captchaId: string;
  /** SVG 图片内容（算数题） */
  image: string;
}

/** MFA 二步验证结果 */
export interface MfaVerifyResult {
  accessToken: string;
  refreshToken?: string;
  /** 访问令牌有效期（秒） */
  expireIn?: number;
}

/** MFA 初始化结果 */
export interface MfaSetupResult {
  /** Authenticator App 二维码 URI */
  qrCodeUri: string;
  /** 密钥（手动输入用） */
  secret: string;
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
