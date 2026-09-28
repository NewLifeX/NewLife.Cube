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
        @click="rememberCursor"
        @keyup="rememberCursor"
      />
    </template>
    <template #actions>
      <div class="comment-editor-actions">
        <a-tag
          v-for="m in mentions"
          :key="m.id"
          size="small"
          closable
          @close="emit('remove-mention', m)"
        >
          @{{ m.name }}
        </a-tag>
        <a-popover
          trigger="click"
          position="top"
          @popup-visible-change="(v: boolean) => v && emit('search-mention', '')"
        >
          <template #content>
            <div class="mention-panel">
              <a-input
                :model-value="mentionKeyword"
                placeholder="搜索用户"
                size="small"
                allow-clear
                @input="(v: string) => emit('search-mention', String(v))"
              />
              <a-spin :loading="mentionLoading" style="width: 100%">
                <div v-if="!mentionUsers.length" class="mention-empty">无匹配用户</div>
                <button
                  v-for="u in mentionUsers"
                  :key="u.id"
                  type="button"
                  class="mention-item"
                  @click="emit('pick-mention', u, cursor)"
                >
                  {{ u.name }}
                </button>
              </a-spin>
            </div>
          </template>
          <a-button size="mini" title="提及用户">
            <icon-park type="people" />
          </a-button>
        </a-popover>
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
      </div>
    </template>
  </a-comment>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useUserStore } from '@/stores/user';
import type { MentionEntry, MentionUser } from '@/views/crud/commentMention';
import UserAvatar from './UserAvatar.vue';

/**
 * 内嵌回复编辑器
 *
 * 以 Arco 评论组件呈现，置于被回复的评论内部；头像取当前登录用户，
 * 无头像时回落为用户名首字符。提及选择：已选标签与 people 按钮在按钮行，
 * 状态由父级 useRecordDrawer 管理（与顶层评论分开，OSC-260926c2b8）。
 */
const props = withDefaults(
  defineProps<{
    /** 被回复的评论 */
    target: { replyUser?: string; createUser?: string } | null;
    /** 回复内容 */
    modelValue?: string;
    /** 提交中 */
    saving?: boolean;
    /** 已选提及（由父级维护，与顶层评论分开） */
    mentions?: MentionEntry[];
    /** 提及候选用户（父级搜索 /Admin/User 的结果） */
    mentionUsers?: MentionUser[];
    mentionLoading?: boolean;
    mentionKeyword?: string;
  }>(),
  {
    mentions: () => [],
    mentionUsers: () => [],
  },
);

const emit = defineEmits<{
  'update:modelValue': [string];
  submit: [];
  cancel: [];
  'search-mention': [keyword: string];
  'pick-mention': [user: MentionUser, cursor: number | null];
  'remove-mention': [entry: MentionEntry];
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

/** 回复输入框最近一次光标（点选提及用户时在光标处插入 `@显示名 `） */
const cursor = ref<number | null>(null);
function rememberCursor(e: Event) {
  const el = e.target as HTMLTextAreaElement | null;
  if (el && typeof el.selectionStart === 'number') cursor.value = el.selectionStart;
}
</script>

<style scoped>
.comment-editor {
  margin-top: 8px;
}
.comment-editor :deep(.arco-comment-inner-content) {
  min-width: 0;
}
.comment-editor-actions {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}
.mention-panel {
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 220px;
  max-height: 260px;
}
.mention-empty {
  padding: 8px 4px;
  font-size: 12px;
  color: var(--color-text-3);
}
.mention-item {
  display: block;
  width: 100%;
  border: none;
  background: none;
  border-radius: 3px;
  font-size: 12px;
  padding: 4px 8px;
  text-align: left;
  cursor: pointer;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.mention-item:hover {
  background: var(--color-fill-2);
}
</style>
