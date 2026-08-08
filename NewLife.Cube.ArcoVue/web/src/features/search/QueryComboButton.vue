<template>
  <div class="query-combo-button">
    <!-- 主按钮：点击直接执行查询（OSC-0016） -->
    <a-button type="primary" class="qcb-search" @click="emit('search')">
      <template #icon><IconSearch /></template>
      查询
    </a-button>
    <!-- 下拉按钮：重置查询参数 / 预定义查询等更多操作 -->
    <a-dropdown trigger="click" @select="onSelect">
      <a-button type="primary" class="qcb-more">
        <IconDown />
      </a-button>
      <template #content>
        <div class="qcb-menu">
          <a-doption value="__reset">
            <template #icon><IconRefresh /></template>
            重置查询参数
          </a-doption>
          <a-doption v-if="hasMoreFields" value="__toggle">
            <template #icon><IconUp v-if="expanded" /><IconDown v-else /></template>
            {{ expanded ? '收起条件' : `展开更多条件（${moreFieldCount}）` }}
          </a-doption>

          <a-divider class="qcb-divider" />
          <div class="qcb-group-title">预定义查询</div>
          <div v-if="!queries.length" class="qcb-empty">暂无预定义查询</div>
          <div v-else class="qcb-list">
            <a-doption
              v-for="q in queries"
              :key="q.id"
              :value="`__apply:${q.id}`"
              :class="{ 'qcb-applied': isApplied(q.id) }"
            >
              <span class="qcb-item">
                <IconCheck v-if="isApplied(q.id)" class="qcb-check" />
                {{ q.name }}
              </span>
              <template #suffix>
                <a-popconfirm content="确认删除该预定义查询？" @ok="onDelete(q.id)">
                  <IconDelete class="qcb-del" @click.stop />
                </a-popconfirm>
              </template>
            </a-doption>
          </div>

          <a-divider class="qcb-divider" />
          <a-doption value="__save" :disabled="!canSave">
            <template #icon><IconSave /></template>
            保存当前查询为预定义…
          </a-doption>
          <a-doption value="__rename" :disabled="!canRename">
            <template #icon><IconEdit /></template>
            重命名当前查询
          </a-doption>
          <a-doption value="__delete" :disabled="!canRename">
            <template #icon><IconDelete /></template>
            删除当前查询
          </a-doption>
        </div>
      </template>
    </a-dropdown>

    <a-modal
      v-model:visible="modalVisible"
      :title="modalTitle"
      :on-before-ok="onModalOk"
      @cancel="modalVisible = false"
    >
      <a-input
        v-model="modalName"
        :max-length="50"
        placeholder="请输入查询名称"
        allow-clear
        @press-enter="onModalOk"
      />
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { Message } from '@arco-design/web-vue';
import {
  IconCheck,
  IconDelete,
  IconDown,
  IconEdit,
  IconRefresh,
  IconSave,
  IconSearch,
  IconUp,
} from '@arco-design/web-vue/es/icon';
import type { SavedQuery } from '@/core/utils/viewProfile';

/** 查询组合按钮（OSC-0016 + 面板重构）：无状态组件，全部状态由 SearchDrawer / InsightPanel / DefaultList / viewProfile store 持有。 */
const props = defineProps<{
  /** 预定义查询列表 */
  queries: SavedQuery[];
  /** 当前应用的预定义查询 id（会话内存） */
  activeQueryId: string | null;
  /** 当前表单参数是否与 activeQuery 不一致（不一致时条目不显示 ✓，应用标记保留） */
  paramsDirty: boolean;
  /** 当前参数是否可保存为预定义（cleanSearchParams 后非空） */
  canSave: boolean;
  /** 是否存在第二行（多余）查询条件字段 */
  hasMoreFields: boolean;
  /** 第二行字段数（用于「展开更多条件（N）」） */
  moreFieldCount: number;
  /** 面板当前是否展开（收起显示一行、展开显示第二行） */
  expanded: boolean;
}>();

const emit = defineEmits<{
  search: [];
  reset: [];
  /** 展开 / 收起第二行条件 */
  toggleExpand: [];
  apply: [id: string];
  save: [name: string];
  rename: [id: string, name: string];
  delete: [id: string];
}>();

const canRename = computed(() => !!props.activeQueryId);

/** 已应用且参数一致时条目显示 ✓ */
function isApplied(id: string): boolean {
  return id === props.activeQueryId && !props.paramsDirty;
}

const modalVisible = ref(false);
const modalTitle = ref('保存为预定义查询');
const modalMode = ref<'save' | 'rename'>('save');
const modalName = ref('');

function openModal(mode: 'save' | 'rename') {
  modalMode.value = mode;
  modalTitle.value = mode === 'save' ? '保存为预定义查询' : '重命名查询';
  modalName.value = '';
  modalVisible.value = true;
}

function onSelect(value: string | number | Record<string, unknown> | undefined) {
  const key = typeof value === 'string' ? value : '';
  if (key === '__reset') {
    emit('reset');
    return;
  }
  if (key === '__toggle') {
    emit('toggleExpand');
    return;
  }
  if (key === '__save') {
    openModal('save');
    return;
  }
  if (key === '__rename') {
    openModal('rename');
    return;
  }
  if (key === '__delete') {
    if (props.activeQueryId) emit('delete', props.activeQueryId);
    return;
  }
  if (key.startsWith('__apply:')) {
    emit('apply', key.slice('__apply:'.length));
  }
}

function onModalOk(): boolean {
  const name = modalName.value.trim();
  if (!name) {
    Message.warning('请输入查询名称');
    return false;
  }
  if (modalMode.value === 'save') emit('save', name);
  else if (props.activeQueryId) emit('rename', props.activeQueryId, name);
  modalVisible.value = false;
  return true;
}

function onDelete(id: string) {
  emit('delete', id);
}
</script>

<style scoped>
.query-combo-button {
  display: inline-flex;
  align-items: center;
}
/* 组合按钮：主按钮执行查询，下拉按钮更多操作；两按钮无缝拼接（边框重叠、中间圆角收掉） */
.qcb-search {
  border-top-right-radius: 0;
  border-bottom-right-radius: 0;
}
.qcb-more {
  margin-left: -1px;
  border-top-left-radius: 0;
  border-bottom-left-radius: 0;
}
.qcb-menu {
  min-width: 220px;
  max-width: 320px;
}
.qcb-divider {
  margin: 4px 0;
}
.qcb-group-title {
  padding: 4px 12px;
  font-size: 12px;
  color: var(--color-text-3);
}
.qcb-empty {
  padding: 6px 12px;
  font-size: 12px;
  color: var(--color-text-3);
}
.qcb-list {
  max-height: 320px;
  overflow-y: auto;
}
.qcb-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.qcb-check {
  color: var(--color-success-6);
  flex-shrink: 0;
}
.qcb-del {
  color: var(--color-text-3);
  cursor: pointer;
}
.qcb-del:hover {
  color: var(--color-danger-6);
}
</style>
