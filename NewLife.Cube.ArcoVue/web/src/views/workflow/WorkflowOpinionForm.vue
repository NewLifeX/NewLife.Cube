<script setup lang="ts">
/**
 * 行内意见表单（共同内容，design §4.2）：常用语 + 意见 + 附件 + 提交按钮。
 * 展示组件：状态由 useWorkflowTaskPage 持有；宽屏 popover 与窄屏全宽 modal 复用同一内容。
 * 附件只先记在本地，点同意/驳回时再由待办页上传。
 */
import { computed, ref } from 'vue';
import { filesFromUploadChange } from './useSubmitApproval';

const props = withDefaults(
  defineProps<{
    kind: 'approve' | 'reject' | 'handle' | 'read' | 'withdraw';
    text: string;
    files: File[];
    phrases?: string[];
    saving?: boolean;
    /** 撤回只填意见，不带附件 */
    allowFiles?: boolean;
  }>(),
  { phrases: () => [], saving: false, allowFiles: true },
);

const emit = defineEmits<{
  (e: 'update:text', v: string): void;
  (e: 'update:files', files: File[]): void;
  (e: 'submit'): void;
}>();

const title = computed(() => {
  if (props.kind === 'reject') return '驳回';
  if (props.kind === 'handle') return '已办理';
  if (props.kind === 'read') return '已阅';
  if (props.kind === 'withdraw') return '撤回';
  return '同意';
});
const placeholder = computed(() =>
  props.kind === 'reject' ? '驳回意见（必填）' : '意见（可选）',
);
const disabled = computed(() => props.saving || (props.kind === 'reject' && !props.text.trim()));

/** 常用语下拉挂在表单内，点击选项仍算气泡内部 */
const box = ref<HTMLElement>();

const uploadList = computed(() =>
  props.files.map((file, i) => ({
    uid: `${i}-${file.name}-${file.size}-${file.lastModified}`,
    name: file.name,
    file,
    status: 'done' as const,
  })),
);

function onFiles(fileList: unknown, current?: unknown) {
  emit('update:files', filesFromUploadChange(fileList, current));
}

function removeFile(index: number) {
  emit(
    'update:files',
    props.files.filter((_, i) => i !== index),
  );
}

/** 拦住组件默认 action（当前页地址），避免文件行上的重试请求失败 */
function holdLocal(option: { onSuccess?: (res?: unknown) => void }) {
  option.onSuccess?.({});
}
</script>

<template>
  <div ref="box" class="wf-opinion" @mousedown.stop>
    <a-textarea
      :model-value="text"
      :max-length="500"
      :placeholder="placeholder"
      allow-clear
      @update:model-value="(v: string) => emit('update:text', v)"
    />
    <div v-if="allowFiles" class="wf-opinion__tools">
      <a-upload
        :file-list="uploadList"
        :auto-upload="false"
        :show-file-list="false"
        :show-retry-button="false"
        :custom-request="holdLocal"
        :limit="5"
        multiple
        @change="onFiles"
      >
        <template #upload-button>
          <a-button type="outline" size="small">
            <icon-park type="upload" />
            点击上传
          </a-button>
        </template>
      </a-upload>
      <a-dropdown v-if="phrases.length" trigger="click" position="bl" :popup-container="box">
        <a-button type="outline" size="small">
          <icon-park type="comments" />
          常用语
        </a-button>
        <template #content>
          <a-doption v-for="p in phrases" :key="p" @click="emit('update:text', p)">{{ p }}</a-doption>
        </template>
      </a-dropdown>
    </div>
    <ul v-if="allowFiles && files.length" class="wf-opinion__files">
      <li v-for="(f, i) in files" :key="`${f.name}-${f.size}-${f.lastModified}`">
        <span class="wf-opinion__file-name">{{ f.name }}</span>
        <a-button type="text" size="mini" @click="removeFile(i)">
          <icon-park type="close" />
        </a-button>
      </li>
    </ul>
    <div class="wf-opinion__foot">
      <a-button
        type="primary"
        size="small"
        :disabled="disabled"
        :loading="saving"
        @click="emit('submit')"
      >
        {{ title }}
      </a-button>
    </div>
  </div>
</template>

<style scoped>
.wf-opinion {
  display: flex;
  flex-direction: column;
  gap: 8px;
  width: 100%;
}
.wf-opinion__files {
  margin: 0;
  padding: 0;
  list-style: none;
}
.wf-opinion__files li {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;
}
.wf-opinion__file-name {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.wf-opinion__tools {
  display: flex;
  align-items: flex-start;
  gap: 8px;
}
.wf-opinion__tools :deep(.arco-upload) {
  width: auto;
}
.wf-opinion__foot {
  display: flex;
  justify-content: flex-end;
}
</style>
