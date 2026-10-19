import { describe, expect, it } from 'vitest';
import {
  buildRegisterPayload,
  getRegisterErrorMessage,
  isValidEmail,
  isValidMobile,
  resolveRegisterModes,
  resolveRegisterTenant,
  validateRegister,
  type RegisterConfig,
  type RegisterFormInput,
} from '@/utils/register';

function makeInput(overrides: Partial<RegisterFormInput> = {}): RegisterFormInput {
  return {
    username: '',
    email: '',
    mobile: '',
    password: '',
    confirmPassword: '',
    code: '',
    captchaCode: '',
    ...overrides,
  };
}

const fullConfig: RegisterConfig = {
  register: { enabled: true, password: true, sms: true, mail: true, captcha: false },
  security: { passwordComplexity: true, passwordStrength: '.{6,}' },
};

describe('resolveRegisterModes', () => {
  it('未开放注册时返回空列表', () => {
    expect(resolveRegisterModes({ enabled: false, password: true })).toEqual([]);
  });

  it('旧版配置未返回 password/sms/mail 时回退密码注册', () => {
    expect(resolveRegisterModes({ enabled: true })).toEqual(['password']);
  });

  it('按密码、手机、邮箱顺序返回已启用方式', () => {
    expect(resolveRegisterModes({ enabled: true, password: true, sms: true, mail: true })).toEqual([
      'password',
      'mobile',
      'mail',
    ]);
  });

  it('只启用验证码注册时不额外回退密码注册', () => {
    expect(resolveRegisterModes({ enabled: true, sms: true })).toEqual(['mobile']);
  });

  it('显式关闭密码注册且无其他方式时返回空列表', () => {
    expect(resolveRegisterModes({ enabled: true, password: false })).toEqual([]);
  });
});

describe('validateRegister - 用户名密码注册', () => {
  it('填写完整时通过并构造无 category 的载荷', () => {
    const input = makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd' });
    const result = validateRegister(input, fullConfig, 'password');

    expect(result.valid).toBe(true);
    expect(result.errors).toEqual({});
    expect(buildRegisterPayload(input, 'password')).toMatchObject({
      username: 'zhangsan',
      password: 'P@ssw0rd',
      confirmPassword: 'P@ssw0rd',
    });
    expect(buildRegisterPayload(input, 'password').category).toBeUndefined();
  });

  it('用户名和密码为空时分别给出提示', () => {
    const result = validateRegister(makeInput(), fullConfig, 'password');

    expect(result.valid).toBe(false);
    expect(result.errors.username).toBe('请输入用户名');
    expect(result.errors.password).toBe('请输入密码');
  });

  it('用户名过短时拦截', () => {
    const result = validateRegister(
      makeInput({ username: '张', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd' }),
      fullConfig,
      'password',
    );

    expect(result.errors.username).toBe('用户名至少 2 个字符');
  });

  it('两次密码不一致时拦截', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd2' }),
      fullConfig,
      'password',
    );

    expect(result.valid).toBe(false);
    expect(result.errors.confirmPassword).toBe('两次输入密码不一致');
  });

  it('密码强度正则不满足时拦截', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: '123', confirmPassword: '123' }),
      fullConfig,
      'password',
    );

    expect(result.valid).toBe(false);
    expect(result.errors.password).toBe('密码强度不符合要求');
  });

  it('passwordStrength 为 * 时不限制强度', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: '1', confirmPassword: '1' }),
      { ...fullConfig, security: { passwordComplexity: true, passwordStrength: '*' } },
      'password',
    );

    expect(result.valid).toBe(true);
  });

  it('非法邮箱/手机号即使非必填也拦截', () => {
    const result = validateRegister(
      makeInput({
        username: 'zhangsan',
        email: 'not-mail',
        mobile: '12345',
        password: 'P@ssw0rd',
        confirmPassword: 'P@ssw0rd',
      }),
      fullConfig,
      'password',
    );

    expect(result.errors.email).toBe('邮箱格式不正确');
    expect(result.errors.mobile).toBe('手机号格式不正确');
  });
});

describe('validateRegister - 手机/邮箱验证码注册', () => {
  it('手机注册：手机号与验证码必填，密码可留空', () => {
    const input = makeInput({ mobile: '13800138000', code: '123456' });
    const result = validateRegister(input, fullConfig, 'mobile');

    expect(result.valid).toBe(true);
    expect(buildRegisterPayload(input, 'mobile')).toMatchObject({
      category: 'mobile',
      mobile: '13800138000',
      code: '123456',
      password: '',
    });
  });

  it('手机注册：手机号格式错误时拦截', () => {
    const result = validateRegister(makeInput({ mobile: '12345', code: '123456' }), fullConfig, 'mobile');

    expect(result.errors.mobile).toBe('手机号格式不正确');
  });

  it('手机注册：缺少验证码时拦截', () => {
    const result = validateRegister(makeInput({ mobile: '13800138000' }), fullConfig, 'mobile');

    expect(result.errors.code).toBe('请输入手机验证码');
  });

  it('邮箱注册：邮箱与验证码必填，密码可留空', () => {
    const input = makeInput({ email: 'zhangsan@example.com', code: '654321' });
    const result = validateRegister(input, fullConfig, 'mail');

    expect(result.valid).toBe(true);
    expect(buildRegisterPayload(input, 'mail')).toMatchObject({
      category: 'mail',
      email: 'zhangsan@example.com',
      code: '654321',
    });
  });

  it('邮箱注册：缺少邮箱或验证码时分别提示', () => {
    const result = validateRegister(makeInput(), fullConfig, 'mail');

    expect(result.errors.email).toBe('请输入邮箱');
    expect(result.errors.code).toBe('请输入邮箱验证码');
  });

  it('验证码注册填写密码时必须二次确认且一致', () => {
    const result = validateRegister(
      makeInput({ mobile: '13800138000', code: '123456', password: 'P@ssw0rd' }),
      fullConfig,
      'mobile',
    );

    expect(result.errors.confirmPassword).toBe('请再次输入密码');
  });
});

describe('validateRegister - 联系方式验证开关与图片验证码', () => {
  it('requireMailVerify 强制填写邮箱', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd' }),
      { ...fullConfig, register: { ...fullConfig.register, requireMailVerify: true } },
      'password',
    );

    expect(result.errors.email).toBe('需要邮箱验证，请填写邮箱');
  });

  it('requireMobileVerify 强制填写手机号', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd' }),
      { ...fullConfig, register: { ...fullConfig.register, requireMobileVerify: true } },
      'password',
    );

    expect(result.errors.mobile).toBe('需要手机验证，请填写手机号');
  });

  it('图片验证码缺少 ID 时提示刷新', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd' }),
      fullConfig,
      'password',
      { captchaRequired: true },
    );

    expect(result.errors.captchaCode).toBe('图片验证码加载失败，请刷新');
  });

  it('图片验证码 ID 存在但未填写时拦截', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd' }),
      fullConfig,
      'password',
      { captchaRequired: true, captchaId: 'cap-1' },
    );

    expect(result.errors.captchaCode).toBe('请输入图片验证码');
  });

  it('填写图片验证码后通过', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd', captchaCode: '12' }),
      fullConfig,
      'password',
      { captchaRequired: true, captchaId: 'cap-1' },
    );

    expect(result.valid).toBe(true);
  });
});

describe('buildRegisterPayload - 租户与验证码透传', () => {
  it('附加 captchaId/captchaCode 与 X-App-Id/X-Tenant 对应字段', () => {
    const payload = buildRegisterPayload(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd', captchaCode: '12' }),
      'password',
      { captchaId: 'cap-1', appId: 'wx-app', tenantCode: 'tenant-a' },
    );

    expect(payload.captchaId).toBe('cap-1');
    expect(payload.captchaCode).toBe('12');
    expect(payload.appId).toBe('wx-app');
    expect(payload.tenantCode).toBe('tenant-a');
  });
});

describe('resolveRegisterTenant', () => {
  it('支持 appId 与 tenantCode/tenant 查询参数', () => {
    expect(resolveRegisterTenant({ appId: ' wx-app ', tenant: 'tenant-a' })).toEqual({
      appId: 'wx-app',
      tenantCode: 'tenant-a',
    });
  });

  it('支持数组查询参数并忽略空值', () => {
    expect(resolveRegisterTenant({ app_id: ['', 'wx-app'], tenantCode: ['tenant-b'] })).toEqual({
      appId: 'wx-app',
      tenantCode: 'tenant-b',
    });
    expect(resolveRegisterTenant({})).toEqual({ appId: undefined, tenantCode: undefined });
  });
});

describe('辅助函数', () => {
  it('邮箱/手机号格式校验', () => {
    expect(isValidEmail('zhangsan@example.com')).toBe(true);
    expect(isValidEmail('zhangsan@')).toBe(false);
    expect(isValidMobile('13800138000')).toBe(true);
    expect(isValidMobile('12800138000')).toBe(false);
  });

  it('从异常或字符串提取错误信息', () => {
    expect(getRegisterErrorMessage(new Error('用户名已存在'))).toBe('用户名已存在');
    expect(getRegisterErrorMessage('网络错误')).toBe('网络错误');
    expect(getRegisterErrorMessage(undefined)).toBe('注册失败，请稍后重试');
  });
});
