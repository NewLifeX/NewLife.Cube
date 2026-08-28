/**
 * 登录页 API 封装
 *
 * 基于 core 请求库（core/utils/request，axios 封装，含拦截器与全局错误处理）调用后端接口。
 * core 库作为通用库，不依赖具体应用创建的 API 客户端；统一复用请求库的超时、拦截、401 处理。
 *
 * 类型定义复用 @cube/api-core 的 LoginConfig、LoginResult、ApiResponse。
 *
 * 字段名归一化说明：
 * 请求库仅透传后端返回的 data，不做字段名归一化，因此以下变体仍需在本文件内手动归一：
 * - LoginConfig: oAuth / providers → oauth
 * - LoginResult: access_token / Token → accessToken（snake_case + PascalCase 兼容）
 *
 * 注意：请求库的响应拦截器会在 code !== 0/200 时自动弹出错误提示并抛异常，
 * 业务调用方在 catch 中处理失败即可（无需自行弹错，避免重复提示）。
 */
import request from './request';
import type { ApiResponse, LoginConfig, LoginResult, OAuthProvider, RegisterModel, VerifyStatus } from '@cube/api-core';

/** 默认请求超时时间（毫秒） */
const REQUEST_TIMEOUT = 15000;

// ── 字段名归一化工具 ────────────────────────────────────────────────

/**
 * 归一化登录配置字段名
 *
 * 后端 /Auth/LoginConfig 可能返回以下字段名变体：
 * - `oAuth`（C# 属性名 PascalCase 风格，首字母大写 O）→ 标准字段 `oauth`
 * - `providers`（v1 旧版字段名）→ 标准字段 `oauth`
 *
 * 归一化后确保 LoginConfig.oauth 始终有值（如果后端返回了的话）。
 *
 * @param data 后端原始返回的 LoginConfig
 * @returns 归一化后的 LoginConfig
 */
function normalizeLoginConfig(data: LoginConfig): LoginConfig {
  // 浅拷贝，避免修改原始对象
  const result: LoginConfig = { ...data };

  // 用 Record 类型访问非标准字段名
  const raw = data as Record<string, unknown>;

  // 如果标准 oauth 字段为空，尝试从 oAuth / providers 补充
  if (!result.oauth || result.oauth.length === 0) {
    const oAuth = raw.oAuth as OAuthProvider[] | undefined;
    if (oAuth && Array.isArray(oAuth) && oAuth.length > 0) {
      result.oauth = oAuth;
    } else if (result.providers && Array.isArray(result.providers) && result.providers.length > 0) {
      // v1 旧版字段名 providers → oauth
      result.oauth = result.providers;
    }
  }

  return result;
}

/**
 * 归一化登录结果字段名
 *
 * 后端 /Auth/Login 可能返回以下字段名变体：
 * - snake_case: `access_token` / `refresh_token` / `expire_in`
 * - PascalCase: `Token` / `RefreshToken` / `ExpireIn`（C# 属性名原样输出）
 *
 * 归一化后统一为 camelCase（accessToken / refreshToken / expireIn），
 * 与 @cube/api-core 的 LoginResult 类型定义一致。
 * 注册待激活字段（pendingActivation/channels/targets）原样透传。
 *
 * @param data 后端原始返回的 LoginResult
 * @returns 归一化后的 LoginResult
 */
function normalizeLoginResult(data: LoginResult): LoginResult {
  const raw = data as unknown as Record<string, unknown>;
  return {
    accessToken:
      (raw.accessToken as string) ??
      (raw.access_token as string) ??
      (raw.Token as string) ??
      '',
    refreshToken:
      (raw.refreshToken as string) ??
      (raw.refresh_token as string) ??
      (raw.RefreshToken as string),
    expireIn:
      (raw.expireIn as number) ??
      (raw.expire_in as number) ??
      (raw.ExpireIn as number),
    // 注册待激活透传
    ...(raw.pendingActivation !== undefined ? { pendingActivation: !!raw.pendingActivation } : {}),
    ...(raw.channels !== undefined ? { channels: raw.channels as string[] } : {}),
    ...(raw.targets !== undefined ? { targets: raw.targets as string[] } : {}),
  };
}

// ── API 函数 ────────────────────────────────────────────────────────

/**
 * 获取登录配置
 *
 * 调用 GET /Auth/LoginConfig 获取后端登录配置，包括：
 * - 系统名称、Logo、版权信息
 * - 登录能力（密码/短信/邮箱/验证码）
 * - OAuth 提供商列表
 * - 安全策略
 *
 * 返回前对 data 做字段名归一化（oAuth / providers → oauth）。
 *
 * AuthController 路由不带 /api 前缀，统一经请求层拼接 baseUrl（API_HOST）转发；
 * 请确保 baseUrl 能到达 Auth 服务。
 *
 * @returns 登录配置响应
 * @throws 网络错误或 HTTP 状态码非 200 时抛出异常
 */
export async function fetchLoginConfig(): Promise<ApiResponse<LoginConfig>> {
  // 请求库响应拦截器已展开为 ApiResponse（{ code, message, data }），此处断言其形态
  const json = (await request.get('/Auth/LoginConfig', {
    timeout: REQUEST_TIMEOUT,
  })) as unknown as ApiResponse<LoginConfig>;

  // 归一化字段名（oAuth / providers → oauth）
  if (json?.data) {
    json.data = normalizeLoginConfig(json.data);
  }

  return json;
}

/**
 * 密码登录
 *
 * 调用 POST /Auth/Login 进行用户名密码登录。
 * 成功后返回包含 accessToken 和 refreshToken 的 LoginResult。
 *
 * 返回前对 data 做字段名归一化
 * （access_token / Token → accessToken，兼容 snake_case 和 PascalCase）。
 *
 * AuthController 路由不带 /api 前缀，统一经请求层拼接 baseUrl（API_HOST）转发。
 *
 * @param username 用户名
 * @param password 密码（明文，通过 HTTPS 传输）
 * @returns 登录结果响应，data.accessToken 为访问令牌
 * @throws 网络错误或 HTTP 状态码非 200 时抛出异常
 */
export async function loginByPassword(
  username: string,
  password: string,
): Promise<ApiResponse<LoginResult>> {
  // 请求库响应拦截器已展开为 ApiResponse（{ code, message, data }），此处断言其形态
  const json = (await request.post(
    '/Auth/Login',
    { username, password },
    { timeout: REQUEST_TIMEOUT },
  )) as unknown as ApiResponse<LoginResult>;

  // 归一化字段名（snake_case / PascalCase → camelCase）
  if (json?.data) {
    json.data = normalizeLoginResult(json.data);
  }

  return json;
}

// ── 注册 / 验证码 / 激活 / 安全中心 ─────────────────────────────────

/**
 * 注册新用户
 *
 * 调用 POST /Auth/Register。开启邮箱/手机验证时，
 * 成功返回 data.pendingActivation=true（待激活），不返回 token。
 *
 * @param data 注册参数（category/username/email/mobile/password/confirmPassword/code/captchaId/captchaCode）
 * @returns 注册结果：data.accessToken 表示已登录；data.pendingActivation 表示待激活
 */
export async function registerByForm(data: RegisterModel): Promise<ApiResponse<LoginResult>> {
  const json = (await request.post(
    '/Auth/Register',
    data,
    { timeout: REQUEST_TIMEOUT },
  )) as unknown as ApiResponse<LoginResult>;

  if (json?.data) {
    json.data = normalizeLoginResult(json.data);
  }

  return json;
}

/**
 * 发送验证码（注册/绑定等场景）
 *
 * @param channel 渠道：Sms / Mail
 * @param username 手机号或邮箱
 * @param action 场景：register / bind / reset / login
 * @param captchaId 图片验证码 ID（LoginConfig.register.captcha / login.sendCode 为 true 时必填）
 * @param captchaCode 图片验证码输入
 */
export async function sendCode(
  channel: string,
  username: string,
  action: string,
  captchaId?: string,
  captchaCode?: string,
): Promise<ApiResponse<number>> {
  return (await request.post(
    '/Auth/SendCode',
    { channel, username, action, captchaId, captchaCode },
    { timeout: REQUEST_TIMEOUT },
  )) as unknown as ApiResponse<number>;
}

/**
 * 获取图片验证码（SVG 算数题）
 *
 * 注册/发码需要图片验证码时（LoginConfig.register.captcha / login.sendCode），
 * 先调用本接口获取 captchaId 与 SVG 文本，随提交请求一并回传。
 */
export async function fetchCaptcha(): Promise<ApiResponse<{ captchaId: string; image: string }>> {
  return (await request.get('/Auth/Captcha', {
    timeout: REQUEST_TIMEOUT,
  })) as unknown as ApiResponse<{ captchaId: string; image: string }>;
}

/**
 * 邮箱激活链接直达（激活邮件中的链接指向 /activate?token=&account=，激活页解析后调用）
 */
export async function activateByLink(
  token: string,
  account: string,
): Promise<ApiResponse<{ activated: boolean }>> {
  return (await request.get('/Auth/Activate', {
    params: { token, account },
    timeout: REQUEST_TIMEOUT,
  })) as unknown as ApiResponse<{ activated: boolean }>;
}

/**
 * 验证码激活（邮箱验证码/手机短信验证码）
 */
export async function activateByCode(
  channel: string,
  account: string,
  code: string,
): Promise<ApiResponse<{ activated: boolean }>> {
  return (await request.post(
    '/Auth/Activate',
    { channel, account, code },
    { timeout: REQUEST_TIMEOUT },
  )) as unknown as ApiResponse<{ activated: boolean }>;
}

/**
 * 重发激活。未激活账号重新发送激活邮件/短信（登录页「未激活？重新发送」）
 */
export async function sendActivateCode(
  channel: string,
  account: string,
): Promise<ApiResponse<{ target: string }>> {
  return (await request.post(
    '/Auth/SendActivateCode',
    { channel, username: account },
    { timeout: REQUEST_TIMEOUT },
  )) as unknown as ApiResponse<{ target: string }>;
}

/**
 * 已登录用户验证/更换邮箱或手机（安全中心）。验证码经 sendCode(action=bind) 发送
 */
export async function verifyContact(
  channel: string,
  account: string,
  code: string,
): Promise<ApiResponse<VerifyStatus>> {
  return (await request.post(
    '/Auth/VerifyContact',
    { channel, account, code },
    { timeout: REQUEST_TIMEOUT },
  )) as unknown as ApiResponse<VerifyStatus>;
}
