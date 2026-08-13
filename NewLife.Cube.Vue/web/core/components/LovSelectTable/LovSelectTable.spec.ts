/**
 * LovSelectTable 组件回归测试
 *
 * 覆盖（防止下次改动回退）：
 *  1. 单选模式 footer 仅含「取消」、无「确定」；多选模式含「取消 + 确定」
 *  2. 左侧勾选列：单选=radio、多选=checkbox（一眼区分交互模式）
 *  3. footer 布局：已选统计在左下角、按钮组在右下角（分居左右）
 *  4. 回显：多选 modelValue 恢复勾选，统计显示对应项数
 *  5. 单选选中并关闭：点 radio → emit select(行) 且关闭弹窗
 *  6. 多选确认：回显后点「确定」→ emit confirm(已选值数组)
 *
 * 外部依赖通过 vi.mock 全部桩掉，组件无需真实后端即可运行：
 *   - @newlifex/cube-vue/core/configure        → getConfig 桩
 *   - @newlifex/cube-vue/core/utils/lov-api     → 直连取数桩（返回本地数据）
 *   - @newlifex/cube-vue/core/utils/request     → axios 实例桩（避免加载 universal-cookie 等缺失依赖）
 *   - @element-plus/icons-vue                   → Search 桩
 */
import { describe, it, expect, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { nextTick } from 'vue';
import ElementPlus from 'element-plus';
import LovSelectTable from './index.vue';
import type { LovListMeta } from '../../types/lov';

// ── jsdom 缺失的全局对象 polyfill ──
(globalThis as unknown as { ResizeObserver: unknown }).ResizeObserver = class {
  observe() {}
  unobserve() {}
  disconnect() {}
};
if (!window.matchMedia) {
  (window as unknown as { matchMedia: unknown }).matchMedia = () => ({
    matches: false,
    media: '',
    onchange: null,
    addListener() {},
    removeListener() {},
    addEventListener() {},
    removeEventListener() {},
    dispatchEvent() {
      return false;
    },
  });
}

// ── Mock 外部依赖 ──
vi.mock('@newlifex/cube-vue/core/configure', () => ({
  getConfig: () => ({ request: { baseUrl: '' } }),
}));

vi.mock('@element-plus/icons-vue', () => ({
  Search: { name: 'Search', template: '<i class="el-icon-search" />' },
}));

// request.ts 顶层会引入 universal-cookie 等当前未安装依赖，直接桩掉避免加载失败
vi.mock('@newlifex/cube-vue/core/utils/request', () => ({
  default: { get: vi.fn(), post: vi.fn() },
  get: vi.fn(),
  post: vi.fn(),
}));

const { MOCK_ROWS, PAGED_ROWS } = vi.hoisted(() => ({
  MOCK_ROWS: [
    { id: 1, name: '管理员' },
    { id: 2, name: '编辑' },
    { id: 3, name: '访客' },
    { id: 4, name: '演示角色' },
  ],
  // 分页回归专用：6 条 + 强制小页(size=2) → 3 页，便于单测复现"跨页回显被当前页勾选裁剪"的 bug
  PAGED_ROWS: Array.from({ length: 6 }, (_, i) => ({ id: i + 1, name: `角色${i + 1}` })),
}));

// code 含 'Paged' → 强制按 size=2 分页，使单测可模拟跨页；其余返回全量（保持既有用例不变）
function resolveList(req?: { lovCode?: string; pageNum?: number; pageSize?: number }) {
  const paged = req?.lovCode?.includes('Paged');
  const source = paged ? PAGED_ROWS : MOCK_ROWS;
  if (!paged) return { data: source, total: source.length };
  const size = 2;
  const page = req?.pageNum ?? 1;
  const start = (page - 1) * size;
  return { data: source.slice(start, start + size), total: source.length };
}

vi.mock('@newlifex/cube-vue/core/utils/lov-api', () => ({
  fetchLovListData: vi.fn(async (req?: { lovCode?: string; pageNum?: number; pageSize?: number }) => resolveList(req)),
  fetchLovListDataDirect: vi.fn(
    async (_cfg?: unknown, req?: { lovCode?: string; pageNum?: number; pageSize?: number }) => resolveList(req),
  ),

  shouldDirectRequest: vi.fn(
    (config: { requestUrl?: string } | null | undefined) =>
      !!(config && config.requestUrl && config.requestUrl.startsWith('/')),
  ),
}));

// lovStore mock
vi.mock('@newlifex/cube-vue/core/components/LovSelect/lovStore', () => ({
  resolveColumnLabels: vi.fn(async (_lovCode: string, raw: unknown) => String(raw ?? '')),
  getColumnLabel: vi.fn((_lovCode: string, value: unknown) => value),
  registerRows: vi.fn(),
}));

// ── 测试夹具 ──
function buildLovMeta(): LovListMeta {
  return {
    lovCode: 'List.CubeDemo.Role',
    type: 'LIST',
    name: '角色',
    valueField: 'id',
    labelField: 'name',
    listConfig: {
      requestUrl: '/Test/TestField/RoleList', // 以 / 开头 → shouldDirectRequest = true
      method: 'GET',
      pageable: true,
      pageNumField: null,
      pageSizeField: null,
      dataPath: 'data',
      totalPath: 'total',
      fixedParams: null,
      proxyRequest: false,
    },
    searchFields: [],
    tableColumns: [
      { field: 'id', title: '编号', width: 80, align: 'left', sortable: false, refLovCode: null, formatType: null },
      { field: 'name', title: '角色名', width: 120, align: 'left', sortable: false, refLovCode: null, formatType: null },
    ],
  };
}

// 打开弹窗并等待异步取数完成（watch(dialogVisible) → fetchListData）
async function openDialog(
  wrapper: ReturnType<typeof mount>,
  multiple = false,
  modelValue?: unknown,
) {
  await wrapper.setProps({ dialogVisible: true, multiple, modelValue } as Record<string, unknown>);
  await flushPromises();
  await nextTick();
  await flushPromises();
  await nextTick();
}

function footerButtons(): string[] {
  const footer = document.querySelector('.el-dialog__footer');
  if (!footer) return [];
  return Array.from(footer.querySelectorAll('.el-button')).map((b) => (b.textContent || '').trim());
}

function footerCountText(): string {
  return (document.querySelector('.lst-selected-count')?.textContent || '').trim();
}

const baseProps = (multiple: boolean, modelValue?: string | number | string[]) => ({
  dialogVisible: false,
  lovCode: 'List.CubeDemo.Role',
  lovMeta: buildLovMeta(),
  inlineEnums: {},
  translateCache: new Map<string, string>(),
  multiple,
  modelValue,
});

describe('LovSelectTable', () => {
  it('单选：footer 仅「取消」、无「确定」；左侧为 radio 列', async () => {
    const wrapper = mount(LovSelectTable, {
      props: baseProps(false),
      global: { plugins: [ElementPlus] },
      attachTo: document.body,
    });
    await openDialog(wrapper, false);

    const btns = footerButtons();
    expect(btns).toContain('取消');
    expect(btns).not.toContain('确定');
    expect(footerCountText()).toContain('已选 0 项');

    expect(document.querySelectorAll('.el-radio').length).toBe(MOCK_ROWS.length);
    expect(document.querySelectorAll('.el-checkbox').length).toBe(0);
    wrapper.unmount();
  });

  it('多选：footer 含「取消 + 确定」；左侧为 checkbox 列', async () => {
    const wrapper = mount(LovSelectTable, {
      props: baseProps(true),
      global: { plugins: [ElementPlus] },
      attachTo: document.body,
    });
    await openDialog(wrapper, true);

    const btns = footerButtons();
    expect(btns).toContain('取消');
    expect(btns).toContain('确定');
    // 多选 type=selection 会额外渲染表头全选 checkbox，故只统计表体行 checkbox
    expect(document.querySelectorAll('.el-table__body .el-checkbox').length).toBe(MOCK_ROWS.length);
    expect(document.querySelectorAll('.el-radio').length).toBe(0);
    expect(footerCountText()).toContain('已选 0 项');
    wrapper.unmount();
  });

  it('footer 布局：已选统计(.lst-selected-count)在左下角、按钮组(.lst-footer-buttons)在右下角（分居左右）', async () => {
    const wrapper = mount(LovSelectTable, {
      props: baseProps(true),
      global: { plugins: [ElementPlus] },
      attachTo: document.body,
    });
    await openDialog(wrapper, true);

    const footer = document.querySelector('.el-dialog__footer') as HTMLElement;
    const footerEl = footer.querySelector('.lst-footer') as HTMLElement;
    expect(footerEl).not.toBeNull();
    const count = footerEl.querySelector('.lst-selected-count') as HTMLElement | null;
    const btns = footerEl.querySelector('.lst-footer-buttons') as HTMLElement | null;
    expect(count).not.toBeNull();
    expect(btns).not.toBeNull();
    // 顺序：已选统计在左、按钮组在右
    expect(footerEl.children[0].classList.contains('lst-selected-count')).toBe(true);
    expect(footerEl.children[footerEl.children.length - 1].classList.contains('lst-footer-buttons')).toBe(true);
    wrapper.unmount();
  });

  it('回显（多选）：modelValue 恢复勾选，统计与高亮行数一致', async () => {
    const wrapper = mount(LovSelectTable, {
      props: baseProps(true, ['1', '2', '3']),
      global: { plugins: [ElementPlus] },
      attachTo: document.body,
    });
    await openDialog(wrapper, true, ['1', '2', '3']);

    expect(document.querySelectorAll('.lst-row--selected').length).toBe(3);
    expect(footerCountText()).toContain('已选 3 项');
    wrapper.unmount();
  });

  it('单选选中并关闭：点左侧 radio → emit select(行) 且关闭弹窗', async () => {
    const wrapper = mount(LovSelectTable, {
      props: baseProps(false),
      global: { plugins: [ElementPlus] },
      attachTo: document.body,
    });
    await openDialog(wrapper, false);

    const radio = document.querySelector('.el-radio input') as HTMLInputElement;
    radio.click();
    await flushPromises();
    await nextTick();

    const selectEvents = wrapper.emitted('select');
    expect(selectEvents).toBeTruthy();
    expect((selectEvents![0][0] as { id: number }).id).toBe(1);
    // 注：单选选中仅 emit('select')，弹窗关闭由父组件 LovSelect 负责，组件本身不 emit update:dialogVisible
    wrapper.unmount();
  });

  it('多选确认：回显后点「确定」→ emit confirm(已选值数组)', async () => {
    const wrapper = mount(LovSelectTable, {
      props: baseProps(true, ['1', '2', '3']),
      global: { plugins: [ElementPlus] },
      attachTo: document.body,
    });
    await openDialog(wrapper, true, ['1', '2', '3']);

    const confirmBtn = Array.from(
      document.querySelectorAll('.el-dialog__footer .el-button'),
    ).find((b) => (b.textContent || '').includes('确定')) as HTMLElement;
    confirmBtn.click();
    await flushPromises();

    const confirmEvents = wrapper.emitted('confirm');
    expect(confirmEvents).toBeTruthy();
    expect(confirmEvents![0][0]).toEqual(['1', '2', '3']);
    wrapper.unmount();
  });

  it('回显（跨页）：modelValue 含跨页已选，打开后统计不被当前页勾选裁剪（根因 C2/C3 修复）', async () => {
    const wrapper = mount(LovSelectTable, {
      // lovCode 含 'Paged' → mock 按 size=2 分页；modelValue 跨 3 页（1..6）
      props: { ...baseProps(true, ['1', '2', '3', '4', '5', '6']), lovCode: 'List.Paged.Role' },
      global: { plugins: [ElementPlus] },
      attachTo: document.body,
    });
    await openDialog(wrapper, true, ['1', '2', '3', '4', '5', '6']);

    // 第 1 页仅返回 id 1,2（size=2）→ 断言统计仍为「已选 6 项」（修复前会被裁成「已选 2 项」）
    expect(footerCountText()).toContain('已选 6 项');
    // 视图：当前页只应高亮命中的 2 行（1,2），其余跨页项不在本页
    expect(document.querySelectorAll('.lst-row--selected').length).toBe(2);
    wrapper.unmount();
  });

  it('回显（跨页）+ 翻页：翻到第 2 页后统计与勾选保持（根因 C2 修复，翻页后已选不正确/勾选才更新）', async () => {
    const wrapper = mount(LovSelectTable, {
      props: { ...baseProps(true, ['1', '2', '3', '4', '5', '6']), lovCode: 'List.Paged.Role' },
      global: { plugins: [ElementPlus] },
      attachTo: document.body,
    });
    await openDialog(wrapper, true, ['1', '2', '3', '4', '5', '6']);
    expect(footerCountText()).toContain('已选 6 项');

    // 翻到第 2 页（el-pagination 下一页按钮）
    const nextBtn = document.querySelector('.el-pagination .btn-next') as HTMLElement | null;
    expect(nextBtn).not.toBeNull();
    nextBtn!.click();
    await flushPromises();
    await nextTick();
    await flushPromises();
    await nextTick();

    // 第 2 页返回 id 3,4 → 断言统计仍为「已选 6 项」（翻页重放不覆盖权威集合），且本页 2 行被回显勾选
    expect(footerCountText()).toContain('已选 6 项');
    expect(document.querySelectorAll('.lst-row--selected').length).toBe(2);
    wrapper.unmount();
  });
});
