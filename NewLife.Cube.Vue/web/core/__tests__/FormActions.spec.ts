/**
 * FormActions 必填图例测试
 *
 * 配置页字段全部可选时不能出现「* 为必填字段」的说明，
 * 否则用户会误以为有必填项未填。
 *
 * 运行：pnpm test:unit core/__tests__/FormActions.spec.ts
 */
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import FormActions from '../views/components/FormActions.vue';
import { testI18n } from './helpers/i18n';

const mountActions = (props: Record<string, unknown> = {}) =>
  mount(FormActions, { props, global: { plugins: [testI18n] } });

describe('FormActions 必填图例', () => {
  it('默认为显示必填图例', () => {
    expect(mountActions().find('.fa-hint').exists()).toBe(true);
  });

  it('表单没有必填项时隐藏必填图例', () => {
    expect(mountActions({ showRequiredHint: false }).find('.fa-hint').exists()).toBe(false);
  });
});
