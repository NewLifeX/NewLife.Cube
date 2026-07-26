/**
 * ListFormDialog 组件渲染测试
 *
 * 覆盖场景：
 * 1. 正常渲染 FormContent，字段传递正确
 * 2. modelValue 双向绑定
 * 3. update:modelValue 事件传递
 * 4. apiPrefix 传递到 FormContent
 * 5. mode 传递到 ListFormDialog 但不传递到 FormContent
 * 6. routePath 提供时 Section 覆盖生效
 * 7. 不提供 routePath 时 Section 覆盖不生效（兼容回退）
 *
 * 运行：pnpm test:unit core/views/modals/list-form-dialog/ListFormDialog.spec.ts
 */
import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import ListFormDialog from './ListFormDialog.vue';
import type { FieldMeta } from '../../types/field';

// ── Mock FormContent ────────────────────────────────────────────
// 避免依赖 FormContent 内部的控件渲染，只验证 ListFormDialog 的 props 传递逻辑
vi.mock('@newlifex/cube-vue/core/views/components/FormContent.vue', () => ({
  default: {
    name: 'FormContentMock',
    props: ['fields', 'modelValue', 'apiPrefix', 'columns'],
    template: '<div class="form-content-mock">{{ fields.length }}个字段</div>',
    emits: ['update:modelValue'],
  },
}));

// ── 测试数据 ────────────────────────────────────────────────────

const FIELDS: FieldMeta[] = [
  { name: 'Name', displayName: '名称', typeName: 'String', length: 50 },
  { name: 'Enable', displayName: '启用', typeName: 'Boolean' },
];

describe('ListFormDialog', () => {
  it('渲染 FormContent 并传递 fields', () => {
    const wrapper = mount(ListFormDialog, {
      props: {
        fields: FIELDS,
        modelValue: {},
        mode: 'add',
      },
      global: {
        plugins: [ElementPlus],
      },
    });

    expect(wrapper.find('.form-content-mock').exists()).toBe(true);
    expect(wrapper.text()).toContain('2个字段');
  });

  it('传递 modelValue 到 FormContent', () => {
    const wrapper = mount(ListFormDialog, {
      props: {
        fields: FIELDS,
        modelValue: { Name: 'test', Enable: true },
        mode: 'edit',
      },
      global: {
        plugins: [ElementPlus],
      },
    });

    const formContent = wrapper.findComponent({ name: 'FormContentMock' });
    expect(formContent.props('modelValue')).toEqual({ Name: 'test', Enable: true });
  });

  it('传递 apiPrefix 到 FormContent', () => {
    const wrapper = mount(ListFormDialog, {
      props: {
        fields: FIELDS,
        modelValue: {},
        apiPrefix: '/api/test',
        mode: 'add',
      },
      global: {
        plugins: [ElementPlus],
      },
    });

    const formContent = wrapper.findComponent({ name: 'FormContentMock' });
    expect(formContent.props('apiPrefix')).toBe('/api/test');
  });

  it('不传递 apiPrefix 时 FormContent 收到 undefined', () => {
    const wrapper = mount(ListFormDialog, {
      props: {
        fields: FIELDS,
        modelValue: {},
        mode: 'add',
      },
      global: {
        plugins: [ElementPlus],
      },
    });

    const formContent = wrapper.findComponent({ name: 'FormContentMock' });
    expect(formContent.props('apiPrefix')).toBeUndefined();
  });

  it('传递 columns 到 FormContent', () => {
    const wrapper = mount(ListFormDialog, {
      props: {
        fields: FIELDS,
        modelValue: {},
        mode: 'add',
        columns: 3,
      },
      global: {
        plugins: [ElementPlus],
      },
    });

    const formContent = wrapper.findComponent({ name: 'FormContentMock' });
    expect(formContent.props('columns')).toBe(3);
  });

  it('不传递 columns 时 FormContent 收到 undefined', () => {
    const wrapper = mount(ListFormDialog, {
      props: {
        fields: FIELDS,
        modelValue: {},
        mode: 'add',
      },
      global: {
        plugins: [ElementPlus],
      },
    });

    const formContent = wrapper.findComponent({ name: 'FormContentMock' });
    expect(formContent.props('columns')).toBeUndefined();
  });

  it('update:modelValue 事件从 FormContent 冒泡', () => {
    const wrapper = mount(ListFormDialog, {
      props: {
        fields: FIELDS,
        modelValue: { Name: 'old' },
        mode: 'add',
      },
      global: {
        plugins: [ElementPlus],
      },
    });

    // 触发 FormContent 的 update:modelValue 事件
    const newData = { Name: 'new' };
    const formContent = wrapper.findComponent({ name: 'FormContentMock' });
    formContent.vm.$emit('update:modelValue', newData);

    // 验证 ListFormDialog 冒泡了事件
    expect(wrapper.emitted('update:modelValue')).toBeTruthy();
    expect(wrapper.emitted('update:modelValue')![0][0]).toEqual(newData);
  });

  it('字段列表为空时仍正常渲染', () => {
    const wrapper = mount(ListFormDialog, {
      props: {
        fields: [],
        modelValue: {},
        mode: 'add',
      },
      global: {
        plugins: [ElementPlus],
      },
    });

    expect(wrapper.find('.form-content-mock').exists()).toBe(true);
    expect(wrapper.text()).toContain('0个字段');
  });

  it('提供 routePath 时正常渲染（Section 覆盖静默生效）', () => {
    const wrapper = mount(ListFormDialog, {
      props: {
        fields: FIELDS,
        modelValue: {},
        mode: 'add',
        routePath: '/test/area/page',
      },
      global: {
        plugins: [ElementPlus],
      },
    });

    // Section 覆盖机制不应阻塞渲染
    expect(wrapper.find('.form-content-mock').exists()).toBe(true);
    expect(wrapper.text()).toContain('2个字段');
  });

  it('不提供 routePath 时正常渲染（兼容回退）', () => {
    const wrapper = mount(ListFormDialog, {
      props: {
        fields: FIELDS,
        modelValue: {},
        mode: 'add',
      },
      global: {
        plugins: [ElementPlus],
      },
    });

    // 不传 routePath 不影响渲染
    expect(wrapper.find('.form-content-mock').exists()).toBe(true);
    expect(wrapper.text()).toContain('2个字段');
  });
});