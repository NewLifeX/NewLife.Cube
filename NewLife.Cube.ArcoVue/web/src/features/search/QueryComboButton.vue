<template>
  <div class="query-combo-button">
    <!-- ▾ 菜单按钮（OSC-260830a1b2 查询簇第三键）：重置查询条件 / 预定义查询等更多操作。查询与自定义按钮在调用方工具栏组装 -->
    <a-dropdown trigger="click" @select="onSelect">
      <icon-park type="down" class="qcb-trigger" />
      <template #content>
        <div class="qcb-menu">
          <!-- 自定义查询（OSC-260830a1b2）：打开条件构建器（键盘/小屏备援），不在工具栏单独出按钮；
               最近搜索已改为输入时自动匹配下拉，不再占用菜单；customEnabled=false 时入口隐藏（视图配置-工具栏「筛选」） -->
          <a-doption v-if="customEnabled !== false" value="__custom">
            <template #icon><icon-park type="message-search" /></template>
            自定义查询
          </a-doption>
          <a-doption value="__save" :disabled="!canSave">
            <template #icon><icon-park type="save" /></template>
            保存当前查询为预定义…
          </a-doption>
          <a-doption value="__rename" :disabled="!canRename">
            <template #icon><icon-park type="edit" /></template>
            重命名
          </a-doption>
          <a-doption value="__reset">
            <template #icon><icon-park type="refresh" /></template>
            重置查询条件
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

        </div>
      </template>
    </a-dropdown>

    <a-modal
      v-model:visible="modalVisible"
      :title="modalTitle"
      :width="420"
      :on-before-ok="onModalOk"
      @cancel="modalVisible = false"
    >
      <a-input
        ref="modalInputRef"
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
  /** 是否提供「自定义查询」入口（视图配置-工具栏「筛选」开关；缺省 true） */
  customEnabled?: boolean;
}>();

const emit = defineEmits<{
  search: [];
  reset: [];
  /** 打开自定义查询构建器（OSC-260830a1b2） */
  custom: [];
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
  modalInputRef,
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
/* ▾ 下拉触发器：内嵌在关键字输入框 suffix，仅图标、可点击 */
.qcb-trigger {
  cursor: pointer;
  color: var(--color-text-2);
  font-size: 13px;
}
.qcb-trigger:hover {
  color: rgb(var(--primary-6));
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
