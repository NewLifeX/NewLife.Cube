<script setup lang="ts">
/**
 * 行「更多」内容（design §4.2）：转交 / 加签（前/后）/ 回退到已办节点 + 选人 + 附言。
 * 展示组件：宽屏第二个 popover 与窄屏全宽 modal 复用；状态由 useWorkflowTaskPage 持有。
 */
import { ref } from 'vue';
import type { RecipientKind } from './recipient';
import WorkflowRecipientPicker from './WorkflowRecipientPicker.vue';

const mode = defineModel<'transfer' | 'addSign' | 'rollback' | 'cc'>('mode', { required: true });
const before = defineModel<boolean>('before', { required: true });
const text = defineModel<string>('text', { required: true });
const recipients = defineModel<number[]>('recipients', { required: true });
const recipientKind = defineModel<RecipientKind>('recipientKind', { required: true });
const rollbackNode = defineModel<string>('rollbackNode', { required: true });

const props = withDefaults(
  defineProps<{
    options: { nodeId: string; name: string }[];
    canSubmit: boolean;
    saving: boolean;
    allowTransfer?: boolean;
    allowAddSign?: boolean;
    allowRollback?: boolean;
    allowCc?: boolean;
  }>(),
  { allowTransfer: true, allowAddSign: true, allowRollback: true, allowCc: false },
);

const emit = defineEmits<{ (e: 'submit'): void }>();

/** 下拉挂在弹层内，点选项不会被当成点到弹层外 */
const box = ref<HTMLElement>();
</script>

<template>
  <div ref="box" class="wf-more" @mousedown.stop>
    <a-radio-group v-model="mode" type="button" size="mini">
      <a-radio v-if="allowTransfer" value="transfer">转交</a-radio>
      <a-radio v-if="allowAddSign" value="addSign">加签</a-radio>
      <a-radio v-if="allowCc" value="cc">知会</a-radio>
      <a-radio v-if="allowRollback" value="rollback">回退</a-radio>
    </a-radio-group>

    <div v-if="mode === 'addSign'" class="wf-more__row">
      <span class="wf-more__label">位置</span>
      <a-radio-group v-model="before" type="button" size="mini">
        <a-radio :value="false">
          <a-tooltip content="我办完再给对方" :popup-container="box">
            <span>后加签</span>
          </a-tooltip>
        </a-radio>
        <a-radio :value="true">
          <a-tooltip content="先给对方，再回到我" :popup-container="box">
            <span>前加签</span>
          </a-tooltip>
        </a-radio>
      </a-radio-group>
    </div>

    <a-select
      v-if="mode === 'rollback'"
      v-model="rollbackNode"
      :options="options.map((t) => ({ value: t.nodeId, label: t.name }))"
      placeholder="回退到已办节点"
      size="small"
      allow-clear
      style="width: 100%"
      :popup-container="box"
    />
    <WorkflowRecipientPicker
      v-else
      v-model:kind="recipientKind"
      v-model:model-value="recipients"
      :multiple="mode !== 'transfer'"
      :placeholder="mode === 'transfer' ? '选择转交人' : mode === 'cc' ? '选择知会人' : '选择加签人'"
      :popup-container="box"
    />

    <a-textarea v-model="text" :max-length="500" placeholder="附言（可选）" allow-clear />

    <div class="wf-more__foot">
      <a-button
        type="primary"
        size="small"
        :disabled="!props.canSubmit"
        :loading="props.saving"
        @click="emit('submit')"
      >
        提交
      </a-button>
    </div>
  </div>
</template>

<style scoped>
.wf-more {
  display: flex;
  flex-direction: column;
  gap: 8px;
  width: 100%;
}
.wf-more__row {
  display: flex;
  align-items: center;
  gap: 8px;
}
.wf-more__label {
  color: var(--color-text-3);
  font-size: 12px;
}
.wf-more__foot {
  display: flex;
  justify-content: flex-end;
}
</style>
