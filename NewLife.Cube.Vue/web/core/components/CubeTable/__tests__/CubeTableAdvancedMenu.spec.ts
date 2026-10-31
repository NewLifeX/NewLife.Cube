/**
 * `CubeTableAdvancedMenu`（高级菜单）组件测试
 *
 * 钉住三件事：菜单项按开关/权限/查询条件显隐；危险操作必须二次确认；
 * 导出格式与开发工具命令打到引擎。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/CubeTableAdvancedMenu.spec.ts
 */
import './domShims';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import CubeTableAdvancedMenu from '../CubeTableAdvancedMenu.vue';
import { makeFakeActions, makeFakeCtx, type FakeCtxOptions } from './fakeCtx';
import type { ToolbarFlags } from '../cubeTable.logic';
import { testI18n } from '../../../__tests__/helpers/i18n';

const FLAGS: ToolbarFlags = {
  add: true,
  batchDelete: true,
  importFile: true,
  exportFile: true,
  refresh: true,
  advanced: true,
  readOnly: false,
};

interface MountOptions extends FakeCtxOptions {
  flags?: Partial<ToolbarFlags>;
}

function mountMenu(options: MountOptions = {}) {
  const ctx = makeFakeCtx(options);
  const actions = makeFakeActions();
  const wrapper = mount(CubeTableAdvancedMenu, {
    props: { ctx, actions, flags: { ...FLAGS, ...options.flags } },
    global: { plugins: [ElementPlus, testI18n] },
    attachTo: document.body,
  });
  return { wrapper, ctx, actions };
}

async function openMenu(wrapper: VueWrapper) {
  await wrapper.find('[data-role="advanced"]').trigger('click');
  await nextTick();
  await nextTick();
}

function menuItemTexts(): string[] {
  return Array.from(document.querySelectorAll('.el-dropdown-menu__item')).map(
    (el) => el.textContent?.trim() ?? '',
  );
}

afterEach(() => {
  document.body.innerHTML = '';
});

describe('CubeTableAdvancedMenu 显隐', () => {
  it('应该导出在前、导入在后 当菜单打开时', async () => {
    const { wrapper } = mountMenu();
    await openMenu(wrapper);

    const texts = menuItemTexts();
    // 导出最常用，Excel 排第一；导入是第二组
    expect(texts[0]).toBe('导出 Excel');
    expect(texts.slice(0, 5)).toEqual(['导出 Excel', '导出 Csv', '导出 Json', '导出 Xml', '导出 Zip']);
    expect(texts.indexOf('导入')).toBe(5);

    // 无查询条件且无勾选时不出现删除与开发工具
    expect(texts).not.toContain('删除全部（当前查询结果）');
    expect(texts).not.toContain('备份到服务器');
  });

  it('应该显示删除全部 当有数据且有删除权限时，无查询条件时置灰', async () => {
    const noQuery = mountMenu({ total: 5 });
    await openMenu(noQuery.wrapper);
    const disabledItem = Array.from(document.querySelectorAll('.el-dropdown-menu__item')).find(
      (el) => el.textContent?.includes('删除全部'),
    );
    expect(disabledItem).toBeTruthy();
    expect(disabledItem!.classList.contains('is-disabled')).toBe(true);

    document.body.innerHTML = '';
    const withQuery = mountMenu({ keyword: 'admin', total: 5 });
    await openMenu(withQuery.wrapper);
    const enabledItem = Array.from(document.querySelectorAll('.el-dropdown-menu__item')).find(
      (el) => el.textContent?.includes('删除全部'),
    );
    expect(enabledItem!.classList.contains('is-disabled')).toBe(false);
  });

  it('应该显示带条数的删除选中 当勾选数据时', async () => {
    const { wrapper } = mountMenu({ selected: [{ id: 1 }, { id: 2 }], total: 5 });
    await openMenu(wrapper);

    expect(menuItemTexts()).toContain('批量删除（2）');
  });

  it('应该不显示删除选中 当没有勾选时', async () => {
    const { wrapper } = mountMenu({ total: 5 });
    await openMenu(wrapper);

    expect(menuItemTexts().some((text) => text.startsWith('批量删除'))).toBe(false);
  });

  it('应该只留导出项 当页面只读时', async () => {
    const { wrapper } = mountMenu({
      keyword: 'admin',
      total: 5,
      selected: [{ id: 1 }],
      setting: { isReadOnly: true },
      flags: { readOnly: true },
    });
    await openMenu(wrapper);

    const texts = menuItemTexts();
    expect(texts).toContain('导出 Excel');
    expect(texts).not.toContain('导入');
    expect(texts).not.toContain('删除全部（当前查询结果）');
    expect(texts.some((text) => text.startsWith('批量删除'))).toBe(false);
  });

  it('应该显示开发工具 当 develop 且系统管理员时', async () => {
    const { wrapper } = mountMenu({
      total: 5,
      setting: { develop: true, isSystem: true },
    });
    await openMenu(wrapper);

    const texts = menuItemTexts();
    expect(texts).toContain('备份到服务器');
    expect(texts).toContain('备份并下载');
    expect(texts).toContain('从最新备份恢复');
    expect(texts).toContain('清空数据表');
  });

  it('应该在有查询条件时隐藏清空数据表 当 develop 且系统管理员时', async () => {
    const { wrapper } = mountMenu({
      keyword: 'admin',
      total: 5,
      setting: { develop: true, isSystem: true },
    });
    await openMenu(wrapper);

    expect(menuItemTexts()).not.toContain('清空数据表');
  });

  it('应该整个按钮不渲染 当高级菜单关闭时', () => {
    const { wrapper } = mountMenu({ flags: { advanced: false } });
    expect(wrapper.find('[data-role="advanced"]').exists()).toBe(false);
  });

  it('应该整个按钮不渲染 当没有任何可用菜单项时', () => {
    const { wrapper } = mountMenu({
      setting: { isReadOnly: true },
      flags: { readOnly: true, importFile: false, exportFile: false, batchDelete: false },
    });
    expect(wrapper.find('[data-role="advanced"]').exists()).toBe(false);
  });
});

describe('CubeTableAdvancedMenu 命令', () => {
  async function emitCommand(wrapper: VueWrapper, command: string) {
    wrapper.findComponent({ name: 'ElDropdown' }).vm.$emit('command', command);
    await flushPromises();
  }

  it('应该把导出格式传给引擎 当选择导出 Csv 时', async () => {
    const { wrapper, ctx } = mountMenu();
    const exportFile = vi.fn(async () => 'file.csv');
    ctx.exportFile = exportFile;

    await emitCommand(wrapper, 'export:csv');
    expect(exportFile).toHaveBeenCalledWith('csv');
  });

  it('应该先确认再删除全部 当选择删除全部时', async () => {
    const { wrapper, ctx } = mountMenu({ keyword: 'admin', total: 5 });
    const removeAll = vi.fn(async () => true);
    ctx.removeAll = removeAll;

    await emitCommand(wrapper, 'delete-all');
    expect(removeAll).not.toHaveBeenCalled();

    (document.querySelector('.el-message-box__btns .el-button--primary') as HTMLElement)?.click();
    await flushPromises();
    expect(removeAll).toHaveBeenCalled();
  });

  it('应该走批量删除动作 当选择删除选中时', async () => {
    const { wrapper, actions } = mountMenu({ selected: [{ id: 1 }], total: 5 });

    await emitCommand(wrapper, 'delete-selected');
    expect(actions.removeSelectedRows).toHaveBeenCalled();
  });

  it('应该把开发工具动作传给引擎 当选择备份时', async () => {
    const { wrapper, ctx } = mountMenu({
      setting: { develop: true, isSystem: true },
    });
    const developAction = vi.fn(async () => true);
    ctx.developAction = developAction;

    await emitCommand(wrapper, 'backup');
    expect(developAction).toHaveBeenCalledWith('Backup');
  });
});
