<template>
  <div class="view-tabs-toolbar">
    <div ref="tabsRef" class="view-tabs">
      <!-- 选中视图滑动指示器（OSC 视图 Tab 交互）：transform 滑动到当前激活 Tab，宽度随 Tab 变化 -->
      <span
        class="view-tab-indicator"
        :style="{
          transform: `translateX(${indicator.x}px)`,
          width: `${indicator.width}px`,
          opacity: indicator.visible ? 1 : 0,
        }"
      />
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

    <!-- 视图命名弹层：相对主界面居中、跟随主题（Arco Modal）；替代原生 prompt（无法居中/主题化） -->
    <a-modal
      :visible="nameModalVisible"
      :title="nameModalTitle"
      :width="360"
      unmount-on-close
      @cancel="closeNameModal"
      @ok="submitName"
    >
      <a-input
        v-model="nameDraft"
        :max-length="32"
        placeholder="请输入视图名称"
        @keyup.enter="submitName"
      />
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
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

/** 视图命名弹层（创建/重命名共用）：相对主界面居中、Arco 组件自动跟随主题 */
const nameModalVisible = ref(false);
const nameModalTitle = ref('新建视图');
const nameDraft = ref('');
/** 'create' 或 'rename' */
const nameMode = ref<'create' | 'rename'>('create');
const nameKind = ref<ViewKind>('table');
const nameTargetId = ref('');

function openCreateModal(kind: ViewKind) {
  nameMode.value = 'create';
  nameKind.value = kind;
  nameModalTitle.value = `新建${viewKindCreateLabel(kind)}`;
  nameDraft.value = viewKindCreateLabel(kind);
  nameTargetId.value = '';
  nameModalVisible.value = true;
}

function openRenameModal(id: string) {
  const cur = props.views.find((v) => v.id === id);
  if (!cur) return;
  nameMode.value = 'rename';
  nameTargetId.value = id;
  nameModalTitle.value = '重命名视图';
  nameDraft.value = cur.name || '';
  nameModalVisible.value = true;
}

function closeNameModal() {
  nameModalVisible.value = false;
}

function submitName() {
  const name = nameDraft.value.trim();
  if (!name) return;
  if (nameMode.value === 'create') {
    emit('create', nameKind.value, name);
  } else {
    emit('rename', nameTargetId.value, name);
  }
  closeNameModal();
}

function onMenuSelect(val: string | number | Record<string, unknown> | undefined) {
  const key = String(val);
  if (key === 'rename') {
    openRenameModal(props.activeId);
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
  openCreateModal(kind);
}

/** 选中 Tab 滑动指示器位置（transform 平滑过渡） */
const tabsRef = ref<HTMLElement | null>(null);
const indicator = ref({ x: 0, width: 0, visible: false });
let indicatorRo: ResizeObserver | null = null;

function updateIndicator() {
  void nextTick(() => {
    const tabs = tabsRef.value;
    if (!tabs) return;
    const el = tabs.querySelector<HTMLElement>('.view-tab.active');
    if (!el) {
      indicator.value = { x: 0, width: 0, visible: false };
      return;
    }
    indicator.value = { x: el.offsetLeft, width: el.offsetWidth, visible: true };
  });
}

watch(() => props.activeId, updateIndicator, { immediate: true });
watch(
  () => props.views.map((v) => v.name).join(','),
  () => updateIndicator(),
);

onBeforeUnmount(() => {
  indicatorRo?.disconnect();
  indicatorRo = null;
});

onMounted(() => {
  updateIndicator();
  if (typeof ResizeObserver !== 'undefined') {
    indicatorRo = new ResizeObserver(() => updateIndicator());
    if (tabsRef.value) indicatorRo.observe(tabsRef.value);
  }
});
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
  position: relative;
}
/* 选中视图滑动指示器：transform 平滑滑动到激活 Tab（需求：切换滑动效果） */
.view-tab-indicator {
  position: absolute;
  bottom: -5px;
  left: 0;
  height: 2px;
  border-radius: 1px;
  background: rgb(var(--primary-6));
  transition:
    transform 0.28s cubic-bezier(0.4, 0, 0.2, 1),
    width 0.28s cubic-bezier(0.4, 0, 0.2, 1),
    opacity 0.2s;
  pointer-events: none;
  z-index: 1;
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
  /* 选中底纹：主题浅色阶（暗色下为主色半透明），配合下方滑动指示器 */
  background: var(--color-primary-light-1);
  color: rgb(var(--primary-6));
  font-weight: 500;
}
.view-tab.active:hover {
  background: var(--color-primary-light-2);
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
