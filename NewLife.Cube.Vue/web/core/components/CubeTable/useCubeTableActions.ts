/**
 * `CubeTable` 的操作编排（确认弹窗、导入导出触发、下载落地）
 *
 * 视图组件只绑定这些动作，不在 SFC 里写确认文案、循环删除、blob 处理
 * （`docs/standards/code-conventions.md` 关注点分离）。
 * 引擎负责真正的请求与状态，本文件只补「需要用户点头」和「需要碰 DOM」这两件 UI 侧的事。
 */
import { watch } from 'vue';
import { ElMessageBox } from 'element-plus';
import { saveBlob } from './downloadFile';
import type { CubeEngineContext } from '../../engine/types';

export interface CubeTableActionOptions {
  /** 删除是否需要二次确认；缺省跟随后端 `pageSetting.doubleDelete`（未显式关闭就确认） */
  confirmDelete?: () => boolean;
  /** 导出格式（默认 xlsx） */
  exportFormat?: () => string | undefined;
  /** 导入可选文件类型 */
  importAccept?: () => string | undefined;
}

const DELETE_TITLE = '确认删除';
const CONFIRM_BUTTON = '删除';
const CANCEL_BUTTON = '取消';

/** 组装删除确认参数（统一走「危险操作 + 明确动词」口径，避免默认 OK/Cancel 英文按钮） */
function confirmOptions() {
  return { confirmButtonText: CONFIRM_BUTTON, cancelButtonText: CANCEL_BUTTON, type: 'warning' as const };
}

export function useCubeTableActions(ctx: CubeEngineContext, options: CubeTableActionOptions = {}) {
  function needsConfirm(): boolean {
    if (options.confirmDelete) return options.confirmDelete();
    return ctx.pageSetting.value?.doubleDelete !== false;
  }

  /** 用户点取消时 ElMessageBox reject 'cancel'，这里收敛成 false 而不是冒泡未捕获拒绝 */
  async function confirmed(message: string): Promise<boolean> {
    if (!needsConfirm()) return true;
    try {
      await ElMessageBox.confirm(message, DELETE_TITLE, confirmOptions());
      return true;
    } catch {
      return false;
    }
  }

  async function removeRow(row: Record<string, unknown>): Promise<boolean> {
    if (!(await confirmed('确定要删除这条数据吗？'))) return false;
    return ctx.remove(row);
  }

  async function removeSelectedRows(): Promise<boolean> {
    const count = ctx.selected.value.length;
    if (!count) return false;
    if (!(await confirmed(`确定删除选中的 ${count} 条数据吗？`))) return false;
    return ctx.removeSelected();
  }

  async function exportNow(): Promise<string | null> {
    return ctx.exportFile(options.exportFormat?.() ?? 'xlsx');
  }

  /** 隐藏 `<input type=file>` 的 change 处理：导入后清空 value，保证重复选同一文件也能触发 */
  async function onImportFileChange(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    await ctx.importFile(file);
    input.value = '';
  }

  /**
   * 触发隐藏文件框。
   * 入参是 `<input>` 元素本身（模板里的 ref 会被自动解包），不要再取 `.value`。
   */
  function openFilePicker(input: HTMLInputElement | null): void {
    if (!input) return;
    const accept = options.importAccept?.();
    if (accept !== undefined) input.accept = accept;
    input.click();
  }

  /** 引擎产出的 blob 在这里落地并清空（避免同一个文件被重复下载） */
  function bindBlobDownload(): void {
    watch(
      () => ctx.pendingBlob.value,
      (pending) => {
        if (!pending) return;
        saveBlob(pending.blob, pending.name);
        ctx.pendingBlob.value = null;
      },
    );
  }

  return {
    removeRow,
    removeSelectedRows,
    exportNow,
    onImportFileChange,
    openFilePicker,
    bindBlobDownload,
    needsConfirm,
  };
}
