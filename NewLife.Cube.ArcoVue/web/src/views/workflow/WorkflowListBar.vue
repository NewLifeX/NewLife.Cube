<script setup lang="ts">
/**
 * 审批列表工具栏：左侧刷新（及行操作），右侧关键字与分组，底部分页。
 * 布局对齐实体列表 list-topbar / list-pager。
 */
import { ref } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import { PAGE_SIZE_OPTIONS } from '@/core/utils/viewMapping';
import type { ViewGroup } from '@/core/utils/viewProfile';
import GroupPopover from '@/views/crud/GroupPopover.vue';

const q = defineModel<string>('q', { default: '' });

const props = defineProps<{
  fields: FieldMeta[];
  group: ViewGroup;
  page: number;
  pageSize: number;
  total: number;
}>();

const emit = defineEmits<{
  (e: 'search'): void;
  (e: 'refresh'): void;
  (e: 'update:group', v: ViewGroup): void;
  (e: 'page-change', page: number): void;
  (e: 'page-size-change', size: number): void;
}>();

const groupVisible = ref(false);

function applyGroup(v: ViewGroup) {
  emit('update:group', v);
  groupVisible.value = false;
}
</script>

<template>
  <div class="list-topbar">
    <a-space>
      <a-button @click="emit('refresh')">刷新</a-button>
      <slot name="actions" />
    </a-space>
    <a-space>
      <a-input
        v-model="q"
        class="wf-list-query"
        placeholder="标题 / 摘要 / 业务对象"
        allow-clear
        @press-enter="emit('search')"
        @clear="emit('search')"
      >
        <template #suffix>
          <icon-park type="search" class="wf-list-query__go" title="查询" @click.stop="emit('search')" />
        </template>
      </a-input>
      <GroupPopover
        :visible="groupVisible"
        :fields="fields"
        :model-value="props.group"
        :can-save="false"
        @update:visible="groupVisible = $event"
        @apply="applyGroup"
      >
        <div class="tb-act" :class="{ 'is-active': props.group.length > 0 }">
          <a-button type="text">
            <icon-park type="connection-box" />
            分组
          </a-button>
          <span
            v-if="props.group.length"
            class="tb-count"
            title="清除分组"
            @click.stop="applyGroup([])"
          >
            {{ props.group.length }}
          </span>
        </div>
      </GroupPopover>
    </a-space>
  </div>

  <slot />

  <div class="list-pager">
    <a-pagination
      :current="page"
      :page-size="pageSize"
      :total="total"
      :page-size-options="[...PAGE_SIZE_OPTIONS]"
      show-total
      show-page-size
      @change="(p: number) => emit('page-change', p)"
      @page-size-change="(s: number) => emit('page-size-change', s)"
    />
  </div>
</template>

<style scoped>
.wf-list-query {
  width: 240px;
}
.wf-list-query__go {
  cursor: pointer;
}
.list-pager {
  margin-top: 12px;
  display: flex;
  justify-content: flex-end;
}
.tb-act {
  position: relative;
  display: inline-flex;
  align-items: center;
}
.tb-act.is-active :deep(.arco-btn) {
  background: color-mix(in srgb, var(--cube-primary, rgb(var(--primary-6))) 12%, transparent);
}
.tb-count {
  position: absolute;
  top: -4px;
  right: -4px;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: 8px;
  background: var(--cube-primary, rgb(var(--primary-6)));
  color: #fff;
  font-size: 11px;
  line-height: 16px;
  text-align: center;
  cursor: pointer;
}
</style>
