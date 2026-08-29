import { flushPromises, mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import ElementPlus from 'element-plus';
import RegisterForm from '../pages/RegisterForm.vue';
import type { LoginConfig } from '@cube/api-core';

function mountForm(registerOverrides: Record<string, unknown> = {}) {
  return mount(RegisterForm, {
    props: {
      loginConfig: {
        register: {
          enabled: true,
          password: true,
          ...registerOverrides,
        },
        security: { passwordStrength: '.{6,}', passwordComplexity: true },
      } as LoginConfig,
    },
    global: {
      plugins: [ElementPlus],
    },
  });
}

/**
 * 按表单标签文本定位输入组并设置内部 input 值。
 * 标签：用户名 / 邮箱 / 手机号 / 邮箱验证码 / 短信验证码 / 密码 / 确认密码（忽略必填 * 号）。
 */
async function setInput(wrapper: ReturnType<typeof mountForm>, label: string, value: string) {
  const groups = wrapper.findAll('.input-group');
  const group = groups.find((g) => {
    const text = g.find('.input-label').text().replace('*', '').trim();
    return text === label;
  });
  expect(group, `未找到标签[${label}]的输入组`).toBeTruthy();
  await group!.find('input').setValue(value);
  await flushPromises();
}

/** 提交表单 */
async function submit(wrapper: ReturnType<typeof mountForm>) {
  await wrapper.find('form').trigger('submit.prevent');
  await flushPromises();
}

describe('RegisterForm 注册表单', () => {
  it('默认场景：用户名+密码注册通过并提交 category=password', async () => {
    const wrapper = mountForm();
    await setInput(wrapper, '用户名', 'zhangsan');
    await setInput(wrapper, '密码', 'P@ssw0rd');
    await setInput(wrapper, '确认密码', 'P@ssw0rd');
    await submit(wrapper);

    const emitted = wrapper.emitted('submit');
    expect(emitted).toBeTruthy();
    const payload = emitted![0][0] as Record<string, unknown>;
    expect(payload.category).toBe('password');
    expect(payload.username).toBe('zhangsan');
  });

  it('requireMailVerify：邮箱必填，为空时拦截并提示', async () => {
    const wrapper = mountForm({ requireMailVerify: true });
    await setInput(wrapper, '用户名', 'zhangsan');
    await setInput(wrapper, '密码', 'P@ssw0rd');
    await setInput(wrapper, '确认密码', 'P@ssw0rd');
    await submit(wrapper);

    expect(wrapper.emitted('submit')).toBeFalsy();
    expect(wrapper.text()).toContain('需要邮箱验证，请填写邮箱');
  });

  it('requireMobileVerify：手机必填，为空时拦截并提示', async () => {
    const wrapper = mountForm({ requireMobileVerify: true });
    await setInput(wrapper, '用户名', 'zhangsan');
    await setInput(wrapper, '密码', 'P@ssw0rd');
    await setInput(wrapper, '确认密码', 'P@ssw0rd');
    await submit(wrapper);

    expect(wrapper.emitted('submit')).toBeFalsy();
    expect(wrapper.text()).toContain('需要手机验证，请填写手机号');
  });

  it('requireMailVerify + requireMobileVerify 同时开启：邮箱手机都必填', async () => {
    const wrapper = mountForm({ requireMailVerify: true, requireMobileVerify: true });
    await setInput(wrapper, '用户名', 'zhangsan');
    await setInput(wrapper, '密码', 'P@ssw0rd');
    await setInput(wrapper, '确认密码', 'P@ssw0rd');
    await submit(wrapper);

    expect(wrapper.emitted('submit')).toBeFalsy();
    expect(wrapper.text()).toContain('需要邮箱验证，请填写邮箱');
    expect(wrapper.text()).toContain('需要手机验证，请填写手机号');
  });

  it('填写手机验证码：提交 category=mobile 并携带手机与验证码', async () => {
    const wrapper = mountForm();
    await setInput(wrapper, '手机号', '13800138000');
    await setInput(wrapper, '短信验证码', '123456');
    await setInput(wrapper, '密码', 'P@ssw0rd');
    await setInput(wrapper, '确认密码', 'P@ssw0rd');
    await submit(wrapper);

    const payload = wrapper.emitted('submit')![0][0] as Record<string, unknown>;
    expect(payload.category).toBe('mobile');
    expect(payload.mobile).toBe('13800138000');
    expect(payload.code).toBe('123456');
  });

  it('填写邮箱验证码：提交 category=mail 并携带邮箱与验证码', async () => {
    const wrapper = mountForm();
    await setInput(wrapper, '邮箱', 'zhangsan@example.com');
    await setInput(wrapper, '邮箱验证码', '123456');
    await setInput(wrapper, '密码', 'P@ssw0rd');
    await setInput(wrapper, '确认密码', 'P@ssw0rd');
    await submit(wrapper);

    const payload = wrapper.emitted('submit')![0][0] as Record<string, unknown>;
    expect(payload.category).toBe('mail');
    expect(payload.email).toBe('zhangsan@example.com');
    expect(payload.code).toBe('123456');
  });

  it('两次密码不一致：拦截并提示', async () => {
    const wrapper = mountForm();
    await setInput(wrapper, '用户名', 'zhangsan');
    await setInput(wrapper, '密码', 'P@ssw0rd');
    await setInput(wrapper, '确认密码', 'P@ssw0rdDiff');
    await submit(wrapper);

    expect(wrapper.emitted('submit')).toBeFalsy();
    expect(wrapper.text()).toContain('两次输入密码不一致');
  });

  it('发码按钮：邮箱为空时提示，不发码', async () => {
    const wrapper = mountForm();
    await wrapper.findAll('.code-btn')[0].trigger('click');
    await flushPromises();

    expect(wrapper.emitted('sendCode')).toBeFalsy();
    expect(wrapper.text()).toContain('请输入邮箱');
  });

  it('发码按钮：填写有效邮箱后发出 sendCode 事件', async () => {
    const wrapper = mountForm();
    await setInput(wrapper, '邮箱', 'zhangsan@example.com');
    await wrapper.findAll('.code-btn')[0].trigger('click');
    await flushPromises();

    const emitted = wrapper.emitted('sendCode');
    expect(emitted).toBeTruthy();
    expect(emitted![0]).toEqual(['mail', 'zhangsan@example.com']);
  });
});
