<template>
  <div class="workbench" :class="{ 'workbench--fullscreen': fullscreen }">
    <div class="wb-banner">
      <div class="wb-hello">
        <div class="wb-hello-title">{{ hello }}</div>
        <div class="wb-hello-meta">
          <span v-if="isNamed" class="wb-name-chip">
            <icon-park type="workbench" :size="13" /> {{ currentLabel }}
          </span>
          <span class="wb-hello-date">{{ todayLabel }}</span>
        </div>
      </div>
      <a-space :size="4">
        <!-- 左：进入/退出编辑态（现有按钮；命名工作台仅系统角色可编辑） -->
        <a-tooltip v-if="showShare" :content="editing ? '完成' : isNamed ? '编辑命名工作台' : '自定义工作台'">
          <a-button type="text" class="wb-icon-btn" :disabled="!canEditToggle" @click="toggleEdit">
            <icon-park :type="editing ? 'check' : 'setting-config'" :size="16" />
          </a-button>
        </a-tooltip>
        <!-- 右：▾ 弹出菜单（OSC-260902ef43；样式对齐 QueryComboButton）：发布/重命名/删除/分隔符/工作台切换 -->
        <a-dropdown v-if="showNamedMenu" trigger="click" @select="onNamedSelect">
          <a-button type="text" class="wb-icon-btn wb-icon-btn--caret" aria-label="工作台操作">
            <icon-park type="down" :size="12" />
          </a-button>
          <template #content>
            <div class="wb-named-menu">
              <a-doption value="__publish" :disabled="!canPublish">
                <template #icon><icon-park type="save" /></template>
                发布…
              </a-doption>
              <a-doption value="__rename" :disabled="!canRename">
                <template #icon><icon-park type="edit" /></template>
                重命名…
              </a-doption>
              <a-doption value="__delete" :disabled="!canDelete" class="danger">
                <template #icon><icon-park type="delete" /></template>
                删除…
              </a-doption>
              <a-divider class="wb-divider" />
              <div class="wb-group-title">切换工作台</div>
              <a-doption value="__default" :class="{ 'wb-option--checked': !isNamed }">
                <icon-park v-if="!isNamed" type="check" class="wb-check" />
                默认工作台
              </a-doption>
              <a-doption
                v-for="n in namedList"
                :key="n.slug"
                :value="'__named:' + n.slug"
                :class="{ 'wb-option--checked': isNamed && currentSlug === n.slug }"
              >
                <icon-park v-if="isNamed && currentSlug === n.slug" type="check" class="wb-check" />
                {{ n.title || n.slug }}
              </a-doption>
            </div>
          </template>
        </a-dropdown>
        <a-tooltip v-if="showShare && !isNamed" content="恢复默认">
          <!-- span：disabled 时按钮不接收指针事件，保证 tooltip 仍可显示 -->
          <span v-if="!isNamed" class="wb-icon-wrap">
            <a-button
              type="text"
              class="wb-icon-btn"
              :disabled="!canRestore"
              @click="restoreDefault"
            >
              <icon-park type="undo" :size="16" />
            </a-button>
          </span>
        </a-tooltip>
        <ShareViewPopover
          v-if="showShare"
          :visible="sharePopoverVisible"
          share-kind="workbench"
          :type-path="shareTypePath"
          :slug="currentSlug"
          title="分享当前看板"
          position="br"
          @update:visible="sharePopoverVisible = $event"
        >
          <a-button type="text" class="wb-icon-btn" aria-label="分享" title="分享">
            <icon-park type="share" :size="16" />
          </a-button>
        </ShareViewPopover>
        <a-tooltip :content="fullscreen ? '退出全屏 (Esc)' : '全屏'">
          <a-button
            type="text"
            class="wb-icon-btn"
            :class="{ 'wb-icon-btn--on': fullscreen }"
            @click="toggleFullscreen"
          >
            <icon-park :type="fullscreen ? 'off-screen' : 'full-screen'" :size="16" />
          </a-button>
        </a-tooltip>
      </a-space>
    </div>
    <a-alert v-if="loadError" type="warning" show-icon class="wb-alert">{{ loadError }}</a-alert>
    <a-spin :loading="loading" class="wb-spin">
      <WidgetHost />
    </a-spin>
    <!-- 命名工作台空槽：只读展示占位（默认工作台空墙由 WidgetHost 提供添加入口） -->
    <div v-if="isNamed && !(dashboard.widgets && dashboard.widgets.length) && !loading" class="wb-empty">
      该命名工作台尚未配置或为空
    </div>

    <!-- 发布/另存为 命名工作台（OSC-260902ef43） -->
    <a-modal
      v-model:visible="publishVisible"
      title="发布为命名工作台"
      :on-before-ok="confirmPublish"
      ok-text="发布并挂载菜单"
      @cancel="publishVisible = false"
    >
      <a-alert v-if="publishError" type="error" show-icon class="wb-modal-alert">{{ publishError }}</a-alert>
      <a-form layout="vertical">
        <a-form-item label="标题" required>
          <a-input v-model="publishTitle" maxlength="40" placeholder="如：运营看板（≤40 字，将作为菜单名）" />
        </a-form-item>
        <a-form-item label="标识 slug" required>
          <a-input
            v-model="publishSlug"
            placeholder="小写字母开头，仅小写字母/数字/中划线，≤32 位"
            @input="publishSlug = publishSlug.toLowerCase()"
          />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- 重命名 命名工作台 -->
    <a-modal
      v-model:visible="renameVisible"
      title="重命名命名工作台"
      :on-before-ok="confirmRename"
      ok-text="重命名"
      @cancel="renameVisible = false"
    >
      <a-form layout="vertical">
        <a-form-item label="标题" required>
          <a-input v-model="renameTitle" maxlength="40" placeholder="≤40 字，将同步菜单显示名" />
        </a-form-item>
      </a-form>
    </a-modal>

    <!-- 删除 命名工作台（连带移除系统菜单项） -->
    <a-modal
      v-model:visible="deleteVisible"
      title="删除命名工作台"
      :on-before-ok="confirmDelete"
      ok-text="删除"
      :ok-button-props="{ status: 'danger' }"
      @cancel="deleteVisible = false"
    >
      <p class="wb-delete-tip">
        将删除「{{ currentLabel }}」及其对应系统菜单项，且不可恢复。删除后回到默认工作台。
      </p>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import WidgetHost from '@/features/widget/WidgetHost.vue';
import ShareViewPopover from '@/views/crud/ShareViewPopover.vue';
import { useWorkbench } from './useWorkbench';

defineOptions({ name: 'Workbench' });

/** 命名工作台 slug（OSC-260902ef43）：空 = 默认工作台（/home 个人墙）；有值 = /Workbench/{slug} 只读命名槽 */
const props = defineProps<{ slug?: string }>();

const {
  loading,
  editing,
  loadError,
  hello,
  todayLabel,
  dashboard,
  isNamed,
  currentSlug,
  currentLabel,
  canEditToggle,
  canRestore,
  canPublish,
  canRename,
  canDelete,
  showNamedMenu,
  namedList,
  fullscreen,
  showShare,
  sharePopoverVisible,
  shareTypePath,
  publishVisible,
  publishTitle,
  publishSlug,
  publishError,
  renameVisible,
  renameTitle,
  deleteVisible,
  toggleEdit,
  toggleFullscreen,
  restoreDefault,
  onNamedSelect,
  confirmPublish,
  confirmRename,
  confirmDelete,
} = useWorkbench(props.slug ?? '');
</script>

<style scoped>
.workbench {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
}
/* 全屏：固定铺满视口，覆盖系统顶部及左侧导航栏；
   z-index 低于 Arco 弹层（1000+），全屏期间的抽屉/弹窗/气泡仍可正常显示。
   暗色 --color-fill-2 为半透明：须先铺不透明 bg-1 再叠 fill-2，否则顶栏/侧栏会透出 */
.workbench--fullscreen {
  position: fixed;
  inset: 0;
  z-index: 900;
  overflow: auto;
  background-color: var(--color-bg-1);
  background-image: linear-gradient(var(--color-fill-2), var(--color-fill-2));
  padding: 16px;
  box-sizing: border-box;
}
.wb-banner {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;
}
.wb-hello-title {
  font-size: 22px;
  font-weight: 600;
  color: var(--color-text-1);
  line-height: 1.3;
}
.wb-hello-date {
  font-size: 13px;
  color: var(--color-text-3);
  line-height: 1.5;
  white-space: nowrap;
}
.wb-icon-wrap {
  display: inline-flex;
}
.wb-icon-btn {
  width: 28px !important;
  height: 28px !important;
  padding: 0 !important;
  color: var(--color-text-2) !important;
}
.wb-icon-btn:hover:not(:disabled) {
  color: rgb(var(--primary-6)) !important;
}
.wb-icon-btn:disabled {
  color: var(--color-text-4) !important;
}
.wb-icon-btn--on {
  color: rgb(var(--primary-6)) !important;
}
.wb-alert {
  margin: 0;
}
.wb-spin {
  width: 100%;
  min-height: 120px;
}
.wb-hello-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  margin-top: 2px;
  line-height: 1.5;
}
.wb-name-chip {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 1px 10px;
  border-radius: 10px;
  font-size: 12px;
  line-height: 1.5;
  color: rgb(var(--primary-6));
  background-color: var(--color-fill-2);
}
.wb-name-chip :deep(svg) {
  display: block;
}
.wb-icon-btn--caret {
  width: 20px !important;
}
.wb-named-menu {
  min-width: 180px;
}
.wb-divider {
  margin: 4px 0;
}
.wb-group-title {
  padding: 6px 12px 2px;
  font-size: 12px;
  color: var(--color-text-3);
}
.wb-check {
  color: rgb(var(--primary-6));
}
.wb-option--checked {
  color: rgb(var(--primary-6));
}
.wb-empty {
  text-align: center;
  color: var(--color-text-3);
  padding: 40px 0;
  font-size: 14px;
}
.wb-modal-alert {
  margin-bottom: 12px;
}
.wb-delete-tip {
  margin: 0;
  color: var(--color-text-2);
}
</style>
