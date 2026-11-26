import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it } from 'vitest';
import ElementPlus from 'element-plus';
import LoginForm from '../pages/LoginForm.vue';
import i18n from '../i18n';

function render(rememberEnabled = false) {
  return mount(LoginForm, { props: { rememberEnabled }, global: { plugins: [ElementPlus, i18n] } });
}

async function submit(wrapper: ReturnType<typeof render>) {
  const inputs = wrapper.findAll('input');
  await inputs[0].setValue('admin');
  await inputs[1].setValue('abcde');
  await wrapper.find('button[type="submit"]').trigger('click');
  await wrapper.find('form').trigger('submit');
  return wrapper.emitted('submit')?.[0]?.[0];
}

describe('LoginForm 可选记住我', () => {
  beforeEach(() => localStorage.clear());

  it('默认隐藏复选框且保持旧提交形状', async () => {
    const wrapper = render();
    expect(wrapper.find('input[type="checkbox"]').exists()).toBe(false);
    expect(await submit(wrapper)).toEqual({ username: 'admin', password: 'abcde' });
    wrapper.unmount();
  });

  it('勾选后透传选择，重挂载恢复选择，取消后透传 false', async () => {
    const first = render(true);
    await first.find('input[type="checkbox"]').setValue(true);
    expect(await submit(first)).toEqual({ username: 'admin', password: 'abcde', remember: true });
    first.unmount();
    const second = render(true);
    expect((second.find('input[type="checkbox"]').element as HTMLInputElement).checked).toBe(true);
    expect((second.find('input[type="password"]').element as HTMLInputElement).value).toBe('');
    await second.find('input[type="checkbox"]').setValue(false);
    expect(await submit(second)).toEqual({ username: 'admin', password: 'abcde', remember: false });
    second.unmount();
  });
});
