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

/** PATCH/批量改字段结果（OSC-260819e483 P3）：部分失败仍 Code=0，由 Ok/Fail/Errors 表达 */
export interface FieldPatchResult {
  /** 成功行数 */
  ok: number;
  /** 失败行数 */
  fail: number;
  /** 失败明细 */
  errors: { id: string; message: string }[];
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
  /** GetPage 物化后的数据源（与 dataSource 同义，兼容 DataSourceMap 序列化） */
  dataSourceMap?: Record<string, string>;
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
  /** 主时间字段名（OSC-0016）；无 MasterTime 时缺省 */
  masterTimeName?: string | null;
  /** 主时间字段显示名（OSC-0016） */
  masterTimeDisplayName?: string | null;
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
  /** 是否系统管理员。角色中包含 IsSystem 标识即为管理员 */
  isSystem?: boolean;
  online?: boolean;
  enable?: boolean;
  /** 邮箱已验证（安全中心展示验证状态） */
  mailVerified?: boolean;
  /** 手机已验证（安全中心展示验证状态） */
  mobileVerified?: boolean;
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
  /** 说明（悬停徽标 Tooltip；来自 OAuth 配置 Remark） */
  remark?: string;
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
  /** 登录时需要图片验证码（CaptchaScene 位 1） */
  captcha?: boolean;
  /** 发送短信/邮件验证码时需要图片验证码（CaptchaScene 位 4） */
  sendCodeCaptcha?: boolean;
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
  /** 是否启用多租户（魔方设置/系统功能） */
  enableTenant?: boolean;
  /**
   * Cube 作为 OAuth2 **服务端**（为其它应用提供 SSO）是否开启。
   * 与登录页「第三方登录」无关；第三方入口仅看 oauth[] 可见列表。
   */
  enableOAuthServer?: boolean;
  /** 工作台/登录后默认内容页（SPA 会映射 MVC 遗留路径） */
  startPage?: string;
  /** ECharts 主题名（魔方设置 EChartsTheme；default 或空=默认主题） */
  echartsTheme?: string;
  /** 文字头像字符数（魔方设置 AvatarChars；1 或 2） */
  avatarChars?: number;
  /** 星尘 Web 根地址（魔方设置 StarWeb；空则无追踪深链） */
  starWeb?: string;
  /** SSO 用户中心根 URL（魔方设置 SsoUserCenter） */
  ssoUserCenter?: string;
  /** 资料/改密是否外跳用户中心（魔方设置 RedirectUserToSso） */
  redirectUserToSso?: boolean;
  /** 登录能力配置 */
  login?: LoginAbility;
  /** 注册能力配置 */
  register?: RegisterAbility;
  /** OAuth 提供商列表（可见 OAuthConfig；与 enableOAuthServer 无关） */
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
  /**
   * 图片数据：SVG 文本，或 `data:image/png;base64,...`（DrawingCaptcha）。
   * 前端展示前经 normalizeCaptchaImageHtml 再 v-html。
   */
  image: string;
}

/** MFA 二步验证结果 */
export interface MfaVerifyResult {
  accessToken: string;
  refreshToken?: string;
}

/** MFA 初始化结果 */
export interface MfaSetupResult {
  /** Authenticator App 二维码 URI（后端可能返回 totpUri 或 qrCodeUri） */
  qrCodeUri?: string;
  totpUri?: string;
  /** 密钥（手动输入用） */
  secret: string;
}

/** 第三方绑定项（GET /Auth/Binds） */
export interface AuthBindItem {
  id: number;
  name?: string;
  nickName?: string;
  logo?: string;
  bound?: boolean;
  connectId?: number;
  connectNickName?: string;
}

/** 租户列表项 */
export interface TenantItem {
  id: number;
  code?: string;
  name?: string;
}

/** GET /Auth/Tenants · POST /Auth/SwitchTenant 返回 */
export interface TenantListResult {
  /** 魔方设置 EnableTenant；false 时 items 为空且前端隐藏租户 UI */
  enableTenant?: boolean;
  currentId: number;
  currentCode?: string;
  items: TenantItem[];
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

/**
 * 用户呈现配置（UserProfile）线缆模型。
 * layout/theme/workspace 以 JSON 字符串列存储，前端负责 parse/stringify。
 */
export interface UserProfileModel {
  id?: number;
  userId?: number;
  layoutJson?: string | null;
  themeJson?: string | null;
  workspaceJson?: string | null;
  /** 首页工作台。JSON：version+widgets（OSC-26082815a1） */
  homeJson?: string | null;
  version?: number;
  enable?: boolean;
  remark?: string | null;
}

/**
 * 实体视图配置（ViewProfile）线缆模型。
 * columns/views 等以 JSON 字符串列存储。
 */
export interface ViewProfileModel {
  id?: number;
  userId?: number;
  typePath?: string;
  view?: string | null;
  columnsJson?: string | null;
  viewsJson?: string | null;
  activeViewId?: string | null;
  ganttJson?: string | null;
  cardJson?: string | null;
  filtersJson?: string | null;
  /** 预定义查询（OSC-0016）。JSON：{version, queries:[{id,name,params}]}；空串表示清除该域 */
  queriesJson?: string | null;
  /** 页面条数（typePath 级偏好）。0/null 表示未配置，回落全局 workspace.pageSize 种子 */
  pageSize?: number;
  /**
   * 表单布局（系统全局唯一配置，仅管理员可写，作用于所有用户）。
   * JSON：add/edit/detail 的字段顺序/显隐/分组折叠；空壳（无任何模式）等价于未配置。
   */
  formJson?: string | null;
  /**
   * 页面仪表盘（OSC-2608280e9e）。JSON：{version:1, widgets:[]}；实体级，不跟命名视图走。
   * 空串表示清除个人域（下次 GET 继承模板）；显式 `{"version":1,"widgets":[]}` 表示用户清空、不继承。
   */
  dashboardJson?: string | null;
  version?: number;
  remark?: string | null;
}

/**
 * 实体评论（EntityComment）线缆模型。
 * 后端 OSC-0002 已就绪；GET parentId 缺省/负数=全部，0=仅顶层，>0=直接回复。
 */
export interface EntityCommentModel {
  id?: number | string;
  /** 实体类别：typePath 去前导 /，如 "Admin/User" */
  category?: string;
  /** 记录主键 */
  linkId?: number | string;
  /** 父评论 ID（0=顶层） */
  parentId?: number | string;
  /** 线程根 ID */
  rootId?: number | string;
  /** 被回复用户 ID */
  replyUserId?: number | string;
  /** 被回复用户名 */
  replyUser?: string;
  /** 评论内容 */
  content?: string;
  /** 创建人 */
  createUser?: string;
  createUserId?: number | string;
  createTime?: string;
  updateTime?: string;
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
