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
      <a-empty v-if="!groups.length" description="暂无消息" />
      <a-timeline v-else class="inbox-tl">
        <a-timeline-item v-for="g in groups" :key="g.key">
          <template #dot>
            <span class="inbox-group-dot">
              <icon-park :type="INBOX_BUCKET_ICONS[g.key]" />
            </span>
          </template>
          <div class="inbox-group">
            <div class="inbox-group__title">{{ g.label }}</div>
            <a-timeline class="inbox-sub-tl">
              <a-timeline-item
                v-for="m in g.items"
                :key="m.id"
                :dot-color="m.read ? 'gray' : 'arcoblue'"
              >
                <div
                  class="inbox-item"
                  :class="{ 'inbox-item--unread': !m.read }"
                  @click="onItemClick(m)"
                >
                  <div class="inbox-item__title">{{ m.title || '（无标题）' }}</div>
                  <div class="inbox-item__time">{{ m.timeText }}</div>
                  <div class="inbox-item__content">{{ m.content || '' }}</div>
                </div>
              </a-timeline-item>
            </a-timeline>
          </div>
        </a-timeline-item>
      </a-timeline>
    </a-spin>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { INBOX_BUCKET_ICONS } from '@/core/utils/iconRegistry';
import { useInboxDrawer } from './useInboxDrawer';

const props = defineProps<{ visible: boolean }>();
const emit = defineEmits<{ 'update:visible': [boolean] }>();

const visibleRef = computed({
  get: () => props.visible,
  set: (v: boolean) => emit('update:visible', v),
});

const { loading, marking, groups, unreadCount, total, markAllRead, onItemClick } = useInboxDrawer(
  visibleRef,
);

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
.inbox-group-dot {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  line-height: 1;
  color: rgb(var(--primary-6));
}
.inbox-sub-tl {
  margin-top: 6px;
}
.inbox-sub-tl :deep(.arco-timeline-item:last-child) {
  padding-bottom: 0;
}
.inbox-group__title {
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
  margin-bottom: 8px;
}
.inbox-item {
  cursor: pointer;
  margin-bottom: 12px;
  font-size: var(--cube-font-size-body);
  color: var(--color-text-2);
}
.inbox-group .inbox-item:last-child {
  margin-bottom: 0;
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
