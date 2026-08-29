import cubeApi from '@/api';
import { getValueByKey, normalizeKeysByFields } from '@/core/utils/url';
import { resolveFieldsForKind } from '@/core/utils/fieldParts';
import type { ListContext } from './listContext';

/**
 * DefaultList 抽屉导航领域（OSC-260813c3e9）：抽屉打开与上一条下一条。
 */
export function useRecordNav(ctx: ListContext) {
  const {
    typePath,
    pkField,
    tableData,
    formModel,
    fieldParts,
    drawerVisible,
    drawerMode,
    drawerRowIndex,
  } = ctx;

  function clearModel() {
    Object.keys(formModel).forEach((k) => delete formModel[k]);
  }

  function findVisibleRowIndex(row: Record<string, unknown>): number {
    const id = getValueByKey(row, pkField.value);
    if (id == null || id === '') return -1;
    return tableData.value.findIndex((r) => getValueByKey(r, pkField.value) === id);
  }

  async function loadRecordIntoDrawer(
    row: Record<string, unknown>,
    mode: 'edit' | 'detail',
  ) {
    drawerMode.value = mode;
    drawerRowIndex.value = findVisibleRowIndex(row);
    clearModel();
    const id = getValueByKey(row, pkField.value);
    // GetPage 字段名为 PascalCase，而 GetDetail 返回数据为 camelCase；
    // 按字段元数据归一化 key，否则编辑表单 model[field.name] 取不到值（内容为空）
    // 回填字段与 drawerFields 同源：detail 分区缺失时回退 edit → list，避免详情全空（OSC-0009）
    const targetFields = resolveFieldsForKind(mode, fieldParts.value);
    try {
      const res = await cubeApi.page.getDetail(typePath.value, id as string | number);
      Object.assign(
        formModel,
        normalizeKeysByFields((res.data as Record<string, unknown>) || row, targetFields),
      );
    } catch {
      Object.assign(formModel, normalizeKeysByFields(row, targetFields));
    }
    drawerVisible.value = true;
  }

  function openAdd() {
    drawerMode.value = 'add';
    drawerRowIndex.value = -1;
    clearModel();
    drawerVisible.value = true;
  }

  async function openEdit(row: Record<string, unknown>) {
    await loadRecordIntoDrawer(row, 'edit');
  }

  async function openDetail(row: Record<string, unknown>) {
    await loadRecordIntoDrawer(row, 'detail');
  }

  async function navigateRecord(delta: -1 | 1) {
    const next = drawerRowIndex.value + delta;
    if (next < 0 || next >= tableData.value.length) return;
    const row = tableData.value[next];
    if (!row) return;
    const mode = drawerMode.value === 'edit' ? 'edit' : 'detail';
    await loadRecordIntoDrawer(row, mode);
  }

  return {
    clearModel,
    findVisibleRowIndex,
    loadRecordIntoDrawer,
    openAdd,
    openEdit,
    openDetail,
    navigateRecord,
  };
}

export type RecordNav = ReturnType<typeof useRecordNav>;
