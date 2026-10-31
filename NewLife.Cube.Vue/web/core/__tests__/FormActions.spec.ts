/**
 * FormActions 按钮与图例测试
 *
 * 覆盖：
 * 1. 默认展示「取消 / 保存并继续 / 保存」三个按钮与必填图例
 * 2. 表单没有必填项时隐藏必填图例
 * 3. showContinue=false 时隐藏「保存并继续」（单对象配置页只有一个保存语义）
 * 4. 提交中禁用全部按钮，避免重复提交
 *
 * 运行：pnpm test:unit core/__tests__/FormActions.spec.ts
 */
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import FormActions from '../views/components/FormActions.vue';
import { testI18n } from './helpers/i18n';

const mountActions = (props: Record<string, unknown> = {}) =>
  mount(FormActions, { props, global: { plugins: [testI18n] } });

describe('FormActions 按钮与图例', () => {
  it('默认为显示必填图例并展示取消/保存并继续/保存三个按钮', () => {
    const wrapper = mountActions();
    expect(wrapper.find('.fa-hint').exists()).toBe(true);
    expect(wrapper.findAll('.fa-btn')).toHaveLength(3);
  });

  it('表单没有必填项时隐藏必填图例', () => {
    expect(mountActions({ showRequiredHint: false }).find('.fa-hint').exists()).toBe(false);
  });

  it('showContinue=false 时隐藏「保存并继续」', () => {
    const wrapper = mountActions({ showContinue: false });
    expect(wrapper.findAll('.fa-btn')).toHaveLength(2);
    expect(wrapper.find('.fa-btn--continue').exists()).toBe(false);
  });

  it('提交中禁用全部按钮，避免重复提交', () => {
    const wrapper = mountActions({ loading: true });
    const buttons = wrapper.findAll('.fa-btn');
    expect(buttons.length).toBeGreaterThan(0);
    for (const button of buttons) expect(button.attributes('disabled')).toBeDefined();
  });
});
