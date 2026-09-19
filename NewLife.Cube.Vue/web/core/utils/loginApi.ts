/**
 * 登录页 API 封装（兼容层）
 *
 * 本文件仅作为皮肤兼容壳：保留原有导出函数名与签名，供 LoginPage / ActivatePage /
 * ProfileSecurity 等业务页零改动调用；实际请求全部委托 @newlifex/api-core 的
 * user 认证 API（注册 / 发码 / 验证码 / 激活 / 验证联系方式等），不重复实现基础库已有功能。
 *
 * 基础库已下沉的能力（不再在此处实现）：
 * - 请求层（host 拼接、/api 前缀、Token 头、204 处理、错误归一化、401 跳转）见 core/utils/request；
 * - 登录结果字段归一化（access_token / Token → accessToken）由 api-core 响应拦截器统一处理
 *   （LOGIN_RESULT_PATHS 含 /Auth/Login、/Auth/Register、/Auth/Refresh 等），皮肤无需重复归一化。
 *
 * 类型定义复用 @newlifex/api-core 的 LoginConfig、LoginResult、ApiResponse 等。
 *
 * 注意：基础库的响应拦截器会在 code !== 0/200 时自动弹出错误提示并抛异常，
 * 业务调用方在 catch 中处理失败即可（无需自行弹错，避免重复提示）。
 */
import { user } from './request';
import type { ApiResponse, LoginConfig, LoginResult, OAuthProvider, RegisterModel, VerifyStatus } from '@newlifex/api-core';

// ── 字段名归一化工具（皮肤专属，基础库无此逻辑，仅保留） ─────────────

/**
 * 归一化登录配置字段名
 *
 * 后端 /Auth/LoginConfig 可能返回以下字段名变体：
 * - `oAuth`（C# 属性名 PascalCase 风格，首字母大写 O）→ 标准字段 `oauth`
 * - `providers`（v1 旧版字段名）→ 标准字段 `oauth`
 *
 * 归一化后确保 LoginConfig.oauth 始终有值（如果后端返回了的话）。
 * 该别名兼容由后端历史版本决定，基础库未做此处理，故保留在皮肤层。
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

// ── API 函数（全部委托 @newlifex/api-core 的 user 认证 API） ─────────

/**
 * 获取登录配置
 *
 * 委托 user.getLoginConfig 调用 GET /Auth/LoginConfig，返回系统名称、Logo、版权、
 * 登录/注册能力、OAuth 提供商列表、安全策略等。
 *
 * 返回前对 data 做字段名归一化（oAuth / providers → oauth），该别名兼容为基础库未覆盖，
 * 故保留在皮肤层（@newlifex/api-core 不含此逻辑）。
 *
 * @returns 登录配置响应
 * @throws 网络错误或 HTTP 状态码非 200 时抛出异常
 */
export async function fetchLoginConfig(): Promise<ApiResponse<LoginConfig>> {
  const json = await user.getLoginConfig();
  if (json?.data) {
    json.data = normalizeLoginConfig(json.data);
  }
  return json;
}

/**
 * 密码登录
 *
 * @deprecated 通用登录逻辑已下沉到 @newlifex/api-core（user.loginWithPassword，皮肤共用），
 * 本函数仅保留骨架兼容旧调用方式，勿再在核心逻辑处使用。新代码请直接调
 * cubeApi.user.loginWithPassword(username, password, { captchaId, captchaCode, remember })。
 *
 * 登录逻辑跟随后端设置：先 GET /Auth/Challenge 获取 RSA 公钥加密密码并携带 challengeId 登录，
 * 服务端不支持 / 不可达 / 加密失败时降级为明文传输（由 api-core 统一处理）。
 *
 * @param username 用户名
 * @param password 密码（明文，内部按需 RSA 加密后传输）
 * @param captchaId 图片验证码 ID（LoginConfig.login.captcha 为 true 时需传入）
 * @param captchaCode 图片验证码输入（LoginConfig.login.captcha 为 true 时需传入）
 * @param remember 记住登录状态（true 时后端把令牌有效期延长到 365 天）
 * @returns 登录结果 ApiResponse，data 含 accessToken（已由 api-core 归一化 camelCase）
 */
export async function loginByPassword(
  username: string,
  password: string,
  captchaId?: string,
  captchaCode?: string,
  remember?: boolean,
): Promise<ApiResponse<LoginResult>> {
  return user.loginWithPassword(username, password, { captchaId, captchaCode, remember });
}

// ── 注册 / 验证码 / 激活 / 安全中心 ─────────────────────────────────

/**
 * 注册新用户
 *
 * 委托 user.register 调用 POST /Auth/Register。开启邮箱/手机验证时，
 * 成功返回 data.pendingActivation=true（待激活），不返回 token。
 *
 * 注册结果字段归一化（access_token / Token → accessToken）已由 api-core 响应拦截器统一处理，
 * 此处不再重复归一化。
 *
 * @param data 注册参数（category/username/email/mobile/password/confirmPassword/code/captchaId/captchaCode）
 * @returns 注册结果：data.accessToken 表示已登录；data.pendingActivation 表示待激活
 */
export async function registerByForm(data: RegisterModel): Promise<ApiResponse<LoginResult>> {
  return user.register(data);
}

/**
 * 发送验证码（注册/绑定等场景）
 *
 * 委托 user.sendCode 调用 POST /Auth/SendCode。
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
  return user.sendCode({ channel, username, action, captchaId, captchaCode });
}

/**
 * 获取图片验证码（SVG 算数题）
 *
 * 委托 user.getCaptcha 调用 GET /Auth/Captcha，返回 captchaId 与 SVG 文本。
 * 注册/发码需要图片验证码时（LoginConfig.register.captcha / login.sendCode），
 * 先调用本接口获取 captchaId 与 SVG 文本，随提交请求一并回传。
 */
export async function fetchCaptcha(): Promise<ApiResponse<{ captchaId: string; image: string }>> {
  return user.getCaptcha();
}

/**
 * 邮箱激活链接直达（激活邮件中的链接指向 /activate?token=&account=，激活页解析后调用）
 */
export async function activateByLink(
  token: string,
  account: string,
): Promise<ApiResponse<{ activated: boolean }>> {
  return user.activateByLink(token, account);
}

/**
 * 验证码激活（邮箱验证码/手机短信验证码）
 */
export async function activateByCode(
  channel: string,
  account: string,
  code: string,
): Promise<ApiResponse<{ activated: boolean }>> {
  return user.activateByCode({ channel, account, code });
}

/**
 * 重发激活。未激活账号重新发送激活邮件/短信（登录页「未激活？重新发送」）
 */
export async function sendActivateCode(
  channel: string,
  account: string,
): Promise<ApiResponse<{ target: string }>> {
  return user.sendActivateCode(channel, account);
}

/**
 * 已登录用户验证/更换邮箱或手机（安全中心）。验证码经 sendCode(action=bind) 发送
 */
export async function verifyContact(
  channel: string,
  account: string,
  code: string,
): Promise<ApiResponse<VerifyStatus>> {
  return user.verifyContact({ channel, account, code });
}
