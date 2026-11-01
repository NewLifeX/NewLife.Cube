<script setup lang="ts">
/**
 * CubeTable「设置列」入口（表格统计行右侧的齿轮）
 *
 * 只负责渲染与交互转发：显隐合并、localStorage 读写在 `useCubeTableColumns`，
 * 组件不碰存储（避免 UI 组件里藏持久化逻辑）。
 */
import { ArrowDown, ArrowUp, Setting } from '@element-plus/icons-vue';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

const props = withDefaults(
  defineProps<{
    /** 可配置列（key/label） */
    items?: Array<{ key: string; label: string }>;
    /** 当前显隐：undefined 视为显示（新增列默认可见） */
    visibility?: Record<string, boolean>;
  }>(),
  { items: () => [], visibility: () => ({}) },
);

const emit = defineEmits<{
  /** 勾选/取消某一列 */
  set: [key: string, visible: boolean];
  /** 列序变化，参数为完整列键顺序 */
  order: [keys: string[]];
  /** 恢复默认 */
  reset: [];
}>();

/** 上移/下移一列，抛出完整顺序由宿主持久化 */
function move(index: number, delta: number): void {
  const keys = props.items.map((item) => item.key);
  const target = index + delta;
  if (target < 0 || target >= keys.length) return;
  [keys[index], keys[target]] = [keys[target], keys[index]];
  emit('order', keys);
}
</script>

<template>
  <el-popover placement="bottom-end" :width="220" trigger="click">
    <!-- 不套 el-tooltip：它的触发指令落在 fragment 根上会告警并失效，按钮自身的 title 已够用 -->
    <template #reference>
      <el-button class="ctcs-trigger" :icon="Setting" circle :title="t('grid.columnSetting')" />
    </template>
    <div class="ctcs-title">{{ t('grid.visibleColumns') }}</div>
    <!-- 列数多 + 大字号时整体高度会超出视口，列表区单独滚动，标题与底部操作常驻 -->
    <div class="ctcs-list">
      <div v-for="(item, index) in items" :key="item.key" class="ctcs-item">
        <el-checkbox
          :model-value="visibility[item.key] !== false"
          @change="(v: unknown) => emit('set', item.key, v === true)"
        >
          {{ item.label }}
        </el-checkbox>
        <span class="ctcs-actions">
          <el-button
            link
            size="small"
            :icon="ArrowUp"
            :disabled="index === 0"
            :title="t('grid.moveUp')"
            @click="move(index, -1)"
          />
          <el-button
            link
            size="small"
            :icon="ArrowDown"
            :disabled="index === items.length - 1"
            :title="t('grid.moveDown')"
            @click="move(index, 1)"
          />
        </span>
      </div>
    </div>
    <div class="ctcs-footer">
      <el-button link type="primary" @click="emit('reset')">{{ t('grid.resetDefault') }}</el-button>
    </div>
  </el-popover>
</template>

<style scoped>
.ctcs-title {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  padding-bottom: 6px;
}
/* 列数多 + 大字号时整体高度会超出视口：列表区限高滚动，标题/底部操作常驻。
   上限同时受两个条件约束：
     - 60vh：小屏/大字号下不超过视口，保证滚动条本身可见可拖；
     - 520px：大屏下不铺满整屏，列少时又不会平白出现滚动条（19 列约 494px）。
   触发按钮在表格工具栏，面板向下展开，因此实际可用高度已由这两个上限覆盖。 */
.ctcs-list {
  max-height: min(520px, 60vh);
  overflow-y: auto;
  overscroll-behavior: contain;
  /* 滚动时留一点右侧空隙，避免滚动条压住行内按钮 */
  margin-right: -4px;
  padding-right: 4px;
}
.ctcs-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 4px;
  padding: 1px 4px;
}
.ctcs-actions {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  opacity: 0.65;
}
.ctcs-actions:hover {
  opacity: 1;
}
.ctcs-footer {
  border-top: 1px solid var(--el-border-color-lighter);
  margin-top: 6px;
  padding-top: 6px;
  text-align: right;
}
</style>
