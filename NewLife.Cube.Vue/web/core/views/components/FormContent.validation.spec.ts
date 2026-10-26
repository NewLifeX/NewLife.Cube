/**
 * FormContent 必填校验测试
 *
 * 覆盖：
 * 1. 必填字段为空时 validate() 返回 false 并渲染错误提示
 * 2. 必填字段有值时通过
 * 3. nullable / Boolean 字段不参与必填
 * 4. 禁用（详情）模式直接通过
 * 5. 用户修改字段后错误提示立即清除
 * 6. requiredWhen 动态必填生效
 *
 * 运行：pnpm test:unit core/views/components/FormContent.validation.spec.ts
 */
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import FormContent from './FormContent.vue';
import type { FieldMeta } from '../../types/field';
import { testI18n } from '../../__tests__/helpers/i18n';

const FIELDS: FieldMeta[] = [
  { name: 'Name', displayName: '方案名称', typeName: 'String', length: 100 },
  { name: 'Remark', displayName: '备注', typeName: 'String', nullable: true },
  { name: 'Enable', displayName: '是否启用', typeName: 'Boolean' },
];

function mountForm(modelValue: Record<string, unknown>, extra: Record<string, unknown> = {}) {
  return mount(FormContent, {
    props: { fields: FIELDS, modelValue, ...extra },
    global: { plugins: [ElementPlus, testI18n] },
  });
}

describe('FormContent 必填校验', () => {
  it('必填字段为空时校验失败并显示提示', async () => {
    const wrapper = mountForm({});

    const valid = (wrapper.vm as unknown as { validate: () => boolean }).validate();
    expect(valid).toBe(false);
    await wrapper.vm.$nextTick();

    const errors = wrapper.findAll('.fmc-error');
    expect(errors.length).toBe(1);
    expect(errors[0].text()).toContain('方案名称');
    expect(errors[0].attributes('role')).toBe('alert');
  });

  it('必填字段有值时校验通过', () => {
    const wrapper = mountForm({ name: '季度保养' });

    expect((wrapper.vm as unknown as { validate: () => boolean }).validate()).toBe(true);
    expect(wrapper.findAll('.fmc-error').length).toBe(0);
  });

  it('nullable 与 Boolean 字段不参与必填', () => {
    const wrapper = mountForm({ name: 'x' });

    // Remark 为空、Enable 未设置，但二者都不是必填
    expect((wrapper.vm as unknown as { validate: () => boolean }).validate()).toBe(true);
  });

  it('禁用（详情）模式直接通过', () => {
    const wrapper = mountForm({}, { disabled: true });

    expect((wrapper.vm as unknown as { validate: () => boolean }).validate()).toBe(true);
  });

  it('修改字段后错误提示立即清除', async () => {
    const wrapper = mountForm({});
    (wrapper.vm as unknown as { validate: () => boolean }).validate();
    await wrapper.vm.$nextTick();
    expect(wrapper.findAll('.fmc-error').length).toBe(1);

    await wrapper.find('input.el-input__inner').setValue('新名称');
    expect(wrapper.findAll('.fmc-error').length).toBe(0);

    const emitted = wrapper.emitted('update:modelValue');
    expect(emitted?.at(-1)?.[0]).toMatchObject({ Name: '新名称' });
  });

  it('requiredWhen 动态必填生效', () => {
    const fields: FieldMeta[] = [
      {
        name: 'Reason',
        displayName: '原因',
        typeName: 'String',
        nullable: true,
        requiredWhen: (model) => model.enabled === true,
      },
    ];

    const off = mount(FormContent, {
      props: { fields, modelValue: { enabled: false } },
      global: { plugins: [ElementPlus, testI18n] },
    });
    expect((off.vm as unknown as { validate: () => boolean }).validate()).toBe(true);

    const on = mount(FormContent, {
      props: { fields, modelValue: { enabled: true } },
      global: { plugins: [ElementPlus, testI18n] },
    });
    expect((on.vm as unknown as { validate: () => boolean }).validate()).toBe(false);
  });
});
