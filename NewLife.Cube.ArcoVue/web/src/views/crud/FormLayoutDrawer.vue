<template>
  <a-drawer
    :visible="visible"
    :width="drawerWidth"
    unmount-on-close
    placement="right"
    class="form-layout-drawer"
    @update:visible="(v: boolean) => emit('update:visible', v)"
  >
    <template #title>
      <span class="drawer-title">表单布局</span>
    </template>

    <div class="fl-modes">
      <a-radio-group v-model="activeMode" type="button" size="small">
        <a-radio v-for="m in modeOptions" :key="m.value" :value="m.value">
          {{ m.label }}
        </a-radio>
      </a-radio-group>
    </div>

    <section class="fl-block">
      <div class="fl-label">
        字段设置
        <a-tooltip content="仅调整展示顺序与显隐；不能绕过字段权限、必填或校验">
          <icon-park type="info" class="fl-hint" />
        </a-tooltip>
      </div>
      <a-empty v-if="!orderedFields.length" description="暂无字段可配置" />
      <ul v-else class="fl-field-list">
        <li
          v-for="(f, idx) in orderedFields"
          :key="f.name"
          class="fl-field-item"
          draggable="true"
          @dragstart="onDragStart(idx, $event)"
          @dragover.prevent
          @drop="onDrop(idx)"
        >
          <span class="fl-drag-handle" title="拖动排序">
            <icon-park type="drag" />
          </span>
          <span class="fl-field-name" :class="{ muted: isHidden(f.name) }">
            {{ f.displayName || f.name }}
          </span>
          <a-button
            type="text"
            size="mini"
            :title="isHidden(f.name) ? '显示' : '隐藏'"
            @click="setHidden(f.name, !isHidden(f.name))"
          >
            <icon-park v-if="!isHidden(f.name)" type="preview-open" />
            <icon-park v-else type="preview-close" />
          </a-button>
        </li>
      </ul>
    </section>

    <section v-if="categoryOptions.length" class="fl-block">
      <div class="fl-label">分组折叠</div>
      <div class="fl-check-list">
        <a-checkbox
          v-for="c in categoryOptions"
          :key="c"
          :model-value="isCollapsed(c)"
          @change="(v: boolean | string | number) => setCollapsed(c, v === true)"
        >
          {{ c }}
        </a-checkbox>
      </div>
    </section>

    <template #footer>
      <a-space>
        <a-button :disabled="!canConfigure" @click="resetLayout">
          恢复默认布局
        </a-button>
        <a-button @click="cancel">取消</a-button>
        <a-button type="primary" :disabled="!canConfigure" @click="saveLayout">
          保存
        </a-button>
      </a-space>
    </template>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import type { FieldMeta } from '@/core/types/field';
import { isAuditField } from '@/core/utils/fieldControl';
import { normalizeFormLayout } from '@/core/utils/fieldGroups';
import {
  buildFormJsonWire,
  emptyFormLayout,
  type FormLayout,
  type FormMode,
} from '@/core/utils/viewProfile';
import { useViewProfileStore } from '@/stores/viewProfile';

const props = defineProps<{
  visible: boolean;
  typePath: string;
  addFields: FieldMeta[];
  editFields: FieldMeta[];
  detailFields: FieldMeta[];
  /** 无配置权限时不渲染入口（父级已控）；组件内再防御禁用 */
  canConfigure: boolean;
}>();

const emit = defineEmits<{
  'update:visible': [boolean];
}>();

const evpStore = useViewProfileStore();

const modeOptions: { value: FormMode; label: string }[] = [
  { value: 'add', label: '新增' },
  { value: 'edit', label: '编辑' },
  { value: 'detail', label: '详情' },
];

const activeMode = ref<FormMode>('edit');
/** 三个模式的本地编辑态（OSC-0013 手动保存）：勾选/拖动只改本地，Tab 切换保留未保存修改，仅「保存」提交 */
const localLayouts = ref<Record<FormMode, FormLayout>>({
  add: emptyFormLayout(),
  edit: emptyFormLayout(),
  detail: emptyFormLayout(),
});

/** 当前模式的本地布局（编辑态） */
const currentLayout = computed(() => localLayouts.value[activeMode.value]);

const drawerWidth = computed(() =>
  typeof window !== 'undefined' && window.innerWidth < 768 ? '100%' : 420,
);

/** 当前模式可配置字段：与 FormContent 同源过滤（审计字段；add 另去 PK/readOnly） */
function modeFields(mode: FormMode): FieldMeta[] {
  const raw =
    mode === 'add'
      ? props.addFields
      : mode === 'edit'
        ? props.editFields
        : props.detailFields;
  const withoutAudit = raw.filter((f) => !isAuditField(f));
  if (mode === 'add') {
    return withoutAudit.filter((f) => !f.primaryKey && !f.readOnly);
  }
  return withoutAudit;
}

/** 按本地 order 排序后的展示字段：order 前 + 未列按元数据原序追加 */
const orderedFields = computed<FieldMeta[]>(() => {
  const fields = modeFields(activeMode.value);
  const orderMap = new Map(currentLayout.value.order.map((n, i) => [n, i]));
  const ordered = fields
    .filter((f) => orderMap.has(f.name))
    .sort((a, b) => orderMap.get(a.name)! - orderMap.get(b.name)!);
  const rest = fields.filter((f) => !orderMap.has(f.name));
  return [...ordered, ...rest];
});

/** 当前字段集实际存在的非空 Category */
const categoryOptions = computed<string[]>(() => {
  const set = new Set<string>();
  for (const f of modeFields(activeMode.value)) {
    const c = (f.category ?? '').trim();
    if (c) set.add(c);
  }
  return [...set];
});

/** 打开时从 store 加载三个模式的已保存布局（丢弃上次未保存修改） */
function loadAll() {
  localLayouts.value = {
    add: normalizeFormLayout(
      evpStore.getFormModeLayout(props.typePath, 'add'),
      modeFields('add'),
    ),
    edit: normalizeFormLayout(
      evpStore.getFormModeLayout(props.typePath, 'edit'),
      modeFields('edit'),
    ),
    detail: normalizeFormLayout(
      evpStore.getFormModeLayout(props.typePath, 'detail'),
      modeFields('detail'),
    ),
  };
}

// immediate：组件挂载时 visible 可能已为 true（如页面加载后立即打开），必须加载一次，否则显示默认全部字段
// Tab 切换不重载：三模式各自保留本地编辑态，未保存修改不因切换丢失
watch(
  () => props.visible,
  (v) => {
    if (v) loadAll();
  },
  { immediate: true },
);

/** 仅更新当前模式的本地布局（不触发保存） */
function patchCurrentLayout(patch: Partial<FormLayout>) {
  localLayouts.value = {
    ...localLayouts.value,
    [activeMode.value]: { ...currentLayout.value, ...patch },
  };
}

function isHidden(name: string): boolean {
  return currentLayout.value.hidden.includes(name);
}

function setHidden(name: string, hidden: boolean) {
  const set = new Set(currentLayout.value.hidden);
  if (hidden) set.add(name);
  else set.delete(name);
  patchCurrentLayout({ hidden: [...set] });
}

function isCollapsed(category: string): boolean {
  return currentLayout.value.collapsedCategories.includes(category);
}

function setCollapsed(category: string, collapsed: boolean) {
  const set = new Set(currentLayout.value.collapsedCategories);
  if (collapsed) set.add(category);
  else set.delete(category);
  patchCurrentLayout({ collapsedCategories: [...set] });
}

let dragFrom = -1;

function onDragStart(idx: number, e: DragEvent) {
  dragFrom = idx;
  e.dataTransfer?.setData('text/plain', String(idx));
}

function onDrop(toIdx: number) {
  if (dragFrom < 0 || dragFrom === toIdx) return;
  const names = orderedFields.value.map((f) => f.name);
  const [item] = names.splice(dragFrom, 1);
  names.splice(toIdx, 0, item);
  dragFrom = -1;
  patchCurrentLayout({ order: names });
}

/** 手动保存：三个模式一次性提交（未修改模式不写入）；保存后关闭抽屉 */
function saveLayout() {
  if (!props.canConfigure) return;
  evpStore.setFormJson(props.typePath, buildFormJsonWire(localLayouts.value));
  Message.success('布局已保存');
  emit('update:visible', false);
}

/** 手动取消：关闭抽屉并丢弃未保存修改（下次打开重新从 store 加载） */
function cancel() {
  emit('update:visible', false);
}

/** 恢复当前模式默认布局：仅本地清空（保存时才提交删除该模式），不影响视图/筛选/PageSize 域 */
function resetLayout() {
  if (!props.canConfigure) return;
  patchCurrentLayout(emptyFormLayout());
  Message.success('已恢复默认布局（保存后生效）');
}
</script>

<style scoped>
.form-layout-drawer {
  min-width: 0;
  max-width: 100%;
}
.fl-modes {
  margin-bottom: 16px;
}
.fl-block {
  margin-bottom: 20px;
}
.fl-label {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 10px;
  font-size: 13px;
  font-weight: 600;
  color: var(--color-text-1);
}
.fl-hint {
  color: var(--color-text-3);
  font-size: 12px;
}
/* 字段列表样式与 ViewConfigDrawer 字段配置保持一致（OSC-0013） */
.fl-field-list {
  list-style: none;
  margin: 0;
  padding: 0;
  max-height: 360px;
  overflow: auto;
  border: 1px solid var(--color-border);
  border-radius: 4px;
}
.fl-field-item {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 6px 8px;
  border-bottom: 1px solid var(--color-border);
  background: var(--color-bg-2);
  cursor: grab;
}
.fl-field-item:last-child {
  border-bottom: none;
}
.fl-drag-handle {
  display: inline-flex;
  align-items: center;
  color: var(--color-text-3);
  flex-shrink: 0;
  cursor: grab;
  padding: 2px;
}
.fl-drag-handle:active {
  cursor: grabbing;
}
.fl-field-name {
  flex: 1;
  min-width: 0;
  font-size: 13px;
  color: var(--color-text-1);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.fl-field-name.muted {
  color: var(--color-text-3);
}
.fl-check-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
</style>
