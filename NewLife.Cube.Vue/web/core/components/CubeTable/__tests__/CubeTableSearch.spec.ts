/**
 * `CubeTableSearch` 组件渲染测试
 *
 * 断言点：条件按元数据出控件、范围类拆两格、内联字典出下拉、
 * 查询/重置打到引擎、条件超一行时折叠并可展开。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/CubeTableSearch.spec.ts
 */
import './domShims';
import { describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import CubeTableSearch from '../CubeTableSearch.vue';
import { FIELDS, makeFakeCtx } from './fakeCtx';
import type { FieldMeta } from '../../../types/field';

/**
 * LovSelect 会在挂载时拉值集元数据（lov-api → 真实网络）。
 * 单测里把 lovStore 整个桩掉：本组件测的是「哪种字段出哪种控件」，不是值集取数。
 */
const lovStubs = vi.hoisted(() => ({
  getMeta: vi.fn(async (_code: string) => ({ type: 'ENUM', options: [] })),
  getCachedMeta: vi.fn((_code: string) => null),
  getListData: vi.fn(async (_code: string) => ({ data: [], total: 0 })),
  getColumnLabel: vi.fn((_c: string, v: string) => v),
  resolveColumnLabels: vi.fn(async (_c: string, v: string) => v),
  getSelectedLabel: vi.fn(async (_c: string, v: string) => v),
  resolveSelectedLabel: vi.fn(async (_c: string, v: string) => v),
  registerRows: vi.fn(),
  registerSelectedRow: vi.fn(),
  invalidateLov: vi.fn(),
}));

vi.mock('../../LovSelect/lovStore', () => ({
  getMeta: (...a: unknown[]) => lovStubs.getMeta(...(a as [string])),
  getCachedMeta: (...a: unknown[]) => lovStubs.getCachedMeta(...(a as [string])),
  getListData: (...a: unknown[]) => lovStubs.getListData(...(a as [string])),
  getColumnLabel: (...a: unknown[]) => lovStubs.getColumnLabel(...(a as [string, string])),
  resolveColumnLabels: (...a: unknown[]) => lovStubs.resolveColumnLabels(...(a as [string, string])),
  getSelectedLabel: (...a: unknown[]) => lovStubs.getSelectedLabel(...(a as [string, string])),
  resolveSelectedLabel: (...a: unknown[]) => lovStubs.resolveSelectedLabel(...(a as [string, string])),
  registerRows: (...a: unknown[]) => lovStubs.registerRows(...(a as [])),
  registerSelectedRow: (...a: unknown[]) => lovStubs.registerSelectedRow(...(a as [])),
  invalidateLov: (...a: unknown[]) => lovStubs.invalidateLov(...(a as [])),
}));

async function mountSearch(searchFields: FieldMeta[], patch: Partial<ReturnType<typeof makeFakeCtx>> = {}) {
  const ctx = { ...makeFakeCtx({ searchFields }), ...patch };
  const wrapper = mount(CubeTableSearch, {
    props: { ctx },
    global: { plugins: [ElementPlus] },
  });
  await nextTick();
  return { wrapper, ctx };
}

describe('CubeTableSearch 条件渲染', () => {
  it('应该给关键字框并回车即查 当组件挂载时', async () => {
    const { wrapper, ctx } = await mountSearch([]);
    const search = vi.fn(async () => undefined);
    ctx.search = search;
    const keyword = wrapper.find('input');
    await keyword.setValue('泵');
    await keyword.trigger('keyup.enter');
    expect(search).toHaveBeenCalled();
  });

  it('应该按字段出控件形态 当条件类型不同', async () => {
    const { wrapper } = await mountSearch([FIELDS.name, FIELDS.count, FIELDS.enable, FIELDS.time]);
    expect(wrapper.text()).toContain('名称');
    expect(wrapper.text()).toContain('启用');
    expect(wrapper.findAll('.el-input-number')).toHaveLength(2);
    expect(wrapper.findAll('.el-date-editor')).toHaveLength(1);
    // 布尔条件是一个「全部/是/否」下拉，日期条件是日期区间控件
    expect(wrapper.findAll('.el-select')).toHaveLength(1);
  });

  it('应该用 min/max 两格表现数值区间 当条件是数值范围时', async () => {
    const { wrapper } = await mountSearch([FIELDS.count]);
    // 占位文案在 input 的属性上，不是文本节点
    const placeholders = wrapper.findAll('input').map((i) => i.attributes('placeholder') ?? '');
    expect(placeholders).toEqual(expect.arrayContaining(['最小', '最大']));
    expect(wrapper.findAll('.el-input-number')).toHaveLength(2);
  });

  it('应该把内联字典渲染成下拉选项 当条件带 dataSource 时', async () => {
    const { wrapper } = await mountSearch([FIELDS.dict]);
    await wrapper.find('.el-select').trigger('click');
    await nextTick();
    const options = document.body.querySelectorAll('.el-select-dropdown__item');
    expect(Array.from(options).map((o) => o.textContent?.trim())).toEqual(
      expect.arrayContaining(['小时', '班次']),
    );
  });

  it('应该给是/否三档 当条件是布尔时', async () => {
    const { wrapper } = await mountSearch([FIELDS.enable]);
    await wrapper.find('.el-select').trigger('click');
    await nextTick();
    const options = Array.from(document.body.querySelectorAll('.el-select-dropdown__item')).map((o) =>
      o.textContent?.trim(),
    );
    expect(options).toEqual(expect.arrayContaining(['全部', '是', '否']));
  });

  it('应该把值集条件交给 LovSelect 当条件带 lovCode 时', async () => {
    const { wrapper } = await mountSearch([FIELDS.kind]);
    expect(wrapper.findComponent({ name: 'LovSelect' }).exists()).toBe(true);
  });
});

describe('CubeTableSearch 动作', () => {
  it('应该把查询与重置打到引擎 当点击两个按钮时', async () => {
    const search = vi.fn(async () => undefined);
    const resetSearch = vi.fn(async () => undefined);
    const { wrapper } = await mountSearch([FIELDS.name], { search, resetSearch });
    const buttons = wrapper.findAll('button');
    await buttons.find((b) => b.text().includes('查询'))!.trigger('click');
    await buttons.find((b) => b.text().includes('重置'))!.trigger('click');
    expect(search).toHaveBeenCalledTimes(1);
    expect(resetSearch).toHaveBeenCalledTimes(1);
  });

  it('应该只给前四个条件并提示剩余数 当条件超过一行时', async () => {
    const many = Array.from({ length: 6 }, (_v, i): FieldMeta => ({
      name: `F${i}`,
      displayName: `条件${i}`,
      typeName: 'String',
    }));
    const { wrapper } = await mountSearch(many);
    expect(wrapper.text()).toContain('条件0');
    expect(wrapper.text()).not.toContain('条件5');
    expect(wrapper.text()).toContain('更多(2)');
  });

  it('应该展开全部条件 当点了更多按钮时', async () => {
    const many = Array.from({ length: 6 }, (_v, i): FieldMeta => ({
      name: `F${i}`,
      displayName: `条件${i}`,
      typeName: 'String',
    }));
    const { wrapper } = await mountSearch(many);
    await wrapper.find('.cts-toggle').trigger('click');
    await nextTick();
    expect(wrapper.text()).toContain('条件5');
    expect(wrapper.text()).toContain('收起');
  });

  it('应该没有字段时只给关键字框与两个按钮 当搜索字段为空时', async () => {
    const { wrapper } = await mountSearch([]);
    expect(wrapper.text()).toContain('查询');
    expect(wrapper.text()).not.toContain('更多');
  });
});
