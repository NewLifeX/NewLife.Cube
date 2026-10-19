/**
 * `CubeTable` 宿主组件测试
 *
 * 宿主只有两件事：上下文的三级解析、插槽的转发与默认实现。
 * 因此断言全部围绕「谁被渲染了 / 谁被业务页顶掉了」。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/CubeTable.spec.ts
 */
import './domShims';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import CubeTable from '../CubeTable.vue';
import { CubeEngineKey } from '../../../engine/useCubeEngine';
import { FIELDS, makeFakeCtx } from './fakeCtx';

const downloadStubs = vi.hoisted(() => ({ saveBlob: vi.fn() }));
vi.mock('../downloadFile', () => ({
  saveBlob: (...args: unknown[]) => downloadStubs.saveBlob(...args),
}));

vi.mock('../../LovSelect/lovStore', () => ({
  getColumnLabel: (_c: string, v: string) => v,
  resolveColumnLabels: async (_c: string, v: string) => v,
}));

const STORAGE_KEY = 'cube-table:columns:/api/Equipments/Equipment';

/**
 * options 走 makeFakeCtx 的入参（refs 由夹具构造，不能事后用普通值覆盖），
 * behavior 只用于替换动作函数（retry / search 之类）。
 */
function baseCtx(
  options: Parameters<typeof makeFakeCtx>[0] = {},
  behavior: Record<string, unknown> = {},
) {
  return {
    ...makeFakeCtx({
      listFields: [FIELDS.name, FIELDS.count],
      searchFields: [FIELDS.name],
      list: [{ Name: '泵体', Count: 3 }],
      total: 123,
      ...options,
    }),
    ...behavior,
  };
}

async function mountHost(options: { props?: Record<string, unknown>; slots?: Record<string, string>; provide?: Record<symbol, unknown> } = {}) {
  const ctx = baseCtx();
  const wrapper = mount(CubeTable, {
    props: { context: ctx, ...options.props },
    slots: options.slots ?? {},
    global: { plugins: [ElementPlus], provide: options.provide ?? {} },
  });
  await nextTick();
  await nextTick();
  return { wrapper, ctx };
}

beforeEach(() => {
  localStorage.clear();
  downloadStubs.saveBlob.mockReset();
});

describe('CubeTable 上下文解析', () => {
  it('应该用外部传入的 context 渲染全套默认区块 当页面自带引擎时', async () => {
    const { wrapper } = await mountHost();
    const text = wrapper.text();
    expect(text).toContain('名称');
    expect(text).toContain('数量');
    expect(text).toContain('泵体');
    expect(text).toContain('新增');
    expect(text).toContain('查询');
    expect(wrapper.findComponent({ name: 'ElPagination' }).exists()).toBe(true);
  });

  it('应该复用父级提供的引擎 当祖先已 useCubeEngine 时', async () => {
    const ctx = baseCtx();
    // 不传 context：只能来自 inject；若走自动创建分支会去请求后端并报错
    const wrapper = mount(CubeTable, {
      props: {},
      global: { plugins: [ElementPlus], provide: { [CubeEngineKey]: ctx } },
    });
    await nextTick();
    await nextTick();
    expect(wrapper.text()).toContain('泵体');
    expect(ctx.error.value).toBeNull();
  });

  it('应该优先用 context prop 当父级同时提供了引擎时', async () => {
    const injected = baseCtx({ list: [{ Name: '来自注入', Count: 1 }] });
    const passed = baseCtx({ list: [{ Name: '来自prop', Count: 2 }] });
    const wrapper = mount(CubeTable, {
      props: { context: passed },
      global: { plugins: [ElementPlus], provide: { [CubeEngineKey]: injected } },
    });
    await nextTick();
    await nextTick();
    const text = wrapper.text();
    expect(text).toContain('来自prop');
    expect(text).not.toContain('来自注入');
  });
});

describe('CubeTable 插槽转发', () => {
  it('应该让业务页整块顶掉搜索区 当提供 search 插槽时', async () => {
    const { wrapper } = await mountHost({
      slots: { search: '<div class="my-search">我的搜索</div>' },
    });
    expect(wrapper.find('.my-search').exists()).toBe(true);
    expect(wrapper.text()).not.toContain('查询');
  });

  it('应该让业务页整块顶掉表格区 当提供 table 插槽时', async () => {
    const { wrapper } = await mountHost({
      slots: { table: '<div class="my-cards">卡片视图</div>' },
    });
    expect(wrapper.find('.my-cards').exists()).toBe(true);
    expect(wrapper.findComponent({ name: 'ElTable' }).exists()).toBe(false);
  });

  it('应该让业务页顶掉工具栏与分页与表单 当提供对应插槽时', async () => {
    const { wrapper } = await mountHost({
      slots: {
        toolbar: '<div class="my-bar">工具栏</div>',
        pagination: '<div class="my-page">翻页</div>',
        form: '<div class="my-form">表单</div>',
      },
    });
    expect(wrapper.find('.my-bar').exists()).toBe(true);
    expect(wrapper.find('.my-page').exists()).toBe(true);
    expect(wrapper.find('.my-form').exists()).toBe(true);
    expect(wrapper.findComponent({ name: 'ElPagination' }).exists()).toBe(false);
  });

  it('应该提供 header 与 footer 横幅位 当页面要放提示条时', async () => {
    const { wrapper } = await mountHost({
      slots: {
        header: '<div class="hd">顶部</div>',
        footer: '<div class="ft">底部</div>',
      },
    });
    expect(wrapper.find('.hd').exists()).toBe(true);
    expect(wrapper.find('.ft').exists()).toBe(true);
  });

  it('应该把按字段名的单元格插槽转进表格区 当页面写了 col-Name', async () => {
    const { wrapper } = await mountHost({
      slots: { 'col-Name': '<span class="custom-name">改名了</span>' },
    });
    expect(wrapper.find('.custom-name').exists()).toBe(true);
    expect(wrapper.text()).not.toContain('泵体');
  });

  it('应该把行操作插槽转进表格区的操作列 当页面写了 table-row-actions', async () => {
    const { wrapper } = await mountHost({
      slots: { 'table-row-actions': '<button class="row-x">派工</button>' },
    });
    expect(wrapper.find('.row-x').exists()).toBe(true);
    expect(wrapper.text()).not.toContain('编辑');
  });

  it('应该把 toolbar-extra 排在设置列之后 当页面追加工具按钮时', async () => {
    const { wrapper } = await mountHost({
      slots: { 'toolbar-extra': '<button class="ex">业务按钮</button>' },
    });
    expect(wrapper.find('.ex').exists()).toBe(true);
    // 内置按钮仍在
    expect(wrapper.text()).toContain('新增');
  });
});

describe('CubeTable 失败态与设置列', () => {
  it('应该在取数失败时给可读提示并能重试 当引擎置错时', async () => {
    const retry = vi.fn(async () => undefined);
    const ctx = baseCtx({ error: '接口 500' }, { retry });
    const wrapper = mount(CubeTable, {
      props: { context: ctx },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();
    expect(wrapper.text()).toContain('接口 500');
    const button = wrapper.findAll('button').filter((b) => b.text().includes('重试'))[0];
    expect(button).toBeTruthy();
    await button!.trigger('click');
    expect(retry).toHaveBeenCalled();
  });

  it('应该按 localStorage 的历史配置隐藏列 当用户上次关掉了名称列', async () => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ Name: false, Count: true }));
    const { wrapper } = await mountHost();
    // 只看表头：搜索区也用了同一个字段，它的标签不能算「列还在」
    const header = wrapper.find('.el-table__header').text();
    expect(header).not.toContain('名称');
    expect(header).toContain('数量');
  });

  it('应该默认全显示并给出设置列入口 当没有历史配置时', async () => {
    const { wrapper } = await mountHost();
    expect(wrapper.find('.el-table__header').text()).toContain('名称');
    expect(wrapper.find('.ct-cols-trigger').exists()).toBe(true);
  });

  it('应该在没有开启设置列时不给齿轮也不过滤列 当页面关闭该能力时', async () => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ Name: false }));
    const { wrapper } = await mountHost({ props: { columnsSetting: false } });
    // 关掉了持久化，历史配置不再生效（避免页面作者以为列丢了）
    expect(wrapper.find('.el-table__header').text()).toContain('名称');
    expect(wrapper.find('.ctt-cols-trigger').exists()).toBe(false);
  });
});

describe('CubeTable 导出下载落地', () => {
  it('应该把引擎产出的文件交给浏览器下载并清空 当导出完成时', async () => {
    const { wrapper, ctx } = await mountHost();
    const blob = new Blob(['x']);
    ctx.pendingBlob.value = { blob, name: '设备_20261019.xlsx' };
    await nextTick();
    expect(downloadStubs.saveBlob).toHaveBeenCalledWith(blob, '设备_20261019.xlsx');
    expect(ctx.pendingBlob.value).toBeNull();
    expect(wrapper.exists()).toBe(true);
  });
});

describe('CubeTable 表单弹窗', () => {
  it('应该在弹窗打开时渲染字段与保存按钮 当新增弹窗可见时', async () => {
    const ctx = makeFakeCtx({
      listFields: [FIELDS.name],
      formFields: [FIELDS.name, { ...FIELDS.enable, nullable: true }],
    });
    ctx.dialogVisible.value = true;
    mount(CubeTable, { props: { context: ctx }, global: { plugins: [ElementPlus] } });
    // el-dialog 是 append-to-body 的，内容不在 wrapper 里
    await vi.waitFor(() => expect(document.body.textContent).toContain('保存'));
    expect(document.body.textContent).toContain('启用');
  });
});
