<template>
  <a-popover
    :popup-visible="visible"
    position="br"
    trigger="click"
    class="filter-builder-popover"
    @popup-visible-change="onVisibleChange"
  >
    <template #content>
      <div class="filter-builder">
        <div class="fb-head">
          <span class="fb-title">筛选</span>
          <a-radio-group v-model="draft.logic" type="button" size="mini">
            <a-radio value="all">且 (AND)</a-radio>
            <a-radio value="any">或 (OR)</a-radio>
          </a-radio-group>
        </div>

        <div class="fb-conds">
          <div
            v-for="(row, i) in draft.rows"
            :key="i"
            class="fb-cond"
            :class="{ 'fb-cond--empty': !row.cond.field }"
          >
            <a-select
              v-model="row.cond.field"
              placeholder="字段"
              allow-clear
              :style="{ width: '112px' }"
              size="small"
              @change="onFieldChange(row)"
            >
              <a-option
                v-for="f in fieldCandidates"
                :key="f.name"
                :value="f.name"
                :label="f.displayName || f.name"
              />
            </a-select>
            <a-select v-model="row.cond.op" :style="{ width: '96px' }" size="small">
              <a-option value="eq" label="等于" />
              <a-option v-if="isRangeField(row.cond.field)" value="between" label="范围" />
            </a-select>
            <div class="fb-value">
              <SearchFieldInput
                v-if="row.cond.field"
                :field="condFieldOf(row.cond.field)"
                :model-value="row.cond.op === 'between' ? undefined : row.cond.value"
                :form="row.form"
                @update:model-value="(v) => (row.cond.value = v)"
                @update:key="(k, v) => onCondKey(row, k, v)"
              />
            </div>
            <a-button type="text" size="mini" class="fb-del" @click="removeCond(i)">
              <IconClose />
            </a-button>
          </div>

          <a-button v-if="!draft.rows.length" type="text" size="small" class="fb-add-first" @click="addCond">
            + 添加条件
          </a-button>
          <a-button v-else type="text" size="small" class="fb-add" @click="addCond">
            + 添加条件
          </a-button>
        </div>

        <div class="fb-foot">
          <a-button size="small" @click="resetDraft">重置</a-button>
          <a-button size="small" :disabled="!canSave" @click="emitSave">保存到此视图</a-button>
          <a-space class="fb-foot-right">
            <a-button size="small" @click="close">取消</a-button>
            <a-button size="small" type="primary" @click="emitApply">应用</a-button>
          </a-space>
        </div>
      </div>
    </template>

    <template #default>
      <slot />
    </template>
  </a-popover>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { IconClose } from '@arco-design/web-vue/es/icon';
import type { FieldMeta } from '@/core/types/field';
import { resolveSearchControl } from '@/core/utils/fieldControl';
import { normalizeFilter, emptyViewFilter, type ViewFilter, type ViewFilterCondition } from '@/core/utils/viewProfile';
import {
  isRangeControl,
  newFilterDraftRow,
  buildCondForm,
  applyCondKey,
  resetCondForField,
  draftToFilter,
  filterToDraftRows,
  type FilterDraftRow,
} from '@/core/utils/filterBuilder';
import SearchFieldInput from '@/components/SearchFieldInput.vue';

const props = defineProps<{
  /** 弹层是否可见（由父级管理，与分组弹层互斥） */
  visible: boolean;
  /** search 分区字段 */
  fields: FieldMeta[];
  /** 当前筛选方案（父级 viewProfile.getFilter） */
  modelValue: ViewFilter;
  /** 是否有命名视图可保存 */
  canSave: boolean;
}>();

const emit = defineEmits<{
  'update:visible': [boolean];
  apply: [ViewFilter];
  save: [ViewFilter];
}>();

function condFieldOf(name: string): FieldMeta {
  return props.fields.find((f) => f.name === name)!;
}

/** 范围型搜索控件判断（委托 filterBuilder.isRangeControl，与 searchFilters 一致） */
function isRangeField(name: string): boolean {
  if (!name) return false;
  return isRangeControl(resolveSearchControl(condFieldOf(name)));
}

const fieldCandidates = computed(() => props.fields.filter((f) => !!f.name));

const draft = reactive<{ logic: 'all' | 'any'; rows: FilterDraftRow[] }>({
  logic: 'all',
  rows: [],
});

function syncDraftFromProps() {
  const f = normalizeFilter(props.modelValue);
  draft.logic = f.logic;
  draft.rows = filterToDraftRows(f);
}

function addCond() {
  draft.rows.push(newFilterDraftRow());
}

function removeCond(i: number) {
  draft.rows.splice(i, 1);
}

function onFieldChange(row: FilterDraftRow) {
  // 字段切换后 op 重置；非范围字段仅支持等于
  resetCondForField(row.cond, isRangeField(row.cond.field));
  row.form = buildCondForm(row.cond);
}

function onCondKey(row: FilterDraftRow, key: string, value: unknown) {
  applyCondKey(row.cond, key, value);
  row.form[key] = value;
}

function resetDraft() {
  draft.logic = 'all';
  draft.rows = [];
}

function toFilter(): ViewFilter {
  return draftToFilter(draft.logic, draft.rows);
}

function emitApply() {
  emit('apply', toFilter());
  close();
}

function emitSave() {
  emit('save', toFilter());
}

function close() {
  emit('update:visible', false);
}

function onVisibleChange(v: boolean) {
  if (v) syncDraftFromProps();
  emit('update:visible', v);
}

// 父级直接关闭（互斥切换）时同步内部
watch(
  () => props.visible,
  (v) => {
    if (v) syncDraftFromProps();
  },
);
</script>

<style scoped>
.filter-builder {
  width: 420px;
  padding: 4px;
}
.fb-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}
.fb-title {
  font-weight: 600;
  font-size: 14px;
}
.fb-conds {
  max-height: 320px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 8px;
}
.fb-cond {
  display: flex;
  align-items: center;
  gap: 6px;
}
.fb-cond--empty .fb-value {
  opacity: 0.45;
}
.fb-value {
  flex: 1;
  min-width: 0;
}
.fb-del {
  flex: 0 0 auto;
}
.fb-add,
.fb-add-first {
  align-self: flex-start;
}
.fb-foot {
  display: flex;
  align-items: center;
  gap: 8px;
  border-top: 1px solid var(--color-border-2);
  padding-top: 8px;
}
.fb-foot-right {
  margin-left: auto;
}
</style>
