/**
 * `CubeTableToolbar` 组件渲染测试
 *
 * 钉住三件容易回归的事：按钮显隐的优先级（页面意图 > 后端开关 > 默认）、
 * 整页只读时写入口全部消失、批量删除按钮与勾选状态的联动。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/CubeTableToolbar.spec.ts
 */
import './domShims';
import { describe, expect, it, vi } from 'vitest';
import { nextTick, ref } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import CubeTableToolbar from '../CubeTableToolbar.vue';
import { makeFakeActions, makeFakeCtx } from './fakeCtx';
import type { PageSetting } from '../../../engine/types';

async function mountToolbar(
  options: { setting?: PageSetting; props?: Record<string, unknown>; selected?: Record<string, unknown>[]; slots?: Record<string, string>; submitting?: boolean } = {},
) {
  const ctx = makeFakeCtx({
    setting: options.setting ?? {},
    selected: options.selected ?? [],
  });
  ctx.submitting = ref(options.submitting ?? false);
  const actions = makeFakeActions();
  const wrapper = mount(CubeTableToolbar, {
    props: { ctx, actions, ...options.props },
    slots: options.slots ?? {},
    global: { plugins: [ElementPlus] },
  });
  await nextTick();
  return { wrapper, ctx, actions };
}

const buttonByText = (wrapper: Awaited<ReturnType<typeof mountToolbar>>['wrapper'], label: string) =>
  wrapper.findAll('button').filter((b) => b.text().includes(label))[0];

describe('CubeTableToolbar 按钮显隐', () => {
  it('应该默认给新增批量删除导入导出与刷新 当后端没给开关时', async () => {
    const { wrapper } = await mountToolbar();
    const text = wrapper.text();
    expect(text).toContain('新增');
    expect(text).toContain('批量删除');
    expect(text).toContain('导入');
    expect(text).toContain('导出');
    // 下载模板对应后端自定义 Action，默认不给（不给就不会点了报错）
    expect(text).not.toContain('下载模板');
    expect(wrapper.find('.ctt-refresh').exists()).toBe(true);
  });

  it('应该跟随后端 enableAdd 关新增 当控制器声明不可新增时', async () => {
    const { wrapper } = await mountToolbar({ setting: { enableAdd: false } });
    expect(wrapper.text()).not.toContain('新增');
  });

  it('应该让页面意图盖过后端开关 当页面显式要求显示时', async () => {
    const { wrapper } = await mountToolbar({
      setting: { enableAdd: false },
      props: { showAdd: true },
    });
    expect(wrapper.text()).toContain('新增');
  });

  it('应该在整页只读时收掉全部写入口 当后端 isReadOnly 时', async () => {
    const { wrapper } = await mountToolbar({ setting: { isReadOnly: true } });
    const text = wrapper.text();
    expect(text).not.toContain('新增');
    expect(text).not.toContain('批量删除');
    expect(text).not.toContain('导入');
    expect(text).not.toContain('下载模板');
    // 查询类动作与刷新不受只读影响
    expect(text).toContain('导出');
    expect(wrapper.find('.ctt-refresh').exists()).toBe(true);
  });

  it('应该显示下载模板并把动作打到引擎 当页面打开该按钮时', async () => {
    const { wrapper, ctx } = await mountToolbar({ props: { showDownloadTemplate: true } });
    expect(wrapper.text()).toContain('下载模板');
    const downloadTemplate = vi.fn(async () => 'tpl.xlsx');
    ctx.downloadTemplate = downloadTemplate;
    await buttonByText(wrapper, '下载模板').trigger('click');
    expect(downloadTemplate).toHaveBeenCalled();
  });
});

describe('CubeTableToolbar 动作', () => {
  it('应该打开新增弹窗 当点新增时', async () => {
    const { wrapper, ctx } = await mountToolbar();
    const openCreate = vi.fn();
    ctx.openCreate = openCreate;
    await buttonByText(wrapper, '新增').trigger('click');
    expect(openCreate).toHaveBeenCalled();
  });

  it('应该把刷新打到引擎 当点刷新时', async () => {
    const { wrapper, ctx } = await mountToolbar();
    const refresh = vi.fn(async () => undefined);
    ctx.refresh = refresh;
    await wrapper.find('.ctt-refresh').trigger('click');
    expect(refresh).toHaveBeenCalled();
  });

  it('应该把导出打到动作集 当点导出时', async () => {
    const { wrapper, actions } = await mountToolbar();
    await buttonByText(wrapper, '导出').trigger('click');
    expect(actions.exportNow).toHaveBeenCalled();
  });

  it('应该在提交中把导入按钮置忙 当引擎正在写时', async () => {
    const { wrapper } = await mountToolbar({ submitting: true });
    expect(buttonByText(wrapper, '导入').classes()).toContain('is-loading');
  });
});

describe('CubeTableToolbar 批量删除与勾选联动', () => {
  it('应该在没有勾选时禁用按钮 当选中数为零时', async () => {
    const { wrapper, actions } = await mountToolbar();
    const button = buttonByText(wrapper, '批量删除');
    expect(button.attributes('disabled')).toBeDefined();
    await button.trigger('click');
    expect(actions.removeSelectedRows).not.toHaveBeenCalled();
  });

  it('应该把勾选条数写进按钮文案并可点 当有勾选时', async () => {
    const { wrapper, actions } = await mountToolbar({ selected: [{ id: 1 }, { id: 2 }] });
    const button = buttonByText(wrapper, '批量删除');
    expect(button.text()).toContain('（2）');
    expect(button.attributes('disabled')).toBeUndefined();
    await button.trigger('click');
    expect(actions.removeSelectedRows).toHaveBeenCalled();
  });
});

describe('CubeTableToolbar 导入文件框', () => {
  it('应该由按钮触发隐藏文件框 当点导入时', async () => {
    const { wrapper, actions } = await mountToolbar();
    await buttonByText(wrapper, '导入').trigger('click');
    expect(actions.openFilePicker).toHaveBeenCalled();
    const passed = actions.openFilePicker.mock.calls[0][0] as HTMLInputElement;
    expect(passed?.type).toBe('file');
  });

  it('应该把选中的文件交给动作集 当文件框变化时', async () => {
    const { wrapper, actions } = await mountToolbar();
    await wrapper.find('input[type=file]').trigger('change');
    expect(actions.onImportFileChange).toHaveBeenCalled();
  });
});

describe('CubeTableToolbar 插槽', () => {
  it('应该渲染业务侧的统计区与追加按钮 当页面使用插槽时', async () => {
    const { wrapper } = await mountToolbar({
      slots: {
        metrics: '<span class="m">共 8 台</span>',
        extra: '<button class="x">派工</button>',
      },
    });
    expect(wrapper.find('.m').exists()).toBe(true);
    expect(wrapper.find('.x').exists()).toBe(true);
  });
});
