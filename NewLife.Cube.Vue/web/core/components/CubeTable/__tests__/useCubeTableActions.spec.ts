/**
 * `useCubeTableActions` 单测：删除确认、批量删除、下载落地、导入复位
 *
 * 只 mock 两件外部事：`ElMessageBox`（确认弹窗）与 `saveBlob`（DOM 下载），
 * 其余全部走真实代码路径，断言的是「用户点取消时到底有没有发请求」这类可见行为。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/useCubeTableActions.spec.ts
 */
import { afterEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { makeFakeCtx } from './fakeCtx';

/**
 * 桩：必须放 `vi.hoisted`，因为 `vi.mock` 的工厂会在所有 import 之前执行，
 * 工厂里引用的普通 `const` 此时还没初始化（表现为「Cannot access before initialization」）。
 */
const stubs = vi.hoisted(() => ({
  confirm: vi.fn(),
  saveBlob: vi.fn(),
}));

vi.mock('element-plus', () => ({
  ElMessageBox: { confirm: (...args: unknown[]) => stubs.confirm(...args) },
}));
vi.mock('../downloadFile', () => ({
  saveBlob: (...args: unknown[]) => stubs.saveBlob(...args),
}));

import { useCubeTableActions } from '../useCubeTableActions';

const confirm = stubs.confirm;
const saveBlob = stubs.saveBlob;

afterEach(() => {
  confirm.mockReset();
  saveBlob.mockReset();
});

describe('useCubeTableActions 删除确认', () => {
  it('应该不发删除请求 当用户在确认框点取消时', async () => {
    confirm.mockRejectedValue('cancel');
    const ctx = makeFakeCtx();
    const remove = vi.fn(async () => true);
    ctx.remove = remove;
    const actions = useCubeTableActions(ctx);

    expect(await actions.removeRow({ id: 1 })).toBe(false);
    expect(remove).not.toHaveBeenCalled();
  });

  it('应该确认后调用引擎删除 当用户点确定时', async () => {
    confirm.mockResolvedValue('confirm');
    const ctx = makeFakeCtx();
    const remove = vi.fn(async () => true);
    ctx.remove = remove;
    const actions = useCubeTableActions(ctx);

    expect(await actions.removeRow({ id: 1 })).toBe(true);
    expect(remove).toHaveBeenCalledWith({ id: 1 });
    // 按钮文案必须是「删除」，默认的 OK/Cancel 在中文后台里读起来像误触
    expect(confirm.mock.calls[0][2]).toMatchObject({ confirmButtonText: '删除', type: 'warning' });
  });

  it('应该跳过确认框 当后端 doubleDelete 显式关闭时', async () => {
    const ctx = makeFakeCtx({ setting: { doubleDelete: false } });
    const remove = vi.fn(async () => true);
    ctx.remove = remove;
    const actions = useCubeTableActions(ctx);

    expect(await actions.removeRow({ id: 1 })).toBe(true);
    expect(confirm).not.toHaveBeenCalled();
    expect(remove).toHaveBeenCalledTimes(1);
  });

  it('应该把确认文案里的条数报准 当批量删除时', async () => {
    confirm.mockResolvedValue('confirm');
    const ctx = makeFakeCtx({ selected: [{ id: 1 }, { id: 2 }, { id: 3 }] });
    const removeSelected = vi.fn(async () => true);
    ctx.removeSelected = removeSelected;
    const actions = useCubeTableActions(ctx);

    expect(await actions.removeSelectedRows()).toBe(true);
    expect(confirm.mock.calls[0][0]).toContain('3 条');
    expect(removeSelected).toHaveBeenCalled();
  });

  it('应该直接返回 false 当没有勾选任何行时', async () => {
    const ctx = makeFakeCtx();
    const actions = useCubeTableActions(ctx);
    expect(await actions.removeSelectedRows()).toBe(false);
    expect(confirm).not.toHaveBeenCalled();
  });
});

describe('useCubeTableActions 导入导出', () => {
  it('应该按入参覆盖导出格式 当页面指定 excel 时', async () => {
    const ctx = makeFakeCtx();
    const exportFile = vi.fn(async () => 'a.xls');
    ctx.exportFile = exportFile;
    const actions = useCubeTableActions(ctx, { exportFormat: () => 'excel' });

    await actions.exportNow();
    expect(exportFile).toHaveBeenCalledWith('excel');
  });

  it('应该选中文件后交给引擎并把输入框清空 当同一个文件要能重复导入时', async () => {
    const ctx = makeFakeCtx();
    const importFile = vi.fn(async () => ({ ok: true, message: '导入 1 行', rows: 1 }));
    ctx.importFile = importFile;
    const actions = useCubeTableActions(ctx);
    const file = new File(['x'], 'a.xlsx');
    const input = { files: [file], value: 'C:\\fakepath\\a.xlsx' } as unknown as HTMLInputElement;

    await actions.onImportFileChange({ target: input } as unknown as Event);
    expect(importFile).toHaveBeenCalledWith(file);
    expect(input.value).toBe('');
  });

  it('应该什么都不做 当用户在选择框里按了取消时', async () => {
    const ctx = makeFakeCtx();
    const importFile = vi.fn(async () => null);
    ctx.importFile = importFile;
    const actions = useCubeTableActions(ctx);
    const input = { files: null, value: '' } as unknown as HTMLInputElement;

    await actions.onImportFileChange({ target: input } as unknown as Event);
    expect(importFile).not.toHaveBeenCalled();
  });

  it('应该把 accept 写进文件框并触发点击 当页面限定导入类型时', () => {
    const ctx = makeFakeCtx();
    const actions = useCubeTableActions(ctx, { importAccept: () => '.csv' });
    const click = vi.fn();
    const input = { accept: '', click } as unknown as HTMLInputElement;

    actions.openFilePicker(input);
    expect(input.accept).toBe('.csv');
    expect(click).toHaveBeenCalled();

    actions.openFilePicker(null);
    expect(click).toHaveBeenCalledTimes(1);
  });
});

describe('useCubeTableActions 下载落地', () => {
  it('应该下载后把容器清空 当引擎产出 blob 时', async () => {
    const blob = new Blob(['x']);
    const ctx = makeFakeCtx();
    const actions = useCubeTableActions(ctx);
    actions.bindBlobDownload();

    ctx.pendingBlob.value = { blob, name: '设备_20261019.xlsx' };
    await nextTick();

    expect(saveBlob).toHaveBeenCalledWith(blob, '设备_20261019.xlsx');
    // 不清空的话，回到同一页再导出一次会因为值没变化而不触发 watch（表现为「点了没反应」）
    expect(ctx.pendingBlob.value).toBeNull();
  });

  it('应该什么都不下载 当容器为空时', async () => {
    const ctx = makeFakeCtx();
    const actions = useCubeTableActions(ctx);
    actions.bindBlobDownload();
    await nextTick();
    expect(saveBlob).not.toHaveBeenCalled();
  });

  it('应该以页面显式意图为准 当 confirmDelete 与后端开关冲突时', () => {
    const ctx = makeFakeCtx({ setting: { doubleDelete: false } });
    // 后端说不用确认，但页面传了 true：以页面为准
    expect(useCubeTableActions(ctx, { confirmDelete: () => true }).needsConfirm()).toBe(true);
    const other = makeFakeCtx({ setting: { doubleDelete: true } });
    expect(useCubeTableActions(other, { confirmDelete: () => false }).needsConfirm()).toBe(false);
  });
});
