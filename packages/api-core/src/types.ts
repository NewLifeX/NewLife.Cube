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
  /** 是否可见。列表字段为 false 时仍作为可用列下发，由表格设置按需打开 */
  visible?: boolean;
  /** 映射字段名 */
  mapField?: string;
  /** 取值字段。列表单元格优先取该字段值，为空时回退到本字段（如名称列优先显示昵称：Name.valueField=DisplayName） */
  valueField?: string;
  /** 链接 URL（支持变量替换 {Id} 等） */
  url?: string;
  /** 链接目标 */
  target?: string;
  /** 链接文字。列表单元格有 url 时优先显示该文字（如「追踪」），为空回退显示字段值，支持占位符 */
  text?: string;
  /** 单元格提示文字，支持占位符 */
  title?: string;
  /** 数据动作。action 表示点击链接走 AJAX 请求后刷新列表 */
  dataAction?: string;
  /** 表头文字 */
  header?: string;
  /** 表头提示文字 */
  headerTitle?: string;
  /** 最大宽度（超长文本截断字符数） */
  maxWidth?: number;
  /** 文本对齐。后端 TextAligns 枚举值或 left/center/right */
  textAlign?: string | number;
  /** 单元格样式类 */
  class?: string;
  /** 数据字典（键值对列表） */
  dataSource?: Record<string, string>;
  /** 值集编码（枚举 / singleSelect / multipleSelect 由后端静态构造下发，前端绝不硬编码） */
  lovCode?: string;
  /** 是否多选（multipleSelect 为 true；亦可由前端在映射时推导） */
  multiple?: boolean;
  /** 级联父字段名。带 `[Parent]` 的枚举子字典下发，用于「父变过滤子」 */
  parentField?: string;
  /** 渲染器名。对应控制器 View/ItemView/GroupView，前端按名注册 */
  view?: string;
  /** 扩展属性。核心不解释，透传给渲染器 */
  props?: Record<string, unknown>;
  /** 是否非实体列（链接列、虚拟查询项） */
  virtual?: boolean;
  /** 搜索项类型。keyword 统一由关键字框处理，range 表示区间 */
  kind?: 'keyword' | 'range';
  /** 区间结束参数名（kind=range） */
  end?: string;
  /** 区间对应的实体字段名（kind=range） */
  field?: string;
}

/** 页面设置（meta.data.setting） */
export interface PageSetting {
  navView?: string;
  enableNavbar?: boolean;
  enableToolbar?: boolean;
  enableAdd?: boolean;
  enableSelect?: boolean;
  enableFooter?: boolean;
  isReadOnly?: boolean;
  enableTableDoubleClick?: boolean;
  orderByKey?: boolean;
  doubleDelete?: boolean;
  /** 开发模式（后端 SysConfig.Develop）。驱动列表页高级菜单显示备份/还原/清空数据表等开发功能 */
  develop?: boolean;
  /** 当前用户是否系统管理员。开发功能仅系统管理员可用 */
  isSystem?: boolean;
  /** 当前页面菜单编号，用于和用户权限串匹配 */
  menuId?: number;
  /** 页面动作所需权限位。有效权限由前端结合用户信息计算 */
  requiredPermissions?: RequiredPermissions;
}

/** 页面动作所需权限位 */
export interface RequiredPermissions {
  /** 新增 */
  add?: number;
  /** 编辑 */
  update?: number;
  /** 删除 */
  delete?: number;
}

/** 实体标识（meta.data.entity） */
export interface EntityMeta {
  /** 实体类型名 */
  name?: string;
  /** 实体显示名 */
  displayName?: string;
  /** 主键字段名 */
  primaryKey?: string;
}

/** 工具栏按钮类型 */
export type ToolbarKind = 'link' | 'request' | 'upload' | 'custom';

/**
 * 工具栏自定义按钮（meta.data.toolbar[]）。
 *
 * 内置按钮（添加/导入/导出/批量删除/刷新）由 `setting` 控制，本类型只描述后端声明的自定义按钮。
 * 对齐 MVC 的 `_List_Toolbar_Custom` / `_List_Toolbar_Batch`：
 * `url`/`action` 对应 `data-url`，`include` 对应 `data-fields`，`method` 对应 `data-method`。
 */
export interface ToolbarItem {
  /** 唯一名称 */
  name: string;
  /** 显示名 */
  displayName?: string;
  /** 图标名 */
  icon?: string;
  /** 按钮风格 primary/danger/warning/success/info */
  type?: string;
  /** 按钮类型 */
  kind: ToolbarKind;
  /** 跳转地址（link） */
  url?: string;
  /** 控制器动作名（request/upload），前端拼接当前控制器地址 */
  action?: string;
  /** 请求方法，缺省 GET */
  method?: string;
  /** 提交字段：keys 表示选中行主键，其它名字取当前查询参数 */
  include?: string[];
  /** 是否携带当前查询参数（link） */
  appendQuery?: boolean;
  /** 链接目标 _blank/_self */
  target?: string;
  /** 上传文件类型 */
  accept?: string;
  /** 二次确认文案 */
  confirm?: string;
  /** 是否要求先选中数据 */
  requiresSelection?: boolean;
  /** 前端渲染器名（custom） */
  renderer?: string;
}

/** meta 聚合元数据 */
export interface PageMeta {
  /** 新结构：页面设置 */
  setting?: PageSetting;
  /** 兼容旧结构 */
  pageSetting?: PageSetting;
  /** 实体标识 */
  entity?: EntityMeta;

  /** 列表字段 */
  list?: DataField[];
  addForm?: DataField[];
  editForm?: DataField[];
  detail?: DataField[];
  search?: DataField[];
  /** 工具栏自定义按钮（内置按钮由 setting 控制） */
  toolbar?: ToolbarItem[];
  /** 页面 UI 扩展。核心不解释，原样透传给前端 preset / 渲染器 */
  ui?: Record<string, unknown>;

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

/** `/meta` 响应数据：各视图直接是字段数组，消费端零合并 */
export interface PageMetaData {
  /** 实体标识 */
  entity?: EntityMeta;
  /** 页面设置 */
  setting?: PageSetting;
  /** 列表字段 */
  list?: DataField[];
  /** 表单字段（编辑表单） */
  form?: DataField[];
  /** 新增表单；与 form 相同则省略 */
  add?: DataField[];
  /** 详情视图；与 form 相同则省略 */
  detail?: DataField[];
  /** 搜索区查询项 */
  search?: DataField[];
  /** 工具栏自定义按钮 */
  toolbar?: ToolbarItem[];
  /** 页面 UI 扩展。核心不解释 */
  ui?: Record<string, unknown>;
}

/** 字段视图类型枚举（保留给需要单场景字段的调用方；实体元数据统一由 meta 返回） */
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
  /** 发送验证码时需要图片验证码（防短信轰炸） */
  sendCode?: boolean;
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

/** 登录配置（新版嵌套结构，v2 起）。站点品牌/语言/开关由 /Cube/Site 提供 */
export interface LoginConfig {
  /** 是否启用多租户。前端据此控制租户相关 UI（切换器/租户字段）显隐 */
  enableTenant?: boolean;
  /** 租户 Code，有租户时非空 */
  code?: string;
  /** 登录能力配置 */
  login?: LoginAbility;
  /** 注册能力配置 */
  register?: RegisterAbility;
  /** OAuth 提供商列表 */
  oauth?: OAuthProvider[];
  /** 安全策略 */
  security?: SecurityConfig;
}

/** 语言信息（对应 NewLife.Core 的 LanguageInfo） */
export interface SiteLanguageInfo {
  /** 语言代码，如 zh-CN */
  name: string;
  /** 该语言的自称，如简体中文 */
  nativeName: string;
  /** 英文名称，如 Chinese (Simplified) */
  englishName: string;
}

/** 站点语言信息 */
export interface SiteLanguage {
  /** 当前请求解析出的语言 */
  current?: string;
  /** 站点默认语言。浏览器语言不受支持时回退到该语言 */
  default?: string;
  /** 是否锁定语言。true 时前端隐藏语言切换入口 */
  locked?: boolean;
  /** 支持的语言列表 */
  options?: SiteLanguageInfo[];
}

/** 站点 AI 能力 */
export interface SiteAiFeature {
  /** 是否启用 AI */
  enabled?: boolean;
  /** AI 主色 */
  primaryColor?: string;
  /** AI 辅色 */
  secondaryColor?: string;
  /** Mermaid 脚本地址 */
  mermaidUrl?: string;
}

/** 站点功能开关 */
export interface SiteFeatures {
  /** AI 能力 */
  ai?: SiteAiFeature;
}

/** 站点配置（匿名接口 /Cube/Site） */
export interface SiteConfig {
  /** 站点名称（租户级优先） */
  name?: string;
  /** 站点 Logo（租户级优先，回退到登录页 Logo 配置） */
  logo?: string;
  /** 登录页背景图 */
  loginBackground?: string;
  /** 登录提示 */
  loginTip?: string;
  /** 版权信息，服务端已替换变量，前端直接渲染（支持 HTML） */
  copyright?: string;
  /** 备案号 */
  registration?: string;
  /** 站点程序版本 */
  version?: string;
  /** 站点构建时间 */
  buildTime?: string;
  /** 当前租户编码。非空表示处于租户上下文 */
  tenantCode?: string;
  /** 语言信息 */
  language?: SiteLanguage;
  /** 功能开关 */
  features?: SiteFeatures;
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
