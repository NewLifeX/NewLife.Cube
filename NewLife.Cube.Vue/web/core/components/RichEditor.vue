<script setup lang="ts">
/**
 * 富文本编辑器（轻量封装）
 *   - mode = 'html'     → wangEditor（@wangeditor/editor + @wangeditor/editor-for-vue）
 *   - mode = 'markdown' → md-editor-v3
 *
 * v-model 绑定「HTML 字符串 / Markdown 字符串」（resolveControl 映射的 control = 'richHtml' / 'richMarkdown'）。
 */
import { ref, shallowRef, onBeforeUnmount, watch, computed } from 'vue';
import { ElMessage } from 'element-plus';
import '@wangeditor/editor/dist/css/style.css';
// @ts-ignore
import { Editor, Toolbar } from '@wangeditor/editor-for-vue';
import type { IDomEditor, IEditorConfig, IToolbarConfig } from '@wangeditor/editor';
import { MdEditor } from 'md-editor-v3';
import 'md-editor-v3/lib/style.css';

const props = withDefaults(
  defineProps<{
    modelValue?: string;
    mode?: 'html' | 'markdown';
    placeholder?: string;
    disabled?: boolean;
  }>(),
  { modelValue: '', mode: 'html', placeholder: '请输入内容', disabled: false },
);

const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>();

const isHtml = computed(() => props.mode === 'html');

// ── wangEditor ──
const editorRef = shallowRef<IDomEditor | null>(null);
const toolbarConfig: Partial<IToolbarConfig> = {};
const editorConfig: Partial<IEditorConfig> = {
  placeholder: props.placeholder,
  readOnly: props.disabled,
};

function handleCreated(editor: IDomEditor) {
  editorRef.value = editor;
}

function handleChange(editor: IDomEditor) {
  emit('update:modelValue', editor.getHtml());
}

// ── markdown ──
function handleMdChange(val: string) {
  emit('update:modelValue', val);
}

watch(
  () => props.disabled,
  (disabled) => {
    if (editorRef.value) {
      if (disabled) {
        editorRef.value.disable();
      } else {
        editorRef.value.enable();
      }
    }
  },
);

onBeforeUnmount(() => {
  editorRef.value?.destroy();
  editorRef.value = null;
});
</script>

<template>
  <div class="rich-editor">
    <!-- HTML 富文本 -->
    <template v-if="isHtml">
      <div class="rich-editor__wangeditor">
        <Toolbar
          :editor="editorRef"
          :default-config="toolbarConfig"
          :mode="'default'"
          class="rich-editor__toolbar"
        />
        <Editor
          :default-config="editorConfig"
          :mode="'default'"
          :model-value="modelValue"
          @on-created="handleCreated"
          @on-change="handleChange"
          class="rich-editor__body"
        />
      </div>
    </template>

    <!-- Markdown 富文本 -->
    <template v-else>
      <MdEditor
        :model-value="modelValue"
        :disabled="disabled"
        :placeholder="placeholder"
        @update:model-value="handleMdChange"
        class="rich-editor__md"
      />
    </template>
  </div>
</template>

<style scoped>
.rich-editor {
  width: 100%;
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  overflow: hidden;
  background: var(--el-bg-color);
}

.rich-editor__wangeditor {
  border: none;
}

.rich-editor__toolbar {
  border-bottom: 1px solid var(--el-border-color-light);
}

.rich-editor__body {
  height: 320px;
  overflow-y: auto;
}

.rich-editor__md {
  height: 360px;
}
</style>
