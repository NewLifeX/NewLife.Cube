<script setup lang="ts">
/**
 * Json 编辑器（vanilla-jsoneditor 轻量封装）
 *
 * v-model 绑定「Json 字符串」：外部传入 Json 文本，组件内解析为结构化内容编辑，
 * 变更后以格式化后的 Json 字符串回写（resolveControl 映射的 control = 'json'）。
 */
import { ref, onMounted, onBeforeUnmount, watch } from 'vue';
import type { Content, JSONContent } from 'vanilla-jsoneditor';
import { createJSONEditor } from 'vanilla-jsoneditor';

const props = withDefaults(
  defineProps<{
    modelValue?: string;
    disabled?: boolean;
    placeholder?: string;
  }>(),
  { modelValue: '', disabled: false, placeholder: '请输入 Json' },
);

const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>();

const container = ref<HTMLElement | null>(null);
let editor: ReturnType<typeof createJSONEditor> | null = null;

/** 将 Json 字符串解析为编辑器 Content */
function toContent(str: string): Content {
  if (!str || !str.trim()) return { text: '' };
  try {
    return { json: JSON.parse(str) };
  } catch {
    return { text: str };
  }
}

/** 将编辑器 Content 序列化回 Json 字符串 */
function serialize(content: JSONContent): string {
  if (content.json !== undefined) return JSON.stringify(content.json, null, 2);
  return '';
}

onMounted(() => {
  if (!container.value) return;
  editor = createJSONEditor({
    target: container.value,
    props: {
      content: toContent(props.modelValue),
      readOnly: props.disabled,
      onChange: (content: JSONContent) => {
        emit('update:modelValue', serialize(content));
      },
    },
  });
});

// 外部值变化时同步（避免与 onChange 回环）
watch(
  () => props.modelValue,
  (val) => {
    if (!editor) return;
    const current = editor.get();
    if (current && serialize(current as JSONContent) !== val) {
      editor.set(toContent(val));
    }
  },
);

watch(
  () => props.disabled,
  (disabled) => {
    editor?.updateProps({ readOnly: disabled });
  },
);

onBeforeUnmount(() => {
  editor?.destroy();
  editor = null;
});
</script>

<template>
  <div class="json-editor">
    <div ref="container" class="json-editor__body"></div>
    <p v-if="!modelValue" class="json-editor__placeholder">{{ placeholder }}</p>
  </div>
</template>

<style scoped>
.json-editor {
  width: 100%;
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  background: var(--el-bg-color);
  overflow: hidden;
}

.json-editor__body {
  min-height: 220px;
}

.json-editor__placeholder {
  margin: 0;
  padding: 8px 10px;
  font-size: 12px;
  color: var(--el-text-color-placeholder);
}
</style>
