import type { AxiosRequestConfig } from 'axios';
import type {
  ApiResponse,
  DataField,
  FieldKind,
  PageMeta,
  UserInfo,
  LoginResult,
  LoginConfig,
  ChallengeResult,
  ResetPasswordModel,
  SiteInfo,
  MenuItem,
  PageParams,
  RegisterModel,
  OAuthPendingInfo,
} from './types';

type RequestFn = <T>(config: AxiosRequestConfig) => Promise<ApiResponse<T>>;

/**
 * 用户认证相关 API
 *
 * 新版使用 /Auth/* 路径，由 AuthController 提供
 */
export function createUserApi(request: RequestFn) {
  return {
    /** 密码登录 */
    login: (data: { username: string; password: string; challengeId?: string }) =>
      request<LoginResult>({ url: '/Auth/Login', method: 'post', data }),

    /** 验证码登录（手机/邮箱） */
    loginByCode: (data: { username: string; password: string; loginCategory: number }) =>
      request<LoginResult>({ url: '/Auth/LoginByCode', method: 'post', data }),

    /** 发送验证码 */
    sendCode: (data: { channel: string; username: string; action?: string }) =>
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

    /** 获取登录页配置（OAuth 提供商列表等） */
    getLoginConfig: () =>
      request<LoginConfig>({ url: '/Auth/LoginConfig', method: 'get' }),

    /** 获取站点信息（名称/Logo/版权），由 CubeController 提供 */
    getSiteInfo: () =>
      request<SiteInfo>({ url: '/Cube/SiteInfo', method: 'get' }),

    /** 注册新用户 */
    register: (data: RegisterModel) =>
      request<LoginResult>({ url: '/Auth/Register', method: 'post', data }),

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

    /** 批量删除，id 参数支持逗号分隔多个主键 */
    deleteSelect: (type: string, keys: (number | string)[]) =>
      request<unknown>({ url: type, method: 'delete', params: { id: keys.join(',') } }),

    /** 按条件删除，params 为搜索条件（至少需携带一个参数，否则后端拒绝） */
    deleteAll: (type: string, params?: Record<string, unknown>) =>
      request<unknown>({ url: type, method: 'delete', params }),

    /** 字典查询（codes 逗号分隔） */
    lookup: (codes: string) =>
      request<Record<string, Array<Record<string, unknown>>>>({ url: '/Cube/Lookup', method: 'get', params: { codes } }),

    /** 获取导出 URL（直接下载，不走 ajax） */
    getExportUrl: (type: string, format: string): string => {
      const base = baseApiUrl ?? '';
      return `${base}/${type}/ExportFile?format=${encodeURIComponent(format)}`;
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
    /** 获取站点信息（站点名称/Logo/版权等），与 user.getSiteInfo 等价 */
    getPageConfig: () =>
      request<SiteInfo>({ url: '/Cube/SiteInfo', method: 'get' }),

    /** 获取系统配置 */
    getSetting: () =>
      request<Record<string, unknown>>({ url: '/Cube/Setting', method: 'get' }),

    /** 更新系统配置 */
    updateSetting: (data: Record<string, unknown>) =>
      request<unknown>({ url: '/Cube/Setting', method: 'put', data }),
  };
}
