<script setup lang="ts">
/**
 * 操作列：外部插槽动作的渲染与收起
 *
 * `#row-actions` 由页面自己写按钮，形态可以是纯文字 / 文字+图标 / 纯图标。
 * 这里按「一个节点 = 一个操作」把插槽内容拆成两段（`splitRowActions`，与内置动作同一收起规则）：
 * ≤3 个全部直显；>3 个只直显前 3 个，其余进「更多」下拉。
 *
 * 下拉项就是原按钮本体（不是文本克隆），点击仍走页面自己的回调。
 */
import { computed } from 'vue';
import { ArrowDown } from '@element-plus/icons-vue';
import RenderVNodes from './RenderVNodes';
import { splitRowActions } from './cubeTable.logic';
import type { VNode } from 'vue';

const props = defineProps<{
  /** 本行的插槽动作节点（每次渲染由宿主重新调插槽函数生成） */
  nodes: VNode[];
}>();

const parts = computed(() => splitRowActions(props.nodes));
</script>

<template>
  <span class="ctg-actions">
    <RenderVNodes v-for="(node, index) in parts.direct" :key="index" :nodes="[node]" />
    <el-dropdown v-if="parts.overflow.length" trigger="click" class="ctg-more">
      <el-button link type="primary">
        更多<el-icon><ArrowDown /></el-icon>
      </el-button>
      <template #dropdown>
        <el-dropdown-menu>
          <el-dropdown-item v-for="(node, index) in parts.overflow" :key="index" class="ctg-more-item">
            <RenderVNodes :nodes="[node]" />
          </el-dropdown-item>
        </el-dropdown-menu>
      </template>
    </el-dropdown>
  </span>
</template>

<style scoped>
/* 与 CubeTableGrid 内置分支的 .ctg-actions 同口径：间距由 flex gap 提供，取消按钮默认 margin
   （插槽动作原先直挂单元格，会带上 el-button 默认 margin-left:12px，实测把列宽顶爆 4px） */
.ctg-actions {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}
.ctg-actions :deep(.el-button + .el-button) {
  margin-left: 0;
}
.ctg-more {
  vertical-align: middle;
}
/* 下拉项里的按钮占满一行，纯图标与纯文字都能整行点中 */
.ctg-more-item :deep(.el-button) {
  width: 100%;
  justify-content: flex-start;
  margin-left: 0;
}
</style>
