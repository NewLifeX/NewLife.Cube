<template>
  <a-drawer
    :visible="visible"
    :width="420"
    :footer="false"
    unmount-on-close
    title="站内通知"
    placement="right"
    @update:visible="(v: boolean) => emit('update:visible', v)"
  >
    <div class="inbox-toolbar">
      <span class="inbox-toolbar__meta">共 {{ total }} 条</span>
      <a-button
        type="text"
        size="mini"
        :loading="marking"
        :disabled="!unreadCount"
        @click="markAllRead"
      >
        全部已读
      </a-button>
    </div>

    <a-spin :loading="loading" class="inbox-spin">
      <a-empty v-if="!items.length" description="暂无消息" />
      <a-timeline v-else class="inbox-tl">
        <a-timeline-item v-for="m in items" :key="m.id" :dot-color="m.read ? 'gray' : 'arcoblue'">
          <div
            class="inbox-item"
            :class="{ 'inbox-item--unread': !m.read }"
            @click="onItemClick(m)"
          >
            <div class="inbox-item__title">{{ m.title || '（无标题）' }}</div>
            <div class="inbox-item__body">
              <div class="inbox-item__time">{{ m.timeText }}</div>
              <div class="inbox-item__content">{{ m.content || '' }}</div>
            </div>
          </div>
        </a-timeline-item>
      </a-timeline>
    </a-spin>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useInboxDrawer, type InboxRow } from './useInboxDrawer';

const props = defineProps<{ visible: boolean }>();
const emit = defineEmits<{ 'update:visible': [boolean] }>();

const visibleRef = computed({
  get: () => props.visible,
  set: (v: boolean) => emit('update:visible', v),
});

const { loading, marking, items, unreadCount, total, markRead, markAllRead } = useInboxDrawer(
  visibleRef,
);

function onItemClick(m: InboxRow) {
  if (!m.read && m.id) void markRead(m.id);
}

defineExpose({ unreadCount });
</script>

<style scoped>
.inbox-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}
.inbox-toolbar__meta {
  font-size: var(--cube-font-size-meta);
  color: var(--color-text-3);
}
.inbox-spin {
  width: 100%;
  min-height: 120px;
}
.inbox-tl {
  padding-left: 4px;
}
.inbox-item {
  cursor: pointer;
  padding-bottom: 4px;
}
.inbox-item__title {
  font-size: var(--cube-font-size-title, var(--cube-font-size-body));
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
  margin-bottom: 4px;
}
.inbox-item--unread .inbox-item__title {
  color: rgb(var(--primary-6));
}
.inbox-item__body {
  position: relative;
  padding-left: 10px;
  border-left: 2px solid var(--color-border-2);
  font-size: var(--cube-font-size-body);
  color: var(--color-text-2);
}
.inbox-item__time {
  font-size: var(--cube-font-size-meta);
  color: var(--color-text-3);
  margin-bottom: 4px;
}
.inbox-item__content {
  white-space: pre-wrap;
  word-break: break-word;
  line-height: 1.5;
}
</style>
