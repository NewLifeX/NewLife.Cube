/**
 * `AreaSelect` 组件测试
 *
 * 纯函数归一/叶子判定已由 `areaSelect.logic.spec` 覆盖；这里只钉住组件接线：
 * 回显父级路径、选择后只向 v-model 抛出最后一级编号。
 *
 * 运行：pnpm test:unit core/components/AreaSelect/__tests__/AreaSelect.spec.ts
 */
import { describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import AreaSelect from '../index.vue';

const requestStub = vi.hoisted(() => vi.fn(async (..._args: unknown[]): Promise<unknown> => undefined));
vi.mock('../../../utils/request', () => ({
  default: (...args: unknown[]) => requestStub(...args),
}));

describe('AreaSelect 组件', () => {
  it('应该按 AreaParents 回显完整父级路径', async () => {
    requestStub.mockClear();
    requestStub.mockResolvedValueOnce({
      data: [
        { id: 1, name: '省', level: 1 },
        { id: 2, name: '市', level: 2 },
        { id: 3, name: '区', level: 3 },
      ],
    });

    const wrapper = mount(AreaSelect, {
      props: { modelValue: 3 },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    await vi.waitFor(() => {
      const cascader = wrapper.findComponent({ name: 'ElCascader' });
      expect(cascader.props('modelValue')).toEqual([1, 2, 3]);
    });
    expect(requestStub.mock.calls[0][0]).toMatchObject({
      url: '/Cube/AreaParents',
      method: 'get',
      params: { id: 3, isContainSelf: true },
    });
  });

  it('应该只向 v-model 抛出最后一级编号 当用户选择完整路径时', async () => {
    requestStub.mockResolvedValue({ data: [] });
    const wrapper = mount(AreaSelect, {
      props: { modelValue: null, level: 3 },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    wrapper.findComponent({ name: 'ElCascader' }).vm.$emit('update:model-value', [1, 2, 3]);
    await nextTick();

    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([3]);
  });

  it('应该清空值 当用户清空级联选择时', async () => {
    requestStub.mockResolvedValue({ data: [] });
    const wrapper = mount(AreaSelect, {
      props: { modelValue: 3 },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    wrapper.findComponent({ name: 'ElCascader' }).vm.$emit('update:model-value', []);
    await nextTick();

    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([undefined]);
  });
});
