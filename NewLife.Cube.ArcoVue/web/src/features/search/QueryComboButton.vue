<template>
  <div class="query-combo-button">
    <!-- 主按钮：点击直接执行查询（OSC-0016） -->
    <a-button type="primary" class="qcb-search" @click="emit('search')">
      <template #icon><icon-park type="search" /></template>
      查询
    </a-button>
    <!-- 下拉按钮：重置查询参数 / 预定义查询等更多操作 -->
    <a-dropdown trigger="click" @select="onSelect">
      <a-button type="primary" class="qcb-more">
        <icon-park type="down" />
      </a-button>
      <template #content>
        <div class="qcb-menu">
          <a-doption value="__reset">
            <template #icon><icon-park type="refresh" /></template>
            重置查询参数
          </a-doption>
          <a-doption v-if="hasMoreFields" value="__toggle">
            <template #icon>
              <icon-park v-if="expanded" type="up" />
              <icon-park v-else type="down" />
            </template>
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
                <icon-park v-if="isApplied(q.id)" type="check" class="qcb-check" />
                {{ q.name }}
              </span>
              <template #suffix>
                <a-popconfirm content="确认删除该预定义查询？" @ok="onDelete(q.id)">
                  <icon-park type="delete" class="qcb-del" @click.stop />
                </a-popconfirm>
              </template>
            </a-doption>
          </div>

          <a-divider class="qcb-divider" />
          <a-doption value="__save" :disabled="!canSave">
            <template #icon><icon-park type="save" /></template>
            保存当前查询为预定义…
          </a-doption>
          <a-doption value="__rename" :disabled="!canRename">
            <template #icon><icon-park type="edit" /></template>
            重命名当前查询
          </a-doption>
          <a-doption value="__delete" :disabled="!canRename">
            <template #icon><icon-park type="delete" /></template>
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
import type { SavedQuery } from '@/core/utils/viewProfile';
import { useQueryComboButton } from './useQueryComboButton';

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

const {
  canRename,
  isApplied,
  modalVisible,
  modalTitle,
  modalName,
  onSelect,
  onModalOk,
  onDelete,
} = useQueryComboButton(props, emit);
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
