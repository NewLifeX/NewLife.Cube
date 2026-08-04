<template>
  <a-form ref="formRef" :model="model" layout="vertical" :disabled="readonly">
    <div class="form-groups">
      <section v-for="group in fieldGroups" :key="group.category || '__default'" class="form-group">
        <div v-if="group.title" class="form-group__title">{{ group.title }}</div>
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
      </section>
    </div>
  </a-form>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import { isAuditField, isFullWidthControl, resolveControl } from '@/core/utils/fieldControl';
import { groupFieldsByCategory } from '@/core/utils/fieldGroups';
import { isFieldRequired } from '@/core/utils/submitPayload';
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
  }>(),
  { mode: 'edit', fieldErrors: () => [] },
);

const formRef = ref();

const visibleFields = computed(() => {
  // 新增/编辑均隐藏审计字段（创建/更新用户、IP、时间），由后端自动维护
  const withoutAudit = props.fields.filter((f) => !isAuditField(f));
  if (props.mode === 'add') {
    return withoutAudit.filter((f) => !f.primaryKey && !f.readOnly);
  }
  return withoutAudit;
});

const fieldGroups = computed(() => groupFieldsByCategory(visibleFields.value));

function rulesFor(field: FieldMeta) {
  if (!isFieldRequired(field)) return undefined;
  return [{ required: true, message: `${field.displayName || field.name}不可以为空！` }];
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
</style>
