import { flushPromises, mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import ElementPlus from 'element-plus';
import LoginForm from '../pages/LoginForm.vue';
import i18n from '../i18n';
import type { LoginConfig } from '@newlifex/cube';

function mountForm(loginConfig?: LoginConfig) {
  return mount(LoginForm, {
    props: { loginConfig },
    global: {
      plugins: [ElementPlus, i18n],
    },
  });
}

async function setLoginCredentials(wrapper: ReturnType<typeof mountForm>, username: string, password: string) {
  const inputs = wrapper.findAllComponents({ name: 'ElInput' });
  expect(inputs.length).toBeGreaterThanOrEqual(2);
  await inputs[0].vm.$emit('update:modelValue', username);
  await inputs[1].vm.$emit('update:modelValue', password);
  await flushPromises();
}

describe('LoginForm 登录密码校验', () => {
  it('场景 A：弱密码（默认 admin）不被密码复杂度拦截，直接提交', async () => {
    // 后端启用严格复杂度正则也不影响登录：存量用户可能使用弱密码
    const wrapper = mountForm({
      security: { passwordStrength: '^(?=.*\\d)(?=.*[a-z])(?=.*[A-Z]).{8,}$', passwordComplexity: true },
    } as LoginConfig);
    await setLoginCredentials(wrapper, 'admin', 'admin');

    // 不展示密码复杂度提示
    expect(wrapper.findAll('.password-hint-item').length).toBe(0);

    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(wrapper.emitted('submit')).toBeTruthy();
    expect(wrapper.emitted('submit')?.[0]?.[0]).toEqual({ username: 'admin', password: 'admin' });
  });

  it('场景 B：空密码被拦截，提示输入密码', async () => {
    const wrapper = mountForm({ security: { passwordStrength: '^(?=.*\\d)(?=.*[a-z])(?=.*[A-Z]).{8,}$' } } as LoginConfig);
    await setLoginCredentials(wrapper, 'admin', '');

    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(wrapper.text()).toContain(i18n.global.t('register.enterPassword'));
    expect(wrapper.emitted('submit')).toBeFalsy();
  });

  it('场景 C：复杂度开关关闭时同样允许弱密码提交', async () => {
    const wrapper = mountForm({ security: { passwordStrength: '.{8,}', passwordComplexity: false } } as LoginConfig);
    await setLoginCredentials(wrapper, 'admin', 'weak');

    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(wrapper.emitted('submit')).toBeTruthy();
    expect(wrapper.emitted('submit')?.[0]?.[0]).toEqual({ username: 'admin', password: 'weak' });
  });
});
