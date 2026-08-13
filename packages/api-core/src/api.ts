import { isAxiosError, type AxiosRequestConfig } from 'axios';
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
  UserProfileModel,
  ViewProfileModel,
  EntityCommentModel,
} from './types';

type RequestFn = <T>(config: AxiosRequestConfig) => Promise<ApiResponse<T>>;

async function requestWithPostFallback<T>(request: RequestFn, config: AxiosRequestConfig) {
  try {
    return await request<T>(config);
  } catch (error) {
    if ((config.method === 'put' || config.method === 'PUT') && isAxiosError(error) && error.response?.status === 405) {
      return await request<T>({ ...config, method: 'post' });
    }
    throw error;
  }
}

/**
 * 用户认证相关 API
 *
 * 新版使用 /Auth/* 路径，由 AuthController 提供
 */
export function createUserApi(request: RequestFn) {
  return {
    /** 密码登录（传入 category 可切换：手机验证码登录/邮箱验证码登录） */
    login: (data: { username: string; password: string; category?: AuthCategory; challengeId?: string; captchaId?: string; captchaCode?: string }) =>
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
export function createPageApi(request: RequestFn, baseApiUrl?: string) {
  return {
    /** 获取页面元数据（setting + list/addForm/editForm/detail/search） */
    getPage: (type: string) =>
      request<PageMeta>({ url: `${type}/GetPage`, method: 'get' }),

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

    /** 批量启用（须有 Update 权限；实体需含 Enable 字段；复用后端 EnableOrDisableSelect） */
    enableSelect: (type: string, keys: (number | string)[], reason?: string) =>
      request<unknown>({
        url: `${type}/EnableSelect`,
        method: 'get',
        params: { keys: keys.join(','), ...(reason ? { reason } : {}) },
      }),

    /** 批量禁用（须有 Update 权限；实体需含 Enable 字段；复用后端 EnableOrDisableSelect） */
    disableSelect: (type: string, keys: (number | string)[], reason?: string) =>
      request<unknown>({
        url: `${type}/DisableSelect`,
        method: 'get',
        params: { keys: keys.join(','), ...(reason ? { reason } : {}) },
      }),

    /**
     * 批量删除，默认使用重复参数 id=1&id=2 （文档标准）。
     * 若后端仅支持逗号形式，可传入 compatCommaJoin: true 切换为兼容模式。
     */
    deleteSelect: (type: string, keys: (number | string)[], options?: { compatCommaJoin?: boolean }) => {
      const ids = options?.compatCommaJoin
        ? { id: keys.join(',') }
        : keys.map(id => `id=${encodeURIComponent(id)}`).reduce<Record<string, (number | string)[]>>(
            (acc) => { acc.id = keys as (number | string)[]; return acc; },
            { id: [] }
          );
      // 使用 qs 逗号逗号逗号 重复参数：id=1&id=2&id=3
      const idArr = options?.compatCommaJoin ? keys.join(',') : keys;
      return request<unknown>({ url: type, method: 'delete', params: { id: idArr } });
    },

    /** 按条件删除，params 为搜索条件（至少需携带一个参数，否则后端拒绝） */
    deleteAll: (type: string, params?: Record<string, unknown>) =>
      request<unknown>({ url: type, method: 'delete', params }),

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

    /**
     * 获取图表数据。
     * params 可选：缺省保持原 URL 与行为；提供时经 query serializer 编码（数组遵循 GetList 约定）。
     * 只传搜索条件 effectiveSearch，不传分页、排序与视图 UI 配置。
     */
    getChartData: (type: string, params?: Record<string, unknown>) =>
      request<unknown[]>({ url: `${type}/GetChartData`, method: 'get', params }),
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

/**
 * 用户呈现配置 API（布局 / 主题 / 工作台偏好）
 */
export function createProfileApi(request: RequestFn) {
  return {
    /** 获取当前用户 UserProfile；无记录时 data 可能为 null */
    getUserProfile: () =>
      request<UserProfileModel | null>({ url: '/Cube/UserProfile', method: 'get' }),

    /** 保存当前用户 UserProfile（upsert；仅非 null 的 Json 字段会更新） */
    putUserProfile: (data: Partial<UserProfileModel>) =>
      requestWithPostFallback<UserProfileModel>(request, { url: '/Cube/UserProfile', method: 'put', data }),

    /** 获取当前用户指定实体的视图配置；无记录时 data 可能为 null */
    getViewProfile: (typePath: string) =>
      request<ViewProfileModel | null>({
        url: '/Cube/ViewProfile',
        method: 'get',
        params: { typePath },
      }),

    /** 保存实体视图配置（upsert） */
    putViewProfile: (data: Partial<ViewProfileModel> & { typePath: string }) =>
      requestWithPostFallback<ViewProfileModel>(request, { url: '/Cube/ViewProfile', method: 'put', data }),

    /** 删除实体视图配置（恢复默认） */
    deleteViewProfile: (typePath: string) =>
      request<unknown>({
        url: '/Cube/ViewProfile',
        method: 'delete',
        params: { typePath },
      }),

    /** 获取全局模板（视图/筛选域）；仅系统管理员可调用（OSC-0014） */
    getViewProfileTemplate: (typePath: string) =>
      request<ViewProfileModel | null>({
        url: '/Cube/ViewProfileTemplate',
        method: 'get',
        params: { typePath },
      }),

    /** 发布/更新全局模板（视图/筛选域）；仅系统管理员可调用（OSC-0014） */
    putViewProfileTemplate: (data: Partial<ViewProfileModel> & { typePath: string }) =>
      requestWithPostFallback<ViewProfileModel>(request, {
        url: '/Cube/ViewProfileTemplate',
        method: 'put',
        data,
      }),

    /** 删除全局模板（视图/筛选域回落系统默认）；仅系统管理员可调用（OSC-0014） */
    deleteViewProfileTemplate: (typePath: string) =>
      request<unknown>({
        url: '/Cube/ViewProfileTemplate',
        method: 'delete',
        params: { typePath },
      }),
  };
}

/**
 * 实体评论 API（M4b，消费 OSC-0002 后端 EntityComment）
 */
export function createCommentApi(request: RequestFn) {
  return {
    /** 评论列表；parentId 缺省/负数=全部，0=仅顶层，>0=直接回复 */
    getList: (params: {
      category: string;
      linkId: number | string;
      parentId?: number | string;
      pageIndex?: number;
      pageSize?: number;
    }) => request<EntityCommentModel[]>({ url: '/Cube/EntityComment', method: 'get', params }),

    /** 发表评论；body 含 parentId 表示回复 */
    post: (data: {
      category: string;
      linkId: number | string;
      content: string;
      parentId?: number | string;
    }) => request<EntityCommentModel>({ url: '/Cube/EntityComment', method: 'post', data }),

    /** 删除评论（本人或管理员） */
    remove: (id: number | string) =>
      request<unknown>({ url: '/Cube/EntityComment', method: 'delete', params: { id } }),
  };
}
