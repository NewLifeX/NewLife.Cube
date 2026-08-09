<template>
  <a-popover
    :popup-visible="visible"
    position="bottom"
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
              :style="{ width: '110px' }"
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
            <a-select
              v-model="row.cond.op"
              :style="{ width: '104px' }"
              size="small"
              @change="onOpChange(row)"
            >
              <a-option
                v-for="op in opsOf(row)"
                :key="op"
                :value="op"
                :label="FILTER_OP_LABELS[op]"
              />
            </a-select>
            <div class="fb-value">
              <!-- 为空/不为空：无值控件 -->
              <template v-if="row.cond.field && opNeedsValue(row.cond.op)">
                <!-- 人员：用户实体下拉 -->
                <a-select
                  v-if="kindOfName(row.cond.field) === 'person'"
                  :model-value="row.cond.value"
                  placeholder="请选择人员"
                  allow-clear
                  :loading="userLoading"
                  size="small"
                  style="width: 132px"
                  @update:model-value="onCondValue(row, $event)"
                >
                  <a-option
                    v-for="u in userOptions"
                    :key="u.value"
                    :value="u.value"
                    :label="u.label"
                  >
                    {{ u.label }}
                  </a-option>
                </a-select>
                <!-- 枚举/值集：dataSource 已物化优先本地下拉 -->
                <a-select
                  v-else-if="kindOfName(row.cond.field) === 'enum' && enumOptionsOf(row).length"
                  :model-value="row.cond.value"
                  placeholder="请选择"
                  allow-clear
                  size="small"
                  style="width: 132px"
                  @update:model-value="onCondValue(row, $event)"
                >
                  <a-option
                    v-for="o in enumOptionsOf(row)"
                    :key="o.value"
                    :value="o.value"
                    :label="o.label"
                  >
                    {{ o.label }}
                  </a-option>
                </a-select>
                <!-- 枚举/值集：无 dataSource 的 LOV 值集 -->
                <LovSelect
                  v-else-if="kindOfName(row.cond.field) === 'enum' && !!condFieldOf(row.cond.field)?.lovCode"
                  :code="condFieldOf(row.cond.field)!.lovCode!"
                  :model-value="row.cond.value as string | number | null"
                  size="small"
                  style="width: 132px"
                  @update:model-value="onCondValue(row, $event)"
                />
                <!-- 数字 -->
                <a-input-number
                  v-else-if="kindOfName(row.cond.field) === 'number'"
                  :model-value="row.cond.value"
                  placeholder="数值"
                  size="small"
                  style="width: 132px"
                  @update:model-value="onCondValue(row, $event)"
                />
                <!-- 日期/时间 -->
                <a-date-picker
                  v-else-if="kindOfName(row.cond.field) === 'datetime'"
                  :model-value="row.cond.value"
                  placeholder="日期"
                  size="small"
                  style="width: 132px"
                  value-format="YYYY-MM-DD"
                  @update:model-value="onCondValue(row, $event)"
                />
                <!-- 字符 -->
                <a-input
                  v-else
                  :model-value="row.cond.value"
                  :placeholder="kindOfName(row.cond.field) === 'string' ? '值' : '请输入'"
                  size="small"
                  style="width: 132px"
                  @update:model-value="onCondValue(row, $event)"
                />
              </template>
            </div>
            <a-button type="text" size="mini" class="fb-del" @click="removeCond(i)">
              <icon-park type="close" />
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
import type { FieldMeta } from '@/core/types/field';
import { normalizeFilter, type ViewFilter, type ViewFilterOp } from '@/core/utils/viewProfile';
import {
  resolveFieldFilterKind,
  FILTER_OPS_BY_KIND,
  FILTER_OP_LABELS,
  opNeedsValue,
  newFilterDraftRow,
  resetCondForField,
  draftToFilter,
  filterToDraftRows,
  type FilterDraftRow,
} from '@/core/utils/filterBuilder';
import { normalizeDataSource } from '@/core/utils/viewMapping';
import cubeApi from '@/api';
import LovSelect from '@/components/LovSelect.vue';

const props = defineProps<{
  /** 弹层是否可见（由父级管理，与分组弹层互斥） */
  visible: boolean;
  /** 筛选候选字段（当前视图可见字段） */
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

function condFieldOf(name: string): FieldMeta | undefined {
  return props.fields.find((f) => f.name === name);
}

/** 字段筛选类别（枚举/字符/人员/数字/日期） */
function kindOfName(name: string): ReturnType<typeof resolveFieldFilterKind> {
  const f = condFieldOf(name);
  return f ? resolveFieldFilterKind(f) : 'string';
}

/** 该行当前字段可用的操作符 */
function opsOf(row: FilterDraftRow): readonly ViewFilterOp[] {
  return row.cond.field ? FILTER_OPS_BY_KIND[kindOfName(row.cond.field)] : ['eq'];
}

/** 枚举/值集下拉选项（dataSource 物化；无则空） */
function enumOptionsOf(row: FilterDraftRow): { value: string; label: string }[] {
  const f = row.cond.field ? condFieldOf(row.cond.field) : undefined;
  if (f?.dataSource && Object.keys(f.dataSource).length) {
    return normalizeDataSource(f.dataSource).options;
  }
  return [];
}

/** 人员字段值控件需要用户实体下拉：懒加载 /Admin/User 前 500 条 */
const userOptions = ref<{ value: string; label: string }[]>([]);
const userLoading = ref(false);
async function ensureUserOptions() {
  if (userOptions.value.length || userLoading.value) return;
  userLoading.value = true;
  try {
    const res = await cubeApi.page.getList('/Admin/User', { pageIndex: 0, pageSize: 500 });
    const rows = (res.data as Record<string, unknown>[]) || [];
    userOptions.value = rows.map((u) => {
      const id = u.id ?? u.Id;
      const name = u.name ?? u.Name ?? u.account ?? u.Account;
      return { value: String(id), label: String(name ?? id) };
    });
  } catch {
    /* 忽略：下拉保持空 */
  } finally {
    userLoading.value = false;
  }
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
  // 字段切换：op 重置为该类别默认（eq）并清空值
  resetCondForField(row.cond, kindOfName(row.cond.field));
  // 人员字段首次使用时懒加载用户下拉
  if (row.cond.field && kindOfName(row.cond.field) === 'person') void ensureUserOptions();
}

function onOpChange(row: FilterDraftRow) {
  // 操作符切换后清空旧值（如 isNull ↔ eq 之间的值残留）
  row.cond.value = undefined;
  row.cond.value2 = undefined;
}

/** 值控件统一写回（各控件类型 emit 的 value 直接落到条件值） */
function onCondValue(row: FilterDraftRow, v: unknown) {
  row.cond.value = v;
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
