/**
 * `CubeTableActions`（操作栏，对应 MES ListPage 的 `.page-actions`）组件测试
 *
 * 钉住三件容易回归的事：按钮显隐优先级（页面意图 > 后端开关 > 默认）、
 * 整页只读时写入口全部消失、批量删除与勾选状态的联动。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/CubeTableActions.spec.ts
 */
import './domShims';
import { describe, expect, it, vi } from 'vitest';
import { nextTick, ref } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import CubeTableActions from '../CubeTableActions.vue';
import { makeFakeActions, makeFakeCtx } from './fakeCtx';
import type { PageSetting } from '../../../engine/types';

interface MountOptions {
  setting?: PageSetting;
  flags?: Record<string, unknown>;
  selected?: Record<string, unknown>[];
  slots?: Record<string, string>;
  submitting?: boolean;
}

async function mountActions(options: MountOptions = {}) {
  const ctx = makeFakeCtx({ setting: options.setting ?? {}, selected: options.selected ?? [] });
  ctx.submitting = ref(options.submitting ?? false);
  const actions = makeFakeActions();
  const wrapper = mount(CubeTableActions, {
    props: { ctx, actions, flags: options.flags ?? {} },
    slots: options.slots ?? {},
    global: { plugins: [ElementPlus] },
  });
  await nextTick();
  return { wrapper, ctx, actions };
}

const buttonByText = (wrapper: Awaited<ReturnType<typeof mountActions>>['wrapper'], label: string) =>
  wrapper.findAll('button').filter((b) => b.text().includes(label))[0];

describe('CubeTableActions 按钮显隐', () => {
  it('应该默认给添加/导入/导出，不给下载模板 当后端没表明确时', async () => {
    const { wrapper } = await mountActions();
    const text = wrapper.text();
    expect(text).toContain('添加');
    expect(text).toContain('批量删除');
    expect(text).toContain('导入');
    expect(text).toContain('导出');
    // 下载模板对应后端自定义 Action，默认不给（给了点了就报错）
    expect(text).not.toContain('下载模板');
  });

  it('应该跟随后端 enableAdd 关添加 当控制器声明不可新增时', async () => {
    const { wrapper } = await mountActions({ setting: { enableAdd: false } });
    expect(wrapper.text()).not.toContain('添加');
  });

  it('应该让页面意图盖过后端开关 当页面显式打开时', async () => {
    const { wrapper } = await mountActions({
      setting: { enableAdd: false, enableSelect: false },
      flags: { add: true, batchDelete: true },
    });
    expect(wrapper.text()).toContain('添加');
    expect(wrapper.text()).toContain('批量删除');
  });

  it('应该在整页只读时收掉全部写入口 当后端 isReadOnly 时', async () => {
    const { wrapper } = await mountActions({ setting: { isReadOnly: true } });
    const text = wrapper.text();
    expect(text).not.toContain('添加');
    expect(text).not.toContain('批量删除');
    expect(text).not.toContain('导入');
    // 查询类动作不受只读影响
    expect(text).toContain('导出');
  });

  it('应该显示下载模板并把动作打到引擎 当页面打开该按钮时', async () => {
    const { wrapper, ctx } = await mountActions({ flags: { downloadTemplate: true } });
    const downloadTemplate = vi.fn(async () => 'tpl.xlsx');
    ctx.downloadTemplate = downloadTemplate;
    await buttonByText(wrapper, '下载模板').trigger('click');
    expect(downloadTemplate).toHaveBeenCalled();
  });

  it('应该整行不渲染 当既无内置按钮也无业务按钮时', async () => {
    const { wrapper } = await mountActions({
      setting: { isReadOnly: true, enableAdd: false, enableSelect: false },
      flags: { exportFile: false },
    });
    expect(wrapper.find('.cta').exists()).toBe(false);
  });
});

describe('CubeTableActions 动作', () => {
  it('应该打开新增弹窗 当点添加时', async () => {
    const { wrapper, ctx } = await mountActions();
    const openCreate = vi.fn();
    ctx.openCreate = openCreate;
    await buttonByText(wrapper, '添加').trigger('click');
    expect(openCreate).toHaveBeenCalled();
  });

  it('应该把导出打到动作集 当点导出时', async () => {
    const { wrapper, actions } = await mountActions();
    await buttonByText(wrapper, '导出').trigger('click');
    expect(actions.exportNow).toHaveBeenCalled();
  });

  it('应该在提交中把导入按钮置忙 当引擎正在写时', async () => {
    const { wrapper } = await mountActions({ submitting: true });
    expect(buttonByText(wrapper, '导入').classes()).toContain('is-loading');
  });
});

describe('CubeTableActions 批量删除与勾选联动', () => {
  it('应该在没有勾选时禁用按钮 当选中数为零时', async () => {
    const { wrapper, actions } = await mountActions();
    const button = buttonByText(wrapper, '批量删除');
    expect(button.attributes('disabled')).toBeDefined();
    await button.trigger('click');
    expect(actions.removeSelectedRows).not.toHaveBeenCalled();
  });

  it('应该把勾选条数写进按钮文案并可点 当有勾选时', async () => {
    const { wrapper, actions } = await mountActions({ selected: [{ id: 1 }, { id: 2 }] });
    const button = buttonByText(wrapper, '批量删除');
    expect(button.text()).toContain('（2）');
    expect(button.attributes('disabled')).toBeUndefined();
    await button.trigger('click');
    expect(actions.removeSelectedRows).toHaveBeenCalled();
  });
});

describe('CubeTableActions 导入文件框', () => {
  it('应该由按钮触发隐藏文件框 当点导入时', async () => {
    const { wrapper, actions } = await mountActions();
    await buttonByText(wrapper, '导入').trigger('click');
    expect(actions.openFilePicker).toHaveBeenCalled();
    const passed = actions.openFilePicker.mock.calls[0][0] as HTMLInputElement;
    expect(passed?.type).toBe('file');
  });

  it('应该把选中的文件交给动作集 当文件框变化时', async () => {
    const { wrapper, actions } = await mountActions();
    await wrapper.find('input[type=file]:not([data-role="toolbar-upload"])').trigger('change');
    expect(actions.onImportFileChange).toHaveBeenCalled();
  });
});

describe('CubeTableActions 插槽', () => {
  it('应该把业务按钮排在内置按钮之前 当页面使用默认插槽时', async () => {
    const { wrapper } = await mountActions({ slots: { default: '<button class="biz">派工</button>' } });
    const buttons = wrapper.findAll('.cta-left button');
    const bizIndex = buttons.findIndex((b) => b.classes().includes('biz'));
    const addIndex = buttons.findIndex((b) => b.text().includes('添加'));
    expect(bizIndex).toBeGreaterThanOrEqual(0);
    expect(bizIndex).toBeLessThan(addIndex);
  });

  it('应该把追加内容排在行尾 当使用 #extra 时', async () => {
    const { wrapper } = await mountActions({ slots: { extra: '<span class="x">右侧动作</span>' } });
    expect(wrapper.find('.cta-right .x').exists()).toBe(true);
  });
});
describe('CubeTableActions 后端声明的工具栏按钮', () => {
  const toolbar = [
    { name: '导入数据', displayName: '导入数据', kind: 'link' as const, url: '/School/Student/Import', target: '_blank' },
    { name: '批量刷新', displayName: '批量刷新', kind: 'request' as const, action: 'Refresh', include: ['keys'], requiresSelection: true },
    { name: '高级导入', displayName: '高级导入', kind: 'custom' as const, renderer: 'not-registered' },
  ];

  async function mountWithToolbar(selected: Record<string, unknown>[]) {
    const ctx = makeFakeCtx({ setting: {}, selected, toolbar });
    const wrapper = mount(CubeTableActions, {
      props: { ctx, actions: makeFakeActions(), flags: {} },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();
    return wrapper;
  }

  it('应该渲染后端声明的按钮，并跳过未注册渲染器的 custom 按钮', async () => {
    const wrapper = await mountWithToolbar([{ Id: 1 }]);
    const text = wrapper.text();

    expect(text).toContain('导入数据');
    expect(text).toContain('批量刷新');
    expect(text).not.toContain('高级导入');
  });

  it('应该禁用需要选中数据的按钮 当没有勾选时', async () => {
    const wrapper = await mountWithToolbar([]);
    const btn = wrapper.findAll('button').filter((b) => b.text().includes('批量刷新'))[0];

    expect(btn.attributes('disabled')).toBeDefined();
  });

  it('应该启用需要选中数据的按钮 当已勾选时', async () => {
    const wrapper = await mountWithToolbar([{ Id: 1 }]);
    const btn = wrapper.findAll('button').filter((b) => b.text().includes('批量刷新'))[0];

    expect(btn.attributes('disabled')).toBeUndefined();
  });
});