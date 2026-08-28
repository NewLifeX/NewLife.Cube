<template>
  <a-drawer
    :visible="visible"
    :width="400"
    unmount-on-close
    title="编辑快捷入口"
    @update:visible="(v: boolean) => emit('update:visible', v)"
  >
    <p class="ql-edit-hint">勾选有权限的菜单项，保存后写入个人工作台。</p>
    <a-input v-model="keyword" allow-clear placeholder="搜索菜单" class="ql-edit-search" />
    <a-empty v-if="!filtered.length" description="暂无可选菜单" />
    <div v-else class="ql-edit-list">
      <label
        v-for="m in filtered"
        :key="m.url"
        class="ql-edit-row"
        @click.prevent="toggle(m.url)"
      >
        <a-checkbox :model-value="isChecked(m.url)" />
        <icon-park :type="m.icon || 'application'" class="ql-edit-ico" />
        <span class="ql-edit-name">{{ m.name }}</span>
        <span class="ql-edit-url">{{ m.url }}</span>
      </label>
    </div>
    <template #footer>
      <a-space>
        <a-button @click="clearToDefault">恢复默认</a-button>
        <a-button @click="cancel">取消</a-button>
        <a-button type="primary" :loading="saving" @click="confirm">保存</a-button>
      </a-space>
    </template>
  </a-drawer>
</template>

<script setup lang="ts">
import type { QuickLinkPin } from '@/core/utils/quickLinks';
import { useQuickLinksEdit } from './useQuickLinksEdit';

const props = defineProps<{
  visible: boolean;
  menuLeaves: QuickLinkPin[];
  pins: QuickLinkPin[];
  saving?: boolean;
}>();
const emit = defineEmits<{
  'update:visible': [boolean];
  save: [pins: QuickLinkPin[]];
}>();

const {
  keyword,
  filtered,
  toggle,
  isChecked,
  cancel,
  confirm,
  clearToDefault,
} = useQuickLinksEdit(props, emit);
</script>

<style scoped>
.ql-edit-hint {
  margin: 0 0 12px;
  font-size: 12px;
  color: var(--color-text-3);
}
.ql-edit-search {
  margin-bottom: 12px;
}
.ql-edit-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
  max-height: calc(100vh - 220px);
  overflow: auto;
}
.ql-edit-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 6px;
  border-radius: 6px;
  cursor: pointer;
}
.ql-edit-row:hover {
  background: var(--color-fill-1);
}
.ql-edit-ico {
  color: rgb(var(--primary-6));
  flex-shrink: 0;
}
.ql-edit-name {
  font-size: 13px;
  color: var(--color-text-1);
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.ql-edit-url {
  margin-left: auto;
  font-size: 11px;
  color: var(--color-text-3);
  flex-shrink: 0;
  max-width: 40%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
