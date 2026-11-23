<script setup lang="ts">
/**
 * NotificationBell - 通知按钮组件
 *
 * 铃铛按钮 + 未读角标 + 下拉消息面板。点击消息标记已读并跳转业务页面，
 * 实时连接由 notification store 统一维护（SSE 优先，轮询兜底）。
 */
import { computed, onMounted, onUnmounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import ActionButton from './ActionButton.vue';
import { useNotificationStore, type NotificationItem } from '../stores/notification';

interface Props {
  /** 是否显示文字标签 */
  showLabel?: boolean;
  /** 按钮文本 */
  label?: string;
  /** 自定义点击回调，不传则使用默认逻辑 */
  onClick?: () => void;
}

const props = withDefaults(defineProps<Props>(), {
  showLabel: false,
  label: undefined,
});

const emit = defineEmits<{
  click: [];
}>();
const { t } = useI18n();
const router = useRouter();
const store = useNotificationStore();

const bellLabel = computed(() => props.label || t('topBar.notifications'));

function handleClick() {
  if (props.onClick) {
    props.onClick();
  } else {
    emit('click');
  }
}

function onShow() {
  store.refreshUnread();
  store.loadRecent();
  store.requestSystemPermission();
}

async function onOpenItem(item: NotificationItem) {
  try {
    if (!item.read) await store.markRead(item);
  } catch {
    // 请求层已提示错误，这里继续跳转
  }

  if (!item.link) return;
  if (/^https?:\/\//i.test(item.link)) {
    window.location.assign(item.link);
  } else {
    router.push(item.link).catch(() => {});
  }
}

async function onMarkAll() {
  try {
    const count = await store.markAllRead();
    ElMessage.success(t('notify.markedAll', { count }));
  } catch {
    // 请求层已提示错误
  }
}

function onViewAll() {
  router.push('/notifications').catch(() => {});
}

function levelClass(level?: string) {
  switch ((level || '').toLowerCase()) {
    case 'warning':
      return 'level-warning';
    case 'error':
    case 'urgent':
      return 'level-error';
    case 'success':
      return 'level-success';
    default:
      return 'level-info';
  }
}

function formatTime(value?: string) {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;

  return date.toLocaleString();
}

onMounted(() => {
  store.start();
});

onUnmounted(() => {
  store.stop();
});
</script>

<template>
  <el-popover placement="bottom-end" :width="380" trigger="click" @show="onShow">
    <template #reference>
      <el-badge :value="store.unread" :hidden="!store.hasUnread" :max="99" class="notify-badge">
        <ActionButton :title="t('topBar.notifications')" @click="handleClick">
          <svg
            width="15"
            height="15"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
          >
            <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9" />
            <path d="M13.73 21a2 2 0 01-3.46 0" />
          </svg>
          <span v-if="showLabel" class="bell-label">{{ bellLabel }}</span>
        </ActionButton>
      </el-badge>
    </template>

    <div class="notify-panel">
      <div class="notify-head">
        <strong>{{ t('notify.title') }}</strong>
        <el-button link size="small" :disabled="!store.hasUnread" @click="onMarkAll">
          {{ t('notify.markAll') }}
        </el-button>
      </div>

      <el-scrollbar max-height="360px">
        <el-empty v-if="!store.recent.length" :description="t('notify.empty')" :image-size="72" />
        <div
          v-for="item in store.recent"
          :key="item.id"
          class="notify-item"
          :class="{ unread: !item.read }"
          @click="onOpenItem(item)"
        >
          <div class="notify-item-title">
            <span class="notify-dot" :class="levelClass(item.level)" />
            <span class="notify-item-text">{{ item.title }}</span>
            <el-tag v-if="item.shared" size="small" type="info">{{ t('notify.shared') }}</el-tag>
          </div>
          <div class="notify-item-content">{{ item.content }}</div>
          <div class="notify-item-time">{{ formatTime(item.createTime) }}</div>
        </div>
      </el-scrollbar>

      <div class="notify-foot">
        <el-button link type="primary" @click="onViewAll">{{ t('notify.viewAll') }}</el-button>
        <span class="notify-status">
          {{ store.realtime ? t('notify.realtime') : t('notify.polling') }}
        </span>
      </div>
    </div>
  </el-popover>
</template>

<style lang="scss" scoped>
.bell-label {
  margin-left: 6px;
  font-size: 13px;
  white-space: nowrap;
}

.notify-badge {
  :deep(.el-badge__content) {
    border: none;
  }
}

.notify-panel {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.notify-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 2px 4px 8px;
  border-bottom: 1px solid var(--el-border-color-light);
}

.notify-item {
  padding: 10px 8px;
  border-radius: 6px;
  cursor: pointer;
  transition: background-color 0.15s;

  &:hover {
    background: var(--el-fill-color-light);
  }

  &.unread {
    background: var(--el-color-primary-light-9);
  }
}

.notify-item-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.notify-item-text {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.notify-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  flex-shrink: 0;

  &.level-info {
    background: var(--el-color-primary);
  }

  &.level-success {
    background: var(--el-color-success);
  }

  &.level-warning {
    background: var(--el-color-warning);
  }

  &.level-error {
    background: var(--el-color-danger);
  }
}

.notify-item-content {
  margin-top: 4px;
  font-size: 12px;
  line-height: 1.5;
  color: var(--el-text-color-secondary);
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.notify-item-time {
  margin-top: 4px;
  font-size: 11px;
  color: var(--el-text-color-placeholder);
}

.notify-foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 8px;
  border-top: 1px solid var(--el-border-color-light);
}

.notify-status {
  font-size: 11px;
  color: var(--el-text-color-placeholder);
}
</style>