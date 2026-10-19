/**
 * `CubeTablePagination` 组件测试
 *
 * 分页最容易错的是「页码基数」与「换条数后停在哪一页」，
 * 这里把组件与引擎之间的口径钉住：1-based 直传、动作全部交给引擎。
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

async function mountPagination(patch: Partial<ReturnType<typeof makeFakeCtx>> = {}, props: Record<string, unknown> = {}) {
  const ctx = makeFakeCtx({ total: 123, page: 2, pageSize: 20 });
  Object.assign(ctx, patch);
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

  it('应该按 1-based 页码请求 当用户点页码时', async () => {
    const changePage = vi.fn(async () => undefined);
    const { wrapper } = await mountPagination({ changePage });
    wrapper.findComponent({ name: 'ElPagination' }).vm.$emit('current-change', 3);
    await nextTick();
    expect(changePage).toHaveBeenCalledWith(3);
  });

  it('应该把每页条数变更交给引擎 当用户改条数时', async () => {
    const changePageSize = vi.fn(async () => undefined);
    const { wrapper } = await mountPagination({ changePageSize });
    wrapper.findComponent({ name: 'ElPagination' }).vm.$emit('size-change', 50);
    await nextTick();
    expect(changePageSize).toHaveBeenCalledWith(50);
  });

  it('应该用页面给的条数档位 当业务页覆盖 pageSizes 时', async () => {
    const { wrapper } = await mountPagination({}, { pageSizes: [15, 30] });
    expect(wrapper.findComponent({ name: 'ElPagination' }).props('pageSizes')).toEqual([15, 30]);
  });

  it('应该默认提供常用档位与完整布局 当业务页不指定时', async () => {
    const { wrapper } = await mountPagination();
    const pager = wrapper.findComponent({ name: 'ElPagination' });
    expect(pager.props('pageSizes')).toEqual([10, 20, 50, 100]);
    expect(pager.props('layout')).toContain('jumper');
  });
});
