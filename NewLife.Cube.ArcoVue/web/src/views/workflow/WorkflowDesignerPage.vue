<script setup lang="ts">
/**
 * 流程设计器页（OSC-26090347f1 T8e）：固定布局（链式）编辑 GraphJson，节点仅 oa.*；
 * 菜单 URL /Cube/Workflow/Designer（含 ?id= 直达）。<1024 只读提示（IA §3.4）。
 * 薄 .vue：逻辑全部在 useWorkflowDesigner；运行时为 C# 引擎，浏览器只读写定义图。
 */
import { computed, ref } from 'vue';
import { wfNodeTypeLabel, type WfToKind } from '@/core/types/workflow';
import { useWorkflowDesigner } from './useWorkflowDesigner';
import type { RecipientKind } from './recipient';
import WorkflowAssigneePicker from './WorkflowAssigneePicker.vue';
import WorkflowRecipientPicker from './WorkflowRecipientPicker.vue';
import WorkflowViewFilterField from './WorkflowViewFilterField.vue';
import FilterBuilderPanel from '@/views/crud/FilterBuilderPanel.vue';
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
  propertyPanel,
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
  selectedToLabels,
  selectedStarterPickScope,
  selectedStarterPickMultiple,
  selectedFieldName,
  selectedFieldAs,
  selectedEmptyPolicy,
  selectedEmptyUserIds,
  selectedExcludeStats,
  rootExcludeStats,
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
  selectedBranch,
  selectedBranchName,
  selectedBranchFilter,
  selectedDefaultTarget,
  nodeOptions,
  phrases,
  phraseDraft,
  openDefinition,
  openFlowProperties,
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

/** 分支条件抽屉：内嵌条件面板 ref（抽屉 Footer 的重置/应用按钮触发） */
const branchFilterPanel = ref<{ reset(): void; apply(): void } | null>(null);

/** 流程属性：发起条件面板 ref（「且/或」开关外置到标签行右侧） */
const startFilterPanel = ref<{ draft?: { logic: 'all' | 'any' } } | null>(null);
const startFilterLogic = computed<'all' | 'any'>({
  get: () => startFilterPanel.value?.draft?.logic ?? 'all',
  set: (v) => {
    if (startFilterPanel.value?.draft) startFilterPanel.value.draft.logic = v;
  },
});

/** 抽屉 Footer：流程属性（含「新建流程」）与分支条件（可编辑）时显示 */
const hasDrawerFooter = computed(
  () =>
    propertyPanel.value === 'flow' ||
    (!!selectedBranch.value && !selectedBranch.value.isElse && canEdit.value),
);

/** 常用语就地管理：当前编辑行 */
const phraseEditId = ref<number | string | null>(null);
const phraseEditText = ref('');
function startEditPhrase(p: { id: number | string; text: string }) {
  phraseEditId.value = p.id;
  phraseEditText.value = p.text;
}
function cancelEditPhrase() {
  phraseEditId.value = null;
}
async function confirmEditPhrase() {
  const id = phraseEditId.value;
  if (id == null) return;
  d.updatePhrase(id, phraseEditText.value);
  phraseEditId.value = null;
  await d.savePhrases();
}
async function onDeletePhrase(id: number | string) {
  d.removePhrase(id);
  await d.savePhrases();
}
async function onAddPhrase() {
  const before = phrases.value.length;
  d.addPhrase();
  // 去重/空输入不会新增，不触发落库
  if (phrases.value.length !== before) await d.savePhrases();
}

function nodeIcon(type: string): string {
  switch (type) {
    case 'oa.start':
      return 'play';
    case 'oa.approve':
      return 'audit';
    case 'oa.handle':
      return 'checklist';
    case 'oa.cc':
      return 'notification';
    case 'oa.xor':
      return 'branch';
    case 'oa.parallel':
      return 'parallel-gateway';
    case 'oa.end':
      return 'stop';
    default:
      return 'circle';
  }
}

/** 表单人员可选字段（选人组件的 field 下拉） */
const assigneeFields = computed(() =>
  filterFields.value.map((f) => ({ name: f.name, label: f.displayName || f.name })),
);

/** 知会节点仅支持用户/角色/部门（后端 RecipientResolver 的 to schema）；六种选人值降级取前三种 */
const asCcKind = (k: WfToKind): RecipientKind => (k === 'roles' || k === 'departments' ? k : 'users');
const ccKind = computed({
  get: () => asCcKind(selectedToKind.value),
  set: (v: RecipientKind) => (selectedToKind.value = v),
});
const insertCcKind = computed({
  get: () => asCcKind(insertToKind.value),
  set: (v: RecipientKind) => (insertToKind.value = v),
});
</script>

<template>
  <div class="wf-designer">
    <a-empty
      v-if="!graph"
      class="wf-designer__empty"
      description="从流程定义的「设计」进入，或点击此处打开流程属性"
      @click="openFlowProperties"
    />

    <div v-else class="wf-designer__canvas">
      <div v-if="!narrow" ref="canvasEl" class="wf-designer__flowgram" />
      <div v-else class="wf-designer__chain" @click.self="openFlowProperties">
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

    <a-drawer
      class="wf-node-drawer"
      :visible="propertyPanel !== 'none'"
      placement="right"
      :width="400"
      :mask="false"
      :footer="hasDrawerFooter"
      unmount-on-close
      @cancel="selectNode(null)"
    >
      <template #title>{{ propertyPanel === 'flow' ? '流程属性' : '节点属性' }}</template>
      <a-form v-if="propertyPanel === 'flow'" :model="{}" layout="vertical" size="small">
        <a-alert v-if="narrow" type="warning">
          当前视口宽度小于 1024px，设计器为只读预览（保存/编辑已禁用）。
        </a-alert>
        <a-form-item v-if="current" label="流程名称">
          <a-input v-if="canEdit" :model-value="current.name" @change="onRename" />
          <span v-else>{{ current.name }}</span>
        </a-form-item>
        <a-form-item label="流程定义">
          <a-select
            :model-value="currentId ?? undefined"
            :loading="defsLoading"
            placeholder="选择流程定义"
            allow-search
            :disabled="narrow"
            @update:model-value="(v: unknown) => openDefinition(v ? String(v) : null)"
          >
            <a-option v-for="def in definitions" :key="def.id" :value="def.id">
              {{ def.name }}（{{ def.typePath }}·v{{ def.version }}{{ def.published ? '·已发布' : '' }}）
            </a-option>
          </a-select>
        </a-form-item>
        <template v-if="current">
          <a-form-item label="状态">
            <a-tag v-if="current.published" color="green" size="small">已发布 v{{ current.version }}</a-tag>
            <a-tag v-else color="orange" size="small">草稿</a-tag>
          </a-form-item>
          <a-form-item v-if="canEdit" label="审批中如何改单据">
            <a-radio-group
              class="wf-designer__lock"
              :model-value="lockPolicy"
              type="button"
              size="mini"
              @change="(v: string) => (lockPolicy = v)"
            >
              <a-tooltip content="审批进行中，这条业务记录不能改、也不能删。要改内容，先办完或撤回流程。">
                <a-radio value="full">整单锁定</a-radio>
              </a-tooltip>
              <a-tooltip content="审批进行中仍不能删除。当前节点的办理人只能改该节点勾选的可写字段，其它字段不能改。">
                <a-radio value="nodeFields">仅改指定字段</a-radio>
              </a-tooltip>
            </a-radio-group>
          </a-form-item>
          <a-form-item label="不纳入效率统计">
            <a-switch
              :model-value="rootExcludeStats"
              :disabled="!canEdit"
              @update:model-value="(v: string | number | boolean) => (rootExcludeStats = !!v)"
            />
          </a-form-item>
          <a-form-item class="wf-filter-item">
            <div class="wf-filter-head">
              <div class="wf-filter-head__left">
                <span class="wf-filter-head__label">发起条件</span>
                <a-tooltip content="仅作提交准入校验：条件命中的记录才允许提交审批，不满足时无法提交；不会自动发起流程">
                  <icon-park type="info" class="wf-filter-head__hint" :size="14" />
                </a-tooltip>
              </div>
              <a-radio-group v-if="canEdit" v-model="startFilterLogic" type="button" size="mini">
                <a-radio value="all">且 (AND)</a-radio>
                <a-radio value="any">或 (OR)</a-radio>
              </a-radio-group>
            </div>
            <FilterBuilderPanel
              v-if="canEdit"
              ref="startFilterPanel"
              inline
              :show-logic="false"
              :fields="filterFields"
              :model-value="startFilter"
              @apply="(v) => (startFilter = v)"
            />
            <a-typography-text v-else type="secondary" style="font-size: 12px">
              {{ viewFilterSummary(current.startFilter) === '不限制' ? '未设条件，任意记录均可提交审批' : '已设条件，仅条件命中的记录可提交审批' }}
            </a-typography-text>
          </a-form-item>
          <a-form-item label="审批常用语">
            <div class="wf-phrase-list">
              <div v-for="p in phrases" :key="p.id" class="wf-phrase-row">
                <template v-if="phraseEditId === p.id">
                  <a-input
                    v-model="phraseEditText"
                    size="small"
                    placeholder="常用语内容"
                    @press-enter="confirmEditPhrase"
                  />
                  <a-button size="mini" type="text" @click="confirmEditPhrase">
                    <icon-park type="check" />
                  </a-button>
                  <a-button size="mini" type="text" @click="cancelEditPhrase">
                    <icon-park type="close" />
                  </a-button>
                </template>
                <template v-else>
                  <span class="wf-phrase-text">{{ p.text }}</span>
                  <a-button v-if="canEdit" size="mini" type="text" title="编辑" @click="startEditPhrase(p)">
                    <icon-park type="edit" />
                  </a-button>
                  <a-button
                    v-if="canEdit"
                    size="mini"
                    type="text"
                    status="danger"
                    title="删除"
                    @click="onDeletePhrase(p.id)"
                  >
                    <icon-park type="delete" />
                  </a-button>
                </template>
              </div>
              <a-input-search
                v-if="canEdit"
                v-model="phraseDraft"
                button-text="添加"
                search-button
                size="small"
                placeholder="新常用语"
                @search="onAddPhrase"
              />
              <div class="wf-designer__hint">办理时一键填入意见。留空则使用内置「同意 / 请补充材料 / 驳回」。</div>
            </div>
          </a-form-item>
          <a-alert v-if="graph && graphErrors.length" type="warning">
            发布前需处理：{{ graphErrors.join('；') }}
          </a-alert>
        </template>
      </a-form>
      <a-form v-else-if="selectedNode" :model="{}" layout="vertical" size="small">
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
                :model-value="selectedMode"
                :disabled="!canEdit"
                type="button"
                size="mini"
                @change="(v: unknown) => d.setApproveMode(String(v))"
              >
                <a-radio value="or">或签</a-radio>
                <a-radio value="and">会签</a-radio>
                <a-radio value="sequence">依次</a-radio>
              </a-radio-group>
              <div v-if="selectedMode === 'or'" class="wf-designer__hint">可多选。任一人通过即可</div>
              <div v-else-if="selectedMode === 'and'" class="wf-designer__hint">可多选。须全部通过后才继续流转</div>
              <div v-else-if="selectedMode === 'sequence'" class="wf-designer__hint">可多选。按顺序逐人审批，前一位通过后下一位才收到待办</div>
            </a-form-item>
            <a-form-item v-if="selectedHasTo" label="审批人">
              <WorkflowAssigneePicker
                v-if="canEdit"
                v-model:kind="selectedToKind"
                v-model:model-value="selectedToIds"
                v-model:labels="selectedToLabels"
                v-model:scope="selectedStarterPickScope"
                v-model:starter-multiple="selectedStarterPickMultiple"
                v-model:field="selectedFieldName"
                v-model:field-as="selectedFieldAs"
                :fields="assigneeFields"
              />
              <span v-else>已配置接收人</span>
            </a-form-item>
            <a-form-item label="人为空时">
              <a-select
                :model-value="selectedEmptyPolicy || 'default'"
                :disabled="!canEdit"
                style="width: 100%"
                @update:model-value="(v: unknown) => (selectedEmptyPolicy = String(v))"
              >
                <a-option value="default">停住并通知发起人</a-option>
                <a-option value="pass">自动通过</a-option>
                <a-option value="manager">转部门负责人</a-option>
                <a-option value="user">指定某人</a-option>
              </a-select>
              <div v-if="selectedEmptyPolicy === 'user'" class="wf-designer__hint">
                <WorkflowRecipientPicker
                  v-if="canEdit"
                  :model-value="selectedEmptyUserIds"
                  :multiple="false"
                  @update:model-value="(ids: number[]) => (selectedEmptyUserIds = ids)"
                />
                <span v-else>已指定 {{ selectedEmptyUserIds.length }} 人</span>
              </div>
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
            <a-form-item label="效率统计">
              <a-checkbox :model-value="selectedExcludeStats" :disabled="!canEdit" @change="(v: boolean) => (selectedExcludeStats = !!v)">
                不计入效率
              </a-checkbox>
            </a-form-item>
          </template>

          <!-- 办理节点：只完成，不能驳回；不参与同一人跳过与效率统计 -->
          <template v-else-if="selectedNode.type === 'oa.handle'">
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
              <div class="wf-designer__hint">可多选。办理只完成，不能驳回整单；不参与「同一人自动跳过」，也不计入效率统计</div>
            </a-form-item>
            <a-form-item v-if="selectedHasTo" label="办理人">
              <WorkflowAssigneePicker
                v-if="canEdit"
                v-model:kind="selectedToKind"
                v-model:model-value="selectedToIds"
                v-model:labels="selectedToLabels"
                v-model:scope="selectedStarterPickScope"
                v-model:starter-multiple="selectedStarterPickMultiple"
                v-model:field="selectedFieldName"
                v-model:field-as="selectedFieldAs"
                :fields="assigneeFields"
              />
              <span v-else>已配置接收人</span>
            </a-form-item>
            <a-form-item label="人为空时">
              <a-select
                :model-value="selectedEmptyPolicy || 'default'"
                :disabled="!canEdit"
                style="width: 100%"
                @update:model-value="(v: unknown) => (selectedEmptyPolicy = String(v))"
              >
                <a-option value="default">停住并通知发起人</a-option>
                <a-option value="pass">自动通过</a-option>
                <a-option value="manager">转部门负责人</a-option>
                <a-option value="user">指定某人</a-option>
              </a-select>
              <div v-if="selectedEmptyPolicy === 'user'" class="wf-designer__hint">
                <WorkflowRecipientPicker
                  v-if="canEdit"
                  :model-value="selectedEmptyUserIds"
                  :multiple="false"
                  @update:model-value="(ids: number[]) => (selectedEmptyUserIds = ids)"
                />
                <span v-else>已指定 {{ selectedEmptyUserIds.length }} 人</span>
              </div>
            </a-form-item>
          </template>

          <a-form-item v-else-if="selectedNode.type === 'oa.cc'" label="知会对象">
            <WorkflowRecipientPicker
              v-if="canEdit"
              v-model:kind="ccKind"
              v-model:model-value="selectedToIds"
              @update:labels="(labels: string[]) => (selectedToLabels = labels)"
              multiple
            />
            <span v-else>已配置 {{ selectedToIds.length }} 个接收人</span>
          </a-form-item>

          <div v-else-if="selectedNode.type === 'oa.xor' || selectedNode.type === 'oa.parallel'" class="wf-designer__xor">
            <p class="wf-designer__xor-hint">
              <template v-if="selectedNode.type === 'oa.parallel'">
                画布上每个分支是一张卡片：点卡片改条件。没写条件的分支都会进入；写了条件的按<strong>第一条记录</strong>判断。都不进入才走「其他情况」。进入的分支全部完成后才汇合。
              </template>
              <template v-else>
                画布上每个分支是一张卡片：点卡片改条件，点分支下方的「＋ 添加条件」增加分支。运行时按<strong>第一条记录</strong>从左到右匹配，命中第一条即走；都不命中走「其他情况」。
              </template>
            </p>
            <a-form-item label="其他情况（必填，都不命中时走这里）" style="margin-top: 12px">
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
      <!-- 分支条件卡片（点画布上的条件/其他情况卡片进入）：改名称/条件；目标由分支卡片内拖入的节点决定 -->
      <a-form v-else-if="selectedBranch" :model="{}" layout="vertical" size="small">
        <template v-if="!selectedBranch.isElse">
          <a-form-item label="条件名称">
            <a-input
              :model-value="selectedBranchName"
              :disabled="!canEdit"
              :max-length="20"
              placeholder="留空显示「条件N」"
              size="small"
              @update:model-value="(v: string) => (selectedBranchName = v)"
            />
          </a-form-item>
          <div class="wf-designer__hint">命中后自动流向本分支卡片内的节点；把节点拖进卡片即可调整去向</div>
          <a-form-item label="条件">
            <FilterBuilderPanel
              v-if="canEdit"
              ref="branchFilterPanel"
              inline
              :show-actions="false"
              :fields="filterFields"
              :model-value="selectedBranchFilter"
              @apply="(v) => (selectedBranchFilter = v)"
            />
            <WorkflowViewFilterField
              v-else
              :model-value="selectedBranchFilter"
              :fields="filterFields"
              disabled
            />
          </a-form-item>
        </template>
        <a-form-item v-else label="其他情况">
          <a-typography-text type="secondary" style="font-size: 12px">
            都不命中时走这条分支（默认分支）。把节点拖进卡片即可调整去向。
          </a-typography-text>
        </a-form-item>
      </a-form>

      <template #footer>
        <div class="wf-drawer-footer">
          <!-- 左侧：新建审批流程（仅流程属性） -->
          <a-button v-if="propertyPanel === 'flow'" type="primary" :disabled="narrow" @click="createVisible = true">新建审批流程</a-button>
          <!-- 右侧：操作按钮 -->
          <div class="wf-drawer-footer__right">
            <!-- 流程属性：草稿 / 发布（对齐实体编辑/添加抽屉 Footer） -->
            <template v-if="propertyPanel === 'flow' && current">
              <a-button :disabled="!canEdit || narrow" :loading="saving" @click="save()">保存草稿</a-button>
              <a-button
                type="primary"
                :disabled="!canEdit || narrow"
                :loading="saving"
                @click="publish()"
              >
                发布
              </a-button>
            </template>
            <!-- 分支条件：重置 / 应用（作用于内嵌条件面板） -->
            <template v-else-if="selectedBranch && !selectedBranch.isElse && canEdit">
              <a-button @click="branchFilterPanel?.reset()">重置</a-button>
              <a-button type="primary" @click="branchFilterPanel?.apply()">应用</a-button>
            </template>
          </div>
        </div>
      </template>
    </a-drawer>

    <!-- 新建审批流程弹窗 -->
    <a-modal
      v-model:visible="createVisible"
      title="新建审批流程"
      :on-before-ok="onCreate"
      :ok-loading="creating"
    >
      <a-form :model="{}" layout="vertical" size="small">
        <a-form-item label="流程名称" required>
          <a-input v-model="createName" placeholder="如：请假审批" />
        </a-form-item>
        <a-form-item label="实体" required>
          <a-select
            v-model="createTypePath"
            allow-search
            placeholder="选择要挂审批的实体"
            :options="entityOptions"
          />
        </a-form-item>
      </a-form>
    </a-modal>

    <a-modal
      :visible="insertVisible"
      :title="insertType === 'oa.cc' ? '添加知会节点' : insertType === 'oa.handle' ? '添加办理节点' : '添加审批节点'"
      ok-text="添加到画布"
      :on-before-ok="confirmInsert"
      @cancel="cancelInsert"
      @update:visible="(v: boolean) => !v && cancelInsert()"
    >
      <p class="wf-designer__hint" style="margin-bottom: 12px">
        {{
          insertType === 'oa.cc'
            ? '选择知会对象后，画布节点将显示「知会 · 对象」。'
            : insertType === 'oa.handle'
              ? '办理人只完成任务，不能驳回整单；画布节点将显示「办理 · 办理人」。'
              : '选择签核方式与审批人后，画布节点将显示「审批人 · 方式」。'
        }}
      </p>
      <a-form :model="{}" layout="vertical" size="small">
        <a-form-item v-if="insertType !== 'oa.cc'" label="签核模式">
          <a-radio-group v-model="insertMode" type="button" size="mini">
            <a-radio value="or">或签（任一人通过即可）</a-radio>
            <a-radio value="and">会签（全部通过才流转）</a-radio>
            <a-radio v-if="insertType === 'oa.approve'" value="sequence">依次（逐个审批）</a-radio>
          </a-radio-group>
        </a-form-item>
        <a-form-item
          :label="insertType === 'oa.cc' ? '知会对象' : insertType === 'oa.handle' ? '办理人' : '审批人'"
          required
        >
          <WorkflowAssigneePicker
            v-if="insertType !== 'oa.cc'"
            v-model:kind="insertToKind"
            v-model:model-value="insertToIds"
            :fields="assigneeFields"
          />
          <WorkflowRecipientPicker
            v-else
            v-model:kind="insertCcKind"
            v-model:model-value="insertToIds"
            multiple
          />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<style scoped>
.wf-designer {
  display: flex;
  flex-direction: column;
  margin: -12px;
  width: calc(100% + 24px);
  height: calc(100vh - 124px);
  min-height: 480px;
  box-sizing: border-box;
  background: var(--color-bg-2);
}
.wf-designer__empty {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}
.wf-designer__lock :deep(.arco-radio-button-content) {
  white-space: nowrap;
}
.wf-designer__hint {
  margin-top: 4px;
  font-size: 12px;
  color: var(--color-text-3);
  line-height: 1.4;
}
.wf-designer__canvas {
  flex: 1 1 auto;
  min-width: 0;
  min-height: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}
.wf-designer__flowgram {
  flex: 1 1 auto;
  min-height: 0;
  height: 100%;
  position: relative;
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
.wf-designer__xor-hint {
  color: var(--color-text-3);
  font-size: 12px;
  line-height: 1.6;
  background: var(--color-fill-1);
  padding: 8px;
  border-radius: 4px;
}
/* 无 label 的 form-item：Arco 默认将内容置为 flex 行，这里强制纵向堆叠（头部 + 内嵌面板） */
.wf-filter-item :deep(.arco-form-item-content) {
  display: block;
}
/* 内嵌筛选头部：表单标签 + 「且/或」开关（右对齐） */
.wf-filter-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 4px;
}
/* 左组：标签 + 说明符（与右侧开关分组，维持两端对齐） */
.wf-filter-head__left {
  display: flex;
  align-items: center;
  gap: 4px;
  flex: none;
}
/* 「发起条件」说明符：悬停展示语义（提交准入校验，不会自动发起） */
.wf-filter-head__hint {
  color: var(--color-text-3);
  cursor: help;
  flex: none;
}
/* 且/或开关不参与压缩，避免按钮内文字换行 */
.wf-filter-head :deep(.arco-radio-group) {
  flex: none;
}
.wf-filter-head__label {
  flex: none;
  white-space: nowrap;
  font-size: 14px;
  line-height: 22px;
  color: var(--color-text-2);
}
/* 抽屉 Footer：左侧辅助按钮 + 右侧操作区 */
.wf-drawer-footer {
  display: flex;
  align-items: center;
  gap: 8px;
}
.wf-drawer-footer__right {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-left: auto;
}
/* 常用语就地编辑列表 */
.wf-phrase-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
}
.wf-phrase-row {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 2px 4px 2px 8px;
  background: var(--color-fill-1);
  border-radius: 4px;
}
.wf-phrase-row > .arco-input-wrapper {
  flex: 1;
}
.wf-phrase-text {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 13px;
}
</style>

<style>
/* 抽屉挂到 body，容器铺满视口。关掉遮罩后让点击穿透到画布，只有抽屉面板自己接收事件 */
.wf-node-drawer.arco-drawer-container {
  pointer-events: none;
  background: transparent;
}
.wf-node-drawer .arco-drawer {
  pointer-events: auto;
}
.wf-node-drawer .arco-select,
.wf-node-drawer .arco-input-wrapper,
.wf-node-drawer .arco-input-number {
  width: 100%;
}
</style>
