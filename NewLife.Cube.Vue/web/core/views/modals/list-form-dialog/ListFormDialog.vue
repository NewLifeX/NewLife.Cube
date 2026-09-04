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
import { inject, provide, defineAsyncComponent, computed } from 'vue';
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
  /** add=新增 / edit=编辑 / view=查看（只读） */
  mode: 'add' | 'edit' | 'view';
  /** 路由路径，用于 Section 覆盖机制查找对应覆盖组件 */
  routePath?: string;
  /** 栅格列数，默认 2 */
  columns?: number;
  /** 详情加载中（edit / view 模式下由 openListFormDialog 拉详情时置 true） */
  loading?: boolean;
}

const props = defineProps<ListFormDialogData>();

const emit = defineEmits<{
  'update:modelValue': [val: Record<string, unknown>];
}>();

/** 查看模式下表单整体只读 */
const readonly = computed(() => props.mode === 'view');

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
  <!-- 详情加载中先出骨架，避免「先渲染空表单再回填」造成的闪烁与误输入 -->
  <el-skeleton v-if="loading" :rows="Math.min(fields.length || 4, 8)" animated />
  <component
    v-else
    :is="FormContentComp"
    :fields="fields"
    :model-value="modelValue"
    :api-prefix="apiPrefix"
    :columns="columns"
    :disabled="readonly"
    @update:model-value="emit('update:modelValue', $event)"
  />
</template>
