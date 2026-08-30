import type { AxiosRequestConfig } from 'axios';
import { resolveRequestUrl } from './service-path';
import type {
  ApiResponse,
  AuthCategory,
  CaptchaResult,
  DataField,
  FieldKind,
  MfaSetupResult,
  MfaVerifyResult,
  PageMeta,
  UserInfo,
  LoginResult,
  LoginConfig,
  ChallengeResult,
  ResetPasswordModel,
  MenuItem,
  PageParams,
  RegisterModel,
  OAuthPendingInfo,
  ActivateModel,
  VerifyContactModel,
  VerifyStatus,
} from './types';

type RequestFn = <T>(config: AxiosRequestConfig) => Promise<ApiResponse<T>>;

/**
 * 用户认证相关 API
 *
 * 新版使用 /Auth/* 路径，由 AuthController 提供
 */
export function createUserApi(request: RequestFn) {
  return {
    /** 密码登录（传入 category 可切换：手机验证码登录/邮箱验证码登录） */
    login: (data: { username: string; password: string; category?: AuthCategory; challengeId?: string; captchaId?: string; captchaCode?: string; remember?: boolean }) =>
      request<LoginResult>({ url: '/Auth/Login', method: 'post', data }),

    /** 发送验证码 */
    sendCode: (data: { channel: string; username: string; action?: string; captchaId?: string; captchaCode?: string }) =>
      request<number>({ url: '/Auth/SendCode', method: 'post', data }),

    /** 刷新令牌 */
    refreshToken: (data: { refreshToken: string; userName?: string }) =>
      request<LoginResult>({ url: '/Auth/Refresh', method: 'post', data }),

    /** 登出 */
    logout: () =>
      request<void>({ url: '/Auth/Logout', method: 'post' }),

    /** 获取当前用户信息 */
    info: () =>
      request<UserInfo>({ url: '/Auth/Info', method: 'get' }),

    /** 切换当前租户（多租户开启时）。0=管理后台（仅系统管理员），>0=租户编号；成功后前端应刷新页面（菜单/数据随租户变化） */
    switchTenant: (tenantId: number) =>
      request<boolean>({ url: '/Auth/SwitchTenant', method: 'post', data: { tenantId } }),

    /** 获取登录页配置（OAuth 提供商列表等），可传入租户标识（id/code/name/domain） */
    getLoginConfig: (tenant?: string) =>
      request<LoginConfig>({ url: '/Auth/LoginConfig', method: 'get', params: tenant ? { tenant } : undefined }),

    /** 获取图片验证码（SVG 算数题），返回 captchaId 和 image */
    getCaptcha: () =>
      request<CaptchaResult>({ url: '/Auth/Captcha', method: 'get' }),

    /** 注册新用户。多租户场景可传 appId（转为 X-App-Id 头）或 tenantCode（转为 X-Tenant 头） */
    register: (data: RegisterModel & { captchaId?: string; captchaCode?: string; appId?: string; tenantCode?: string }) => {
      const { appId, tenantCode, ...rest } = data;
      const headers: Record<string, string> = {};
      if (appId) headers['X-App-Id'] = appId;
      if (tenantCode) headers['X-Tenant'] = tenantCode;
      return request<LoginResult>({ url: '/Auth/Register', method: 'post', data: rest, headers: Object.keys(headers).length > 0 ? headers : undefined });
    },

    /** 微信小程序登录。appId 可选，后端支持请求体或 X-App-Id 头兜底 */
    wxMiniLogin: (data: { code: string; appId?: string }) =>
      request<LoginResult>({ url: '/Sso/WxMiniLogin', method: 'post', data }),

    /** 微信APP登录。appId 可选，后端支持请求体或 X-App-Id 头兜底 */
    wxAppLogin: (data: { code: string; appId?: string }) =>
      request<LoginResult>({ url: '/Sso/WxAppLogin', method: 'post', data }),

    /** 获取OAuth回跳待注册预填信息 */
    getOAuthPendingInfo: (token: string) =>
      request<OAuthPendingInfo>({ url: '/Auth/OAuthPendingInfo', method: 'get', params: { token } }),

    /**
     * 获取 RSA 公钥挑战，用于加密密码防明文传输
     *
    * 流程：getChallenge() → 用 publicKey 加密密码 → login(username, encryptedPwd, challengeId)
     * 密钥有效期 300s，使用一次后服务端立即删除防重放。
     */
    getChallenge: () =>
      request<ChallengeResult>({ url: '/Auth/Challenge', method: 'get' }),

    /**
     * 完成 MFA 二步验证登录
     *
     * 当 Login 返回消息中含有 mfa_required:xxx 时，提取 mfaToken 并让用户输入 Authenticator App 验证码
     * 再调用此接口完成登录。
     */
    mfaVerify: (data: { mfaToken: string; code: string }) =>
      request<MfaVerifyResult>({ url: '/Mfa/Verify', method: 'post', data }),

    /** 初始化 MFA（返回二维码 URI 和密钥） */
    mfaSetup: () =>
      request<MfaSetupResult>({ url: '/Mfa/Setup', method: 'get' }),

    /** 激活 MFA（输入扫码后第一个验证码），返回备用码 */
    mfaActivate: (code: string) =>
      request<{ backupCodes: string[] }>({ url: '/Mfa/Activate', method: 'post', data: { code } }),

    /** 禁用 MFA */
    mfaDisable: (code: string) =>
      request<void>({ url: '/Mfa/Disable', method: 'post', data: { code } }),

    /** 查询 MFA 开启状态 */
    mfaStatus: () =>
      request<{ enabled: boolean; available: boolean }>({ url: '/Mfa/Status', method: 'get' }),

    /**
     * 通过验证码重置密码（忘记密码流程）
     *
     * 先调用 sendCode({ channel, username, action: 'reset' }) 发送验证码，
     * 再调用本接口提交验证码 + 新密码完成重置。
     */
    resetPassword: (data: ResetPasswordModel) =>
      request<boolean>({ url: '/Auth/ResetPassword', method: 'post', data }),

    /**
     * 邮箱激活链接直达。激活邮件中的链接指向 {ActivateUrl}?token=&account=，前端 /activate 页解析后调用
     */
    activateByLink: (token: string, account: string) =>
      request<{ activated: boolean }>({ url: '/Auth/Activate', method: 'get', params: { token, account } }),

    /** 验证码激活（邮箱验证码/手机短信验证码） */
    activateByCode: (data: ActivateModel) =>
      request<{ activated: boolean }>({ url: '/Auth/Activate', method: 'post', data }),

    /** 重发激活。未激活账号重新发送激活邮件/短信（登录页「未激活？重新发送」） */
    sendActivateCode: (channel: string, account: string) =>
      request<{ target: string }>({ url: '/Auth/SendActivateCode', method: 'post', data: { channel, username: account } }),

    /** 已登录用户验证/更换邮箱或手机（安全中心）。验证码经 sendCode(action=bind) 发送 */
    verifyContact: (data: VerifyContactModel) =>
      request<VerifyStatus>({ url: '/Auth/VerifyContact', method: 'post', data }),
  };
}

/**
 * 菜单 API
 */
export function createMenuApi(request: RequestFn) {
  return {
    /** 获取菜单树，新路径 /Cube/MenuTree（旧路径 /Admin/Index/GetMenuTree 保持兼容） */
    getMenuTree: (params?: { module?: string }) =>
      request<MenuItem[]>({ url: '/Cube/MenuTree', method: 'get', params }),
  };
}

/**
 * 通用 CRUD + 数据操作 API（核心）
 *
 * 所有方法的 `type` 参数为路径前缀，如 "/Admin/User"、"/Cube/App"
 */

/**
 * 页面元数据缓存（会话级内存缓存）
 *
 * GetPage 返回的页面配置（列表/搜索/表单字段）由控制器静态配置决定，同一会话内稳定不变。
 * 按 type 缓存后，列表页「探测 + loadFields」与表单页只需请求一次，切换/重进页面不再重复请求。
 * 仅在浏览器刷新（内存重置）或登出换用户时失效。
 */
const pageMetaCache = new Map<String, ApiResponse<PageMeta>>();

/** 清空页面元数据缓存（登出/切换用户时调用，避免串用上一账号的配置） */
export function clearPageMetaCache(): void {
  pageMetaCache.clear();
}

export function createPageApi(request: RequestFn, baseApiUrl?: string) {
  return {
    /** 获取页面元数据（setting + list/addForm/editForm/detail/search）。同一 type 会话内缓存，避免重复请求 */
    getPage: (type: string) => {
      const hit = pageMetaCache.get(type);
      if (hit) return Promise.resolve(hit);

      return request<PageMeta>({ url: `${type}/GetPage`, method: 'get' }).then((res) => {
        // 仅缓存实体页配置（data 为对象）；非实体页返回 HTML 字符串，不缓存
        if (res && res.data && typeof res.data !== 'string') pageMetaCache.set(type, res);
        return res;
      });
    },

    /** 获取字段元数据 */
    getFields: (type: string, kind: FieldKind) =>
      request<DataField[]>({ url: `${type}/GetFields`, method: 'get', params: { kind } }),

    /** 列表查询（分页） */
    getList: <T = Record<string, unknown>>(type: string, params: PageParams) =>
      request<T[]>({ url: type, method: 'get', params }),

    /** 查看详情 */
    getDetail: <T = Record<string, unknown>>(type: string, id: number | string, extra?: Record<string, unknown>) =>
      request<T>({ url: `${type}/Detail`, method: 'get', params: { id, ...extra } }),

    /** 新增 */
    add: (type: string, data: Record<string, unknown>) =>
      request<unknown>({ url: type, method: 'post', data }),

    /** 编辑 */
    update: (type: string, data: Record<string, unknown>) =>
      request<unknown>({ url: type, method: 'put', data }),

    /** 删除单条 */
    remove: (type: string, id: number | string) =>
      request<unknown>({ url: type, method: 'delete', params: { id } }),

    /** 恢复软删除单条（后端 Delete 支持 restore=true 参数） */
    restore: (type: string, id: number | string) =>
      request<unknown>({ url: type, method: 'delete', params: { id, restore: true } }),

    /**
     * 批量删除选中，调用专用端点 DeleteSelect。
     * 默认传数组（qs 序列化为索引形式 id[0]=1&id[1]=2，后端 String[] 绑定）；
     * 后端不支持索引形式时可用 compatCommaJoin 传逗号分隔 id=1,2（后端已兼容拆分）。
     */
    deleteSelect: (type: string, keys: (number | string)[], options?: { compatCommaJoin?: boolean }) => {
      const params = options?.compatCommaJoin ? { id: keys.join(',') } : { id: keys };
      return request<unknown>({ url: `${type}/DeleteSelect`, method: 'delete', params });
    },

    /** 按条件删除全部，params 为搜索条件（至少需携带一个参数，否则后端拒绝）。调用专用端点 DeleteAll */
    deleteAll: (type: string, params?: Record<string, unknown>) =>
      request<unknown>({ url: `${type}/DeleteAll`, method: 'delete', params }),

    /** 字典查询（codes 逗号分隔） */
    lookup: (codes: string) =>
      request<Record<string, Array<Record<string, unknown>>>>({ url: '/Cube/Lookup', method: 'get', params: { codes } }),

    /** 获取导出 URL（直接下载，不走 ajax） */
    getExportUrl: (type: string, format: string): string => {
      const url = resolveRequestUrl(baseApiUrl ?? '', `/${type}/ExportFile`);
      return `${url}?format=${encodeURIComponent(format)}`;
    },

    /** 导入文件 */
    importFile: (type: string, file: File) => {
      const formData = new FormData();
      formData.append('file', file);
      return request<unknown>({
        url: `${type}/ImportFile`,
        method: 'post',
        headers: { 'Content-Type': 'multipart/form-data' },
        data: formData,
      });
    },

    /** 上传文件，type 为实体路径前缀（如 '/Admin/User'），options.id 为主记录主键（0 表示新增） */
    uploadFile: (type: string, file: File, options?: { id?: number; title?: string }) => {
      const formData = new FormData();
      formData.append('file', file);
      return request<Record<string, unknown>>({
        url: `${type}/UploadFile`,
        method: 'post',
        headers: { 'Content-Type': 'multipart/form-data' },
        data: formData,
        params: options,
      });
    },

    /** 获取图表数据 */
    getChartData: (type: string) =>
      request<unknown[]>({ url: `${type}/GetChartData`, method: 'get' }),
  };
}

/**
 * 系统配置 API
 */
export function createConfigApi(request: RequestFn) {
  return {
    /** 获取登录页配置（系统名称/Logo/版权/OAuth 提供商等） */
    getPageConfig: () =>
      request<LoginConfig>({ url: '/Auth/LoginConfig', method: 'get' }),

    /** 获取系统配置 */
    getSetting: () =>
      request<Record<string, unknown>>({ url: '/Cube/Setting', method: 'get' }),

    /** 更新系统配置 */
    updateSetting: (data: Record<string, unknown>) =>
      request<unknown>({ url: '/Cube/Setting', method: 'put', data }),
  };
}
