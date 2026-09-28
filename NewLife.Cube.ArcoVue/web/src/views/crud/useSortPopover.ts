import { computed, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ViewSort } from '@/core/utils/viewProfile';

interface SortPopoverProps {
  visible: boolean;
  fields: FieldMeta[];
  modelValue: ViewSort[];
}

interface SortPopoverEmits {
  'update:visible': [boolean];
  apply: [ViewSort[]];
}

type SortPopoverEmit = <K extends keyof SortPopoverEmits>(event: K, ...args: SortPopoverEmits[K]) => void;

const MAX = 3;

/** 有索引可排序的字段排前（主键首列或索引最左列），组内保持原序 */
export function orderSortCandidates<T extends { name?: string; indexed?: boolean; primaryKey?: boolean }>(
  fields: T[],
): T[] {
  const indexed: T[] = [];
  const rest: T[] = [];
  for (const f of fields) {
    if (!f.name) continue;
    if (f.indexed || f.primaryKey) indexed.push(f);
    else rest.push(f);
  }
  return [...indexed, ...rest];
}

/** 工具栏多级排序草稿：最多 3 列，应用后写回视图 */
export function useSortPopover(props: SortPopoverProps, emit: SortPopoverEmit) {
  const draft = ref<ViewSort[]>([]);

  watch(
    () => props.visible,
    (open) => {
      if (!open) return;
      draft.value = (props.modelValue || []).slice(0, MAX).map((s) => ({ field: s.field, desc: !!s.desc }));
    },
    { immediate: true },
  );

  const used = computed(() => new Set(draft.value.map((s) => s.field)));

  const candidateFields = computed(() =>
    orderSortCandidates(props.fields.filter((f) => f.name && !used.value.has(f.name))),
  );

  const labelOf = (name: string) => props.fields.find((f) => f.name === name)?.displayName || name;

  function addField(name: string | number | boolean | Record<string, unknown> | undefined) {
    const field = typeof name === 'string' ? name : '';
    if (!field || draft.value.length >= MAX || used.value.has(field)) return;
    draft.value = [...draft.value, { field, desc: false }];
  }

  function toggleDir(index: number) {
    draft.value = draft.value.map((s, i) => (i === index ? { ...s, desc: !s.desc } : s));
  }

  function removeAt(index: number) {
    draft.value = draft.value.filter((_, i) => i !== index);
  }

  function close() {
    emit('update:visible', false);
  }

  function emitApply() {
    emit('apply', draft.value.slice(0, MAX).map((s) => ({ field: s.field, desc: !!s.desc })));
    close();
  }

  function onVisibleChange(v: boolean) {
    emit('update:visible', v);
  }

  return {
    draft,
    candidateFields,
    canAdd: computed(() => draft.value.length < MAX && candidateFields.value.length > 0),
    labelOf,
    addField,
    toggleDir,
    removeAt,
    close,
    emitApply,
    onVisibleChange,
  };
}
