/**
 * `CubeTableSearch` 组件渲染测试
 *
 * 结构对齐 MES ListPage 的一体式搜索区：收起态只有关键字一行，展开才出条件面板。
 * 断言点：展开入口的有无、条件按元数据出控件、范围类拆两格、内联字典出下拉、
 * 面板内查询/重置打到引擎、插槽整块接管。
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

async function mountSearch(
  searchFields: FieldMeta[],
  patch: Partial<ReturnType<typeof makeFakeCtx>> = {},
  slots: Record<string, string> = {},
  keyword = '',
) {
  const ctx = { ...makeFakeCtx({ searchFields, keyword }), ...patch };
  const wrapper = mount(CubeTableSearch, {
    props: { ctx },
    global: { plugins: [ElementPlus] },
    slots,
  });
  await nextTick();
  return { wrapper, ctx };
}

/** 六个纯文本条件，用来验证展开前后看到几条 */
const MANY: FieldMeta[] = Array.from({ length: 6 }, (_v, i): FieldMeta => ({
  name: `F${i}`,
  displayName: `条件${i}`,
  typeName: 'String',
}));

/** 展开条件面板（只用到 find/trigger，结构类型即可，避免绑死 VTU 的包装类型） */
async function expand(wrapper: { find: (selector: string) => { trigger: (event: string) => Promise<void> } }) {
  await wrapper.find('.cts-toggle').trigger('click');
  await nextTick();
}

describe('CubeTableSearch 一体式结构', () => {
  it('应该收起态只渲染关键字一行 当有条件但未展开时', async () => {
    const { wrapper } = await mountSearch(MANY);
    expect(wrapper.find('.cts-bar').exists()).toBe(true);
    expect(wrapper.find('.cts-panel').exists()).toBe(false);
    expect(wrapper.text()).not.toContain('条件0');
  });

  it('应该展开全部条件并切成「收起」文案 当点了展开按钮时', async () => {
    const { wrapper } = await mountSearch(MANY);
    await expand(wrapper);
    expect(wrapper.find('.cts-panel').exists()).toBe(true);
    expect(wrapper.text()).toContain('条件0');
    expect(wrapper.text()).toContain('条件5');
    expect(wrapper.find('.cts-toggle').text()).toContain('收起');
  });

  it('应该不给展开按钮也不出面板 当元数据没有搜索条件时', async () => {
    const { wrapper } = await mountSearch([]);
    expect(wrapper.find('.cts-toggle').exists()).toBe(false);
    expect(wrapper.find('.cts-panel').exists()).toBe(false);
  });

  it('应该用页面的占位文案 当业务页覆盖 placeholder 时', async () => {
    const ctx = makeFakeCtx({ searchFields: [] });
    const wrapper = mount(CubeTableSearch, {
      props: { ctx, placeholder: '输入名称搜索' },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();
    expect(wrapper.find('input').attributes('placeholder')).toBe('输入名称搜索');
  });

  it('应该给展开入口 当业务页自带条件面板但没有元数据条件时', async () => {
    const { wrapper } = await mountSearch([], {}, { advanced: '<div class="mine">自定义</div>' });
    expect(wrapper.find('.cts-toggle').exists()).toBe(true);
    await expand(wrapper);
    expect(wrapper.find('.mine').exists()).toBe(true);
  });

  it('应该整块接管搜索栏 当使用 #bar 插槽时', async () => {
    const { wrapper } = await mountSearch(MANY, {}, { bar: '<div class="mybar">自定义搜索栏</div>' });
    expect(wrapper.find('.mybar').exists()).toBe(true);
    expect(wrapper.find('input').exists()).toBe(false);
  });
});

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
    await expand(wrapper);
    expect(wrapper.text()).toContain('名称');
    expect(wrapper.text()).toContain('启用');
    expect(wrapper.findAll('.el-input-number')).toHaveLength(2);
    expect(wrapper.findAll('.el-date-editor')).toHaveLength(1);
    // 布尔条件是一个「全部/是/否」下拉，日期条件是日期区间控件
    expect(wrapper.findAll('.el-select')).toHaveLength(1);
  });

  it('应该用 min/max 两格表现数值区间 当条件是数值范围时', async () => {
    const { wrapper } = await mountSearch([FIELDS.count]);
    await expand(wrapper);
    // 占位文案在 input 的属性上，不是文本节点
    const placeholders = wrapper.findAll('input').map((i) => i.attributes('placeholder') ?? '');
    expect(placeholders).toEqual(expect.arrayContaining(['最小', '最大']));
    expect(wrapper.findAll('.el-input-number')).toHaveLength(2);
  });

  it('应该把内联字典渲染成下拉选项 当条件带 dataSource 时', async () => {
    const { wrapper } = await mountSearch([FIELDS.dict]);
    await expand(wrapper);
    await wrapper.find('.el-select').trigger('click');
    await nextTick();
    const options = document.body.querySelectorAll('.el-select-dropdown__item');
    expect(Array.from(options).map((o) => o.textContent?.trim())).toEqual(
      expect.arrayContaining(['小时', '班次']),
    );
  });

  it('应该给是/否三档 当条件是布尔时', async () => {
    const { wrapper } = await mountSearch([FIELDS.enable]);
    await expand(wrapper);
    await wrapper.find('.el-select').trigger('click');
    await nextTick();
    const options = Array.from(document.body.querySelectorAll('.el-select-dropdown__item')).map((o) =>
      o.textContent?.trim(),
    );
    expect(options).toEqual(expect.arrayContaining(['全部', '是', '否']));
  });

  it('应该把值集条件交给 LovSelect 当条件带 lovCode 时', async () => {
    const { wrapper } = await mountSearch([FIELDS.kind]);
    await expand(wrapper);
    expect(wrapper.findComponent({ name: 'LovSelect' }).exists()).toBe(true);
  });
});

describe('CubeTableSearch 动作', () => {
  it('应该把搜索与重置打到引擎 当点击面板内两个按钮时', async () => {
    const search = vi.fn(async () => undefined);
    const resetSearch = vi.fn(async () => undefined);
    const { wrapper } = await mountSearch([FIELDS.name], { search, resetSearch });
    await expand(wrapper);
    const buttons = wrapper.findAll('.cts-actions button');
    await buttons.find((b) => b.text().includes('搜索'))!.trigger('click');
    await buttons.find((b) => b.text().includes('重置'))!.trigger('click');
    expect(search).toHaveBeenCalledTimes(1);
    expect(resetSearch).toHaveBeenCalledTimes(1);
  });

  it('应该清空关键字即查 当触发关键字框的 clear 时', async () => {
    const search = vi.fn(async () => undefined);
    const { wrapper } = await mountSearch([], { search }, {}, '泵');
    wrapper.findComponent({ name: 'ElInput' }).vm.$emit('clear');
    await nextTick();
    expect(search).toHaveBeenCalled();
  });
});
