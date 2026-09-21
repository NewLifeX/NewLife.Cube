<template>
  <!-- 有部件才渲染墙面；insight 空墙不占位（入口在视图配置/高级菜单） -->
  <div v-if="hasWidgets" class="widget-host">
    <WidgetGrid :widgets="widgets">
      <template #default="{ widget }">
        <div class="widget-shell" :class="{ 'widget-shell--with-add': showInlineAdd(widget) }">
          <div v-if="canEdit" class="widget-shell-ops">
            <a-button-group class="widget-ops-group" size="mini">
              <a-tooltip v-if="showInlineAdd(widget)" content="添加部件">
                <a-button
                  type="text"
                  class="widget-ops-btn"
                  :disabled="widgets.length >= maxWidgets"
                  @click="openAdd"
                >
                  <icon-park type="plus" :size="14" />
                </a-button>
              </a-tooltip>
              <a-dropdown trigger="click" position="br">
                <a-button type="text" class="widget-ops-btn widget-ops-btn--caret">
                  <icon-park type="down" :size="12" />
                </a-button>
                <template #content>
                  <a-doption v-if="widget.kind !== 'legacyChart'" @click="openEdit(widget)">
                    <icon-park type="edit" class="menu-item-icon" /> 编辑…
                  </a-doption>
                  <a-doption v-if="widget.kind === 'legacyChart'" @click="openUpgrade(widget)">
                    <icon-park type="upload" class="menu-item-icon" /> 升级为迷你图表
                  </a-doption>
                  <a-doption @click="moveWidget(widget.id, -1)">
                    <icon-park type="left" class="menu-item-icon" /> 左移
                  </a-doption>
                  <a-doption @click="moveWidget(widget.id, 1)">
                    <icon-park type="right" class="menu-item-icon" /> 右移
                  </a-doption>
                  <a-doption class="danger" @click="removeWidget(widget.id)">
                    <icon-park type="delete" class="menu-item-icon" /> 删除
                  </a-doption>
                </template>
              </a-dropdown>
            </a-button-group>
          </div>
          <component :is="resolveComponent(widget)" v-bind="cardProps(widget)" />
        </div>
      </template>
    </WidgetGrid>
  </div>
  <!-- 仅工作台保留空态添加入口；页面仪表盘空时不显示「+添加部件」 -->
  <div v-else-if="canEdit && isWorkbench" class="widget-host widget-host--empty">
    <a-button type="dashed" long @click="openAdd">
      <icon-park type="plus" />
      添加部件
    </a-button>
  </div>
  <!-- 抽屉始终挂载，供视图配置「打开页面仪表盘」/高级菜单在空槽时添加 -->
  <WidgetConfigDrawer
    v-model:visible="configVisible"
    :editing="editing"
    :host-type-path="ctx?.hostTypePath"
    :host-fields="ctx?.listFields ?? []"
    :host-filter-fields="ctx?.hostFilterFields"
    :surface="ctx?.surface ?? 'insight'"
    @save="onConfigSave"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { WidgetInstance } from '@newlifex/api-core';
import WidgetGrid from './WidgetGrid.vue';
import WidgetConfigDrawer from './WidgetConfigDrawer.vue';
import { useWidgetHost } from './useWidgetHost';

const {
  ctx,
  widgets,
  canEdit,
  hasWidgets,
  isWorkbench,
  resolveComponent,
  cardProps,
  configVisible,
  editing,
  maxWidgets,
  openAdd,
  openEdit,
  openUpgrade,
  onConfigSave,
  removeWidget,
  moveWidget,
} = useWidgetHost();

/**
 * 部件行内「+」入口（OSC-260921）：
 * - 多维视图（insight）：无顶层菜单，仅在最后一个部件上保留入口；
 * - 工作台：顶部菜单已有「添加部件…」时不再给行内入口；无该菜单（普通用户）时同样只在最后一个部件保留。
 */
const inlineAddWidgetId = computed(() => {
  const last = widgets.value[widgets.value.length - 1]?.id ?? '';
  if (ctx?.surface === 'insight') return last;
  if (ctx?.surface === 'workbench' && !ctx.topAddEntry) return last;
  return '';
});
function showInlineAdd(w: WidgetInstance) {
  return w.id === inlineAddWidgetId.value;
}

defineExpose({ openAdd });
</script>

<style scoped>
.widget-host {
  min-width: 0;
}
.widget-host--empty {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 48px;
  padding: 8px 0;
}
.widget-shell {
  position: relative;
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  min-height: 0;
  height: 100%;
  overflow: hidden;
  /* 操作条实际宽度（OSC-260921）：仅下拉 22 = 按钮 20 + 组边框 1×2；「+」与下拉并存 48。
     卡片标题行用 calc(var(--widget-ops-w) + 7px) 预留右侧空间，两种形态下按钮间距都约 8px */
  --widget-ops-w: 22px;
}
.widget-shell--with-add {
  --widget-ops-w: 48px;
}
/* 与卡片内标题行水平对齐（卡片 padding-top: 10px，标题行高约 22px） */
.widget-shell-ops {
  position: absolute;
  top: 10px;
  right: 12px;
  z-index: 2;
  height: 22px;
  display: flex;
  align-items: center;
}
/* 默认与视图工具栏 text 按钮一致：无边框底；悬停卡片时才显出按钮组底 */
.widget-ops-group {
  display: inline-flex;
  border-radius: 4px;
  overflow: hidden;
  background: transparent;
  border: 1px solid transparent;
  transition: background 0.15s ease, border-color 0.15s ease, box-shadow 0.15s ease;
}
.widget-shell:hover .widget-ops-group {
  background: var(--color-bg-2);
  border-color: var(--color-border-2);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
}
/* 高度 20 + 组边框 1px×2 = 与 22px 标题行等高，避免按钮底/悬浮底色超出标题行 */
.widget-ops-group :deep(.arco-btn) {
  padding: 0 6px;
  height: 20px;
  border: none;
  border-radius: 0;
  background: transparent;
  color: var(--color-text-3);
}
.widget-shell:hover .widget-ops-group :deep(.arco-btn + .arco-btn),
.widget-shell:hover .widget-ops-group :deep(.arco-btn-group .arco-btn:not(:first-child)) {
  border-left: 1px solid var(--color-border-2);
}
/* IconPark 以行内 svg 渲染，基线对齐会让图形比按钮中心高约 2px；改块级后由按钮 flex 精确居中 */
.widget-ops-group :deep(.i-icon),
.widget-ops-group :deep(.i-icon svg) {
  display: block;
}
.widget-ops-btn:hover {
  color: rgb(var(--primary-6)) !important;
  background: var(--color-fill-2) !important;
}
.widget-ops-btn--caret {
  padding: 0 4px !important;
}
.menu-item-icon {
  margin-right: 6px;
  vertical-align: -2px;
}
:deep(.arco-dropdown-option.danger) {
  color: rgb(var(--danger-6));
}
</style>
