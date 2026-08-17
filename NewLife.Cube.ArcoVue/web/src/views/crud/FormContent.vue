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
          <icon-park type="down" class="form-group__caret" :class="{ open: !collapsedSet.has(group.category) }" />
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
import type { FieldMeta } from '@/core/types/field';
import type { FormLayout } from '@/core/utils/viewProfile';
import { useFormContent } from './useFormContent';
import FieldInput from '@/components/FieldInput.vue';

const props = withDefaults(
  defineProps<{
    fields: FieldMeta[];
    model: Record<string, unknown>;
    typePath: string;
    readonly?: boolean;
    /** 新增时隐藏主键/只读；编辑默认隐藏主键「编号」（对齐 Cube.Vue） */
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

const {
  formRef,
  visibleGroups,
  collapsedSet,
  isFullWidthControl,
  resolveControl,
  isFieldRequired,
  rulesFor,
  validate,
  clearValidate,
} = useFormContent(props);

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
