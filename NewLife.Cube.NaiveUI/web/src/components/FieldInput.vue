<template>
  <!-- 根据 widget 类型渲染不同的 NaiveUI 表单控件 -->
  <component :is="renderWidget" />
</template>

<script setup lang="ts">
import { h, computed, type VNode } from 'vue';
import {
  NInput,
  NInputNumber,
  NSelect,
  NSwitch,
  NDatePicker,
  NUpload,
  NButton,
} from 'naive-ui';
import type { FieldMapping } from '@cube/field-mapping';

const props = defineProps<{
  value: any;
  mapping: FieldMapping;
}>();

const emit = defineEmits<{
  (e: 'update:value', val: any): void;
}>();

/** 构建下拉选项 */
function buildSelectOptions(ds?: Record<string, string>) {
  if (!ds) return [];
  return Object.entries(ds).map(([value, label]) => ({ label, value }));
}

const renderWidget = computed<VNode>(() => {
  const { widget, field } = props.mapping;
  const val = props.value;
  const update = (v: any) => emit('update:value', v);

  switch (widget) {
    case 'number':
      return h(NInputNumber, {
        value: val,
        onUpdateValue: update,
        precision: (props.mapping.props?.precision as number) ?? undefined,
        style: 'width: 100%',
      });

    case 'switch':
      return h(NSwitch, {
        value: !!val,
        onUpdateValue: update,
      });

    case 'select':
      return h(NSelect, {
        value: val,
        onUpdateValue: update,
        options: buildSelectOptions(field.dataSource),
        clearable: true,
        style: 'width: 100%',
      });

    case 'date':
      return h(NDatePicker, {
        value: val ? new Date(val).getTime() : null,
        onUpdateValue: (ts: number | null) => update(ts ? new Date(ts).toISOString().split('T')[0] : null),
        type: 'date',
        clearable: true,
        style: 'width: 100%',
      });

    case 'datetime':
      return h(NDatePicker, {
        value: val ? new Date(val).getTime() : null,
        onUpdateValue: (ts: number | null) => update(ts ? new Date(ts).toISOString() : null),
        type: 'datetime',
        clearable: true,
        style: 'width: 100%',
      });

    case 'textarea':
      return h(NInput, {
        value: val ?? '',
        onUpdateValue: update,
        type: 'textarea',
        autosize: { minRows: 3, maxRows: 8 },
      });

    case 'password':
      return h(NInput, {
        value: val ?? '',
        onUpdateValue: update,
        type: 'password',
        showPasswordOn: 'click',
      });

    case 'readonly':
      return h('span', { class: 'field-readonly' }, String(val ?? ''));

    case 'image':
      return h('div', { class: 'field-image' }, [
        val ? h('img', { src: val, style: 'max-width: 120px; max-height: 120px; border-radius: 4px' }) : null,
        h(NUpload, {
          showFileList: false,
          action: '/Admin/File/UploadLayui',
          onFinish: ({ event }: any) => {
            try {
              const res = JSON.parse((event?.target as XMLHttpRequest)?.response);
              if (res?.data?.url) update(res.data.url);
            } catch { /* ignore */ }
          },
        }, { default: () => h(NButton, { size: 'small' }, { default: () => '上传图片' }) }),
      ]);

    case 'file':
      return h(NUpload, {
        showFileList: true,
        action: '/Admin/File/UploadLayui',
        max: 1,
        onFinish: ({ event }: any) => {
          try {
            const res = JSON.parse((event?.target as XMLHttpRequest)?.response);
            if (res?.data?.url) update(res.data.url);
          } catch { /* ignore */ }
        },
      }, { default: () => h(NButton, null, { default: () => '选择文件' }) });

    case 'email':
      return h(NInput, {
        value: val ?? '',
        onUpdateValue: update,
        placeholder: '请输入邮箱',
        inputProps: { type: 'email' },
      });

    case 'phone':
      return h(NInput, {
        value: val ?? '',
        onUpdateValue: update,
        placeholder: '请输入手机号',
      });

    case 'url':
      return h(NInput, {
        value: val ?? '',
        onUpdateValue: update,
        placeholder: '请输入 URL',
      });

    case 'link':
      return h('a', {
        href: val ?? '#',
        target: (props.mapping.props?.target as string) ?? '_blank',
      }, String(val ?? ''));

    case 'color':
      return h('input', {
        type: 'color',
        value: val ?? '#000000',
        onInput: (e: Event) => update((e.target as HTMLInputElement).value),
        style: 'width: 48px; height: 32px; border: none; cursor: pointer;',
      });

    // text, code, html, checkbox 等默认使用文本输入
    default:
      return h(NInput, {
        value: val ?? '',
        onUpdateValue: update,
        placeholder: `请输入${field.displayName ?? field.name}`,
        maxlength: field.length ?? undefined,
        showCount: (field.length ?? 0) > 0,
      });
  }
});
</script>

<style scoped>
.field-readonly {
  color: rgba(0, 0, 0, 0.65);
}
.field-image {
  display: flex;
  align-items: flex-end;
  gap: 8px;
}
</style>
