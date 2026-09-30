import { computed, ref } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import { ApiError } from '@newlifex/api-core';
import cubeApi from '@/api';
import { isEnableField, isTruthy } from '@/core/utils/fieldBadge';
import { shouldReloadAfterPatch } from '@/core/utils/patchReload';
import { BATCH_ENABLE_MAX } from '@/core/utils/viewMapping';
import { getValueByKey, setValueByKey } from '@/core/utils/url';
import { formatApiError } from '@/core/utils/apiError';
import { readFieldPatchResult } from '@/core/utils/fieldPatchResult';
import { resolveFieldsForKind } from '@/core/utils/fieldParts';
import { prepareSubmitPayload } from '@/core/utils/submitPayload';
import { isIamBatchDeleteBlocked, isIamRowActionDisabled } from '@/core/utils/iamGuards';
import { wfRowCanPatchWritable, wfRowEditLocked, wfRowWritable } from '@/core/types/workflow';
import { kanbanGroupDragMeta } from '@/features/views/kanbanMove';
import type { ListContext } from './listContext';

interface ListCrudDeps {
  loadData: (skipFetch?: boolean) => Promise<void>;
  openEdit: (row: Record<string, unknown>) => Promise<void>;
  openDetail: (row: Record<string, unknown>) => Promise<void>;
}

/**
 * DefaultList 增删改领域（OSC-260813c3e9）：增删改 / 启停 / 导入导出 / 图表打开。
 */
export function useListCrud(ctx: ListContext, deps: ListCrudDeps) {
  const {
    typePath,
    listFields,
    pkField,
    flags,
    enableBusy,
    fieldParts,
    fieldErrors,
    activeKanbanMapping,
    saving,
    drawerMode,
    drawerVisible,
    formModel,
    batchDeleteState,
    batchEnableState,
    selectedKeys,
    tableData,
    activeSorts,
    viewFilter,
    chartVisible,
    chartList,
  } = ctx;
  const { loadData, openEdit, openDetail } = deps;

  function selectedRowsForKeys(): Record<string, unknown>[] {
    const keys = new Set(selectedKeys.value.map((k) => String(k)));
    return tableData.value.filter((row) => {
      const id = getValueByKey(row, pkField.value);
      return id != null && id !== '' && keys.has(String(id));
    });
  }

  function onTableAction(payload: { action: string; row: Record<string, unknown> }) {
    if (payload.action.startsWith('auto:')) return;
    if (payload.action === 'edit') {
      openEdit(payload.row);
    } else if (payload.action === 'delete') {
      if (!flags.value.canDelete) return;
      if (wfRowEditLocked(payload.row)) {
        Message.warning('审批中的记录不可删除');
        return;
      }
      if (isIamRowActionDisabled(typePath.value, payload.row, 'delete')) return;
      Modal.confirm({
        title: '确认删除？',
        content: '删除后不可恢复',
        onOk: () => handleDelete(payload.row),
      });
    } else openDetail(payload.row);
  }

  /**
   * 点击 Boolean 字段徽标（Enable 及任意 Boolean 字段）：受 Update 权限控制（flags.canEdit）。
   * fieldName 由列表/树/卡片/看板点击携带；未携带时回退到 Enable 字段（兼容）。
   * 先乐观更新本地行——按切换后的实际值即时展示（开→success 徽标、关→danger 徽标，双向而非单一禁用态），
   * 再调后端确认。Enable 走启停接口，其它布尔走 patchFields。
   * 字段在当前排序或筛选里才 loadData；失败回滚本地值，不刷新列表。
   */
  async function onToggleEnable(row: Record<string, unknown>, fieldName?: string) {
    if (!flags.value.canEdit) return;
    const field = fieldName
      ? listFields.value.find(
          (f) => f.name === fieldName || f.name.toLowerCase() === (fieldName || '').toLowerCase(),
        )
      : listFields.value.find((f) => isEnableField(f));
    if (!field) return;
    const id = getValueByKey(row, pkField.value);
    if (id == null || id === '') return;
    // 防并发：切换请求进行中忽略再次点击，避免快速双击并发回跳
    if (enableBusy.value) return;
    enableBusy.value = true;
    const oldRaw = getValueByKey(row, field.name);
    const target = !isTruthy(oldRaw);
    const label = field.displayName || field.name;
    // 按字段类型写切换后的实际值（Boolean→true/false，数值→1/0），ListTable deep watch 即时重绘徽标
    const newRaw = field.typeName === 'Boolean' ? target : target ? 1 : 0;
    setValueByKey(row, field.name, newRaw);
    try {
      if (field.name.toLowerCase() === 'enable') {
        if (target) await cubeApi.page.enableSelect(typePath.value, [id as string | number]);
        else await cubeApi.page.disableSelect(typePath.value, [id as string | number]);
        Message.success(target ? '启用成功' : '禁用成功');
      } else {
        const res = await cubeApi.page.patchFields(typePath.value, {
          id: id as string | number,
          values: { [field.name]: newRaw },
        });
        const patch = readFieldPatchResult(res);
        if (patch.fail > 0) throw new Error(patch.errors?.[0]?.message || '操作失败');
        Message.success(target ? `${label}：已开启` : `${label}：已关闭`);
      }
      if (shouldReloadAfterPatch(field.name, activeSorts.value, viewFilter.value)) await loadData();
    } catch (err) {
      // 失败回滚：恢复原状态展示
      setValueByKey(row, field.name, oldRaw);
      Message.error(formatApiError(err, '操作失败'));
    } finally {
      enableBusy.value = false;
    }
  }

  const kanbanMoveBusy = ref(false);

  /** 分组字段在编辑表单、非只读、且有更新权时，看板卡片才可跨列拖 */
  const kanbanGroupDrag = computed(() => {
    const name = activeKanbanMapping.value?.groupField || '';
    const edit = resolveFieldsForKind('edit', fieldParts.value);
    const meta = kanbanGroupDragMeta(edit, name);
    if (!meta || !flags.value.canEdit) return { enabled: false, required: false };
    return { enabled: true, required: meta.required };
  });

  /**
   * 看板跨列放下：先改本地分组字段让卡片换列，再 PATCH。
   * 审批中且字段可写走流程补丁。失败只回滚这一字段，成功后再刷新列表。
   */
  async function onKanbanMove(payload: { row: Record<string, unknown>; field: string; value: unknown }) {
    if (kanbanMoveBusy.value) return;
    const id = getValueByKey(payload.row, pkField.value);
    if (id == null || id === '') return;
    const previous = getValueByKey(payload.row, payload.field);
    kanbanMoveBusy.value = true;
    setValueByKey(payload.row, payload.field, payload.value);
    try {
      if (wfRowEditLocked(payload.row)) {
        const allowed = wfRowWritable(payload.row).some(
          (name) => name.toLowerCase() === payload.field.toLowerCase(),
        );
        if (!allowed) throw new Error('审批中的记录不可修改该字段');
        await cubeApi.workflow.patchEntity(typePath.value, id as string | number, {
          [payload.field]: payload.value,
        });
      } else {
        const res = await cubeApi.page.patchFields(typePath.value, {
          id: id as string | number,
          values: { [payload.field]: payload.value },
        });
        const patch = readFieldPatchResult(res);
        if (patch.fail > 0) {
          throw new Error(patch.errors?.[0]?.message || '更新失败');
        }
      }
    } catch (err) {
      setValueByKey(payload.row, payload.field, previous);
      Message.error(formatApiError(err, '更新失败'));
      kanbanMoveBusy.value = false;
      return;
    }
    try {
      if (shouldReloadAfterPatch(payload.field, activeSorts.value, viewFilter.value)) await loadData();
    } catch (err) {
      Message.error(formatApiError(err, '更新失败'));
    } finally {
      kanbanMoveBusy.value = false;
    }
  }

  async function handleSave() {
    saving.value = true;
    try {
      const mode = drawerMode.value === 'add' ? 'add' : 'edit';
      // 保存字段集与表单回填同源（editForm → addForm），避免字段名不一致
      const fields = resolveFieldsForKind(mode, fieldParts.value);
      // 审批中可写字段：走 Workflow Patch，仅提交白名单字段
      const writable = wfRowWritable(formModel);
      if (mode === 'edit' && wfRowCanPatchWritable(formModel)) {
        const id = getValueByKey(formModel, pkField.value);
        if (id == null || id === '') throw new Error('缺少主键');
        const patch: Record<string, unknown> = {};
        const allow = new Set(writable.map((n) => n.toLowerCase()));
        for (const f of fields) {
          if (!allow.has(f.name.toLowerCase())) continue;
          if (f.name in formModel) patch[f.name] = formModel[f.name];
        }
        await cubeApi.workflow.patchEntity(typePath.value, id as string | number, patch);
        Message.success('可写字段已保存');
        fieldErrors.value = [];
        drawerVisible.value = false;
        await loadData();
        return;
      }
      const payload = prepareSubmitPayload({ ...formModel }, fields, {
        mode,
        pkField: pkField.value,
      });
      if (mode === 'add') await cubeApi.page.add(typePath.value, payload);
      else await cubeApi.page.update(typePath.value, payload);
      Message.success('保存成功');
      fieldErrors.value = [];
      drawerVisible.value = false;
      await loadData();
    } catch (err) {
      // 后端字段级错误优先映射到表单字段；其余保留全局提示（OSC-0009）
      const errors =
        err instanceof ApiError
          ? (err.fieldErrors ?? [])
          : ((err as { response?: { data?: { fieldErrors?: { field: string; message: string }[] } } })
              .response?.data?.fieldErrors ?? []);
      fieldErrors.value = errors;
      if (!errors.length) {
        Message.error(formatApiError(err, '保存失败'));
      }
    } finally {
      saving.value = false;
    }
  }

  async function handleDelete(row: Record<string, unknown>) {
    if (wfRowEditLocked(row)) {
      Message.warning('审批中的记录不可删除');
      return;
    }
    if (isIamRowActionDisabled(typePath.value, row, 'delete')) return;
    const id = getValueByKey(row, pkField.value);
    await cubeApi.page.remove(typePath.value, id as string | number);
    Message.success('删除成功');
    loadData();
  }

  function confirmBatchDelete() {
    if (!batchDeleteState.value.visible || batchDeleteState.value.disabled) return;
    if (!selectedKeys.value.length) return;
    const rows = selectedRowsForKeys();
    if (rows.some((r) => wfRowEditLocked(r))) {
      Message.error('选中记录含审批中，无法删除');
      return;
    }
    if (isIamBatchDeleteBlocked(typePath.value, rows)) {
      Message.error('含系统角色，无法批量删除');
      return;
    }
    const count = selectedKeys.value.length;
    Modal.confirm({
      title: '确认批量删除？',
      content: `将删除已选中的 ${count} 条记录，删除后不可恢复`,
      onOk: () => handleBatchDelete(),
    });
  }

  async function handleBatchDelete() {
    if (!batchDeleteState.value.visible || batchDeleteState.value.disabled) return;
    if (!selectedKeys.value.length) return;
    const rows = selectedRowsForKeys();
    if (rows.some((r) => wfRowEditLocked(r))) {
      Message.error('选中记录含审批中，无法删除');
      return;
    }
    if (isIamBatchDeleteBlocked(typePath.value, rows)) {
      Message.error('含系统角色，无法批量删除');
      return;
    }
    await cubeApi.page.deleteSelect(typePath.value, selectedKeys.value);
    Message.success('批量删除成功');
    selectedKeys.value = [];
    loadData();
  }

  function confirmBatchEnable(enable: boolean) {
    if (!batchEnableState.value.visible || batchEnableState.value.disabled) return;
    const n = selectedKeys.value.length;
    if (n > BATCH_ENABLE_MAX) {
      Message.error('一次最多启用/禁用 200 条');
      return;
    }
    Modal.confirm({
      title: enable ? `确认启用已选 ${n} 条？` : `确认禁用已选 ${n} 条？`,
      content: enable ? '启用后记录将恢复可用。' : '禁用后记录将不可用。',
      onOk: () => handleBatchEnable(enable),
    });
  }

  async function handleBatchEnable(enable: boolean) {
    if (!batchEnableState.value.visible) return;
    const n = selectedKeys.value.length;
    if (n <= 0) return;
    if (n > BATCH_ENABLE_MAX) {
      Message.error('一次最多启用/禁用 200 条');
      return;
    }
    try {
      const res = enable
        ? await cubeApi.page.enableSelect(typePath.value, selectedKeys.value)
        : await cubeApi.page.disableSelect(typePath.value, selectedKeys.value);
      const msg = (res as { message?: string })?.message;
      Message.success(msg || (enable ? '启用成功' : '禁用成功'));
      selectedKeys.value = [];
      await loadData();
    } catch (err) {
      Message.error(formatApiError(err, enable ? '批量启用失败' : '批量禁用失败'));
    }
  }

  function handleExport(format: string | number | Record<string, unknown> | undefined) {
    const key = String(format);
    window.open(`${typePath.value}/ExportFile?format=${encodeURIComponent(key)}`, '_blank');
  }

  async function handleImport(option: {
    fileItem: { file?: File };
    onSuccess: () => void;
    onError: () => void;
  }) {
    const file = option.fileItem.file;
    if (!file) {
      option.onError();
      return;
    }
    try {
      await cubeApi.page.importFile(typePath.value, file);
      Message.success('导入成功');
      option.onSuccess();
      loadData();
    } catch {
      Message.error('导入失败');
      option.onError();
    }
  }

  function onCardDelete(row: Record<string, unknown>) {
    if (!flags.value.canDelete) return;
    if (wfRowEditLocked(row)) {
      Message.warning('审批中的记录不可删除');
      return;
    }
    if (isIamRowActionDisabled(typePath.value, row, 'delete')) return;
    Modal.confirm({
      title: '确认删除？',
      content: '删除后不可恢复',
      onOk: () => handleDelete(row),
    });
  }

  function onSelectionChange(keys: (string | number)[]) {
    selectedKeys.value = keys;
  }

  async function openChart() {
    try {
      const res = await cubeApi.page.getChartData(typePath.value);
      chartList.value = Array.isArray(res.data) ? res.data : [];
    } catch {
      chartList.value = [];
    }
    chartVisible.value = true;
  }
  // 图表入口按钮已暂时移除（OSC-0007），图表区由后续独立 OSC 完善；保留 openChart 供其重新接线
  void openChart;

  return {
    onTableAction,
    onToggleEnable,
    kanbanGroupDrag,
    onKanbanMove,
    handleSave,
    handleDelete,
    confirmBatchDelete,
    confirmBatchEnable,
    handleBatchDelete,
    handleExport,
    handleImport,
    onCardDelete,
    onSelectionChange,
    openChart,
  };
}

export type ListCrud = ReturnType<typeof useListCrud>;
