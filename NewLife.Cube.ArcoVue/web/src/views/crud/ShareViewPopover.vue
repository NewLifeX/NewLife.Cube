<template>
  <a-popover
    :popup-visible="visible"
    position="bottom"
    trigger="click"
    class="share-view-popover"
    :content-style="{ padding: '12px 14px', width: '360px' }"
    @popup-visible-change="onVisibleChange"
  >
    <template #content>
      <div class="share-pop">
        <div class="share-pop__head">
          <span class="share-pop__title">分享当前视图</span>
          <a-spin v-if="creating" :size="14" />
        </div>
        <div class="share-pop__hint">
          按你的权限生成链接；打开后不显示系统导航与菜单。
        </div>

        <div class="share-pop__label">有效期</div>
        <a-radio-group v-model="expireKey" type="button" size="mini" class="share-pop__radios">
          <a-radio value="1h">1 小时</a-radio>
          <a-radio value="1d">1 天</a-radio>
          <a-radio value="7d">7 天</a-radio>
          <a-radio value="long">长期</a-radio>
          <a-radio value="custom">自定义</a-radio>
        </a-radio-group>
        <div v-if="expireKey === 'custom'" class="share-pop__custom">
          <a-input-number v-model="customDays" :min="1" :max="365" size="small" />
          <span>天</span>
        </div>

        <div class="share-pop__label">链接分享</div>
        <a-input
          :model-value="shareUrl"
          readonly
          size="small"
          class="share-pop__url"
          placeholder="正在生成…"
        />
        <div v-if="expireText" class="share-pop__expire">过期：{{ expireText }}</div>

        <div class="share-pop__actions">
          <a-button type="primary" size="small" :disabled="!shareUrl || creating" @click="onCopy">
            <icon-park type="copy" />
            复制链接
          </a-button>
        </div>
      </div>
    </template>
    <slot />
  </a-popover>
</template>

<script setup lang="ts">
import { toRef } from 'vue';
import { useShareViewPopover } from './useShareViewPopover';

const props = defineProps<{
  visible: boolean;
  typePath: string;
  viewId: string;
}>();

const emit = defineEmits<{ 'update:visible': [boolean] }>();

const {
  creating,
  shareUrl,
  expireText,
  expireKey,
  customDays,
  onVisibleChange,
  onCopy,
} = useShareViewPopover(
  () => props.typePath,
  () => props.viewId,
  toRef(props, 'visible'),
  (v) => emit('update:visible', v),
);
</script>

<style scoped>
.share-pop {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 320px;
}
.share-pop__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}
.share-pop__title {
  font-weight: 600;
  font-size: 14px;
  color: var(--color-text-1);
}
.share-pop__hint {
  font-size: 12px;
  color: var(--color-text-3);
  line-height: 1.4;
}
.share-pop__label {
  margin-top: 4px;
  font-size: 12px;
  color: var(--color-text-2);
}
.share-pop__radios {
  display: flex;
  flex-wrap: wrap;
  gap: 0;
}
.share-pop__custom {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: var(--color-text-2);
}
.share-pop__url :deep(.arco-input) {
  font-size: 12px;
}
.share-pop__expire {
  font-size: 12px;
  color: var(--color-text-3);
}
.share-pop__actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 4px;
}
</style>
