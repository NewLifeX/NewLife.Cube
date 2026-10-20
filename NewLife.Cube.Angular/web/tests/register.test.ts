/**
 * Angular 注册入口纯逻辑单元测试（SKIN-7c）。
 *
 * Angular 皮肤当前没有 Karma/Vitest 等浏览器测试基建，为不引入大型依赖，
 * 这里使用 Node 内置测试运行器（Node 24 原生 TypeScript 类型擦除）直接运行 `.ts` 测试。
 * 运行：`pnpm --filter ./NewLife.Cube.Angular/web run test:unit`
 */

import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
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
} from '../src/app/utils/register.ts';

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
    assert.deepEqual(resolveRegisterModes({ enabled: false, password: true }), []);
  });

  it('旧版配置未返回 password/sms/mail 时回退密码注册', () => {
    assert.deepEqual(resolveRegisterModes({ enabled: true }), ['password']);
  });

  it('按密码、手机、邮箱顺序返回已启用方式', () => {
    assert.deepEqual(resolveRegisterModes({ enabled: true, password: true, sms: true, mail: true }), [
      'password',
      'mobile',
      'mail',
    ]);
  });

  it('只启用验证码注册时不额外回退密码注册', () => {
    assert.deepEqual(resolveRegisterModes({ enabled: true, sms: true }), ['mobile']);
  });

  it('显式关闭密码注册且无其他方式时返回空列表', () => {
    assert.deepEqual(resolveRegisterModes({ enabled: true, password: false }), []);
  });
});

describe('validateRegister - 用户名密码注册', () => {
  it('填写完整时通过并构造无 category 的载荷', () => {
    const input = makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd' });
    const result = validateRegister(input, fullConfig, 'password');

    assert.equal(result.valid, true);
    assert.deepEqual(result.errors, {});
    assert.deepEqual(buildRegisterPayload(input, 'password'), {
      username: 'zhangsan',
      email: undefined,
      mobile: undefined,
      password: 'P@ssw0rd',
      confirmPassword: 'P@ssw0rd',
    });
    assert.equal(buildRegisterPayload(input, 'password').category, undefined);
  });

  it('用户名和密码为空时分别给出提示', () => {
    const result = validateRegister(makeInput(), fullConfig, 'password');

    assert.equal(result.valid, false);
    assert.equal(result.errors.username, '请输入用户名');
    assert.equal(result.errors.password, '请输入密码');
  });

  it('用户名过短时拦截', () => {
    const result = validateRegister(
      makeInput({ username: '张', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd' }),
      fullConfig,
      'password',
    );

    assert.equal(result.errors.username, '用户名至少 2 个字符');
  });

  it('两次密码不一致时拦截', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd2' }),
      fullConfig,
      'password',
    );

    assert.equal(result.valid, false);
    assert.equal(result.errors.confirmPassword, '两次输入密码不一致');
  });

  it('密码强度正则不满足时拦截', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: '123', confirmPassword: '123' }),
      fullConfig,
      'password',
    );

    assert.equal(result.valid, false);
    assert.equal(result.errors.password, '密码强度不符合要求');
  });

  it('passwordStrength 为 * 时不限制强度', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: '1', confirmPassword: '1' }),
      { ...fullConfig, security: { passwordComplexity: true, passwordStrength: '*' } },
      'password',
    );

    assert.equal(result.valid, true);
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

    assert.equal(result.errors.email, '邮箱格式不正确');
    assert.equal(result.errors.mobile, '手机号格式不正确');
  });
});

describe('validateRegister - 手机/邮箱验证码注册', () => {
  it('手机注册：手机号与验证码必填，密码可留空', () => {
    const input = makeInput({ mobile: '13800138000', code: '123456' });
    const result = validateRegister(input, fullConfig, 'mobile');

    assert.equal(result.valid, true);
    assert.deepEqual(buildRegisterPayload(input, 'mobile'), {
      category: 'mobile',
      username: undefined,
      email: undefined,
      mobile: '13800138000',
      password: '',
      confirmPassword: '',
      code: '123456',
    });
  });

  it('手机注册：手机号格式错误时拦截', () => {
    const result = validateRegister(makeInput({ mobile: '12345', code: '123456' }), fullConfig, 'mobile');

    assert.equal(result.errors.mobile, '手机号格式不正确');
  });

  it('手机注册：缺少验证码时拦截', () => {
    const result = validateRegister(makeInput({ mobile: '13800138000' }), fullConfig, 'mobile');

    assert.equal(result.errors.code, '请输入手机验证码');
  });

  it('邮箱注册：邮箱与验证码必填，密码可留空', () => {
    const input = makeInput({ email: 'zhangsan@example.com', code: '654321' });
    const result = validateRegister(input, fullConfig, 'mail');

    assert.equal(result.valid, true);
    assert.deepEqual(buildRegisterPayload(input, 'mail'), {
      category: 'mail',
      username: undefined,
      email: 'zhangsan@example.com',
      mobile: undefined,
      password: '',
      confirmPassword: '',
      code: '654321',
    });
  });

  it('邮箱注册：缺少邮箱或验证码时分别提示', () => {
    const result = validateRegister(makeInput(), fullConfig, 'mail');

    assert.equal(result.errors.email, '请输入邮箱');
    assert.equal(result.errors.code, '请输入邮箱验证码');
  });

  it('验证码注册填写密码时必须二次确认且一致', () => {
    const result = validateRegister(
      makeInput({ mobile: '13800138000', code: '123456', password: 'P@ssw0rd' }),
      fullConfig,
      'mobile',
    );

    assert.equal(result.errors.confirmPassword, '请再次输入密码');
  });
});

describe('validateRegister - 联系方式验证开关与图片验证码', () => {
  it('requireMailVerify 强制填写邮箱', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd' }),
      { ...fullConfig, register: { ...fullConfig.register, requireMailVerify: true } },
      'password',
    );

    assert.equal(result.errors.email, '需要邮箱验证，请填写邮箱');
  });

  it('requireMobileVerify 强制填写手机号', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd' }),
      { ...fullConfig, register: { ...fullConfig.register, requireMobileVerify: true } },
      'password',
    );

    assert.equal(result.errors.mobile, '需要手机验证，请填写手机号');
  });

  it('图片验证码缺少 ID 时提示刷新', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd' }),
      fullConfig,
      'password',
      { captchaRequired: true },
    );

    assert.equal(result.errors.captchaCode, '图片验证码加载失败，请刷新');
  });

  it('图片验证码 ID 存在但未填写时拦截', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd' }),
      fullConfig,
      'password',
      { captchaRequired: true, captchaId: 'cap-1' },
    );

    assert.equal(result.errors.captchaCode, '请输入图片验证码');
  });

  it('填写图片验证码后通过', () => {
    const result = validateRegister(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd', captchaCode: '12' }),
      fullConfig,
      'password',
      { captchaRequired: true, captchaId: 'cap-1' },
    );

    assert.equal(result.valid, true);
  });
});

describe('buildRegisterPayload - 租户与验证码透传', () => {
  it('附加 captchaId/captchaCode 与 X-App-Id/X-Tenant 对应字段', () => {
    const payload = buildRegisterPayload(
      makeInput({ username: 'zhangsan', password: 'P@ssw0rd', confirmPassword: 'P@ssw0rd', captchaCode: '12' }),
      'password',
      { captchaId: 'cap-1', appId: 'wx-app', tenantCode: 'tenant-a' },
    );

    assert.equal(payload.captchaId, 'cap-1');
    assert.equal(payload.captchaCode, '12');
    assert.equal(payload.appId, 'wx-app');
    assert.equal(payload.tenantCode, 'tenant-a');
  });
});

describe('resolveRegisterTenant', () => {
  it('支持 appId 与 tenantCode/tenant 查询参数', () => {
    assert.deepEqual(resolveRegisterTenant({ appId: ' wx-app ', tenant: 'tenant-a' }), {
      appId: 'wx-app',
      tenantCode: 'tenant-a',
    });
  });

  it('支持数组查询参数并忽略空值', () => {
    assert.deepEqual(resolveRegisterTenant({ app_id: ['', 'wx-app'], tenantCode: ['tenant-b'] }), {
      appId: 'wx-app',
      tenantCode: 'tenant-b',
    });
    assert.deepEqual(resolveRegisterTenant({}), { appId: undefined, tenantCode: undefined });
  });
});

describe('辅助函数', () => {
  it('邮箱/手机号格式校验', () => {
    assert.equal(isValidEmail('zhangsan@example.com'), true);
    assert.equal(isValidEmail('zhangsan@'), false);
    assert.equal(isValidMobile('13800138000'), true);
    assert.equal(isValidMobile('12800138000'), false);
  });

  it('从异常或字符串提取错误信息', () => {
    assert.equal(getRegisterErrorMessage(new Error('用户名已存在')), '用户名已存在');
    assert.equal(getRegisterErrorMessage('网络错误'), '网络错误');
    assert.equal(getRegisterErrorMessage(undefined), '注册失败，请稍后重试');
  });
});
