import { computed } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import { resolveSearchControl } from '@/core/utils/fieldControl';
import { normalizeDataSource } from '@/core/utils/viewMapping';

/** SearchFieldInput 组件 props 类型（与 SearchFieldInput.vue defineProps 泛型逐字一致） */
interface SearchFieldInputProps {
  field: FieldMeta;
  modelValue?: unknown;
  /** 范围字段等需要读写相邻键时传入整个 searchForm */
  form?: Record<string, unknown>;
}

/** SearchFieldInput 组件 emits 类型（与 SearchFieldInput.vue defineEmits 泛型逐字一致） */
interface SearchFieldInputEmits {
  'update:modelValue': [unknown];
  'update:key': [key: string, value: unknown];
  search: [];
}

type SearchFieldInputEmit = <K extends keyof SearchFieldInputEmits>(event: K, ...args: SearchFieldInputEmits[K]) => void;

/** SearchFieldInput 组件全部业务 TS：搜索控件解析与取值回写（自 SearchFieldInput.vue script setup 原样搬移） */
export function useSearchFieldInput(props: SearchFieldInputProps, emit: SearchFieldInputEmit) {
  const label = computed(() => props.field.displayName || props.field.name);
  const searchType = computed(() => resolveSearchControl(props.field));
  const hasDataSource = computed(
    () => !!(props.field.dataSource && Object.keys(props.field.dataSource).length),
  );

  const strValue = computed(() =>
    props.modelValue == null ? '' : String(props.modelValue),
  );
  const selectValue = computed(() =>
    props.modelValue == null || props.modelValue === ''
      ? undefined
      : String(props.modelValue),
  );

  /** 单值日期/时间控件取值：null/空串归一 undefined */
  function strOrUndef(v: unknown): string | undefined {
    return v == null || v === '' ? undefined : String(v);
  }

  /** 单值数值控件取值：null/空串/非法数字归一 undefined */
  const numOfField = computed(() => {
    const v = props.modelValue;
    if (v == null || v === '') return undefined;
    const n = Number(v);
    return Number.isNaN(n) ? undefined : n;
  });

  const dataSourceOptions = computed(() =>
    hasDataSource.value ? normalizeDataSource(props.field.dataSource!).options : [],
  );

  const boolOptions = computed(() => {
    if (searchType.value === 'fileExists') {
      return [
        { value: '', label: '全部' },
        { value: 'true', label: '有' },
        { value: 'false', label: '无' },
      ];
    }
    if (hasDataSource.value) {
      return [{ value: '', label: '全部' }, ...dataSourceOptions.value];
    }
    return [
      { value: '', label: '全部' },
      { value: 'true', label: '是' },
      { value: 'false', label: '否' },
    ];
  });

  function emitScalar(v: unknown) {
    emit('update:modelValue', v);
  }

  function onSelect(v: unknown) {
    if (v == null || v === '') {
      emitScalar(undefined);
      return;
    }
    const s = String(v);
    const tn = props.field.typeName;
    if (tn === 'Boolean' || searchType.value === 'switch' || searchType.value === 'fileExists') {
      if (s === 'true' || s === '1') emitScalar(true);
      else if (s === 'false' || s === '0') emitScalar(false);
      else emitScalar(s);
      return;
    }
    if (tn === 'Int64' || tn === 'UInt64') {
      const n = Number(s);
      emitScalar(/^-?\d+$/.test(s.trim()) && Number.isSafeInteger(n) ? n : s);
      return;
    }
    if (tn === 'Int32' || tn === 'Decimal' || tn === 'Double' || tn === 'Single') {
      const n = Number(s);
      emitScalar(Number.isNaN(n) ? s : n);
      return;
    }
    emitScalar(s);
  }

  return {
    searchType,
    strValue,
    label,
    emitScalar,
    numOfField,
    strOrUndef,
    selectValue,
    boolOptions,
    onSelect,
    dataSourceOptions,
    hasDataSource,
  };
}
