<script setup lang="ts">
/**
 * 流程设计器页（OSC-26090347f1 T8e）：固定布局（链式）编辑 GraphJson，节点仅 oa.*；
 * 菜单 URL /Cube/Workflow/Designer（含 ?id= 直达）。<1024 只读提示（IA §3.4）。
 * 薄 .vue：逻辑全部在 useWorkflowDesigner；运行时为 C# 引擎，浏览器只读写定义图。
 */
import { ref } from 'vue';
import { wfNodeTypeLabel } from '@/core/types/workflow';
import { useWorkflowDesigner } from './useWorkflowDesigner';
import WorkflowRecipientPicker from './WorkflowRecipientPicker.vue';

const d = useWorkflowDesigner();

const {
  definitions,
  defsLoading,
  currentId,
  current,
  graph,
  chain,
  selectedNodeId,
  selectedNode,
  canEdit,
  narrow,
  saving,
  writableFields,
  entityOptions,
  canvasEl,
  selectedToKind,
  selectedToIds,
  selectedName,
  selectedMode,
  selectedTimeoutHours,
  selectedTimeoutAction,
  selectedAddSign,
  selectedRollback,
  selectedWritable,
  selectedHasTo,
  openDefinition,
  addNodeAfter,
  removeSelected,
  selectNode,
  save,
  publish,
} = d;

/** 新建草稿弹窗 */
const createVisible = ref(false);
const createTypePath = ref('');
const createName = ref('');
const creating = ref(false);

async function onCreate() {
  if (!createTypePath.value || !createName.value) return;
  creating.value = true;
  try {
    const id = await d.createDefinition(createTypePath.value, createName.value);
    if (id) createVisible.value = false;
  } finally {
    creating.value = false;
  }
}

function onRename(v: unknown) {
  if (current.value) current.value.name = String(v ?? '');
}

function nodeIcon(type: string): string {
  switch (type) {
    case 'oa.start':
      return 'play';
    case 'oa.approve':
      return 'audit';
    case 'oa.cc':
      return 'notification';
    case 'oa.xor':
      return 'branch';
    case 'oa.end':
      return 'stop';
    default:
      return 'circle';
  }
}
</script>

<template>
  <div class="wf-designer">
    <a-alert v-if="narrow" type="warning" class="wf-designer__narrow">
      当前视口宽度小于 1024px，设计器为只读预览（保存/编辑已禁用）。
    </a-alert>

    <!-- 头部：定义选择 / 新建 / 保存发布 -->
    <div class="wf-designer__head">
      <a-select
        :model-value="currentId ?? undefined"
        :loading="defsLoading"
        placeholder="选择流程定义"
        style="width: 240px"
        :disabled="narrow"
        @update:model-value="(v: unknown) => openDefinition(v ? String(v) : null)"
      >
        <a-option v-for="def in definitions" :key="def.id" :value="def.id">
          {{ def.name }}（{{ def.typePath }}·v{{ def.version }}{{ def.published ? '·已发布' : '' }}）
        </a-option>
      </a-select>
      <a-button size="small" :disabled="narrow" @click="createVisible = true">+ 新建流程</a-button>

      <div v-if="current" class="wf-designer__name">
        <a-input
          v-if="canEdit"
          :model-value="current.name"
          size="small"
          style="width: 220px"
          @change="onRename"
        />
        <span v-else>{{ current.name }}</span>
        <a-tag v-if="current.published" color="green" size="small">已发布 v{{ current.version }}</a-tag>
        <a-tag v-else color="orange" size="small">草稿</a-tag>
      </div>

      <a-space class="wf-designer__actions">
        <a-button type="primary" size="small" :disabled="!canEdit" :loading="saving" @click="save()">
          保存草稿
        </a-button>
        <a-button type="outline" status="success" size="small" :disabled="!canEdit" :loading="saving" @click="publish()">
          发布
        </a-button>
      </a-space>
    </div>

    <a-empty v-if="!graph" description="请选择或新建流程定义" />

    <div v-else class="wf-designer__body">
      <!-- 主区：FlowGram.AI 固定布局画布（宽屏）；窄屏只读链式预览 -->
      <div class="wf-designer__canvas">
        <div class="wf-designer__toolbar">
          <span class="wf-designer__toolbar-hint">在选中节点后插入：</span>
          <a-button size="mini" :disabled="!canEdit || !selectedNode || selectedNode.type === 'oa.end'" @click="addNodeAfter('oa.approve')">
            审批
          </a-button>
          <a-button size="mini" :disabled="!canEdit || !selectedNode || selectedNode.type === 'oa.end'" @click="addNodeAfter('oa.cc')">
            知会
          </a-button>
          <a-button size="mini" :disabled="!canEdit || !selectedNode || selectedNode.type === 'oa.end'" @click="addNodeAfter('oa.xor')">
            条件分流
          </a-button>
          <a-button
            size="mini"
            status="danger"
            :disabled="!canEdit || !selectedNode || selectedNode.type === 'oa.start' || selectedNode.type === 'oa.end'"
            @click="removeSelected"
          >
            删除选中
          </a-button>
          <a-typography-text v-if="narrow" type="secondary" style="font-size: 12px; margin-left: auto">
            视口小于 1024px：只读预览
          </a-typography-text>
        </div>

        <!-- FlowGram 画布挂载点（React root） -->
        <div v-if="!narrow" ref="canvasEl" class="wf-designer__flowgram" />

        <!-- 窄屏只读链式预览 -->
        <div v-else class="wf-designer__chain">
          <template v-for="(node, i) in chain" :key="node.id">
            <div v-if="i > 0" class="wf-designer__arrow">↓</div>
            <div
              class="wf-designer__node"
              :class="{ 'wf-designer__node--selected': selectedNodeId === node.id }"
              @click="selectNode(node.id)"
            >
              <icon-park :type="nodeIcon(node.type)" />
              <div class="wf-designer__node-main">
                <b>{{ wfNodeTypeLabel(node.type) }}</b>
                <span class="wf-designer__node-name">{{ (node.data?.name as string) || node.id }}</span>
              </div>
              <span class="wf-designer__node-id">{{ node.id }}</span>
            </div>
          </template>
        </div>
      </div>

      <!-- 右侧属性面板 -->
      <div v-if="selectedNode" class="wf-designer__props">
        <div class="wf-designer__props-title">
          {{ wfNodeTypeLabel(selectedNode.type) }} 属性
        </div>
        <a-form :model="{}" layout="vertical" size="small">
          <a-form-item label="节点名称">
            <a-input
              :model-value="selectedName"
              :disabled="!canEdit"
              @change="(v: unknown) => d.patchSelected({ name: String(v ?? '') })"
            />
          </a-form-item>

          <template v-if="selectedNode.type === 'oa.approve'">
            <a-form-item label="签核模式">
              <a-radio-group :model-value="selectedMode" :disabled="!canEdit" type="button" size="mini" @change="(v: unknown) => d.patchSelected({ mode: String(v) })">
                <a-radio value="or">或签</a-radio>
                <a-radio value="and">会签</a-radio>
                <a-radio value="sequence">依次签</a-radio>
              </a-radio-group>
            </a-form-item>
            <a-form-item v-if="selectedHasTo" label="审批人">
              <WorkflowRecipientPicker
                v-if="canEdit"
                v-model:kind="selectedToKind"
                v-model:model-value="selectedToIds"
                :multiple="selectedMode !== 'sequence' && selectedMode !== 'or'"
              />
              <span v-else>已配置 {{ selectedToIds.length }} 个接收人</span>
            </a-form-item>
            <a-form-item label="超时（小时，0=不限）">
              <a-input-number
                :model-value="selectedTimeoutHours"
                :disabled="!canEdit"
                :min="0"
                :max="8760"
                @change="(v: number) => d.patchSelected({ timeoutHours: Number(v) || 0 })"
              />
            </a-form-item>
            <a-form-item label="超时动作">
              <a-select :model-value="selectedTimeoutAction" :disabled="!canEdit" @update:model-value="(v: unknown) => d.patchSelected({ timeoutAction: String(v) })">
                <a-option value="pass">自动通过</a-option>
                <a-option value="reject">自动驳回</a-option>
                <a-option value="transfer">转交</a-option>
              </a-select>
            </a-form-item>
            <a-form-item label="允许加签 / 回退">
              <a-space>
                <a-checkbox :model-value="selectedAddSign" :disabled="!canEdit" @change="(v: boolean) => d.patchSelected({ allowAddSign: !!v })">
                  加签
                </a-checkbox>
                <a-checkbox :model-value="selectedRollback" :disabled="!canEdit" @change="(v: boolean) => d.patchSelected({ allowRollback: !!v })">
                  回退
                </a-checkbox>
              </a-space>
            </a-form-item>
            <a-form-item label="可写字段（审批中可改）">
              <a-select
                :model-value="selectedWritable"
                :disabled="!canEdit"
                multiple
                allow-clear
                placeholder="不选则审批中禁止修改业务字段"
                :options="writableFields.map((f) => ({ value: f.name, label: f.label }))"
                @update:model-value="(v: unknown) => d.patchSelected({ fields: { visible: ['*'], writable: (v as string[]) ?? [] } })"
              />
            </a-form-item>
          </template>

          <a-form-item v-else-if="selectedNode.type === 'oa.cc'" label="知会对象">
            <WorkflowRecipientPicker
              v-if="canEdit"
              v-model:kind="selectedToKind"
              v-model:model-value="selectedToIds"
              multiple
            />
            <span v-else>已配置 {{ selectedToIds.length }} 个接收人</span>
          </a-form-item>

          <div v-else-if="selectedNode.type === 'oa.xor'" class="wf-designer__xor-hint">
            条件分流（xor）：按第一条主体的条件匹配分支，命中走对应目标；未命中走默认分支。
            条件可视化编辑将在后续版本提供；当前默认走向下一节点。
          </div>

          <a-form-item v-if="selectedNode.type === 'oa.start' || selectedNode.type === 'oa.end'">
            <a-typography-text type="secondary" style="font-size: 12px">
              {{ selectedNode.type === 'oa.start' ? '流程入口（仅一个）' : '流程结束（仅一个）' }}
            </a-typography-text>
          </a-form-item>
        </a-form>
      </div>
      <a-empty v-else class="wf-designer__props-empty" description="点击左侧节点编辑属性" />
    </div>

    <!-- 新建流程弹窗 -->
    <a-modal
      v-model:visible="createVisible"
      title="新建流程定义"
      :on-before-ok="onCreate"
      :ok-loading="creating"
    >
      <a-form :model="{}" layout="vertical" size="small">
        <a-form-item label="实体" required>
          <a-select
            v-model="createTypePath"
            allow-search
            placeholder="选择要挂审批的实体"
            :options="entityOptions"
          />
        </a-form-item>
        <a-form-item label="流程名称" required>
          <a-input v-model="createName" placeholder="如：请假审批" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<style scoped>
.wf-designer {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-height: 0;
}
.wf-designer__narrow {
  margin-bottom: 0;
}
.wf-designer__head {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}
.wf-designer__name {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
  flex: 1;
}
.wf-designer__actions {
  margin-left: auto;
}
.wf-designer__body {
  display: flex;
  gap: 16px;
  align-items: flex-start;
  min-height: 0;
}
.wf-designer__canvas {
  flex: 1 1 auto;
  min-width: 0;
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  padding: 12px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}
.wf-designer__flowgram {
  flex: 1 1 auto;
  min-height: 420px;
  height: 100%;
  position: relative;
}
.wf-designer__flowgram :deep(.gedit-flow-render-layer),
.wf-designer__flowgram :deep(.gedit-canvas-host) {
  border-radius: 8px;
}
.wf-designer__toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-bottom: 12px;
  margin-bottom: 12px;
  border-bottom: 1px solid var(--color-border-2);
  flex-wrap: wrap;
}
.wf-designer__toolbar-hint {
  color: var(--color-text-3);
  font-size: 12px;
}
.wf-designer__chain {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}
.wf-designer__arrow {
  color: var(--color-text-4);
}
.wf-designer__node {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 340px;
  padding: 10px 14px;
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  cursor: pointer;
  background: var(--color-bg-2);
  transition: border-color 0.2s;
}
.wf-designer__node:hover {
  border-color: var(--color-primary-5);
}
.wf-designer__node--selected {
  border-color: var(--color-primary-6);
  box-shadow: 0 0 0 2px var(--color-primary-light-3);
}
.wf-designer__node-main {
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.wf-designer__node-name {
  color: var(--color-text-3);
  font-size: 12px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.wf-designer__node-id {
  margin-left: auto;
  color: var(--color-text-4);
  font-size: 12px;
}
.wf-designer__props {
  width: 320px;
  flex: 0 0 320px;
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  padding: 12px;
  max-height: 70vh;
  overflow: auto;
}
.wf-designer__props-title {
  font-weight: 600;
  margin-bottom: 8px;
}
.wf-designer__props-empty {
  width: 320px;
  flex: 0 0 320px;
}
.wf-designer__xor-hint {
  color: var(--color-text-3);
  font-size: 12px;
  line-height: 1.6;
  background: var(--color-fill-1);
  padding: 8px;
  border-radius: 4px;
}
</style>
