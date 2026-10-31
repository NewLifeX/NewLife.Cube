/**
 * `TimeSpanInput` 组件测试
 *
 * 换算细节已由 `timeSpan.logic.spec` 覆盖，这里只钉住组件接线：
 * 按描述单位回显为「时:分:秒」、任一段变更后抛回原始存储值、禁用态透传。
 *
 * 运行：pnpm test:unit core/components/TimeSpanInput/__tests__/TimeSpanInput.spec.ts
 */
import { describe, expect, it } from 'vitest';
import { nextTick } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import TimeSpanInput from '../index.vue';

/** 取三个数字输入（时/分/秒） */
function partsOf(wrapper: ReturnType<typeof mount>) {
  return wrapper.findAllComponents({ name: 'ElInputNumber' });
}

describe('TimeSpanInput 组件', () => {
  it('应该按秒回显为 时:分:秒', async () => {
    const wrapper = mount(TimeSpanInput, {
      props: { modelValue: 22515, description: '累计在线总时间，单位秒' },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    const parts = partsOf(wrapper);
    expect(parts).toHaveLength(3);
    expect(parts[0].props('modelValue')).toBe(6);
    expect(parts[1].props('modelValue')).toBe(15);
    expect(parts[2].props('modelValue')).toBe(15);
  });

  it('应该空值回显为 0:0:0', async () => {
    const wrapper = mount(TimeSpanInput, {
      props: { modelValue: null },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    const parts = partsOf(wrapper);
    expect(parts.map((p) => p.props('modelValue'))).toEqual([0, 0, 0]);
  });

  it('应该修改小时后抛回新的秒数', async () => {
    const wrapper = mount(TimeSpanInput, {
      props: { modelValue: 22515, description: '单位秒' },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    partsOf(wrapper)[0].vm.$emit('update:model-value', 7);
    await nextTick();

    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([7 * 3600 + 15 * 60 + 15]);
  });

  it('应该修改分钟后沿用当前时分 抛出新的秒数', async () => {
    const wrapper = mount(TimeSpanInput, {
      props: { modelValue: 22515, description: '单位秒' },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    partsOf(wrapper)[1].vm.$emit('update:model-value', 0);
    await nextTick();

    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([6 * 3600 + 15]);
  });

  it('应该清空某一段时按 0 处理', async () => {
    const wrapper = mount(TimeSpanInput, {
      props: { modelValue: 22515, description: '单位秒' },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    partsOf(wrapper)[2].vm.$emit('update:model-value', undefined);
    await nextTick();

    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([6 * 3600 + 15 * 60]);
  });

  it('应该按描述的分钟单位换算存储值', async () => {
    const wrapper = mount(TimeSpanInput, {
      props: { modelValue: 90, description: '时长，单位分钟' },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    const parts = partsOf(wrapper);
    expect(parts[0].props('modelValue')).toBe(1);
    expect(parts[1].props('modelValue')).toBe(30);

    parts[2].vm.$emit('update:model-value', 30);
    await nextTick();

    // 1 小时 30 分 30 秒 = 90.5 分钟，按整数语义四舍五入为 91 分钟
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([91]);
  });

  it('应该毫秒单位下秒保留 3 位小数', async () => {
    const wrapper = mount(TimeSpanInput, {
      props: { modelValue: 1500, description: '耗时，单位毫秒' },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    const parts = partsOf(wrapper);
    expect(parts[2].props('modelValue')).toBe(1.5);
    expect(parts[2].props('precision')).toBe(3);
  });

  it('应该在首段绑定 id，供表单 label 聚焦', async () => {
    const wrapper = mount(TimeSpanInput, {
      props: { id: 'fmc-OnlineTime', modelValue: 0 },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    expect(partsOf(wrapper)[0].props('id')).toBe('fmc-OnlineTime');
  });

  it('应该禁用态透传给全部输入', async () => {
    const wrapper = mount(TimeSpanInput, {
      props: { modelValue: 0, disabled: true },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    expect(partsOf(wrapper).every((p) => p.props('disabled') === true)).toBe(true);
  });
});
