<template>
  <a-form ref="formRef" :model="model" layout="vertical" :disabled="readonly">
    <a-row :gutter="16">
      <a-col
        v-for="field in visibleFields"
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
  </a-form>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import { isFullWidthControl, resolveControl } from '@/core/utils/fieldControl';
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
  // 新增：对齐 Cube.Vue，不展示主键与只读列
  if (props.mode === 'add') {
    return props.fields.filter((f) => !f.primaryKey && !f.readOnly);
  }
  return props.fields;
});

function rulesFor(field: FieldMeta) {
  if (!isFieldRequired(field)) return undefined;
  return [{ required: true, message: `${field.displayName || field.name}不可以为空！` }];
}

async function validate() {
  return formRef.value?.validate();
}

defineExpose({ validate });
</script>
