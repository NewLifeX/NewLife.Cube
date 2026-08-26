<template>
  <!-- hover 表头时显示各列边界分隔线，辅助定位列宽拖拽区（VTable 原生分隔线仅拖动时显示） -->
  <div ref="hostRef" class="cube-list-table" :style="{ height: height + 'px' }"></div>
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
    /** 服务端排序状态；用于表头升/降序图标（不走 VTable 内部排序） */
    sortState?: { field: string; desc: boolean } | null;
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
  cellLink: [payload: { url: string; target?: string; row: Record<string, unknown> }];
  toggleEnable: [row: Record<string, unknown>, field: string];
  /** 滚动接近底部（剩余不足 200px）时触发，供父级增量加载更多行（列表/树懒加载） */
  scrollBottom: [];
}>();

const { hostRef } = useListTable(props, emit);
</script>

<style scoped>
.cube-list-table {
  position: relative;
  width: 100%;
  max-width: 100%;
  min-width: 0;
  min-height: 320px;
  border: none;
  overflow: hidden;
  background: var(--color-bg-2);
  box-sizing: border-box;
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
