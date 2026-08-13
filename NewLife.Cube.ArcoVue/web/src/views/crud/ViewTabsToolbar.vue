<template>
  <div class="view-tabs-toolbar">
    <!-- 视图 Tab：使用 Arco Tabs；新建视图（+）在最后一个 Tab 页签旁（editable add 按钮），全屏开关在 #extra 附加区最右 -->
    <a-tabs
      class="view-tabs"
      :active-key="activeId"
      editable
      show-add-button
      @change="onTabChange"
      @add="onAddClick"
    >
      <a-tab-pane v-for="v in views" :key="v.id" :closable="false">
        <template #title>
          <span class="view-tab-inner">
            <a-tooltip :content="VIEW_KIND_LABEL[v.view] || v.view">
              <icon-park :type="VIEW_KIND_ICONS[v.view]" class="view-tab-kind" />
            </a-tooltip>
            <span class="view-tab-name">{{ v.name }}</span>
            <a-dropdown v-if="v.id === activeId" trigger="click" @select="onMenuSelect">
              <button type="button" class="view-tab-menu" title="视图菜单" @click.stop>
                <icon-park type="more-one" />
              </button>
              <template #content>
                <a-doption value="rename">
                  <icon-park type="edit" class="menu-item-icon" />
                  重命名
                </a-doption>
                <a-doption value="config">
                  <icon-park type="setting" class="menu-item-icon" />
                  自定义配置
                </a-doption>
                <a-doption value="duplicate">
                  <icon-park type="copy" class="menu-item-icon" />
                  复制
                </a-doption>
                <a-doption value="delete" :disabled="views.length <= 1">
                  <icon-park type="delete" class="menu-item-icon" />
                  删除
                </a-doption>
                <a-doption divider v-if="isAdmin" value="saveAsDefault">
                  <icon-park type="save" class="menu-item-icon" />
                  存为默认{{ defaultViewKindName(activeViewKind) }}视图
                </a-doption>
                <a-doption value="reset">
                  <icon-park type="undo" class="menu-item-icon" />
                  恢复默认
                </a-doption>
              </template>
            </a-dropdown>
          </span>
        </template>
      </a-tab-pane>

      <!-- 新建视图：放置在最后一个 Tab 页签旁边（Arco editable add 按钮） -->
      <template #add-icon>
        <span>+</span>
      </template>

      <!-- extra：全屏开关（Tab 组件附加区最右） -->
      <template #extra>
        <a-tooltip :content="fullscreen ? '退出全屏 (Esc)' : '全屏'">
          <button
            type="button"
            class="view-fullscreen"
            :class="{ active: fullscreen }"
            @click="$emit('toggleFullscreen')"
          >
            <icon-park :type="fullscreen ? 'off-screen' : 'full-screen'" />
          </button>
        </a-tooltip>
      </template>
    </a-tabs>

    <!-- 创建视图类型下拉：锚点定位到 add 按钮下方（保持"点 + → 选类型 → 命名"交互） -->
    <a-dropdown
      trigger="click"
      :popup-visible="createPopupVisible"
      @select="onCreateSelect"
      @popup-visible-change="onCreatePopupChange"
    >
      <span ref="createAnchorRef" class="create-dropdown-anchor" />
      <template #content>
        <a-doption
          v-for="opt in createOptions"
          :key="opt.kind"
          :value="opt.kind"
          :disabled="!opt.ok"
        >
          <icon-park :type="VIEW_KIND_ICONS[opt.kind]" class="menu-item-icon" />
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
import type { FieldMeta } from '@/core/types/field';
import type { NamedView, ViewKind } from '@/core/utils/viewProfile';
import { useViewTabsToolbar } from './useViewTabsToolbar';

const props = defineProps<{
  views: NamedView[];
  activeId: string;
  fields: FieldMeta[];
  typePath: string;
  /** 系统管理员可把当前视图保存为该实体默认（全局模板）视图 */
  isAdmin?: boolean;
  /** 当前视图是否处于全屏展示状态（切换按钮图标/提示随之变化） */
  fullscreen?: boolean;
}>();

const emit = defineEmits<{
  switch: [id: string];
  create: [kind: ViewKind, name: string];
  rename: [id: string, name: string];
  remove: [id: string];
  duplicate: [id: string];
  reset: [];
  openConfig: [];
  saveAsDefault: [];
  toggleFullscreen: [];
}>();

const {
  VIEW_KIND_LABEL,
  VIEW_KIND_ICONS,
  defaultViewKindName,
  activeViewKind,
  createOptions,
  nameModalVisible,
  nameModalTitle,
  nameDraft,
  createPopupVisible,
  createAnchorRef,
  closeNameModal,
  submitName,
  onMenuSelect,
  onAddClick,
  onCreatePopupChange,
  onCreateSelect,
  onTabChange,
} = useViewTabsToolbar(props, emit);
</script>

<style scoped>
.view-tabs-toolbar {
  display: flex;
  align-items: center;
  gap: 2px;
  min-width: 0;
}
/* Arco Tabs 容器：占满整个视图区宽度，使 nav 横线贯穿到视图右端（而非仅 Tab 页签宽度），
   extra 的 +/全屏按钮保持在右端，Tab 数量多时 Arco 内部横向滚动 */
.view-tabs {
  flex: 1;
  min-width: 0;
}
/* Tab 标题：图标 + 名称 + 视图菜单，垂直居中对齐 */
.view-tab-inner {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  line-height: 1.2;
}
.view-tab-name {
  font-size: 13px;
  white-space: nowrap;
}
.view-tab-kind {
  display: inline-flex;
  align-items: center;
  font-size: 14px;
  opacity: 0.75;
  line-height: 1;
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
/* 视图菜单项图标：与文字水平居中对齐 */
.menu-item-icon {
  display: inline-flex;
  align-items: center;
  margin-right: 6px;
  font-size: 14px;
  vertical-align: -2px;
}
.view-tab-menu:hover {
  opacity: 1;
}
/* Arco editable add 按钮（+）：样式与"全屏"按钮一致（透明圆角、15px 字号），
   align-self:center 使其与全屏按钮水平位置对齐（nav 内垂直居中） */
.view-tabs :deep(.arco-tabs-nav-add-btn) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  margin-left: 4px;
  padding: 0;
  box-sizing: border-box;
  border: none;
  background: transparent;
  border-radius: 6px;
  cursor: pointer;
  color: var(--color-text-3);
  font-size: 15px;
  line-height: 1;
  align-self: center;
}
.view-tabs :deep(.arco-tabs-nav-add-btn:hover) {
  color: rgb(var(--primary-6));
  background: var(--color-fill-2);
}
/* 创建视图类型下拉锚点：不可见定位元素（fixed 定位到 + 按钮下方） */
.create-dropdown-anchor {
  position: fixed;
  width: 0;
  height: 0;
  pointer-events: none;
  z-index: 1000;
}
/* 全屏开关按钮：Tab 组件附加区（#extra）最右侧，激活态用主色强调 */
.view-fullscreen {
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
  font-size: 15px;
  line-height: 1;
}
.view-fullscreen:hover {
  color: rgb(var(--primary-6));
  background: var(--color-fill-2);
}
.view-fullscreen.active {
  color: rgb(var(--primary-6));
  background: var(--color-primary-light-1);
}
/* Arco Tabs 微调：extra 区（新建视图 +）与 Tab 垂直居中对齐 */
.view-tabs :deep(.arco-tabs-nav-extra) {
  display: flex;
  align-items: center;
  gap: 2px;
  margin-left: 4px;
}
/* Tab 组件自身的横线：Arco Tabs nav 下边框，贯穿 Tab 组件（占满整个视图区）下方 */
.view-tabs :deep(.arco-tabs-nav) {
  border-bottom: 1px solid var(--color-border-2);
}
/* Arco Tabs 内容区：列表页 TabPane 无实际内容，去掉默认顶部内边距（避免多出空隙） */
.view-tabs :deep(.arco-tabs-content) {
  padding-top: 0;
}
</style>
