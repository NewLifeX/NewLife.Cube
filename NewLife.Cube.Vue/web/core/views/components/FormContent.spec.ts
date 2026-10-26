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
import { testI18n } from '../../__tests__/helpers/i18n';

const FIELDS: FieldMeta[] = [
  { name: 'Name', displayName: '方案名称', typeName: 'String', length: 100 },
  { name: 'Enable', displayName: '是否启用', typeName: 'Boolean' },
];

const GROUPED_FIELDS: FieldMeta[] = [
  { name: 'Name', displayName: '方案名称', typeName: 'String', category: '基本信息' },
  { name: 'Count', displayName: '数量', typeName: 'Int32', category: '基本信息' },
  { name: 'Remark', displayName: '备注', typeName: 'String', category: '扩展信息' },
  { name: 'Loose', displayName: '无分类', typeName: 'String' },
];

describe('FormContent 编辑回填', () => {
  it('camelCase 行数据 + PascalCase 字段名 → 文本与开关正确回填', () => {
    const wrapper = mount(FormContent, {
      props: {
        fields: FIELDS,
        modelValue: { name: '季度保养', enable: true },
      },
      global: { plugins: [ElementPlus, testI18n] },
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
      global: { plugins: [ElementPlus, testI18n] },
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
      global: { plugins: [ElementPlus, testI18n] },
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

describe('FormContent 分组', () => {
  const titles = (wrapper: ReturnType<typeof mount>) =>
    wrapper.findAll('.fmc-group-title').map((node) => node.text());

  it('应该按 Category 保序分组并把无分类字段归入其他 当 groupBy=auto 且 groupDisplay=sections 时', () => {
    const wrapper = mount(FormContent, {
      props: { fields: GROUPED_FIELDS, modelValue: {}, groupDisplay: 'sections' },
      global: { plugins: [ElementPlus, testI18n] },
    });

    expect(titles(wrapper)).toEqual(['基本信息', '扩展信息', '其他']);
  });

  it('应该平铺字段 当 groupBy=none 时', () => {
    const wrapper = mount(FormContent, {
      props: { fields: GROUPED_FIELDS, modelValue: {}, groupBy: 'none' },
      global: { plugins: [ElementPlus, testI18n] },
    });

    expect(titles(wrapper)).toEqual([]);
    expect(wrapper.findAll('.fmc-field')).toHaveLength(GROUPED_FIELDS.length);
    // 单分组不渲染头部与分类 Tab
    expect(wrapper.find('.fmc-header').exists()).toBe(false);
    expect(wrapper.find('.fmc-group-tabs').exists()).toBe(false);
  });

  it('应该强制按 Category 分组 当只有一个分类且 groupBy=category 时', () => {
    const wrapper = mount(FormContent, {
      props: {
        fields: [GROUPED_FIELDS[0], GROUPED_FIELDS[1]],
        modelValue: {},
        groupBy: 'category',
        groupDisplay: 'sections',
      },
      global: { plugins: [ElementPlus, testI18n] },
    });

    expect(titles(wrapper)).toEqual(['基本信息']);
  });

  it('应该按自定义函数分组并对空分组名回退其他 当 groupBy 为函数时', () => {
    const wrapper = mount(FormContent, {
      props: {
        fields: GROUPED_FIELDS,
        modelValue: {},
        groupBy: (field: FieldMeta) => (field.name === 'Remark' ? '自定义' : ''),
        groupDisplay: 'sections',
      },
      global: { plugins: [ElementPlus, testI18n] },
    });

    expect(titles(wrapper)).toEqual(['其他', '自定义']);
  });

  it('默认按 Category 渲染 Tab，空分类显示「默认选项」', () => {
    const wrapper = mount(FormContent, {
      props: { fields: GROUPED_FIELDS, modelValue: {} },
      global: { plugins: [ElementPlus, testI18n] },
    });

    const tabs = wrapper.findAll('.fmc-group-tabs .el-tabs__item');
    expect(tabs.map((node) => node.text())).toEqual(['基本信息', '扩展信息', '默认选项']);
    // 默认只渲染第一个分类的字段
    expect(wrapper.findAll('.fmc-field')).toHaveLength(2);
    expect(wrapper.text()).toContain('方案名称');
    expect(wrapper.text()).not.toContain('备注');
  });

  it('切换分类 Tab 后渲染对应分类字段', async () => {
    const wrapper = mount(FormContent, {
      props: { fields: GROUPED_FIELDS, modelValue: {} },
      global: { plugins: [ElementPlus, testI18n] },
    });

    await wrapper.findAll('.fmc-group-tabs .el-tabs__item')[1].trigger('click');
    expect(wrapper.findAll('.fmc-field')).toHaveLength(1);
    expect(wrapper.text()).toContain('备注');
  });

  it('短分类名补「选项」后缀', () => {
    const wrapper = mount(FormContent, {
      props: {
        fields: [
          { name: 'A', displayName: 'A', typeName: 'String', category: 'AI' },
          { name: 'B', displayName: 'B', typeName: 'String', category: '五字分类名' },
        ],
        modelValue: {},
      },
      global: { plugins: [ElementPlus, testI18n] },
    });

    const tabs = wrapper.findAll('.fmc-group-tabs .el-tabs__item');
    expect(tabs.map((node) => node.text())).toEqual(['AI选项', '五字分类名']);
  });
});

describe('FormContent 字段说明', () => {
  const FIELD: FieldMeta = {
    name: 'Name',
    displayName: '名称',
    typeName: 'String',
    description: '名称的唯一标识，保存后不可修改',
  };

  it('默认把 Description 放到 Label 旁的小问号 Tooltip，输入框使用通用占位', () => {
    const wrapper = mount(FormContent, {
      props: { fields: [FIELD], modelValue: {} },
      global: { plugins: [ElementPlus, testI18n] },
    });

    const help = wrapper.find('.fmc-help');
    expect(help.exists()).toBe(true);
    expect(help.attributes('aria-label')).toBe(FIELD.description);
    const input = wrapper.find('input.el-input__inner');
    expect(input.attributes('placeholder')).toBe('请输入...');
  });

  it('descriptionMode=placeholder 时保留旧行为：无问号，描述进占位符', () => {
    const wrapper = mount(FormContent, {
      props: { fields: [FIELD], modelValue: {}, descriptionMode: 'placeholder' },
      global: { plugins: [ElementPlus, testI18n] },
    });

    expect(wrapper.find('.fmc-help').exists()).toBe(false);
    const input = wrapper.find('input.el-input__inner');
    expect(input.attributes('placeholder')).toBe(FIELD.description);
  });
});
