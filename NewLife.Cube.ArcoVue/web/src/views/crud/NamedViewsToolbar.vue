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
import { computed } from 'vue';
import { DEFAULT_VIEW_NAME, type NamedView } from '@/core/utils/viewProfile';

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

const activeName = computed(
  () => props.views.find((v) => v.id === props.activeId)?.name || DEFAULT_VIEW_NAME,
);

function onSelect(val: string | number | Record<string, unknown> | undefined) {
  const key = String(val);
  if (key.startsWith('switch:')) {
    emit('switch', key.slice('switch:'.length));
    return;
  }
  if (key === 'new') {
    const name = window.prompt('新视图名称（表格视图）', '未命名');
    if (name) emit('create', name);
    return;
  }
  if (key === 'rename') {
    const cur = props.views.find((v) => v.id === props.activeId);
    const name = window.prompt('重命名视图', cur?.name || '');
    if (name) emit('rename', props.activeId, name);
    return;
  }
  if (key === 'delete') {
    if (window.confirm('删除当前视图？')) emit('remove', props.activeId);
    return;
  }
  if (key === 'reset') {
    if (window.confirm(`恢复为默认「${DEFAULT_VIEW_NAME}」视图并清除已保存配置？`)) emit('reset');
  }
}
</script>
