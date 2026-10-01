<template>
  <!-- hover 表头时显示各列边界分隔线，辅助定位列宽拖拽区（VTable 原生分隔线仅拖动时显示） -->
  <div class="cube-list-table-wrap" :style="{ height: height + 'px' }">
    <div ref="hostRef" class="cube-list-table"></div>
    <!-- VTable 构造会清空宿主，空态放在宿主外；加载中不提示，避免先闪「暂无数据」 -->
    <div v-if="!loading && records.length === 0" class="cube-list-table-empty">
      <a-empty description="暂无数据" />
    </div>
  </div>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import type { ColumnPref, ViewFormatRule } from '@/core/utils/viewProfile';
import type { ListTableColumnDef } from './useListTable';
import { useListTable } from './useListTable';

const props = withDefaults(
  defineProps<{
    records: Record<string, unknown>[];
    columns: ListTableColumnDef[];
    rowKey: string;
    selectedKeys?: (string | number)[];
    showCheckbox?: boolean;
    height?: number;
    canEdit?: boolean;
    canDelete?: boolean;
    canViewDetail?: boolean;
    /** 魔方设置 EnableTableDoubleClick；false 时禁用双击进详情（默认 true） */
    enableTableDoubleClick?: boolean;
    showExpand?: boolean;
    enableSort?: boolean;
    /** 行操作列额外按钮（自动化 button 规则） */
    automationButtons?: { id: number | string; name: string }[];
    /** GetPage 合成 Url/dataAction 自定义链接 */
    opsCustomLinks?: {
      name: string;
      label: string;
      url: string;
      target?: string;
      dataAction?: string;
    }[];
    /** OA 审批行操作（OSC-26090347f1）：类型启用时渲染 提交/进度（行级按 __wf* 过滤） */
    workflowButtons?: { submit?: boolean; progress?: boolean };
    /** 服务端排序状态（可多列）；用于表头升/降序图标，不走 VTable 内部排序 */
    sortState?: { field: string; desc: boolean }[] | { field: string; desc: boolean } | null;
    /** 树视图：启用 VTable hierarchy（行含 children） */
    hierarchy?: boolean;
    /** 分组视图（OSC-0015）：records 含 __groupHeader 组头节点行，组头跨列显示并浅色区分 */
    grouped?: boolean;
    /** 分组字段名列表（OSC-0015 重构）：非空时启用 VTable 原生 groupBy 分组（参考官方 list-table-group-checkbox），
     *  checkbox 置于 rowSeriesNumber 列（每行最前面），组标题行左侧显示 checkbox 并与子行选中状态级联同步 */
    groupFields?: string[];
    /** 分组值显示标签翻译（OSC-0015：如 dataSource 枚举翻译）；返回 undefined 则回落显示原值 */
    groupLabelOf?: (field: string, value: unknown) => string | undefined;
    /** 条件填色规则 */
    formatRules?: ViewFormatRule[];
    formatFields?: FieldMeta[];
    /** 用于系统角色隐藏删除（OSC-260824fc7c） */
    typePath?: string;
    /** 列表请求进行中。空记录且加载中时不显示空态 */
    loading?: boolean;
  }>(),
  {
    selectedKeys: () => [],
    showCheckbox: false,
    height: 480,
    canEdit: false,
    canDelete: false,
    canViewDetail: true,
    enableTableDoubleClick: true,
    showExpand: false,
    enableSort: true,
    automationButtons: () => [],
    opsCustomLinks: () => [],
    sortState: null,
    hierarchy: false,
    grouped: false,
    loading: false,
  },
);

const emit = defineEmits<{
  rowClick: [row: Record<string, unknown>];
  rowDblClick: [row: Record<string, unknown>];
  selectionChange: [keys: (string | number)[]];
  columnsChange: [cols: ColumnPref[]];
  sortChange: [payload: { field: string; desc: boolean } | null];
  action: [payload: {
    action: string;
    row: Record<string, unknown>;
    clientX?: number;
    clientY?: number;
  }];
  cellLink: [payload: { url: string; target?: string; label?: string; row: Record<string, unknown> }];
  toggleEnable: [row: Record<string, unknown>, field: string];
  /** 滚动接近底部（剩余不足 200px）时触发，供父级增量加载更多行（列表/树懒加载） */
  scrollBottom: [];
}>();

const { hostRef } = useListTable(props, emit);
</script>

<style scoped>
.cube-list-table-wrap {
  position: relative;
  width: 100%;
  max-width: 100%;
  min-width: 0;
  min-height: 320px;
}

.cube-list-table {
  position: relative;
  width: 100%;
  height: 100%;
  max-width: 100%;
  min-width: 0;
  border: none;
  overflow: hidden;
  background: var(--color-bg-2);
  box-sizing: border-box;
}

.cube-list-table-empty {
  position: absolute;
  z-index: 2;
  /* 让开表头，提示落在表体空白里 */
  top: 40px;
  right: 0;
  bottom: 0;
  left: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  pointer-events: none;
}

/* hover 表头时的列边界分隔线层：JS 动态创建（无 scoped 属性），需 :deep 匹配；不拦截鼠标，浮于 VTable canvas 之上 */
.cube-list-table :deep(.cube-table-separators) {
  position: absolute;
  inset: 0;
  pointer-events: none;
  z-index: 10;
  display: none;
}
.cube-list-table :deep(.cube-table-separators.show) {
  display: block;
}
.cube-list-table :deep(.cube-table-separators .sep) {
  position: absolute;
  top: 0;
  width: 2px;
  /* 高度由 JS 设为表头高度（仅表头区域显示，不贯穿数据区） */
  height: 100%;
  /*
   * 颜色跟随当前主题的 Secondary 色系（light/dark 自动切换）。
   * 用 --color-secondary-hover 而非 --color-secondary：亮色主题下 secondary=#F2F3F5 与表头背景同色几乎不可见，
   * hover 档更深一档，保证分隔线可辨识。
   */
  background: var(--color-secondary-hover);
  border-left: 1px solid var(--color-secondary-hover);
  box-sizing: border-box;
}

/* 用户自定义左/右冻结时的边界示意线：1px、无阴影；默认冻结（勾选/操作列）不画 */
.cube-list-table :deep(.cube-table-freeze-lines) {
  position: absolute;
  inset: 0;
  pointer-events: none;
  z-index: 9;
}
.cube-list-table :deep(.cube-table-freeze-lines .freeze-line) {
  position: absolute;
  top: 0;
  width: 1px;
  background: var(--color-border-2);
  display: none;
}
.cube-list-table :deep(.cube-table-freeze-lines .freeze-line.is-on) {
  display: block;
}
</style>
