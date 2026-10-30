/**
 * LogoBrand 左上角品牌标题回归
 *
 * 历史问题：configs/config.*.ts 写死中文标题，品牌名不随界面语言切换，
 * 英文界面下左上角仍显示「魔方系统」。站点名称未配置时应回退到 i18n 品牌兜底文案。
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { nextTick } from 'vue';
import LogoBrand from '../components/LogoBrand.vue';
import { testI18n } from './helpers/i18n';

vi.mock('../stores/site', () => ({
  useSiteStore: () => ({ name: '', logo: '', load: () => Promise.resolve(null) }),
}));

describe('LogoBrand 品牌标题', () => {
  beforeEach(() => {
    testI18n.global.locale.value = 'zh-CN';
  });

  it('站点未配置名称时跟随界面语言翻译', async () => {
    const wrapper = mount(LogoBrand, { global: { plugins: [testI18n] } });

    expect(wrapper.text()).toContain('魔方系统');

    testI18n.global.locale.value = 'en-US';
    await nextTick();

    expect(wrapper.text()).toContain('Cube system');
  });
});
