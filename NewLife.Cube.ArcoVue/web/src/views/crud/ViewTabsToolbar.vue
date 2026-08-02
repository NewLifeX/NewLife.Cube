<template>
  <div class="view-tabs-toolbar">
    <div class="view-tabs">
      <div
        v-for="v in views"
        :key="v.id"
        class="view-tab"
        :class="{ active: v.id === activeId }"
      >
        <button type="button" class="view-tab-main" @click="$emit('switch', v.id)">
          <span class="view-tab-kind">{{ kindLabel(v.view) }}</span>
          <span class="view-tab-name">{{ v.name }}</span>
        </button>
        <a-dropdown
          v-if="v.id === activeId"
          trigger="click"
          @select="onMenuSelect"
        >
          <button type="button" class="view-tab-menu" title="视图菜单" @click.stop>
            <icon-more-vertical />
          </button>
          <template #content>
            <a-doption value="rename">重命名</a-doption>
            <a-doption value="config">自定义配置</a-doption>
            <a-doption value="duplicate">复制</a-doption>
            <a-doption value="delete" :disabled="views.length <= 1">删除</a-doption>
            <a-doption value="reset">恢复默认</a-doption>
          </template>
        </a-dropdown>
      </div>
    </div>

    <a-dropdown trigger="click" @select="onCreateSelect">
      <button type="button" class="view-add" title="添加视图">+</button>
      <template #content>
        <a-doption
          v-for="opt in createOptions"
          :key="opt.kind"
          :value="opt.kind"
          :disabled="!opt.ok"
        >
          <a-tooltip v-if="!opt.ok" :content="opt.reason || '不可创建'">
            <span>{{ opt.label }}</span>
          </a-tooltip>
          <span v-else>{{ opt.label }}</span>
        </a-doption>
      </template>
    </a-dropdown>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { IconMoreVertical } from '@arco-design/web-vue/es/icon';
import type { FieldMeta } from '@/core/types/field';
import type { NamedView, ViewKind } from '@/core/utils/viewProfile';
import {
  VIEW_KIND_LABEL,
  canCreateViewKind,
  viewKindCreateLabel,
} from '@/core/utils/viewMapping';

const props = defineProps<{
  views: NamedView[];
  activeId: string;
  fields: FieldMeta[];
  typePath: string;
}>();

const emit = defineEmits<{
  switch: [id: string];
  create: [kind: ViewKind, name: string];
  rename: [id: string, name: string];
  remove: [id: string];
  duplicate: [id: string];
  reset: [];
  openConfig: [];
}>();

const createKinds: ViewKind[] = ['table', 'tree', 'card', 'kanban', 'calendar', 'gantt'];

const createOptions = computed(() =>
  createKinds.map((kind) => {
    const gate = canCreateViewKind(kind, props.fields, props.typePath);
    return {
      kind,
      label: viewKindCreateLabel(kind),
      ok: gate.ok,
      reason: gate.reason,
    };
  }),
);

function kindLabel(kind: ViewKind): string {
  return VIEW_KIND_LABEL[kind] || kind;
}

function onMenuSelect(val: string | number | Record<string, unknown> | undefined) {
  const key = String(val);
  if (key === 'rename') {
    const cur = props.views.find((v) => v.id === props.activeId);
    const name = window.prompt('重命名视图', cur?.name || '');
    if (name) emit('rename', props.activeId, name);
    return;
  }
  if (key === 'config') {
    emit('openConfig');
    return;
  }
  if (key === 'duplicate') {
    emit('duplicate', props.activeId);
    return;
  }
  if (key === 'delete') {
    if (window.confirm('删除当前视图？')) emit('remove', props.activeId);
    return;
  }
  if (key === 'reset') {
    if (window.confirm('恢复为默认「默认列表」视图并清除已保存配置？')) emit('reset');
  }
}

function onCreateSelect(val: string | number | Record<string, unknown> | undefined) {
  const kind = String(val) as ViewKind;
  const opt = createOptions.value.find((o) => o.kind === kind);
  if (!opt?.ok) return;
  const defaultName = viewKindCreateLabel(kind);
  const name = window.prompt(`新建${defaultName}名称`, defaultName);
  if (name) emit('create', kind, name);
}
</script>

<style scoped>
.view-tabs-toolbar {
  display: flex;
  align-items: center;
  gap: 2px;
  flex-wrap: wrap;
  min-width: 0;
}
.view-tabs {
  display: flex;
  align-items: center;
  gap: 2px;
  flex-wrap: wrap;
  min-width: 0;
}
.view-tab {
  display: inline-flex;
  align-items: center;
  gap: 0;
  border-radius: 6px;
  color: var(--color-text-2);
  font-size: 13px;
  line-height: 1.2;
}
.view-tab:hover {
  background: var(--color-fill-2);
  color: var(--color-text-1);
}
.view-tab.active {
  color: rgb(var(--primary-6));
  font-weight: 500;
}
.view-tab.active:hover {
  background: var(--color-fill-2);
  color: rgb(var(--primary-6));
}
.view-tab-main {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  border: none;
  background: transparent;
  padding: 6px 4px 6px 12px;
  cursor: pointer;
  color: inherit;
  font: inherit;
}
.view-tab:not(.active) .view-tab-main {
  padding-right: 12px;
}
.view-tab-kind {
  font-size: 11px;
  opacity: 0.7;
}
.view-tab-menu {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: none;
  background: transparent;
  width: 22px;
  padding: 4px 2px;
  cursor: pointer;
  color: inherit;
  font-size: 14px;
  line-height: 1;
  opacity: 0.65;
}
.view-tab-menu:hover {
  opacity: 1;
}
.view-add {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  background: transparent;
  border-radius: 6px;
  cursor: pointer;
  color: var(--color-text-3);
  font-size: 18px;
  line-height: 1;
  font-weight: 400;
}
.view-add:hover {
  color: rgb(var(--primary-6));
  background: var(--color-fill-2);
}
</style>
