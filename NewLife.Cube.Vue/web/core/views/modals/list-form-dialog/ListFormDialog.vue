<script setup lang="ts">
/**
 * 列表页表单弹窗内容组件
 *
 * 封装 FormContent 用于命令式弹窗，不包含表单操作按钮（由弹窗自身 footer 提供）。
 * 接收父组件传入的字段元数据和表单数据，纯渲染表单内容。
 *
 * 支持 Section 覆盖机制：接受 routePath 参数，从全局 PageSectionRegistryKey
 * 获取 FormContent 等组件的覆盖注入，与 form.vue 行为一致（但弹窗通过 Teleport
 * 渲染到 body，不在列表页的组件树中，因此需要自行处理 provide）。
 */
import { inject, provide, defineAsyncComponent } from 'vue';
import type { Component } from 'vue';
import {
  FormContentKey,
  PageSectionRegistryKey,
  SectionKeyMap,
} from '@newlifex/cube-vue/core/composables/useSections';
import DefaultFormContent from '@newlifex/cube-vue/core/views/components/FormContent.vue';
import type { FieldMeta } from '@newlifex/cube-vue/core/types/field';

export interface ListFormDialogData {
  fields: FieldMeta[];
  modelValue: Record<string, unknown>;
  apiPrefix?: string;
  mode: 'add' | 'edit';
  /** 路由路径，用于 Section 覆盖机制查找对应覆盖组件 */
  routePath?: string;
  /** 栅格列数，默认 2 */
  columns?: number;
}

const props = defineProps<ListFormDialogData>();

const emit = defineEmits<{
  'update:modelValue': [val: Record<string, unknown>];
}>();

// ── Section 覆盖机制：与 form.vue 一致 ─────────────────────────
const registry = inject(
  PageSectionRegistryKey,
  {} as Record<string, Record<string, () => Promise<{ default: unknown }>>>,
);
if (props.routePath) {
  /** 将路由路径转为小写以匹配 Section 注册表键 */
  const normalizedPath = props.routePath.toLowerCase();
  const pageOverrides = registry[normalizedPath] ?? {};
  for (const [name, loader] of Object.entries(pageOverrides)) {
    const key = SectionKeyMap[name];
    if (key) {
      provide(key, defineAsyncComponent(loader as () => Promise<{ default: Component }>));
    }
  }
}

const FormContentComp = inject(FormContentKey, DefaultFormContent);
</script>

<template>
  <component
    :is="FormContentComp"
    :fields="fields"
    :model-value="modelValue"
    :api-prefix="apiPrefix"
    :columns="columns"
    @update:model-value="emit('update:modelValue', $event)"
  />
</template>
