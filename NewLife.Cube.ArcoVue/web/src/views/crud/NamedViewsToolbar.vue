<template>
  <a-space>
    <a-dropdown trigger="click" @select="onSelect">
      <a-button>
        {{ activeName }}
        <icon-park type="down" />
      </a-button>
      <template #content>
        <a-doption v-for="v in views" :key="v.id" :value="`switch:${v.id}`">
          <span>{{ v.name }}</span>
          <icon-park v-if="v.id === activeId" type="check" style="margin-left: 8px" />
        </a-doption>
        <a-doption value="new">新建视图…</a-doption>
        <a-doption value="rename" :disabled="!activeId">重命名当前…</a-doption>
        <a-doption value="delete" :disabled="views.length <= 1">删除当前</a-doption>
        <a-doption value="reset">恢复默认</a-doption>
      </template>
    </a-dropdown>
    <a-button @click="$emit('openConfig')">配置</a-button>
  </a-space>
</template>

<script setup lang="ts">
import type { NamedView } from '@/core/utils/viewProfile';
import { useNamedViewsToolbar } from './useNamedViewsToolbar';

const props = defineProps<{
  views: NamedView[];
  activeId: string;
}>();

const emit = defineEmits<{
  switch: [id: string];
  create: [name: string];
  rename: [id: string, name: string];
  remove: [id: string];
  reset: [];
  openConfig: [];
}>();

const {
  activeName,
  onSelect,
} = useNamedViewsToolbar(props, emit);
</script>
