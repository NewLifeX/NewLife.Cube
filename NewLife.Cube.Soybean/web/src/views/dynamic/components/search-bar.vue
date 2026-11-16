<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { buildSearchControl, dictValueOf, type SearchControl } from '../shared';

defineOptions({ name: 'DynamicSearchBar' });

interface Props {
  controls: SearchControl[];
  loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), { loading: false });

const emit = defineEmits<{
  search: [params: Record<string, unknown>];
  reset: [];
}>();

// Element Plus 组件的 v-model 需要具体类型，模板里无法写断言，这里放宽为 any
const model = reactive<Record<string, any>>({});

/** 折叠时保留前 3 个搜索条件，其余条件展开后显示 */
const COLLAPSED_COUNT = 3;
const expanded = ref(false);
const collapsible = computed(() => props.controls.length > COLLAPSED_COUNT);
const visibleControls = computed(() => (expanded.value ? props.controls : props.controls.slice(0, COLLAPSED_COUNT)));

function resetModel() {
  Object.keys(model).forEach(key => {
    delete model[key];
  });
}

function toParams() {
  const params: Record<string, unknown> = {};

  props.controls.forEach(control => {
    const value = model[control.key];
    if (value === undefined || value === null || value === '') return;

    if (control.kind === 'range' && Array.isArray(value)) {
      const [start, end] = value as string[];
      if (start) params[control.key] = start;
      if (end && control.endKey) params[control.endKey] = end;
      return;
    }

    params[control.key] = value;
  });

  return params;
}

function handleSearch() {
  emit('search', toParams());
}

function handleReset() {
  resetModel();
  emit('reset');
}

watch(
  () => props.controls,
  () => resetModel()
);
</script>

<template>
  <ElForm class="dynamic-search" :model="model" @submit.prevent="handleSearch">
    <div class="dynamic-search__body" :class="{ 'is-collapsed': collapsible && !expanded }">
      <div v-for="control in visibleControls" :key="control.key" class="dynamic-search__item">
        <ElFormItem :label="control.field.displayName || control.field.name">
          <ElInput
            v-if="control.kind === 'keyword' || control.kind === 'text'"
            v-model="model[control.key]"
            :placeholder="control.kind === 'keyword' ? '关键字' : `请输入${control.field.displayName || control.field.name}`"
            clearable
            @keyup.enter="handleSearch"
          />
          <ElInputNumber
            v-else-if="control.kind === 'number'"
            v-model="model[control.key]"
            class="w-full"
            controls-position="right"
          />
          <ElDatePicker
            v-else-if="control.kind === 'range' || control.kind === 'date' || control.kind === 'datetime'"
            v-model="model[control.key]"
            :type="control.kind === 'range' ? 'datetimerange' : control.kind === 'date' ? 'date' : 'datetime'"
            class="w-full"
            :value-format="control.kind === 'date' ? 'YYYY-MM-DD' : 'YYYY-MM-DD HH:mm:ss'"
            start-placeholder="开始"
            end-placeholder="结束"
          />
          <ElSelect
            v-else-if="control.kind === 'select'"
            v-model="model[control.key]"
            class="w-full"
            clearable
            placeholder="全部"
          >
            <ElOption
              v-for="(label, key) in control.field.dataSource || { true: '是', false: '否' }"
              :key="key"
              :label="label"
              :value="dictValueOf(key)"
            />
          </ElSelect>
        </ElFormItem>
      </div>
      <div class="dynamic-search__actions">
        <ElFormItem label=" ">
          <ElButton type="primary" :loading="loading" @click="handleSearch">查询</ElButton>
          <ElButton @click="handleReset">重置</ElButton>
          <ElButton v-if="collapsible" text @click="expanded = !expanded">
            {{ expanded ? '收起' : '展开' }}
            <SvgIcon :icon="expanded ? 'mdi:chevron-up' : 'mdi:chevron-down'" class="ml-4px text-icon" />
          </ElButton>
        </ElFormItem>
      </div>
    </div>
  </ElForm>
</template>

<style scoped>
.dynamic-search {
  padding-top: 8px;
}

.dynamic-search :deep(.el-form-item) {
  margin-bottom: 12px;
}

.dynamic-search :deep(.el-form-item__label) {
  white-space: nowrap;
}

/* 折叠时 3 个条件 + 操作按钮共处一行；展开后条件按容器宽度自动换行 */
.dynamic-search__body {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  gap: 0 12px;
}

.dynamic-search__item {
  min-width: 0;
  flex: 1 1 220px;
}

/* 收缩态固定一行：3 个条件等分剩余宽度 */
.dynamic-search__body.is-collapsed {
  flex-wrap: nowrap;
}

.dynamic-search__body.is-collapsed .dynamic-search__item {
  flex: 1 1 0;
}

.dynamic-search__actions {
  flex: 0 0 auto;
  margin-left: auto;
}
</style>
