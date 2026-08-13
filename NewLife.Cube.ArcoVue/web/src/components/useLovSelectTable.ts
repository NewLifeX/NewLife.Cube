import { computed, reactive, ref, watch } from 'vue';
import { fetchLovListData, fetchLovMeta } from '@/core/utils/lov-api';
import { getValueByKey } from '@/core/utils/url';
import type { LovListMeta, LovSearchField } from '@/core/types/lov';

/** LovSelectTable 组件 props 类型（与 LovSelectTable.vue defineProps 泛型逐字一致） */
interface LovSelectTableProps {
  visible: boolean;
  lovCode: string;
  /** LIST LOV 元数据（valueField/labelField/tableColumns/searchFields）；缺省时回退 value/label 约定 */
  meta?: LovListMeta | null;
  multiple?: boolean;
  modelValue?: unknown;
}

/** LovSelectTable 组件 emits 类型（与 LovSelectTable.vue defineEmits 泛型逐字一致） */
interface LovSelectTableEmits {
  'update:visible': [boolean];
  confirm: [values: unknown[]];
}

type LovSelectTableEmit = <K extends keyof LovSelectTableEmits>(event: K, ...args: LovSelectTableEmits[K]) => void;

/** LovSelectTable 组件全部业务 TS：LOV 高级表格弹窗/搜索/分页/选择（自 LovSelectTable.vue script setup 原样搬移） */
export function useLovSelectTable(props: LovSelectTableProps, emit: LovSelectTableEmit) {
  const visibleProxy = computed({
    get: () => props.visible,
    set: (v: boolean) => emit('update:visible', v),
  });

  const loading = ref(false);
  const rows = ref<Record<string, unknown>[]>([]);
  const keyword = ref('');
  const pagination = reactive({ current: 1, pageSize: 20, total: 0, showTotal: true });
  const selectedKeys = ref<(string | number)[]>([]);
  const searched = ref(false);
  /** 搜索条件：key = LOV 搜索字段名 */
  const searchValues = reactive<Record<string, unknown>>({});
  /** 枚举搜索字段选项缓存（refLovCode → options） */
  const enumOptions = reactive<Record<string, { value: string; label: string }[]>>({});

  const valueField = computed(() => props.meta?.valueField || 'value');
  const labelField = computed(() => props.meta?.labelField || 'label');
  const rowKey = computed(() => valueField.value);
  const title = computed(() => `选择值集${props.multiple ? '（可多选）' : ''}`);

  const searchFields = computed(() => props.meta?.searchFields ?? []);
  const tableColumns = computed(() => props.meta?.tableColumns ?? []);

  const columns = computed(() => {
    if (tableColumns.value.length) {
      return tableColumns.value.map((c) => ({
        title: c.title || c.field,
        dataIndex: c.field,
        width: c.width && c.width > 0 ? c.width : undefined,
        render: ({ record }: { record: Record<string, unknown> }) =>
          cellText(record, c.field),
      }));
    }
    return [
      {
        title: labelField.value,
        dataIndex: labelField.value,
        render: ({ record }: { record: Record<string, unknown> }) => cellText(record, labelField.value),
      },
    ];
  });

  const rowSelection = computed(() =>
    props.multiple ? { type: 'checkbox', showCheckedAll: true } : undefined,
  );

  function cellText(row: Record<string, unknown>, field: string): string {
    const v = getValueByKey(row, field);
    return v == null ? '-' : String(v);
  }

  function rowValue(row: Record<string, unknown>): unknown {
    return getValueByKey(row, valueField.value);
  }

  function isEnumSearch(sf: LovSearchField): boolean {
    return (
      (sf.componentType || '').toLowerCase() === 'select' &&
      !!sf.refLovCode &&
      sf.refLovCode.startsWith('Enum.')
    );
  }

  function enumOptionsOf(sf: LovSearchField): { value: string; label: string }[] {
    const code = sf.refLovCode || '';
    if (enumOptions[code]) return enumOptions[code];
    fetchLovMeta(code)
      .then((meta) => {
        const item = meta.meta?.find((m) => m.lovCode === code);
        if (item?.type === 'ENUM') enumOptions[code] = item.options;
      })
      .catch(() => undefined);
    return enumOptions[code] ?? [];
  }

  function buildParams(): Record<string, unknown> {
    const params: Record<string, unknown> = {};
    for (const sf of searchFields.value) {
      const v = searchValues[sf.field];
      if (v != null && v !== '') params[sf.field] = v;
    }
    return params;
  }

  async function load() {
    if (!props.lovCode) return;
    loading.value = true;
    try {
      const params = buildParams();
      if (Object.keys(params).length) params.q = keyword.value || params.q;
      const res = await fetchLovListData({
        lovCode: props.lovCode,
        params,
        pageNum: pagination.current,
        pageSize: pagination.pageSize,
      });
      rows.value = (res.data ?? []) as Record<string, unknown>[];
      pagination.total = res.total ?? rows.value.length;
      searched.value = Object.keys(params).length > 0;
    } catch {
      rows.value = [];
      pagination.total = 0;
    } finally {
      loading.value = false;
    }
  }

  function onSearch() {
    pagination.current = 1;
    load();
  }

  function onReset() {
    Object.keys(searchValues).forEach((k) => delete searchValues[k]);
    keyword.value = '';
    pagination.current = 1;
    load();
  }

  function onPage(page: number) {
    pagination.current = page;
    load();
  }

  function onRowClick(record: Record<string, unknown>) {
    // 多选由复选框管理，行点击不触发选择，避免误关弹窗
    if (props.multiple) return;
    emit('confirm', [rowValue(record)]);
    visibleProxy.value = false;
  }

  function onConfirm() {
    emit('confirm', [...selectedKeys.value]);
    visibleProxy.value = false;
  }

  function onCancel() {
    // 丢弃临时勾选，恢复初始选中
    selectedKeys.value = [...initialKeys.value];
    visibleProxy.value = false;
  }

  /** 打开弹窗时的初始选中：由外部 modelValue（valueField 值列表）推导 */
  const initialKeys = ref<(string | number)[]>([]);

  watch(
    () => props.visible,
    (v) => {
      if (v) {
        pagination.current = 1;
        initialKeys.value = toKeys(props.modelValue);
        selectedKeys.value = [...initialKeys.value];
        load();
      }
    },
  );

  function toKeys(v: unknown): (string | number)[] {
    if (v == null || v === '') return [];
    const arr = Array.isArray(v) ? v : [v];
    return arr.filter((x) => x != null && x !== '').map((x) => String(x));
  }

  return {
    visibleProxy,
    loading,
    rows,
    pagination,
    selectedKeys,
    searched,
    searchValues,
    rowKey,
    title,
    searchFields,
    columns,
    rowSelection,
    isEnumSearch,
    enumOptionsOf,
    onSearch,
    onReset,
    onPage,
    onRowClick,
    onConfirm,
    onCancel,
  };
}
