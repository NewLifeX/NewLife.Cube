<script setup lang="ts">
/**
 * CubeTable「设置列」入口（表格统计行右侧的齿轮）
 *
 * 只负责渲染与交互转发：显隐合并、localStorage 读写在 `useCubeTableColumns`，
 * 组件不碰存储（避免 UI 组件里藏持久化逻辑）。
 */
import { Setting } from '@element-plus/icons-vue';

withDefaults(
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
  /** 恢复默认 */
  reset: [];
}>();
</script>

<template>
  <el-popover placement="bottom-end" :width="220" trigger="click">
    <!-- 不套 el-tooltip：它的触发指令落在 fragment 根上会告警并失效，按钮自身的 title 已够用 -->
    <template #reference>
      <el-button class="ctcs-trigger" :icon="Setting" circle title="设置列" />
    </template>
    <div class="ctcs-title">展示列</div>
    <div v-for="item in items" :key="item.key" class="ctcs-item">
      <el-checkbox
        :model-value="visibility[item.key] !== false"
        @change="(v: unknown) => emit('set', item.key, v === true)"
      >
        {{ item.label }}
      </el-checkbox>
    </div>
    <div class="ctcs-footer">
      <el-button link type="primary" @click="emit('reset')">恢复默认</el-button>
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
  padding: 1px 4px;
}
.ctcs-footer {
  border-top: 1px solid var(--el-border-color-lighter);
  margin-top: 6px;
  padding-top: 6px;
  text-align: right;
}
</style>
