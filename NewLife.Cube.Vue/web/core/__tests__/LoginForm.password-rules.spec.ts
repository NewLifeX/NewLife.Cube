import { flushPromises, mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import ElementPlus from 'element-plus';
import LoginForm from '../pages/LoginForm.vue';
import type { LoginConfig } from '@cube/api-core';

interface Scenario {
  key: string;
  strength: string;
  failingPwd: string;
  validPwd: string;
  hintCount: number;
  expectedError: string;
}

const SCENARIOS: Scenario[] = [
  { key: 'A', strength: '*', failingPwd: 'ab', validPwd: 'abcde', hintCount: 1, expectedError: '密码需至少 5 位' },
  { key: 'B', strength: '.{6,}', failingPwd: 'abc', validPwd: 'abcdef', hintCount: 1, expectedError: '密码需至少 6 位' },
  { key: 'C', strength: '^(?=.*\\d)(?=.*[a-z])(?=.*[A-Z]).{8,}$', failingPwd: 'Ab1', validPwd: 'Abcdef12', hintCount: 4, expectedError: '密码需至少 8 位' },
  { key: 'D', strength: '', failingPwd: 'ab', validPwd: 'abcde', hintCount: 1, expectedError: '密码需至少 5 位' },
  { key: 'E', strength: '^[a-z]+$', failingPwd: 'abc123', validPwd: 'abc', hintCount: 1, expectedError: '密码需符合密码安全规则' },
];

function mountForm(strength: string) {
  return mount(LoginForm, {
    props: {
      loginConfig: {
        security: { passwordStrength: strength },
      } as LoginConfig,
    },
    global: {
      plugins: [ElementPlus],
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

describe('LoginForm 密码规则动态校验', () => {
  SCENARIOS.forEach((scenario) => {
    it(`场景 ${scenario.key}：失败时给出正确错误，成功时触发提交`, async () => {
      const wrapper = mountForm(scenario.strength);
      await setLoginCredentials(wrapper, 'admin', scenario.failingPwd);

      expect(wrapper.findAll('.password-hint-item').length).toBe(scenario.hintCount);
      await wrapper.find('form').trigger('submit.prevent');
      await flushPromises();
      expect(wrapper.text()).toContain(scenario.expectedError);
      expect(wrapper.emitted('submit')).toBeFalsy();

      await setLoginCredentials(wrapper, 'admin', scenario.validPwd);
      if (scenario.hintCount > 0) {
        expect(wrapper.findAll('.password-hint-item.satisfied').length).toBe(scenario.hintCount);
      }
      await wrapper.find('form').trigger('submit.prevent');
      await flushPromises();
      expect(wrapper.emitted('submit')).toBeTruthy();
      expect(wrapper.emitted('submit')?.[0]?.[0]).toEqual({ username: 'admin', password: scenario.validPwd });
    });
  });
});
