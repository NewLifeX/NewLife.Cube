/**
 * `CubeTable` 宿主组件测试
 *
 * 宿主只有两件事：上下文的三级解析、五段结构与插槽转发。
 * 因此断言全部围绕「谁被渲染了 / 顺序对不对 / 谁被业务页顶掉了」。
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
import { testI18n } from '../../../__tests__/helpers/i18n';

const downloadStubs = vi.hoisted(() => ({ saveBlob: vi.fn() }));
vi.mock('../downloadFile', () => ({
  saveBlob: (...args: unknown[]) => downloadStubs.saveBlob(...args),
}));

vi.mock('../../LovSelect/lovStore', () => ({
  getColumnLabel: (_c: string, v: string) => v,
  getMeta: async (_c: string) => ({ type: 'ENUM' }),
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

async function mountHost(
  options: {
    props?: Record<string, unknown>;
    slots?: Record<string, string>;
    provide?: Record<symbol, unknown>;
  } = {},
) {
  const ctx = baseCtx();
  const wrapper = mount(CubeTable, {
    props: { context: ctx, ...options.props },
    slots: options.slots ?? {},
    global: { plugins: [ElementPlus, testI18n], provide: options.provide ?? {} },
  });
  await nextTick();
  await nextTick();
  return { wrapper, ctx };
}

beforeEach(() => {
  localStorage.clear();
  downloadStubs.saveBlob.mockReset();
});

describe('CubeTable 五段结构', () => {
  it('应该按「操作栏→搜索→统计行→表格→分页」自上而下排 当用默认区块时', async () => {
    const { wrapper } = await mountHost();
    const selectors = ['.cta', '.cts-wrap', '.ct-panel .ctm', '.ct-panel .ct-table-wrap', '.ct-panel .ctp'];
    const nodes = selectors.map((sel) => {
      const found = wrapper.find(sel);
      expect(found.exists(), sel).toBe(true);
      return found.element;
    });
    for (let i = 1; i < nodes.length; i += 1) {
      const before = nodes[i - 1].compareDocumentPosition(nodes[i]);
      // 前一段必须在文档流里早于后一段（视觉自上而下）
      expect(before & Node.DOCUMENT_POSITION_FOLLOWING, selectors[i]).toBeTruthy();
    }
  });

  it('应该渲染全套默认区块的内容 当页面自带引擎时', async () => {
    const { wrapper } = await mountHost();
    const text = wrapper.text();
    expect(text).toContain('名称');
    expect(text).toContain('数量');
    expect(text).toContain('泵体');
    expect(text).toContain('添加');
    expect(text).toContain('共 123 条');
    expect(wrapper.findComponent({ name: 'ElPagination' }).exists()).toBe(true);
  });

  it('应该用外部传入的 context 而不是再建一个引擎 当页面自带引擎时', async () => {
    const { ctx } = await mountHost();
    expect(ctx.error.value).toBeNull();
  });

  it('应该复用父级提供的引擎 当祖先已 useCubeEngine 时', async () => {
    const ctx = baseCtx();
    // 不传 context：只能来自 inject；若走自动创建分支会去请求后端并报错
    const wrapper = mount(CubeTable, {
      props: {},
      global: { plugins: [ElementPlus, testI18n], provide: { [CubeEngineKey]: ctx } },
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
      global: { plugins: [ElementPlus, testI18n], provide: { [CubeEngineKey]: injected } },
    });
    await nextTick();
    await nextTick();
    const text = wrapper.text();
    expect(text).toContain('来自prop');
    expect(text).not.toContain('来自注入');
  });
});

describe('CubeTable 区块开关', () => {
  it('应该收掉搜索区 当 showSearch 为 false 时', async () => {
    const { wrapper } = await mountHost({ props: { showSearch: false } });
    expect(wrapper.find('.cts-wrap').exists()).toBe(false);
  });

  it('应该收掉刷新按钮 当 showRefresh 为 false 且无指标时', async () => {
    const { wrapper } = await mountHost({ props: { showRefresh: false, columnsSetting: false } });
    expect(wrapper.find('.ctm').exists()).toBe(false);
  });

  it('应该渲染统计指标行 当页面传入 metrics 时', async () => {
    const { wrapper } = await mountHost({
      props: { metrics: [{ label: '设备总数', value: 128 }] },
    });
    expect(wrapper.find('.ctm-item').text()).toContain('设备总数');
  });

  it('应该把单位文案传给分页栏 当页面覆盖 totalUnit 时', async () => {
    const { wrapper } = await mountHost({ props: { totalUnit: '台设备' } });
    expect(wrapper.find('.ctp-total').text()).toBe('共 123 台设备');
  });
});

describe('CubeTable 插槽转发', () => {
  it('应该让业务页整块顶掉搜索区 当提供 search 插槽时', async () => {
    const { wrapper } = await mountHost({
      slots: { search: '<div class="my-search">我的搜索</div>' },
    });
    expect(wrapper.find('.my-search').exists()).toBe(true);
    expect(wrapper.find('.cts-wrap').exists()).toBe(false);
  });

  it('应该只换搜索栏一行而保留条件面板能力 当提供 search-bar 插槽时', async () => {
    const { wrapper } = await mountHost({
      slots: { 'search-bar': '<div class="my-bar">自定义搜索栏</div>' },
    });
    expect(wrapper.find('.my-bar').exists()).toBe(true);
    // 默认搜索条（三列栅格）被整行接管，不再渲染
    expect(wrapper.find('.cts-grid input').exists()).toBe(false);
  });

  it('应该让业务页整块顶掉表格区 当提供 table 插槽时', async () => {
    const { wrapper } = await mountHost({
      slots: { table: '<div class="my-cards">卡片视图</div>' },
    });
    expect(wrapper.find('.my-cards').exists()).toBe(true);
    expect(wrapper.findComponent({ name: 'ElTable' }).exists()).toBe(false);
  });

  it('应该让业务页顶掉分页与表单 当提供对应插槽时', async () => {
    const { wrapper } = await mountHost({
      slots: {
        footer: '<div class="my-page">翻页</div>',
        form: '<div class="my-form">表单</div>',
      },
    });
    expect(wrapper.find('.my-page').exists()).toBe(true);
    expect(wrapper.find('.my-form').exists()).toBe(true);
    expect(wrapper.findComponent({ name: 'ElPagination' }).exists()).toBe(false);
  });

  it('应该提供 header 横幅位 当页面要放标题或提示条时', async () => {
    const { wrapper } = await mountHost({ slots: { header: '<div class="hd">顶部</div>' } });
    const hd = wrapper.find('.hd');
    expect(hd.exists()).toBe(true);
    // 页头排在操作栏之前
    const following = hd.element.compareDocumentPosition(wrapper.find('.cta').element);
    expect(following & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it('应该把业务按钮转进操作栏、把追加内容转进统计行与分页栏 当页面使用这些插槽时', async () => {
    const { wrapper } = await mountHost({
      slots: {
        actions: '<button class="biz">派工</button>',
        'toolbar-extra': '<span class="te">视图切换</span>',
        'footer-extra': '<span class="fe">已选 2</span>',
      },
    });
    expect(wrapper.find('.cta-left .biz').exists()).toBe(true);
    expect(wrapper.find('.ctm-controls .te').exists()).toBe(true);
    expect(wrapper.find('.ctp-left .fe').exists()).toBe(true);
  });

  it('应该把按字段名的单元格插槽转进表格区 当页面写了 col-Name', async () => {
    const { wrapper } = await mountHost({
      slots: { 'col-Name': '<span class="custom-name">改名了</span>' },
    });
    expect(wrapper.find('.custom-name').exists()).toBe(true);
    expect(wrapper.text()).not.toContain('泵体');
  });

  it('应该把行操作插槽转进表格区的操作列 当页面写了 row-actions', async () => {
    const { wrapper } = await mountHost({
      slots: { 'row-actions': '<button class="row-x">派工</button>' },
    });
    expect(wrapper.find('.row-x').exists()).toBe(true);
    expect(wrapper.text()).not.toContain('编辑');
  });

  it('应该让业务自写列接管数据列 当页面写了 default 插槽', async () => {
    const { wrapper } = await mountHost({
      slots: {
        default:
          '<el-table-column label="自写列" prop="Name"><template #default><span class="mine">X</span></template></el-table-column>',
      },
    });
    expect(wrapper.find('.mine').exists()).toBe(true);
    // 元数据生成列让位（表头不再有「数量」）
    expect(wrapper.find('.el-table__header').text()).not.toContain('数量');
    expect(wrapper.find('.el-table__header').text()).toContain('自写列');
  });

  it('应该把 #extra 类内容排在内置齿轮之前 当页面追加工具按钮时', async () => {
    const { wrapper } = await mountHost({
      slots: { 'toolbar-extra': '<button class="ex">业务按钮</button>' },
    });
    const controls = wrapper.find('.ctm-controls');
    expect(controls.find('.ex').exists()).toBe(true);
    expect(controls.find('.ctcs-trigger').exists()).toBe(true);
    // 内置按钮仍在
    expect(wrapper.text()).toContain('添加');
  });
});

describe('CubeTable 失败态与设置列', () => {
  it('应该把失败原因就地显示在空表格里 当引擎置错时', async () => {
    const { wrapper } = await mountHost({
      props: { context: baseCtx({ error: '接口 500', list: [] }) },
    });
    expect(wrapper.text()).toContain('接口 500');
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
    expect(wrapper.find('.ctcs-trigger').exists()).toBe(true);
  });

  it('应该在没有开启设置列时不给齿轮也不过滤列 当页面关闭该能力时', async () => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ Name: false }));
    const { wrapper } = await mountHost({ props: { columnsSetting: false } });
    // 关掉了持久化，历史配置不再生效（避免页面作者以为列丢了）
    expect(wrapper.find('.el-table__header').text()).toContain('名称');
    expect(wrapper.find('.ctcs-trigger').exists()).toBe(false);
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
    mount(CubeTable, { props: { context: ctx }, global: { plugins: [ElementPlus, testI18n] } });
    // el-dialog 是 append-to-body 的，内容不在 wrapper 里
    await vi.waitFor(() => expect(document.body.textContent).toContain('保存'));
    expect(document.body.textContent).toContain('启用');
  });
});
