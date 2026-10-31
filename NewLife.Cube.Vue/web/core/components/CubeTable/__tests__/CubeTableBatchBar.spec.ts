/**
 * `CubeTableBatchBar`（选中态批量条）组件测试
 *
 * 钉住：不勾选不渲染、勾选后显示数量与动作、只读写入口消失、启用/禁用请求口径。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/CubeTableBatchBar.spec.ts
 */
import './domShims';
import { afterEach, describe, expect, it, vi } from 'vitest';

const requestStub = vi.hoisted(() => vi.fn(async (..._args: unknown[]): Promise<unknown> => undefined));
vi.mock('../../../utils/request', () => ({
  default: (...args: unknown[]) => requestStub(...args),
}));
import { nextTick } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import CubeTableBatchBar from '../CubeTableBatchBar.vue';
import { makeFakeActions, makeFakeCtx, FIELDS, type FakeCtxOptions } from './fakeCtx';
import type { ToolbarFlags } from '../cubeTable.logic';
import { testI18n } from '../../../__tests__/helpers/i18n';

const FLAGS: ToolbarFlags = {
  add: true,
  batchDelete: true,
  importFile: true,
  exportFile: true,
  downloadTemplate: false,
  refresh: true,
  advanced: true,
  readOnly: false,
};

interface MountOptions extends FakeCtxOptions {
  flags?: Partial<ToolbarFlags>;
}

function mountBar(options: MountOptions = {}) {
  const ctx = makeFakeCtx(options);
  const actions = makeFakeActions();
  const wrapper = mount(CubeTableBatchBar, {
    props: { ctx, actions, flags: { ...FLAGS, ...options.flags } },
    global: { plugins: [ElementPlus, testI18n] },
  });
  return { wrapper, ctx, actions };
}

afterEach(() => {
  document.body.innerHTML = '';
});

describe('CubeTableBatchBar 显隐', () => {
  it('应该不渲染 当没有勾选时', () => {
    const { wrapper } = mountBar();
    expect(wrapper.find('[data-role="batch-bar"]').exists()).toBe(false);
  });

  it('应该显示已选数量与批量删除 当有勾选时', () => {
    const { wrapper } = mountBar({ selected: [{ id: 1 }, { id: 2 }] });
    const bar = wrapper.find('[data-role="batch-bar"]');
    expect(bar.exists()).toBe(true);
    expect(bar.text()).toContain('已选 2 项');
    expect(bar.find('[data-role="batch-delete"]').exists()).toBe(true);
    expect(bar.find('[data-role="clear-selection"]').exists()).toBe(true);
  });

  it('应该只在实体含 Boolean Enable 字段时显示批量启用/禁用', () => {
    const withoutEnable = mountBar({ selected: [{ id: 1 }], listFields: [FIELDS.id, FIELDS.name] });
    expect(withoutEnable.wrapper.find('[data-role="batch-enable"]').exists()).toBe(false);

    const withEnable = mountBar({ selected: [{ id: 1 }], listFields: [FIELDS.id, FIELDS.enable] });
    expect(withEnable.wrapper.find('[data-role="batch-enable"]').exists()).toBe(true);
    expect(withEnable.wrapper.find('[data-role="batch-disable"]').exists()).toBe(true);
  });

  it('应该只留计数与取消选择 当页面只读时', () => {
    const { wrapper } = mountBar({
      selected: [{ id: 1 }],
      listFields: [FIELDS.id, FIELDS.enable],
      flags: { readOnly: true, batchDelete: false },
    });
    const bar = wrapper.find('[data-role="batch-bar"]');
    expect(bar.text()).toContain('已选 1 项');
    expect(bar.find('[data-role="batch-enable"]').exists()).toBe(false);
    expect(bar.find('[data-role="batch-delete"]').exists()).toBe(false);
    expect(bar.find('[data-role="clear-selection"]').exists()).toBe(true);
  });
});

describe('CubeTableBatchBar 动作', () => {
  it('应该调用批量删除动作 当点删除时', async () => {
    const { wrapper, actions } = mountBar({ selected: [{ id: 1 }] });
    await wrapper.find('[data-role="batch-delete"]').trigger('click');
    expect(actions.removeSelectedRows).toHaveBeenCalled();
  });

  it('应该清空勾选 当点取消选择时', async () => {
    const { wrapper, ctx } = mountBar({ selected: [{ id: 1 }] });
    const setSelected = vi.fn();
    ctx.setSelected = setSelected;

    await wrapper.find('[data-role="clear-selection"]').trigger('click');
    expect(setSelected).toHaveBeenCalledWith([]);
  });

  it('应该在批量动作进行中禁用其它动作与取消选择 当删除请求未返回时', async () => {
    const { wrapper, actions } = mountBar({
      selected: [{ id: 1 }],
      listFields: [FIELDS.id, FIELDS.enable],
    });
    // 请求挂起，模拟动作进行中
    actions.removeSelectedRows.mockImplementation(() => new Promise(() => {}));

    await wrapper.find('[data-role="batch-delete"]').trigger('click');
    await nextTick();

    // 当前动作保留 loading，其余动作与取消选择置灰，避免请求仍在跑而用户以为已取消
    expect(wrapper.find('[data-role="batch-delete"]').classes()).toContain('is-loading');
    expect(wrapper.find('[data-role="batch-enable"]').attributes('disabled')).toBeDefined();
    expect(wrapper.find('[data-role="clear-selection"]').attributes('disabled')).toBeDefined();
  });

  it('应该按主键集合请求 enableSelect 并刷新 当确认批量启用时', async () => {    const { wrapper, ctx } = mountBar({
      selected: [{ id: 7 }, { id: 8 }],
      listFields: [FIELDS.id, FIELDS.enable],
    });
    const refresh = vi.fn(async () => undefined);
    ctx.refresh = refresh;
    requestStub.mockResolvedValueOnce({ code: 0, message: 'ok' });

    await wrapper.find('[data-role="batch-enable"]').trigger('click');
    await nextTick();
    (document.querySelector('.el-message-box__btns .el-button--primary') as HTMLElement)?.click();
    for (let i = 0; i < 4; i++) await new Promise((resolve) => setTimeout(resolve, 0));

    const call = requestStub.mock.calls.at(-1)?.[0] as {
      url: string;
      method: string;
      data: { keys: string };
    };
    expect(call.url).toContain('enableSelect');
    expect(call.method).toBe('post');
    expect(call.data.keys).toBe('7,8');
    expect(refresh).toHaveBeenCalled();
  });
});
