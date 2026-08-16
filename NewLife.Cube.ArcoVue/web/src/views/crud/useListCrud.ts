import { Message, Modal } from '@arco-design/web-vue';
import { ApiError } from '@cube/api-core';
import cubeApi from '@/api';
import type { FieldMeta } from '@/core/types/field';
import { isEnableField, isTruthy } from '@/core/utils/fieldBadge';
import { getValueByKey, normalizeKeysByFields, setValueByKey } from '@/core/utils/url';
import { formatApiError } from '@/core/utils/apiError';
import { resolveFieldsForKind } from '@/core/utils/fieldParts';
import { prepareSubmitPayload } from '@/core/utils/submitPayload';
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
    chrome,
    enableBusy,
    fieldParts,
    fieldErrors,
    saving,
    drawerMode,
    drawerVisible,
    formModel,
    batchDeleteState,
    selectedKeys,
    chartVisible,
    chartList,
  } = ctx;
  const { loadData, openEdit, openDetail } = deps;

  function onTableAction(payload: { action: string; row: Record<string, unknown> }) {
    if (payload.action.startsWith('auto:')) return;
    if (payload.action === 'edit') openEdit(payload.row);
    else if (payload.action === 'delete') {
      if (!chrome.value.allowDelete) return;
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
   * 再调后端确认；成功后 loadData 权威刷新，失败回滚并提示。
   * Enable 字段走既有 EnableSelect/DisableSelect；其余 Boolean 字段走单字段 Update（复用 Update 接口，不改后端）。
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
        await updateSingleBooleanField(row, field, id as string | number, target);
        Message.success(target ? `${label}：已开启` : `${label}：已关闭`);
      }
      // 后端权威刷新，保证展示与后端一致（含筛选/排序/统计）
      await loadData();
    } catch (err) {
      // 失败回滚：恢复原状态展示
      setValueByKey(row, field.name, oldRaw);
      Message.error(formatApiError(err, '操作失败'));
    } finally {
      enableBusy.value = false;
    }
  }

  /**
   * 单字段 Update：拉完整详情 → 仅改目标字段 → 走既有 Update(PUT) 接口（与表单保存同模式），
   * 避免直接提交最小 payload 时覆盖其它字段。
   */
  async function updateSingleBooleanField(
    row: Record<string, unknown>,
    field: FieldMeta,
    id: string | number,
    target: boolean,
  ) {
    // 与表单编辑同源的字段集（edit 分区回退）
    const targetFields = resolveFieldsForKind('edit', fieldParts.value);
    let detail: Record<string, unknown> = {};
    try {
      const res = await cubeApi.page.getDetail(typePath.value, id);
      detail = (res.data as Record<string, unknown>) || row;
    } catch {
      detail = row;
    }
    // 归一化到字段元数据名（PascalCase），仅保留可编辑字段
    const model = normalizeKeysByFields(detail, targetFields);
    // 主键 + 目标字段
    model[pkField.value] = getValueByKey(detail, pkField.value) ?? id;
    model[field.name] = target;
    const payload = prepareSubmitPayload(model, targetFields, {
      mode: 'edit',
      pkField: pkField.value,
    });
    await cubeApi.page.update(typePath.value, payload);
  }

  async function handleSave() {
    saving.value = true;
    try {
      const mode = drawerMode.value === 'add' ? 'add' : 'edit';
      // 保存字段集与表单回填同源（editForm → addForm），避免字段名不一致
      const fields = resolveFieldsForKind(mode, fieldParts.value);
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
    const id = getValueByKey(row, pkField.value);
    await cubeApi.page.remove(typePath.value, id as string | number);
    Message.success('删除成功');
    loadData();
  }

  function confirmBatchDelete() {
    if (!batchDeleteState.value.visible || batchDeleteState.value.disabled) return;
    if (!selectedKeys.value.length) return;
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
    await cubeApi.page.deleteSelect(typePath.value, selectedKeys.value);
    Message.success('批量删除成功');
    selectedKeys.value = [];
    loadData();
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
    if (!chrome.value.allowDelete) return;
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
    updateSingleBooleanField,
    handleSave,
    handleDelete,
    confirmBatchDelete,
    handleBatchDelete,
    handleExport,
    handleImport,
    onCardDelete,
    onSelectionChange,
    openChart,
  };
}

export type ListCrud = ReturnType<typeof useListCrud>;
