<template>
  <a-form ref="formRef" :model="model" layout="vertical" :disabled="readonly">
    <div class="form-groups">
      <section
        v-for="group in visibleGroups"
        :key="group.category || '__default'"
        class="form-group"
      >
        <button
          v-if="group.title"
          type="button"
          class="form-group__title form-group__collapse"
          :aria-expanded="!collapsedSet.has(group.category)"
          @click="$emit('toggle-collapse', group.category)"
        >
          <span>{{ group.title }}</span>
          <icon-down class="form-group__caret" :class="{ open: !collapsedSet.has(group.category) }" />
        </button>
        <div v-show="!collapsedSet.has(group.category)" class="form-group__body">
          <a-row :gutter="16">
            <a-col
              v-for="field in group.fields"
              :key="field.name"
              :span="isFullWidthControl(resolveControl(field)) ? 24 : 12"
            >
              <a-form-item
                :field="field.name"
                :label="field.displayName || field.name"
                :required="isFieldRequired(field)"
                :rules="rulesFor(field)"
              >
                <FieldInput
                  :field="field"
                  :model-value="model[field.name]"
                  :disabled="readonly || !!field.readOnly || !!field.primaryKey"
                  :type-path="typePath"
                  @update:model-value="(v) => (model[field.name] = v)"
                />
              </a-form-item>
            </a-col>
          </a-row>
        </div>
      </section>
    </div>
  </a-form>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { IconDown } from '@arco-design/web-vue/es/icon';
import type { FieldMeta } from '@/core/types/field';
import { isAuditField, isFullWidthControl, resolveControl } from '@/core/utils/fieldControl';
import { applyFormLayout, groupFieldsByCategory } from '@/core/utils/fieldGroups';
import type { FormLayout } from '@/core/utils/viewProfile';
import { isFieldRequired } from '@/core/utils/submitPayload';
import { fieldFormatRules } from '@/core/utils/validation';
import FieldInput from '@/components/FieldInput.vue';

const props = withDefaults(
  defineProps<{
    fields: FieldMeta[];
    model: Record<string, unknown>;
    typePath: string;
    readonly?: boolean;
    /** 新增时隐藏主键/只读（对齐 Cube.Vue） */
    mode?: 'add' | 'edit' | 'detail';
    /** 后端字段级错误（FieldErrors），映射到对应 a-form-item（OSC-0009） */
    fieldErrors?: { field: string; message: string }[];
    /** 受限表单布局（OSC-0013）：字段排序/显隐/Category 折叠；null 表示元数据原序 */
    layout?: FormLayout | null;
  }>(),
  { mode: 'edit', fieldErrors: () => [], layout: null },
);

defineEmits<{
  'toggle-collapse': [category: string];
}>();

const formRef = ref();

const visibleFields = computed(() => {
  // 新增/编辑均隐藏审计字段（创建/更新用户、IP、时间），由后端自动维护
  const withoutAudit = props.fields.filter((f) => !isAuditField(f));
  if (props.mode === 'add') {
    return withoutAudit.filter((f) => !f.primaryKey && !f.readOnly);
  }
  return withoutAudit;
});

/** 应用受限布局：hidden 过滤 + order 排序 + Category 折叠（OSC-0013） */
const appliedGroups = computed(() =>
  applyFormLayout(groupFieldsByCategory(visibleFields.value), props.layout),
);
const visibleGroups = computed(() => appliedGroups.value.groups);
const collapsed = computed(() => appliedGroups.value.collapsed);
const collapsedSet = computed(() => new Set(collapsed.value));

function rulesFor(field: FieldMeta) {
  const rules = [...fieldFormatRules(field)];
  if (isFieldRequired(field)) {
    rules.unshift({ required: true, message: `${field.displayName || field.name}不可以为空！` });
  }
  return rules.length ? rules : undefined;
}

/** 后端 FieldErrors 的 field 名（PascalCase）与表单字段名做大小写不敏感匹配后写入 Arco Form 字段错误 */
function applyFieldErrors() {
  const errs = props.fieldErrors;
  if (!errs?.length || !formRef.value) return;
  const names = new Set(visibleFields.value.map((f) => f.name));
  const fields: Record<string, { status: 'error'; message: string }> = {};
  for (const e of errs) {
    const key = names.has(e.field)
      ? e.field
      : visibleFields.value.find((f) => f.name.toLowerCase() === e.field.toLowerCase())?.name;
    if (key && !fields[key]) fields[key] = { status: 'error', message: e.message };
  }
  if (Object.keys(fields).length) formRef.value.setFields(fields);
}

watch(() => props.fieldErrors, applyFieldErrors);

async function validate() {
  return formRef.value?.validate();
}

function clearValidate() {
  formRef.value?.clearValidate();
}

defineExpose({ validate, clearValidate });
</script>

<style scoped>
.form-groups {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.form-group {
  padding: 16px 16px 4px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
}
.form-group__title {
  margin: 0 0 12px;
  font-size: 14px;
  font-weight: 600;
  line-height: 22px;
  color: var(--color-text-1);
}
/* 分组标题可点击折叠（OSC-0013）；display:flex 去除 button 默认内边距 */
.form-group__collapse {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  border: none;
  background: transparent;
  padding: 0;
  cursor: pointer;
  text-align: left;
  font: inherit;
  color: inherit;
}
.form-group__caret {
  color: var(--color-text-3);
  transition: transform 0.2s;
  font-size: 12px;
}
.form-group__caret.open {
  transform: rotate(180deg);
}
.form-group__body {
  min-width: 0;
}
</style>
