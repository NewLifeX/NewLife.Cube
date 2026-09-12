/**
 * 审批接收人选择逻辑（OSC-26090347f1 T8c）：承载原 WorkflowRecipientPicker.vue 内的
 * 远端搜索、类别切换与已选值规范化，使 SFC 通过「不内嵌业务 TS」门禁
 * （sfcThin.spec.ts 禁止 .vue 内出现 watch/onMounted 等业务钩子）。
 * 搜索复用 recipient.ts searchRecipients（与自动化动作卡片同策略）。
 */
import { computed, ref, watch } from 'vue';
import type { RecipientKind, RecipientOption } from './recipient';
import { normalizeSelectIds, searchRecipients } from './recipient';

/** 接收人类别显示名 */
export const RECIPIENT_KIND_LABEL: Record<RecipientKind, string> = {
  users: '用户',
  roles: '角色',
  departments: '部门',
};

/** 接收人选择器属性（默认值由 SFC withDefaults 提供） */
export interface RecipientPickerProps {
  /** 当前类别 */
  kind: RecipientKind;
  /** 已选 Id */
  modelValue: number[];
  /** 多选（转办固定单选） */
  multiple: boolean;
  /** 占位文案 */
  placeholder: string;
}

/** 接收人选择器事件 */
export interface RecipientPickerEmits {
  /** 类别变化 */
  (e: 'update:kind', kind: RecipientKind): void;
  /** 已选项变化 */
  (e: 'update:modelValue', ids: number[]): void;
}

/**
 * 接收人选择器组合式函数。
 * 返回的 doSearch 供外部（弹窗打开时）主动触发搜索，SFC 通过 defineExpose 暴露。
 */
export function useRecipientPicker(props: RecipientPickerProps, emit: RecipientPickerEmits) {
  const kind = computed(() => props.kind);
  const selected = computed(() => props.modelValue ?? []);
  /** 单选时 a-select 要标量，多选才是数组；否则切换角色/部门后点选无效 */
  const selectValue = computed(() =>
    props.multiple ? selected.value : (selected.value[0] ?? undefined),
  );

  const options = ref<RecipientOption[]>([]);
  const loading = ref(false);
  let seq = 0;

  async function doSearch(keyword = '', searchKind: RecipientKind = kind.value) {
    const s = ++seq;
    loading.value = true;
    try {
      const list = await searchRecipients(searchKind, keyword);
      if (s !== seq) return;
      // 已选项不在当前页时保留占位标签
      const map = new Map(list.map((x) => [x.id, x]));
      const keep = searchKind === kind.value ? selected.value : [];
      for (const id of keep) {
        if (!map.has(id)) map.set(id, { id, name: String(id), displayName: String(id) });
      }
      options.value = [...map.values()];
    } finally {
      if (s === seq) loading.value = false;
    }
  }

  function onKindChange(v: string | number | boolean) {
    const next = String(v) as RecipientKind;
    if (!RECIPIENT_KIND_LABEL[next] || next === kind.value) return;
    emit('update:kind', next);
    emit('update:modelValue', []);
    options.value = [];
    void doSearch('', next);
  }

  function onUpdate(v: unknown) {
    emit('update:modelValue', normalizeSelectIds(v));
  }

  watch(
    () => props.kind,
    (k) => {
      void doSearch('', k);
    },
  );

  return { kind, selected, selectValue, options, loading, doSearch, onKindChange, onUpdate };
}
