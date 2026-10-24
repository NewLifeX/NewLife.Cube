/**
 * `CubeTableGrid` 组件渲染测试
 *
 * 只 mock 一处外部依赖：`lovStore`（值集标签解析会打后端）。
 * 其余走真实渲染，断言「用户看得见什么」：列头、空值占位、开关、值集标签、
 * 勾选列开关、操作列按钮与插槽覆盖。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/CubeTableGrid.spec.ts
 */
import './domShims';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { defineComponent, h, nextTick } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus, { ElMessage } from 'element-plus';
import CubeTableGrid from '../CubeTableGrid.vue';
import { clearFieldRenderers, registerFieldRenderer } from '../../../engine/fieldRenderers';
import { FIELDS, makeFakeActions, makeFakeCtx } from './fakeCtx';
import { testI18n } from '../../../__tests__/helpers/i18n';

const lovStubs = vi.hoisted(() => ({
  /** 默认按「未缓存」返回原值，从而触发异步解析分支 */
  getColumnLabel: vi.fn((_code: string, value: string) => value),
  getMeta: vi.fn(async (_code: string) => ({ type: 'ENUM' })),
  resolveColumnLabels: vi.fn(async (_code: string, value: string) => `类型${value}`),
}));

const requestStub = vi.hoisted(() => vi.fn(async (..._args: unknown[]): Promise<unknown> => undefined));
vi.mock('../../../utils/request', () => ({
  default: (...args: unknown[]) => requestStub(...args),
}));

vi.mock('../../LovSelect/lovStore', () => ({
  getColumnLabel: (...args: unknown[]) => lovStubs.getColumnLabel(...(args as [string, string])),
  getMeta: (...args: unknown[]) => lovStubs.getMeta(...(args as [string])),
  resolveColumnLabels: (...args: unknown[]) =>
    lovStubs.resolveColumnLabels(...(args as [string, string])),
}));

/** el-table 要先跑完一次列宽计算才有表体，挂载后必须等两拍再断言 */
async function mountGrid(
  ctx: ReturnType<typeof makeFakeCtx>,
  options: {
    slots?: Record<string, string>;
    props?: Record<string, unknown>;
    actions?: ReturnType<typeof makeFakeActions>;
  } = {},
) {
  const wrapper = mount(CubeTableGrid, {
    props: { ctx, actions: options.actions ?? makeFakeActions(), ...options.props },
    slots: options.slots ?? {},
    global: { plugins: [ElementPlus, testI18n] },
  });
  await nextTick();
  await nextTick();
  return wrapper;
}

const textOf = async (...args: Parameters<typeof mountGrid>) => (await mountGrid(...args)).text();

/** 取「操作」列的宽（自适应断言入口） */
const actionWidthOf = (wrapper: Awaited<ReturnType<typeof mountGrid>>) => {
  const column = wrapper
    .findAllComponents({ name: 'ElTableColumn' })
    .find((c) => c.props('label') === '操作');
  return column?.props('width');
};

describe('CubeTableGrid 列渲染', () => {
  it('应该按元数据渲染列头与行数据 当列表有数据时', async () => {
    const ctx = makeFakeCtx({
      listFields: [FIELDS.id, FIELDS.name],
      list: [{ Id: 1, Name: '泵体' }, { Id: 2, Name: '模具' }],
      total: 2,
    });
    const text = await textOf(ctx);
    expect(text).toContain('编号');
    expect(text).toContain('名称');
    expect(text).toContain('泵体');
    expect(text).toContain('模具');
  });

  it('应该给空值一个占位而不是留白 当单元格为空时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: null }] });
    expect(await textOf(ctx)).toContain('—');
  });

  it('应该把 0 当有效值显示 当数值列为零时', async () => {
    // 0 / false 被当成「空」是列表最常见的误读来源
    const ctx = makeFakeCtx({ listFields: [FIELDS.count], list: [{ Count: 0 }] });
    const text = await textOf(ctx);
    expect(text).toContain('0');
    expect(text).not.toContain('—');
  });

  it('应该用开关表现布尔列 当列是布尔时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.enable], list: [{ Enable: true }] });
    expect((await mountGrid(ctx)).find('.el-switch').exists()).toBe(true);
  });

  it('应该把值集码值翻译成标签 当列带 lovCode 时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.kind], list: [{ Kind: '1' }] });
    const wrapper = await mountGrid(ctx);
    await vi.waitFor(() => expect(wrapper.text()).toContain('类型1'));
  });

  it('应该先播种 meta 再解析标签 当列是枚举值集时', async () => {
    // resolveColumnLabels 的 ENUM 快路径只读 getCachedMeta，不先 getMeta 就会一直回吐码值
    const ctx = makeFakeCtx({ listFields: [FIELDS.kind], list: [{ Kind: '1' }] });
    const wrapper = await mountGrid(ctx);
    await vi.waitFor(() => expect(lovStubs.getMeta).toHaveBeenCalledWith('EquipmentKinds'));
    expect(lovStubs.getMeta.mock.invocationCallOrder[0]).toBeLessThan(
      lovStubs.resolveColumnLabels.mock.invocationCallOrder[0],
    );
    expect(wrapper.text()).toContain('类型1');
  });

  it('应该直接采用共享缓存里已有的标签且不重复拉 meta 当搜索区已替该值集预热时', async () => {
    // 复现曾经的 bug：共享缓存已命中时旧代码把值整个跳过，本地响应式副本始终为空
    // → 单元格一直显示码值。用 mockImplementation 常驻改写（load 会跑多轮，Once 会被后续轮次打穿）
    lovStubs.getColumnLabel.mockImplementation(() => '预置标签');
    lovStubs.getMeta.mockClear();
    lovStubs.resolveColumnLabels.mockClear();
    try {
      const ctx = makeFakeCtx({ listFields: [FIELDS.kind], list: [{ Kind: '1' }] });
      const wrapper = await mountGrid(ctx);
      await vi.waitFor(() => expect(wrapper.text()).toContain('预置标签'));
      expect(lovStubs.resolveColumnLabels).not.toHaveBeenCalled();
      expect(lovStubs.getMeta).not.toHaveBeenCalled();
    } finally {
      lovStubs.getColumnLabel.mockImplementation((_code: string, value: string) => value);
    }
  });

  it('应该把图片列渲染成缩略图、附件列渲染成链接 当列形态为 image/file 时', async () => {
    const ctx = makeFakeCtx({
      listFields: [FIELDS.avatar, FIELDS.doc],
      list: [{ Avatar: '/cube/image/a.png', Doc: '/cube/file/b.pdf' }],
    });
    const wrapper = await mountGrid(ctx);
    expect(wrapper.find('.el-image').exists()).toBe(true);
    expect(wrapper.text()).toContain('查看文件');
  });

  it('应该原样显示 Json 摘要 当列是 json 且短于截断长度时', async () => {
    const ctx = makeFakeCtx({
      listFields: [FIELDS.payload],
      list: [{ Payload: JSON.stringify({ a: 1 }) }],
    });
    // 7 个字符 < 40 不触发截断；全文由 popover 承担
    expect(await textOf(ctx)).toContain('{"a":1}');
  });

  it('应该按列显隐结果过滤列 当用户关掉了某一列时', async () => {
    const ctx = makeFakeCtx({
      listFields: [FIELDS.name, FIELDS.count],
      list: [{ Name: '泵体', Count: 3 }],
    });
    const text = await textOf(ctx, { props: { visibility: { Name: false, Count: true } } });
    expect(text).not.toContain('名称');
    expect(text).toContain('数量');
  });

  it('应该按用户列序渲染列 当传入 columnOrder 时', async () => {
    const ctx = makeFakeCtx({
      listFields: [FIELDS.name, FIELDS.count],
      list: [{ Name: '泵体', Count: 3 }],
    });
    const wrapper = await mountGrid(ctx, { props: { columnOrder: ['Count', 'Name'] } });
    const labels = wrapper
      .findAllComponents({ name: 'ElTableColumn' })
      .map((c) => c.props('label'))
      .filter((label): label is string => typeof label === 'string');

    expect(labels.indexOf('数量')).toBeLessThan(labels.indexOf('名称'));
  });

  it('应该阻止跳转并请求动作后刷新 当单元格配置 dataAction 时', async () => {
    requestStub.mockClear();
    requestStub.mockResolvedValueOnce({ message: '已处理' });
    const field = {
      ...FIELDS.name,
      url: '/api/Equipments/Equipment/{Id}/refresh',
      dataAction: 'post',
    };
    const ctx = makeFakeCtx({ listFields: [field], list: [{ Id: 7, Name: '泵体' }] });
    const refresh = vi.fn(async () => undefined);
    ctx.refresh = refresh as never;
    const wrapper = await mountGrid(ctx);

    await wrapper.find('.el-link').trigger('click');

    await vi.waitFor(() => expect(requestStub).toHaveBeenCalled());
    expect(requestStub.mock.calls[0][0]).toMatchObject({
      url: '/api/Equipments/Equipment/7/refresh',
      method: 'post',
    });
    await vi.waitFor(() => expect(refresh).toHaveBeenCalled());
  });

  it('应该兼容 ApiResponse 包裹的 message 当动作返回统一响应时', async () => {
    requestStub.mockClear();
    requestStub.mockResolvedValueOnce({ code: 0, data: { message: '已处理' } });
    const field = {
      ...FIELDS.name,
      url: '/Test/TestField/Touch/{Id}',
      dataAction: 'get',
    };
    const ctx = makeFakeCtx({ listFields: [field], list: [{ Id: 7, Name: '泵体' }] });
    const wrapper = await mountGrid(ctx);
    const success = vi.spyOn(ElMessage, 'success').mockImplementation(() => undefined as never);

    try {
      await wrapper.find('.el-link').trigger('click');
      await vi.waitFor(() => expect(success).toHaveBeenCalledWith('已处理'));
    } finally {
      success.mockRestore();
    }
  });
});

describe('CubeTableGrid 字段级渲染器', () => {
  afterEach(() => clearFieldRenderers());

  it('应该把单元格交给已注册的 view 组件 当字段声明 view 时', async () => {
    const Badge = defineComponent({
      props: { value: { type: null, default: null } },
      setup(props) {
        return () => h('strong', { class: 'custom-badge' }, `标签:${props.value}`);
      },
    });
    registerFieldRenderer('cellBadge', { component: Badge });
    const ctx = makeFakeCtx({
      listFields: [{ ...FIELDS.name, view: 'cellBadge' }],
      list: [{ Name: '泵体' }],
    });
    const wrapper = await mountGrid(ctx);
    expect(wrapper.find('.custom-badge').text()).toContain('标签:泵体');
  });

  it('应该回退内置控件 当 view 名称未注册时', async () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {});
    const ctx = makeFakeCtx({
      listFields: [{ ...FIELDS.name, view: 'notRegistered' }],
      list: [{ Name: '泵体' }],
    });
    expect(await textOf(ctx)).toContain('泵体');
    expect(warn).toHaveBeenCalledWith('[CubeTable] 字段渲染器未注册：notRegistered');
  });
});

describe('CubeTableGrid 勾选列与操作列', () => {
  it('应该默认给勾选列 当后端没关 enableSelect 时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    expect((await mountGrid(ctx)).find('.el-table__header .el-checkbox').exists()).toBe(true);
  });

  it('应该收起勾选列 当后端 enableSelect 为 false 时', async () => {
    const ctx = makeFakeCtx({
      listFields: [FIELDS.name],
      list: [{ Name: 'a' }],
      setting: { enableSelect: false },
    });
    expect((await mountGrid(ctx)).find('.el-table__header .el-checkbox').exists()).toBe(false);
  });

  it('应该把勾选结果交回引擎 当表格选中变化时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    const setSelected = vi.fn();
    ctx.setSelected = setSelected;
    const wrapper = await mountGrid(ctx);
    wrapper.findComponent({ name: 'ElTable' }).vm.$emit('selection-change', [{ Name: 'a' }]);
    await nextTick();
    expect(setSelected).toHaveBeenCalledWith([{ Name: 'a' }]);
  });

  it('应该默认给查看/编辑/删除三个动作 当未覆盖操作列时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }, { Name: 'b' }] });
    const text = await textOf(ctx);
    expect(text).toContain('操作');
    // 两行数据 → 每行一组按钮
    expect(text.match(/查看/g)?.length).toBe(2);
    expect(text.match(/编辑/g)?.length).toBe(2);
    expect(text.match(/删除/g)?.length).toBe(2);
  });

  it('应该在只读时不给操作列 当后端 isReadOnly 时', async () => {
    const ctx = makeFakeCtx({
      listFields: [FIELDS.name],
      list: [{ Name: 'a' }],
      setting: { isReadOnly: true },
    });
    expect(await textOf(ctx)).not.toContain('操作');
  });

  it('应该让删除按钮走确认动作而不是直连引擎 当用户点删除时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    const actions = makeFakeActions();
    const remove = vi.fn(async () => true);
    ctx.remove = remove;
    const wrapper = await mountGrid(ctx, { actions });
    const deleteButton = wrapper.findAll('button').filter((b) => b.text().includes('删除'))[0];
    expect(deleteButton).toBeTruthy();
    await deleteButton!.trigger('click');
    expect(actions.removeRow).toHaveBeenCalledWith({ Name: 'a' });
    // 组件不得绕过确认层直接调引擎
    expect(remove).not.toHaveBeenCalled();
  });

  it('应该让业务页整列替换操作按钮 当提供 row-actions 插槽时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    const wrapper = await mountGrid(ctx, {
      slots: { 'row-actions': '<button class="mine">派工</button>' },
    });
    expect(wrapper.find('.mine').exists()).toBe(true);
    expect(wrapper.text()).not.toContain('编辑');
  });

  it('应该按动作数自适应列宽 当未显式指定 actionWidth 时', async () => {
    // 内置三键按 208 给宽（旧固定 168 会把「删除」裁成只剩图标）
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    expect(actionWidthOf(await mountGrid(ctx))).toBe(208);
  });

  it('应该让页面显式列宽优先于自适应 当传入 actionWidth 时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    const wrapper = await mountGrid(ctx, { props: { actionWidth: 300 } });
    expect(actionWidthOf(wrapper)).toBe(300);
  });

  it('应该把追加动作排在删除之后并撑宽列 当 extraRowActions 给 1 个时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    const extra = { key: 'dispatch', label: '派工', onClick: vi.fn() };
    const wrapper = await mountGrid(ctx, { props: { extraRowActions: [extra] } });
    expect(wrapper.text()).toContain('派工');
    expect(actionWidthOf(wrapper)).toBe(272);
  });

  it('应该只露前 3 个加更多 当单行动作超过 4 个时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    const dispatch = { key: 'dispatch', label: '派工', onClick: vi.fn() };
    const suspend = { key: 'suspend', label: '停用', onClick: vi.fn() };
    const wrapper = await mountGrid(ctx, { props: { extraRowActions: [dispatch, suspend] } });
    const text = wrapper.text();
    expect(text).toContain('更多');
    // 第 4、5 个不再直接铺在行里，而是收进下拉（popper 未展开不渲染）
    expect(text).not.toContain('派工');
    expect(text).not.toContain('停用');
    // 列宽按 4 个单元封顶，不再随动作数继续增长
    expect(actionWidthOf(wrapper)).toBe(272);
    // 下拉 command 接回原动作回调，带的是本行数据
    wrapper.findComponent({ name: 'ElDropdown' }).vm.$emit('command', 0);
    await nextTick();
    expect(dispatch.onClick).toHaveBeenCalledWith({ Name: 'a' });
  });

  it('应该把行级不可见的追加动作剔除 当 visible 返回 false 时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }, { Name: 'b' }] });
    const extra = { key: 'dispatch', label: '派工', onClick: vi.fn(), visible: (row: Record<string, unknown>) => row.Name === 'a' };
    const wrapper = await mountGrid(ctx, { props: { extraRowActions: [extra] } });
    // a 行有、b 行没有：两行合计只出现一次
    expect(wrapper.text().match(/派工/g)?.length).toBe(1);
  });

  it('应该给禁用行的追加动作置灰而不是隐藏 当 disabled 返回 true 时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    const extra = { key: 'dispatch', label: '派工', onClick: vi.fn(), disabled: () => true };
    const wrapper = await mountGrid(ctx, { props: { extraRowActions: [extra] } });
    const button = wrapper.findAll('button').find((b) => b.text().includes('派工'));
    expect(button?.attributes('disabled')).toBeDefined();
  });

  it('应该让按字段名的插槽覆盖该列内置渲染 当业务页写了 col-Name 时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    const wrapper = await mountGrid(ctx, {
      slots: { 'col-Name': '<span class="custom">自定义列</span>' },
    });
    expect(wrapper.find('.custom').exists()).toBe(true);
  });

  it('应该双击进编辑 当后端开了 enableTableDoubleClick 时', async () => {
    const ctx = makeFakeCtx({
      listFields: [FIELDS.name],
      list: [{ Name: 'a' }],
      setting: { enableTableDoubleClick: true },
    });
    const openEdit = vi.fn(async () => undefined);
    ctx.openEdit = openEdit;
    const wrapper = await mountGrid(ctx);
    wrapper.findComponent({ name: 'ElTable' }).vm.$emit('row-dblclick', { Name: 'a' });
    await nextTick();
    expect(openEdit).toHaveBeenCalledWith({ Name: 'a' });
  });

  it('应该双击不做任何事 当后端没开双击编辑时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    const openEdit = vi.fn(async () => undefined);
    ctx.openEdit = openEdit;
    const wrapper = await mountGrid(ctx);
    wrapper.findComponent({ name: 'ElTable' }).vm.$emit('row-dblclick', { Name: 'a' });
    await nextTick();
    expect(openEdit).not.toHaveBeenCalled();
  });
});

describe('CubeTableGrid 空态与失败态', () => {
  it('应该说明是「暂无数据」 当列表为空且没有错误时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [] });
    expect(await textOf(ctx)).toContain('暂无数据');
  });

  it('应该把失败原因就地显示 当取数失败且列表为空时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [], error: '接口 500' });
    const text = await textOf(ctx);
    expect(text).toContain('接口 500');
    expect(text).not.toContain('暂无数据');
  });
});

describe('CubeTableGrid 业务自写列', () => {
  /** 业务页在 #default 里写 el-table-column（与 MES ListPage 同一用法） */
  const CUSTOM = `<template #default="scope"><el-table-column label="自写列" prop="Name"><template #default="s"><span class="mine">{{ s.row.Name }}/{{ scope.visibleColumns.Name === false ? '隐藏' : '显示' }}</span></template></el-table-column></template>`;

  it('应该让元数据生成列整块让位 当业务页写了 #default 时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.id, FIELDS.name], list: [{ Id: 1, Name: '泵体' }] });
    const wrapper = await mountGrid(ctx, { slots: { default: CUSTOM } });
    expect(wrapper.find('.mine').exists()).toBe(true);
    const text = wrapper.text();
    // 生成列的列头（编号/名称）不再出现，只剩业务自写的那一列
    expect(text).toContain('自写列');
    expect(text).not.toContain('编号');
    expect(text).not.toContain('名称');
  });

  it('应该把列显隐结果与恢复函数放进作用域 当业务自写列时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: '泵体' }] });
    const resetVisibility = vi.fn();
    const wrapper = await mountGrid(ctx, {
      props: { visibility: { Name: false }, resetVisibility },
      slots: {
        default:
          '<template #default="scope"><el-table-column label="L"><template #default><span class="kind">{{ typeof scope.resetColumnVisibility }}</span></template></el-table-column></template>',
      },
    });
    expect(wrapper.find('.kind').text()).toBe('function');
    expect(typeof resetVisibility).toBe('function');
  });

  it('应该保留勾选列与操作列 当业务自写数据列时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: '泵体' }] });
    const wrapper = await mountGrid(ctx, { slots: { default: CUSTOM } });
    expect(wrapper.find('.el-table__header .el-checkbox').exists()).toBe(true);
    expect(wrapper.text()).toContain('操作');
  });
});

describe('CubeTableGrid 滚动模型', () => {
  it('应该默认 height=100% 让表体内部滚动 当未指定高度时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    const wrapper = await mountGrid(ctx);
    expect(wrapper.findComponent({ name: 'ElTable' }).props('height')).toBe('100%');
  });

  it('应该允许页面覆盖高度 当业务页给定 height 时', async () => {
    const ctx = makeFakeCtx({ listFields: [FIELDS.name], list: [{ Name: 'a' }] });
    const wrapper = await mountGrid(ctx, { props: { height: 360 } });
    expect(wrapper.findComponent({ name: 'ElTable' }).props('height')).toBe(360);
  });
});
