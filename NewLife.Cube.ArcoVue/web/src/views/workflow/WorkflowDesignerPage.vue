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
import WorkflowViewFilterField from './WorkflowViewFilterField.vue';
import { viewFilterSummary } from './wfFilterText';
import './workflowChrome.css';

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
  writableFieldsLoading,
  filterFields,
  entityOptions,
  canvasEl,
  graphErrors,
  lockPolicy,
  startFilter,
  selectedToKind,
  selectedToIds,
  selectedName,
  selectedMode,
  selectedTimeoutHours,
  selectedTimeoutAction,
  selectedTimeoutToKind,
  selectedTimeoutToIds,
  selectedAddSign,
  selectedRollback,
  selectedTransfer,
  selectedWritable,
  selectedHasTo,
  selectedXorCases,
  selectedDefaultTarget,
  nodeOptions,
  addXorCase,
  removeXorCase,
  updateXorCase,
  phrases,
  phrasesVisible,
  phraseDraft,
  addPhrase,
  removePhrase,
  savePhrases,
  openDefinition,
  selectNode,
  save,
  publish,
  insertVisible,
  insertType,
  insertMode,
  insertToKind,
  insertToIds,
  confirmInsert,
  cancelInsert,
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
    <div class="list-surface">
    <a-alert v-if="narrow" type="warning" class="wf-designer__narrow">
      当前视口宽度小于 1024px，设计器为只读预览（保存/编辑已禁用）。
    </a-alert>

    <!-- 头部：定义选择 / 新建 / 保存发布 -->
    <div class="wf-designer__head list-panel">
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
          style="width: 180px"
          @change="onRename"
        />
        <span v-else>{{ current.name }}</span>
        <a-tag v-if="current.published" color="green" size="small">已发布 v{{ current.version }}</a-tag>
        <a-tag v-else color="orange" size="small">草稿</a-tag>
        <a-radio-group v-if="canEdit" :model-value="lockPolicy" type="button" size="mini" @change="(v: string) => (lockPolicy = v)">
          <a-radio value="full">整单锁定</a-radio>
          <a-radio value="nodeFields">仅改指定字段</a-radio>
        </a-radio-group>
        <WorkflowViewFilterField
          v-if="canEdit"
          :model-value="startFilter"
          :fields="filterFields"
          empty-text="发起条件：不限制"
          @update:model-value="(v) => (startFilter = v)"
        />
        <a-typography-text v-else-if="current" type="secondary" style="font-size: 12px">
          {{ viewFilterSummary(current.startFilter) === '不限制' ? '任意记录可发起' : '已设发起条件' }}
        </a-typography-text>
      </div>

      <a-space class="wf-designer__actions">
        <a-button size="small" :disabled="narrow" @click="phrasesVisible = true">常用语</a-button>
        <a-button type="primary" size="small" :disabled="!canEdit" :loading="saving" @click="save()">
          保存草稿
        </a-button>
        <a-button type="outline" status="success" size="small" :disabled="!canEdit" :loading="saving" @click="publish()">
          发布
        </a-button>
      </a-space>
    </div>

    <!-- 多维视图节奏：顶栏与画布之间 12px 分隔条（对齐 list-surface gap / list-view-tabs margin-bottom） -->
    <div v-if="graph" class="wf-designer__sep" aria-hidden="true" />

    <a-empty v-if="!graph" class="list-panel" description="请选择或新建流程定义" />

    <div v-else class="wf-designer__body">
      <!-- 主区：FlowGram.AI 固定布局画布（宽屏）；窄屏只读链式预览 -->
      <div class="wf-designer__canvas list-panel">
        <!-- FlowGram 画布挂载点（React root）；插入/删除在节点「+」与右上角 × -->
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
      <div v-if="selectedNode" class="wf-designer__props list-panel">
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
              <a-radio-group
                :model-value="selectedMode === 'and' ? 'and' : 'or'"
                :disabled="!canEdit"
                type="button"
                size="mini"
                @change="(v: unknown) => d.setApproveMode(String(v))"
              >
                <a-radio value="or">或签</a-radio>
                <a-radio value="and">会签</a-radio>
              </a-radio-group>
              <div v-if="selectedMode === 'and'" class="wf-designer__hint">会签须全部通过后才继续流转</div>
            </a-form-item>
            <a-form-item v-if="selectedHasTo" label="审批人">
              <WorkflowRecipientPicker
                v-if="canEdit"
                v-model:kind="selectedToKind"
                v-model:model-value="selectedToIds"
                :multiple="selectedMode === 'and'"
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
            <a-form-item v-if="selectedTimeoutAction === 'transfer' && Number(selectedTimeoutHours) > 0" label="超时转交给">
              <WorkflowRecipientPicker
                v-if="canEdit"
                v-model:kind="selectedTimeoutToKind"
                v-model:model-value="selectedTimeoutToIds"
                :multiple="false"
              />
              <span v-else>已配置 {{ selectedTimeoutToIds.length }} 人</span>
            </a-form-item>
            <a-form-item label="办理时可做">
              <a-space wrap>
                <a-checkbox :model-value="selectedAddSign" :disabled="!canEdit" @change="(v: boolean) => (selectedAddSign = !!v)">
                  加签
                </a-checkbox>
                <a-checkbox :model-value="selectedRollback" :disabled="!canEdit" @change="(v: boolean) => (selectedRollback = !!v)">
                  回退
                </a-checkbox>
                <a-checkbox :model-value="selectedTransfer" :disabled="!canEdit" @change="(v: boolean) => (selectedTransfer = !!v)">
                  转办
                </a-checkbox>
              </a-space>
            </a-form-item>
            <a-form-item label="可写字段（审批中可改）">
              <a-select
                :model-value="selectedWritable"
                :disabled="!canEdit"
                multiple
                allow-clear
                allow-search
                :loading="writableFieldsLoading"
                placeholder="不选则审批中禁止修改业务字段"
                :options="writableFields.map((f) => ({ value: f.name, label: f.label }))"
                @update:model-value="(v: unknown) => d.patchSelected({ fields: { visible: ['*'], writable: (v as string[]) ?? [] } })"
              />
              <div v-if="!writableFields.length && !writableFieldsLoading" class="wf-designer__hint">
                未加载到实体字段，请确认流程已绑定实体类型
              </div>
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

          <div v-else-if="selectedNode.type === 'oa.xor'" class="wf-designer__xor">
            <p class="wf-designer__xor-hint">
              画布上左支为<strong>满足</strong>、右支为<strong>不满足</strong>。按第一条单据匹配；都不满足则走默认分支。
            </p>
            <div v-for="(c, i) in selectedXorCases" :key="i" class="wf-designer__xor-row">
              <div class="wf-designer__xor-row-head">满足</div>
              <WorkflowViewFilterField
                :model-value="c.filter"
                :fields="filterFields"
                :disabled="!canEdit"
                empty-text="点击设置条件"
                @update:model-value="(v) => updateXorCase(i, { filter: v })"
              />
              <span class="wf-designer__xor-then">则去</span>
              <a-select
                :model-value="c.target"
                :disabled="!canEdit"
                :options="nodeOptions"
                placeholder="选择节点"
                size="small"
                allow-clear
                style="width: 160px"
                @update:model-value="(v: unknown) => updateXorCase(i, { target: String(v ?? '') })"
              />
              <a-button v-if="canEdit" size="mini" status="danger" @click="removeXorCase(i)">删除</a-button>
            </div>
            <a-button v-if="canEdit" size="mini" type="dashed" long @click="addXorCase">添加条件</a-button>
            <a-form-item label="不满足（默认，必填）" style="margin-top: 12px">
              <a-select
                :model-value="selectedDefaultTarget"
                :disabled="!canEdit"
                :options="nodeOptions"
                placeholder="必须指定默认节点"
                size="small"
                @update:model-value="(v: unknown) => (selectedDefaultTarget = String(v ?? ''))"
              />
            </a-form-item>
          </div>

          <a-form-item v-if="selectedNode.type === 'oa.start' || selectedNode.type === 'oa.end'">
            <a-typography-text type="secondary" style="font-size: 12px">
              {{ selectedNode.type === 'oa.start' ? '流程入口。需要知会请在其后点「+」插入知会节点。' : '流程结束。需要办结知会请在结束前插入知会节点。' }}
            </a-typography-text>
          </a-form-item>
        </a-form>
      </div>
      <a-empty v-else class="wf-designer__props-empty list-panel" description="点左侧节点即可设置审批人" />
    </div>
    <a-alert v-if="graph && graphErrors.length" type="warning" class="wf-designer__errors">
      发布前需处理：{{ graphErrors.join('；') }}
    </a-alert>

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

    <a-modal
      v-model:visible="phrasesVisible"
      title="审批常用语"
      :on-before-ok="savePhrases"
    >
      <p class="wf-designer__xor-hint">办理时一键填入意见。留空则使用内置「同意 / 请补充材料 / 驳回」。</p>
      <div class="wf-phrases">
        <a-tag v-for="p in phrases" :key="p.id" closable @close="removePhrase(p.id)">{{ p.text }}</a-tag>
      </div>
      <a-input-search
        v-model="phraseDraft"
        button-text="添加"
        search-button
        placeholder="新常用语"
        @search="addPhrase"
      />
    </a-modal>

    <a-modal
      :visible="insertVisible"
      :title="insertType === 'oa.cc' ? '添加知会节点' : '添加审批节点'"
      ok-text="添加到画布"
      :on-before-ok="confirmInsert"
      @cancel="cancelInsert"
      @update:visible="(v: boolean) => !v && cancelInsert()"
    >
      <p class="wf-designer__hint" style="margin-bottom: 12px">
        {{
          insertType === 'oa.cc'
            ? '选择知会对象后，画布节点将直接显示「知会 · 用户/角色/部门」及具体名称。'
            : '选择或签/会签与审批人后，画布节点将直接显示签核方式与具体人员/角色/部门。'
        }}
      </p>
      <a-form :model="{}" layout="vertical" size="small">
        <a-form-item v-if="insertType === 'oa.approve'" label="签核模式">
          <a-radio-group v-model="insertMode" type="button" size="mini">
            <a-radio value="or">或签（任一人通过即可）</a-radio>
            <a-radio value="and">会签（全部通过才流转）</a-radio>
          </a-radio-group>
        </a-form-item>
        <a-form-item :label="insertType === 'oa.cc' ? '知会对象' : '审批人'" required>
          <WorkflowRecipientPicker
            v-model:kind="insertToKind"
            v-model:model-value="insertToIds"
            :multiple="insertType === 'oa.cc' || insertMode === 'and'"
          />
        </a-form-item>
      </a-form>
    </a-modal>
    </div>
  </div>
</template>

<style scoped>
.wf-designer {
  display: flex;
  flex-direction: column;
  gap: 0;
  min-width: 0;
  max-width: 100%;
  /* 撑满内容区剩余高度，保证画布与属性栏可对齐拉伸 */
  min-height: calc(100vh - 148px);
  box-sizing: border-box;
}
.list-surface {
  flex: 1 1 auto;
  min-height: 0;
  gap: 0;
  display: flex;
  flex-direction: column;
}
/* 对齐多维视图 list-surface gap / list-view-tabs margin-bottom */
.wf-designer__sep {
  flex: 0 0 12px;
  height: 12px;
  margin: 0;
  padding: 0;
  border: none;
  background: transparent;
  pointer-events: none;
}
.wf-designer__narrow {
  margin-bottom: 8px;
}
.wf-designer__head {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
  flex: 0 0 auto;
  border-radius: 8px;
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
.wf-designer__hint {
  margin-top: 4px;
  font-size: 12px;
  color: var(--color-text-3);
  line-height: 1.4;
}
.wf-designer__body {
  display: flex;
  gap: 12px;
  align-items: stretch;
  flex: 1 1 auto;
  min-height: 0;
}
.wf-designer__canvas {
  flex: 1 1 auto;
  min-width: 0;
  min-height: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  border-radius: 8px;
}
.wf-designer__flowgram {
  flex: 1 1 auto;
  min-height: 360px;
  height: 100%;
  position: relative;
}
.wf-designer__flowgram :deep(.gedit-flow-render-layer),
.wf-designer__flowgram :deep(.gedit-canvas-host) {
  border-radius: 8px;
}
.wf-designer__chain {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  flex: 1;
  overflow: auto;
}
.wf-designer__arrow {
  color: var(--color-text-4);
}
.wf-designer__node {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 220px;
  padding: 6px 10px;
  border: 1px solid var(--color-border-2);
  border-radius: 6px;
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
  width: 360px;
  flex: 0 0 360px;
  min-height: 0;
  overflow: auto;
  border-radius: 8px;
}
.wf-designer__props-title {
  font-weight: 600;
  margin-bottom: 8px;
}
.wf-designer__props-empty {
  width: 360px;
  flex: 0 0 360px;
  min-height: 0;
  border-radius: 8px;
}
.wf-designer__xor {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.wf-designer__xor-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
  padding: 8px;
  background: var(--color-fill-1);
  border-radius: 6px;
}
.wf-designer__xor-row-head,
.wf-designer__xor-then {
  font-size: 12px;
  color: var(--color-text-3);
}
.wf-designer__errors {
  margin-top: 8px;
}
.wf-phrases {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
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
