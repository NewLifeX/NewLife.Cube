<script setup lang="ts">
/**
 * 审批接收人选择（OSC-26090347f1 T8c）：用户/角色/部门 单选/多选 + 远端搜索。
 * 逻辑复用 recipient.ts searchRecipients（自动化同策略），UI 对齐自动化动作卡片。
 */
import { computed, ref, watch } from 'vue';
import type { RecipientKind, RecipientOption } from './recipient';
import { normalizeSelectIds, searchRecipients } from './recipient';

const props = withDefaults(
  defineProps<{
    /** 当前类别 */
    kind?: RecipientKind;
    /** 已选 Id */
    modelValue?: number[];
    /** 多选（转办固定单选） */
    multiple?: boolean;
    placeholder?: string;
  }>(),
  { kind: 'users', modelValue: () => [], multiple: true, placeholder: '' },
);

const emit = defineEmits<{
  (e: 'update:kind', kind: RecipientKind): void;
  (e: 'update:modelValue', ids: number[]): void;
}>();

const kind = computed(() => props.kind);
const selected = computed(() => props.modelValue ?? []);
/** 单选时 a-select 要标量，多选才是数组；否则切换角色/部门后点选无效 */
const selectValue = computed(() =>
  props.multiple ? selected.value : (selected.value[0] ?? undefined),
);

const options = ref<RecipientOption[]>([]);
const loading = ref(false);
let seq = 0;

const KIND_LABEL: Record<RecipientKind, string> = {
  users: '用户',
  roles: '角色',
  departments: '部门',
};

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
  if (!KIND_LABEL[next] || next === kind.value) return;
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

defineExpose({ doSearch });
</script>

<template>
  <div class="wf-recipient">
    <a-radio-group
      :model-value="kind"
      type="button"
      size="small"
      class="wf-recipient__kind"
      @change="onKindChange"
    >
      <a-radio v-for="(label, k) in KIND_LABEL" :key="k" :value="k">
        {{ label }}
      </a-radio>
    </a-radio-group>
    <a-select
      :key="kind + (multiple ? '-m' : '-s')"
      :model-value="selectValue"
      :multiple="multiple"
      allow-clear
      allow-search
      :filter-option="false"
      :loading="loading"
      :placeholder="placeholder || `选择${KIND_LABEL[kind]}`"
      size="small"
      @search="doSearch"
      @focus="() => doSearch('')"
      @popup-visible-change="(v: boolean) => v && doSearch('')"
      @update:model-value="onUpdate"
    >
      <a-option
        v-for="o in options"
        :key="o.id"
        :value="o.id"
        :label="o.displayName || o.name || String(o.id)"
      >
        {{ o.displayName || o.name || o.id }}
      </a-option>
    </a-select>
  </div>
</template>

<style scoped>
.wf-recipient {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.wf-recipient__kind {
  width: fit-content;
}
</style>
