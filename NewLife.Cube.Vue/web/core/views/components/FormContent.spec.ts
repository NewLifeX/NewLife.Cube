/**
 * FormContent 编辑回填回归测试
 *
 * 背景：后端字段元数据 name 为 PascalCase（如 Name/Enable），而列表行/Detail 返回
 * camelCase（如 name/enable）。此前 FormContent 按字段名精确取值（modelValue[field.name]），
 * 导致通用列表页「点击编辑」弹窗字段回填为空。
 *
 * 覆盖：
 * 1. camelCase 行数据 + PascalCase 字段名 → 文本/开关正确回填
 * 2. PascalCase 行数据（兼容旧版）仍正常回填
 * 3. 编辑回写保持实际键（camelCase），不产生大小写双键
 *
 * 运行：pnpm test:unit core/views/components/FormContent.spec.ts
 */
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import FormContent from './FormContent.vue';
import type { FieldMeta } from '../../types/field';

const FIELDS: FieldMeta[] = [
  { name: 'Name', displayName: '方案名称', typeName: 'String', length: 100 },
  { name: 'Enable', displayName: '是否启用', typeName: 'Boolean' },
];

describe('FormContent 编辑回填', () => {
  it('camelCase 行数据 + PascalCase 字段名 → 文本与开关正确回填', () => {
    const wrapper = mount(FormContent, {
      props: {
        fields: FIELDS,
        modelValue: { name: '季度保养', enable: true },
      },
      global: { plugins: [ElementPlus] },
    });

    // 文本输入框应显示行数据
    const input = wrapper.find('input.el-input__inner');
    expect((input.element as HTMLInputElement).value).toBe('季度保养');
    // 布尔开关应处于开启状态
    const sw = wrapper.find('.el-switch');
    expect(sw.exists()).toBe(true);
    expect(sw.classes()).toContain('is-checked');
  });

  it('PascalCase 行数据仍正常回填（兼容）', () => {
    const wrapper = mount(FormContent, {
      props: {
        fields: FIELDS,
        modelValue: { Name: '月度保养', Enable: true },
      },
      global: { plugins: [ElementPlus] },
    });

    const input = wrapper.find('input.el-input__inner');
    expect((input.element as HTMLInputElement).value).toBe('月度保养');
    const sw = wrapper.find('.el-switch');
    expect(sw.exists()).toBe(true);
    expect(sw.classes()).toContain('is-checked');
  });

  it('编辑回写保持实际键（camelCase），不产生大小写双键', async () => {
    const wrapper = mount(FormContent, {
      props: {
        fields: FIELDS,
        modelValue: { name: '季度保养', enable: true },
      },
      global: { plugins: [ElementPlus] },
    });

    const input = wrapper.find('input.el-input__inner');
    await input.setValue('半年保养');

    const emitted = wrapper.emitted('update:modelValue');
    expect(emitted).toBeTruthy();
    const next = emitted![0][0] as Record<string, unknown>;
    // 更新后的对象应只含原 camelCase 键，且值为新输入
    expect(next).toEqual({ name: '半年保养', enable: true });
  });
});
