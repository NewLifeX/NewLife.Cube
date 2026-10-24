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
