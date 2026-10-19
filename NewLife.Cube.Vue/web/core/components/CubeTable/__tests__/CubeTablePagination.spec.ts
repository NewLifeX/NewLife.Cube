/**
 * `CubeTablePagination` 组件测试
 *
 * 结构与 MES `ListPage/PaginationBar` 一致：左侧「共 N 条」，右侧「分页器 + 每页条数下拉」。
 * 这里钉住三件最容易错的事：页码基数、条数下拉与分页器分离后的接线、单位文案。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/CubeTablePagination.spec.ts
 */
import './domShims';
import { describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import CubeTablePagination from '../CubeTablePagination.vue';
import { makeFakeCtx } from './fakeCtx';

async function mountPagination(
  options: Parameters<typeof makeFakeCtx>[0] = {},
  props: Record<string, unknown> = {},
  behavior: Record<string, unknown> = {},
) {
  const ctx = {
    ...makeFakeCtx({ total: 123, page: 2, pageSize: 20, ...options }),
    ...behavior,
  };
  const wrapper = mount(CubeTablePagination, {
    props: { ctx, ...props },
    global: { plugins: [ElementPlus] },
  });
  await nextTick();
  return { wrapper, ctx };
}

describe('CubeTablePagination', () => {
  it('应该把引擎的总数与当前页原样交给分页器 当渲染时', async () => {
    const { wrapper } = await mountPagination();
    const pager = wrapper.findComponent({ name: 'ElPagination' });
    expect(pager.props('total')).toBe(123);
    expect(pager.props('currentPage')).toBe(2);
    expect(pager.props('pageSize')).toBe(20);
  });

  it('应该按 1-based 页码请求 当分页器改写页码时', async () => {
    const changePage = vi.fn(async () => undefined);
    const { wrapper } = await mountPagination({}, {}, { changePage });
    wrapper.findComponent({ name: 'ElPagination' }).vm.$emit('update:current-page', 3);
    await nextTick();
    expect(changePage).toHaveBeenCalledWith(3);
  });

  it('应该用不含 total/sizes 的布局 当渲染分页器时（总数与条数各自成控件）', async () => {
    const { wrapper } = await mountPagination();
    expect(wrapper.findComponent({ name: 'ElPagination' }).props('layout')).toBe(
      'prev, pager, next, jumper',
    );
  });

  it('应该把每页条数交给引擎 当条数下拉改写模型时', async () => {
    const changePageSize = vi.fn(async () => undefined);
    const { wrapper } = await mountPagination({}, {}, { changePageSize });
    wrapper.findComponent({ name: 'ElSelect' }).vm.$emit('update:model-value', 50);
    await nextTick();
    expect(changePageSize).toHaveBeenCalledWith(50);
  });

  it('应该渲染「共 123 条」 当默认单位时', async () => {
    const { wrapper } = await mountPagination();
    expect(wrapper.find('.ctp-total').text()).toBe('共 123 条');
  });

  it('应该按页面单位渲染总数与条数档位 当业务页覆盖 unit 时', async () => {
    const { wrapper } = await mountPagination({ total: 8 }, { unit: '台设备' });
    expect(wrapper.find('.ctp-total').text()).toBe('共 8 台设备');
    const options = wrapper.findAllComponents({ name: 'ElOption' });
    expect(options[0]?.props('label')).toBe('10 台设备/页');
  });

  it('应该让每页条数单位单独可配 当指定 sizeUnit 时', async () => {
    const { wrapper } = await mountPagination({}, { unit: '台设备', sizeUnit: '条' });
    const options = wrapper.findAllComponents({ name: 'ElOption' });
    expect(options[0]?.props('label')).toBe('10 条/页');
  });

  it('应该默认给 10/20/50 三档 当业务页不指定 pageSizes 时', async () => {
    const { wrapper } = await mountPagination();
    const values = wrapper.findAllComponents({ name: 'ElOption' }).map((o) => o.props('value'));
    expect(values).toEqual([10, 20, 50]);
  });

  it('应该用页面给的档位 当业务页覆盖 pageSizes 时', async () => {
    const { wrapper } = await mountPagination({}, { pageSizes: [15, 30] });
    const values = wrapper.findAllComponents({ name: 'ElOption' }).map((o) => o.props('value'));
    expect(values).toEqual([15, 30]);
  });

  it('应该把附加内容排在总数旁 当使用 #extra 插槽时', async () => {
    const ctx = makeFakeCtx({ total: 123 });
    const wrapper = mount(CubeTablePagination, {
      props: { ctx },
      global: { plugins: [ElementPlus] },
      slots: { extra: '<span class="picked">已选 2</span>' },
    });
    await nextTick();
    expect(wrapper.find('.ctp-left .picked').text()).toBe('已选 2');
  });
});
