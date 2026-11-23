<script setup lang="ts">
/**
 * NotificationPage - 消息中心
 *
 * 站内信全量列表、筛选、已读/处理/删除，以及按分类配置投递渠道。
 */
import { onMounted, onUnmounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useNotificationStore, type NotificationItem } from '../stores/notification';

const { t } = useI18n();
const router = useRouter();
const store = useNotificationStore();

const category = ref('');
const readState = ref('');
const keyword = ref('');
const preferences = ref<Array<{ category: string; channels: string[]; }>>([]);
const saving = ref(false);

const categories = ['System', 'Business', 'Security', 'Task', 'Marketing'];
const channelOptions = [
  { value: 'InApp', label: 'InApp' },
  { value: 'Mail', label: 'Mail' },
  { value: 'Sms', label: 'Sms' },
  { value: 'AppPush', label: 'AppPush' },
];

async function search(page = 1) {
  await store.loadList({
    pageIndex: page,
    category: category.value || undefined,
    read: readState.value === '' ? undefined : readState.value === 'read',
    key: keyword.value || undefined,
  });
}

async function loadPreferences() {
  try {
    preferences.value = await store.loadPreferences();
  } catch {
    preferences.value = [];
  }
}

async function savePreference(item: { category: string; channels: string[]; }) {
  saving.value = true;
  try {
    await store.savePreferences(item.category, item.channels);
    ElMessage.success(t('notify.preferenceSaved'));
  } finally {
    saving.value = false;
  }
}

async function open(item: NotificationItem) {
  try {
    if (!item.read) await store.markRead(item);
  } catch {
    // 请求层已提示错误，继续跳转
  }

  if (!item.link) return;
  if (/^https?:\/\//i.test(item.link)) {
    window.location.assign(item.link);
  } else {
    router.push(item.link).catch(() => {});
  }
}

async function markAll() {
  try {
    const count = await store.markAllRead();
    ElMessage.success(t('notify.markedAll', { count }));
  } catch {
    // 请求层已提示错误
  }
}

async function handle(item: NotificationItem) {
  try {
    const result = await ElMessageBox.prompt(t('notify.handlePrompt'), t('notify.handle'), {
      inputValue: '',
      confirmButtonText: t('button.confirm'),
      cancelButtonText: t('button.cancel'),
    });
    await store.handle(item, result.value || '');
    ElMessage.success(t('notify.handled'));
  } catch {
    // 取消或请求失败
  }
}

async function remove(item: NotificationItem) {
  try {
    await ElMessageBox.confirm(t('notify.deleteConfirm'), t('notify.delete'), {
      type: 'warning',
      confirmButtonText: t('button.confirm'),
      cancelButtonText: t('button.cancel'),
    });
    await store.remove(item);
    ElMessage.success(t('notify.deleted'));
  } catch {
    // 取消或请求失败
  }
}

function levelType(level?: string) {
  switch ((level || '').toLowerCase()) {
    case 'warning':
      return 'warning';
    case 'error':
    case 'urgent':
      return 'danger';
    case 'success':
      return 'success';
    default:
      return 'info';
  }
}

function formatTime(value?: string) {
  if (!value) return '';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

onMounted(async () => {
  store.start();
  await Promise.all([search(1), loadPreferences()]);
});

onUnmounted(() => {
  store.stop();
});
</script>

<template>
  <div class="notification-page">
    <div class="page-head">
      <div>
        <h2>{{ t('notify.center') }}</h2>
        <p class="page-subtitle">
          {{ store.realtime ? t('notify.realtime') : t('notify.polling') }}
        </p>
      </div>
      <div class="page-actions">
        <el-button :disabled="!store.hasUnread" @click="markAll">
          {{ t('notify.markAll') }}
        </el-button>
        <el-button @click="search(store.pageIndex)">{{ t('notify.refresh') }}</el-button>
      </div>
    </div>

    <el-card shadow="never" class="filter-card">
      <div class="filters">
        <el-select v-model="category" :placeholder="t('notify.category')" clearable class="filter-category">
          <el-option v-for="item in categories" :key="item" :label="t(`notify.category${item}`)" :value="item" />
        </el-select>
        <el-select v-model="readState" :placeholder="t('notify.readState')" clearable class="filter-read">
          <el-option :label="t('notify.all')" value="" />
          <el-option :label="t('notify.unread')" value="unread" />
          <el-option :label="t('notify.read')" value="read" />
        </el-select>
        <el-input
          v-model="keyword"
          :placeholder="t('notify.searchPlaceholder')"
          clearable
          class="filter-key"
          @keyup.enter="search(1)"
        />
        <el-button type="primary" @click="search(1)">{{ t('button.search') }}</el-button>
      </div>
    </el-card>

    <el-card shadow="never" class="list-card">
      <el-empty v-if="!store.loading && !store.list.length" :description="t('notify.empty')" />
      <div v-else v-loading="store.loading" class="notify-list">
        <div
          v-for="item in store.list"
          :key="item.id"
          class="notify-row"
          :class="{ unread: !item.read }"
        >
          <div class="notify-main" @click="open(item)">
            <div class="notify-title-line">
              <span class="notify-title">{{ item.title }}</span>
              <el-tag size="small" :type="levelType(item.level)">{{ item.level || 'Info' }}</el-tag>
              <el-tag v-if="item.shared" size="small" type="info">{{ t('notify.shared') }}</el-tag>
              <el-tag v-if="item.handled" size="small" type="success">{{ t('notify.handledTag') }}</el-tag>
            </div>
            <div class="notify-content">{{ item.content }}</div>
            <div class="notify-meta">
              <span>{{ formatTime(item.createTime) }}</span>
              <span v-if="item.readChannel">{{ t('notify.readChannel') }}: {{ item.readChannel }}</span>
            </div>
          </div>
          <div class="notify-actions">
            <el-button v-if="!item.read" link type="primary" @click="store.markRead(item)">
              {{ t('notify.markRead') }}
            </el-button>
            <el-button v-if="!item.handled" link type="success" @click="handle(item)">
              {{ t('notify.handle') }}
            </el-button>
            <el-button v-if="!item.shared" link type="danger" @click="remove(item)">
              {{ t('notify.delete') }}
            </el-button>
          </div>
        </div>
      </div>

      <div class="pager">
        <el-pagination
          background
          layout="prev, pager, next, total"
          :current-page="store.pageIndex"
          :page-size="store.pageSize"
          :total="store.total"
          @current-change="search"
        />
      </div>
    </el-card>

    <el-card shadow="never" class="preference-card">
      <template #header>
        <div class="preference-head">
          <strong>{{ t('notify.preferences') }}</strong>
          <span class="page-subtitle">{{ t('notify.preferencesHint') }}</span>
        </div>
      </template>
      <div v-for="item in preferences" :key="item.category" class="preference-row">
        <span class="preference-name">{{ t(`notify.category${item.category}`) }}</span>
        <el-checkbox-group v-model="item.channels" :disabled="saving" @change="savePreference(item)">
          <el-checkbox v-for="channel in channelOptions" :key="channel.value" :value="channel.value">
            {{ channel.label }}
          </el-checkbox>
        </el-checkbox-group>
      </div>
    </el-card>
  </div>
</template>

<style lang="scss" scoped>
.notification-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px;
}

.page-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;

  h2 {
    margin: 0 0 4px;
    font-size: 20px;
    color: var(--el-text-color-primary);
  }
}

.page-subtitle {
  margin: 0;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.page-actions {
  display: flex;
  gap: 8px;
}

.filters {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.filter-category,
.filter-read {
  width: 150px;
}

.filter-key {
  width: 240px;
}

.notify-list {
  display: flex;
  flex-direction: column;
}

.notify-row {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 8px;
  border-bottom: 1px solid var(--el-border-color-lighter);

  &:last-child {
    border-bottom: none;
  }

  &.unread {
    background: var(--el-color-primary-light-9);
  }
}

.notify-main {
  flex: 1;
  min-width: 0;
  cursor: pointer;
}

.notify-title-line {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
}

.notify-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.notify-content {
  margin-top: 6px;
  font-size: 13px;
  line-height: 1.6;
  color: var(--el-text-color-regular);
  white-space: pre-wrap;
  word-break: break-word;
}

.notify-meta {
  display: flex;
  gap: 16px;
  margin-top: 6px;
  font-size: 12px;
  color: var(--el-text-color-placeholder);
}

.notify-actions {
  display: flex;
  align-items: flex-start;
  flex-shrink: 0;
}

.pager {
  display: flex;
  justify-content: flex-end;
  padding-top: 16px;
}

.preference-head {
  display: flex;
  align-items: center;
  gap: 12px;
}

.preference-row {
  display: flex;
  align-items: center;
  gap: 24px;
  padding: 8px 0;
  border-bottom: 1px solid var(--el-border-color-lighter);

  &:last-child {
    border-bottom: none;
  }
}

.preference-name {
  width: 120px;
  font-size: 13px;
  color: var(--el-text-color-primary);
}
</style>
