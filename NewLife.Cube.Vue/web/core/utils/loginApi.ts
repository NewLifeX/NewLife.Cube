/**
 * 登录页 API 封装
 *
 * 使用原生 fetch 调用后端接口，不依赖 demo app 的 cubeApi 实例。
 * core 库作为通用库，不能依赖具体应用创建的 API 客户端。
 *
 * 类型定义复用 @cube/api-core 的 LoginConfig、LoginResult、ApiResponse。
 *
 * 注意：由于使用原生 fetch（不经 @cube/api-core 的 axios 拦截器），
 * 后端返回的字段名归一化需在本文件内手动完成：
 * - LoginConfig: oAuth / providers → oauth
 * - LoginResult: access_token / Token → accessToken（snake_case + PascalCase 兼容）
 */
import type { ApiResponse, LoginConfig, LoginResult, OAuthProvider } from '@cube/api-core';

/** 默认请求超时时间（毫秒） */
const REQUEST_TIMEOUT = 15000;

/**
 * 带超时的 fetch 封装
 * @param url 请求地址
 * @param init fetch 初始化配置
 * @returns fetch Response
 */
async function fetchWithTimeout(url: string, init: RequestInit): Promise<Response> {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT);
  try {
    return await fetch(url, { ...init, signal: controller.signal });
  } finally {
    clearTimeout(timeoutId);
  }
}

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
 * @param baseUrl 后端 API 基础地址（如 https://localhost:7116）
 * @returns 登录配置响应
 * @throws 网络错误或 HTTP 状态码非 200 时抛出异常
 */
export async function fetchLoginConfig(baseUrl: string): Promise<ApiResponse<LoginConfig>> {
  const url = `${baseUrl}/Auth/LoginConfig`;
  const response = await fetchWithTimeout(url, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`获取登录配置失败: HTTP ${response.status} ${response.statusText}`);
  }

  const json = (await response.json()) as ApiResponse<LoginConfig>;

  // 归一化字段名（oAuth / providers → oauth）
  if (json.data) {
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
 * @param baseUrl 后端 API 基础地址（如 https://localhost:7116）
 * @param username 用户名
 * @param password 密码（明文，通过 HTTPS 传输）
 * @returns 登录结果响应，data.accessToken 为访问令牌
 * @throws 网络错误或 HTTP 状态码非 200 时抛出异常
 */
export async function loginByPassword(
  baseUrl: string,
  username: string,
  password: string,
): Promise<ApiResponse<LoginResult>> {
  const url = `${baseUrl}/Auth/Login`;
  const response = await fetchWithTimeout(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ username, password }),
  });

  if (!response.ok) {
    throw new Error(`登录请求失败: HTTP ${response.status} ${response.statusText}`);
  }

  const json = (await response.json()) as ApiResponse<LoginResult>;

  // 归一化字段名（snake_case / PascalCase → camelCase）
  if (json.data) {
    json.data = normalizeLoginResult(json.data);
  }

  return json;
}
