<script setup lang="ts">
/**
 * 实例流程只读画布（OSC-260922201a 反馈四）：以实例快照（GraphSnapshot）渲染 FlowGram 只读画布，
 * 节点卡片标题栏显示状态（已完成 / 当前 / 未处理）；不可编辑、不弹属性抽屉。
 * 提交预览场景无实例：传定义快照 graphJson，全部按未处理展示。
 * 高度拉伸填满宿主页签（height:100%），画布到抽屉四周由外层 tabs/body 留白保持等距。
 */
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import type { WorkflowInstanceDetail } from '@newlifex/api-core';
import { FlowGramDesigner, type WfNodeState } from './flowgram/FlowGramDesigner';
import { graphToFlowDoc } from './flowgram/flowgramGraph';
import { branchInfosOf, cardsOf } from './wfNodeCard';
import { definitionOutline, definitionPaint, parseSnapshot } from './wfProgressFlow';

const props = withDefaults(
  defineProps<{
    /** 图 JSON：实例快照（graphSnapshot）优先；提交预览传定义快照（publishedGraphJson） */
    graphJson?: string | null;
    /** 实例详情：有则按任务状态上色；无则全部未处理 */
    detail?: WorkflowInstanceDetail | null;
  }>(),
  { graphJson: null, detail: null },
);

const elRef = ref<HTMLDivElement | null>(null);
let root: Root | null = null;

const graph = computed(() => parseSnapshot(props.graphJson ?? undefined));
const flowDoc = computed(() => (graph.value ? graphToFlowDoc(graph.value) : null));
const cards = computed(() => cardsOf(graph.value));
const branchInfos = computed(() => branchInfosOf(graph.value));
const nodeStates = computed<Record<string, WfNodeState>>(() => {
  const paint = props.detail ? definitionPaint(props.detail) : definitionOutline(props.graphJson);
  const m: Record<string, WfNodeState> = {};
  for (const n of paint) m[n.key] = n.state;
  return m;
});

/** 渲染只读画布（桥接 React 组件）；只读模式：无插入/删除/拖拽，点节点不发属性抽屉 */
function render() {
  const el = elRef.value;
  const doc = flowDoc.value;
  if (!el || !doc) return;
  root ??= createRoot(el);
  root.render(
    createElement(FlowGramDesigner, {
      doc,
      readonly: true,
      labels: {},
      cards: cards.value,
      branchInfos: branchInfos.value,
      nodeStates: nodeStates.value,
      selectedId: '',
      onSelectNode: () => undefined,
      onDocChanged: () => undefined,
    }),
  );
}

onMounted(render);
watch([flowDoc, cards, branchInfos, nodeStates], render, { flush: 'post' });
onBeforeUnmount(() => {
  const r = root;
  root = null;
  r?.unmount();
});
</script>

<template>
  <div v-if="!flowDoc" class="wf-instance-graph__empty">未找到流程快照</div>
  <div v-else ref="elRef" class="wf-instance-graph" />
</template>

<style scoped>
/* 画布容器作为定位/层叠基准：FlowGram 内部绝对定位浮层不得溢出容器、遮挡外部（如抽屉页签栏） */
.wf-instance-graph {
  position: relative;
  isolation: isolate;
  z-index: 0;
  width: 100%;
  height: 100%;
  min-height: 240px;
  overflow: hidden;
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  background: var(--color-fill-1);
}
.wf-instance-graph__empty {
  padding: 24px 0;
  text-align: center;
  color: var(--color-text-3);
  font-size: 13px;
}
</style>
