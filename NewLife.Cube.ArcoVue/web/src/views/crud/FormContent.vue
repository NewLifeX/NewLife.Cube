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
import { computed, ref } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import { isFullWidthControl, resolveControl } from '@/core/utils/fieldControl';
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
  }>(),
  { mode: 'edit' },
);

const formRef = ref();

const visibleFields = computed(() => {
  if (props.mode === 'add') {
    return props.fields.filter((f) => !f.primaryKey && !f.readOnly);
  }
  return props.fields;
});

const fieldGroups = computed(() => groupFieldsByCategory(visibleFields.value));

function rulesFor(field: FieldMeta) {
  if (!isFieldRequired(field)) return undefined;
  return [{ required: true, message: `${field.displayName || field.name}不可以为空！` }];
}

async function validate() {
  return formRef.value?.validate();
}

defineExpose({ validate });
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
