<template>
  <a-comment class="comment-editor">
    <template #avatar>
      <UserAvatar :name="userName" :avatar="userAvatar" />
    </template>
    <template #content>
      <a-textarea
        v-model="text"
        :placeholder="placeholder"
        :max-length="500"
        allow-clear
        auto-size
      />
    </template>
    <template #actions>
      <a-space>
        <a-button size="mini" @click="emit('cancel')">取消</a-button>
        <a-button
          size="mini"
          type="primary"
          :disabled="!text.trim()"
          :loading="saving"
          @click="emit('submit')"
        >
          回复
        </a-button>
      </a-space>
    </template>
  </a-comment>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useUserStore } from '@/stores/user';
import UserAvatar from './UserAvatar.vue';

/**
 * 内嵌回复编辑器
 *
 * 以 Arco 评论组件呈现，置于被回复的评论内部；头像取当前登录用户，
 * 无头像时回落为用户名首字符。
 */
const props = defineProps<{
  /** 被回复的评论 */
  target: { replyUser?: string; createUser?: string } | null;
  /** 回复内容 */
  modelValue?: string;
  /** 提交中 */
  saving?: boolean;
}>();

const emit = defineEmits<{
  'update:modelValue': [string];
  submit: [];
  cancel: [];
}>();

const userStore = useUserStore();
const userName = computed(() => userStore.displayName || '我');
const userAvatar = computed(() => userStore.userInfo?.avatar ?? '');

const text = computed({
  get: () => props.modelValue ?? '',
  set: (v: string) => emit('update:modelValue', v),
});

const placeholder = computed(() => {
  const name = props.target?.replyUser || props.target?.createUser;
  return name ? `回复 ${name}` : '写下你的回复…';
});
</script>

<style scoped>
.comment-editor {
  margin-top: 8px;
}
.comment-editor :deep(.arco-comment-inner-content) {
  min-width: 0;
}
</style>
