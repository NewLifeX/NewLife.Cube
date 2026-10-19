import type { LoginConfig, RegisterModel } from '@newlifex/api-core';

type RegisterAbility = NonNullable<LoginConfig['register']>;
type SecurityConfig = NonNullable<LoginConfig['security']>;

/** 注册方式。password=用户名密码，mobile=手机验证码，mail=邮箱验证码 */
export type RegisterMode = 'password' | 'mobile' | 'mail';

/** 注册表单输入 */
export interface RegisterFormInput {
  username: string;
  email: string;
  mobile: string;
  password: string;
  confirmPassword: string;
  code: string;
  captchaCode: string;
}

/** 注册表单字段错误 */
export interface RegisterFormErrors {
  username?: string;
  email?: string;
  mobile?: string;
  password?: string;
  confirmPassword?: string;
  code?: string;
  captchaCode?: string;
}

/** 注册校验所需配置（LoginConfig 的子集，便于单测构造） */
export interface RegisterConfig {
  register?: RegisterAbility;
  security?: SecurityConfig;
}

/** 注册请求载荷。在 @newlifex/api-core 的 RegisterModel 基础上补充图片验证码 */
export interface RegisterPayload extends RegisterModel {
  captchaId?: string;
  captchaCode?: string;
}

/** 注册载荷附加信息 */
export interface RegisterPayloadExtras {
  captchaId?: string;
  captchaCode?: string;
  appId?: string;
  tenantCode?: string;
}

/** 注册校验选项 */
export interface RegisterValidationOptions {
  /** 当前场景是否需要图片验证码 */
  captchaRequired?: boolean;
  /** 已获取的图片验证码 ID */
  captchaId?: string;
}

/** 注册校验结果 */
export interface RegisterValidationResult {
  valid: boolean;
  errors: RegisterFormErrors;
}

const MailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const MobilePattern = /^1[3-9]\d{9}$/;

/** 校验邮箱格式 */
export function isValidEmail(value: string): boolean {
  return MailPattern.test(value.trim());
}

/** 校验中国大陆手机号格式 */
export function isValidMobile(value: string): boolean {
  return MobilePattern.test(value.trim());
}

/**
 * 计算当前后端配置允许的注册方式。
 *
 * 旧版后端可能不返回 register.password 等布尔字段，此时回退为允许密码注册。
 * 返回顺序固定为：用户名密码 → 手机 → 邮箱。
 */
export function resolveRegisterModes(register?: RegisterAbility | null): RegisterMode[] {
  if (!register?.enabled) return [];

  const modes: RegisterMode[] = [];
  const hasCodeMode = register.sms === true || register.mail === true;
  // password 显式为 false 时尊重后端配置；仅旧版未返回该字段时回退密码注册
  if (register.password === true || (register.password === undefined && !hasCodeMode)) modes.push('password');
  if (register.sms === true) modes.push('mobile');
  if (register.mail === true) modes.push('mail');

  return modes;
}

/**
 * 校验注册表单。
 *
 * 与后端 `AuthEnhancedService.Register` 契约保持一致：
 * - 用户名密码注册：用户名、密码、确认密码必填；
 * - 手机/邮箱验证码注册：对应联系方式与验证码必填，密码可选（留空由后端生成随机密码）；
 * - `requireMailVerify` / `requireMobileVerify` 开启时，对应联系方式在任意注册方式下必填；
 * - 图片验证码按当前场景（注册或发送验证码）动态要求。
 */
export function validateRegister(
  input: RegisterFormInput,
  config: RegisterConfig | null | undefined,
  mode: RegisterMode,
  options: RegisterValidationOptions = {},
): RegisterValidationResult {
  const errors: RegisterFormErrors = {};
  const register = config?.register;
  const username = input.username.trim();
  const email = input.email.trim();
  const mobile = input.mobile.trim();
  const password = input.password;
  const confirmPassword = input.confirmPassword;

  // 用户名：密码注册必填；验证码注册可选，但填写后至少 2 个字符
  if (mode === 'password' && !username) {
    errors.username = '请输入用户名';
  } else if (username && username.length < 2) {
    errors.username = '用户名至少 2 个字符';
  }

  // 邮箱：邮箱注册或 requireMailVerify 时必填
  if (mode === 'mail' && !email) {
    errors.email = '请输入邮箱';
  } else if (register?.requireMailVerify === true && !email) {
    errors.email = '需要邮箱验证，请填写邮箱';
  } else if (email && !isValidEmail(email)) {
    errors.email = '邮箱格式不正确';
  }

  // 手机号：手机注册或 requireMobileVerify 时必填
  if (mode === 'mobile' && !mobile) {
    errors.mobile = '请输入手机号';
  } else if (register?.requireMobileVerify === true && !mobile) {
    errors.mobile = '需要手机验证，请填写手机号';
  } else if (mobile && !isValidMobile(mobile)) {
    errors.mobile = '手机号格式不正确';
  }

  // 验证码：手机/邮箱验证码注册必填
  if (mode === 'mobile' && !input.code.trim()) {
    errors.code = '请输入手机验证码';
  } else if (mode === 'mail' && !input.code.trim()) {
    errors.code = '请输入邮箱验证码';
  }

  // 密码：密码注册必填；验证码注册可选。填写密码时必须二次确认且满足强度要求
  if (mode === 'password' && !password) {
    errors.password = '请输入密码';
  } else if (password) {
    if (!confirmPassword) {
      errors.confirmPassword = '请再次输入密码';
    } else if (password !== confirmPassword) {
      errors.confirmPassword = '两次输入密码不一致';
    }

    const strength = config?.security?.passwordStrength?.trim();
    if (config?.security?.passwordComplexity !== false && strength && strength !== '*') {
      try {
        if (!new RegExp(strength).test(password)) {
          errors.password = '密码强度不符合要求';
        }
      } catch {
        // 后端正则可能包含 JavaScript 不支持的语法，此时交由后端做最终校验
      }
    }
  } else if (confirmPassword) {
    errors.confirmPassword = '请先输入密码';
  }

  // 图片验证码：仅在当前场景需要时校验
  if (options.captchaRequired) {
    if (!options.captchaId) {
      errors.captchaCode = '图片验证码加载失败，请刷新';
    } else if (!input.captchaCode.trim()) {
      errors.captchaCode = '请输入图片验证码';
    }
  }

  return { valid: Object.keys(errors).length === 0, errors };
}

/**
 * 构造 `/Auth/Register` 请求载荷。
 *
 * category 为空表示用户名密码注册；appId/tenantCode 由 api-core 转为
 * `X-App-Id` / `X-Tenant` 请求头，用于多租户场景定位租户。
 */
export function buildRegisterPayload(
  input: RegisterFormInput,
  mode: RegisterMode,
  extras: RegisterPayloadExtras = {},
): RegisterPayload {
  const category = mode === 'mobile' ? 'mobile' : mode === 'mail' ? 'mail' : undefined;
  const code = input.code.trim();
  const captchaCode = input.captchaCode.trim();

  return {
    ...(category ? { category } : {}),
    username: input.username.trim() || undefined,
    email: input.email.trim() || undefined,
    mobile: input.mobile.trim() || undefined,
    password: input.password,
    confirmPassword: input.confirmPassword,
    ...(mode !== 'password' && code ? { code } : {}),
    ...(extras.captchaId ? { captchaId: extras.captchaId } : {}),
    ...(captchaCode ? { captchaCode } : {}),
    ...(extras.appId ? { appId: extras.appId } : {}),
    ...(extras.tenantCode ? { tenantCode: extras.tenantCode } : {}),
  };
}

function firstQueryString(value: unknown): string | undefined {
  if (Array.isArray(value)) {
    for (const item of value) {
      const text = firstQueryString(item);
      if (text) return text;
    }
    return undefined;
  }
  if (typeof value === 'string') {
    const text = value.trim();
    return text || undefined;
  }
  if (typeof value === 'number') return String(value);
  return undefined;
}

/**
 * 从路由查询参数解析注册租户。
 *
 * 支持 `appId`（OAuth 应用 ID，对应 X-App-Id）与 `tenantCode` / `tenant`
 * （租户编码，对应 X-Tenant）。
 */
export function resolveRegisterTenant(query: Record<string, unknown> = {}): { appId?: string; tenantCode?: string } {
  return {
    appId: firstQueryString(query.appId) ?? firstQueryString(query.app_id),
    tenantCode: firstQueryString(query.tenantCode) ?? firstQueryString(query.tenant) ?? firstQueryString(query.code),
  };
}

/** 从异常中提取用户可读的注册错误信息 */
export function getRegisterErrorMessage(error: unknown): string {
  if (error instanceof Error && error.message) return error.message;
  if (typeof error === 'string' && error) return error;
  return '注册失败，请稍后重试';
}
