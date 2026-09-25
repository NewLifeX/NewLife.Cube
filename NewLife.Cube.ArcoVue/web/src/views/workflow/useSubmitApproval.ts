import { computed, ref, toValue, unref, watch, type MaybeRefOrGetter, type Ref } from 'vue';
import type { WorkflowDefinitionItem } from '@newlifex/api-core';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { wfToKindOf } from '@/core/types/workflow';
import { xorCaseList } from './wfNodeCard';
import { parseGraph, topoChain } from './useWorkflowDesigner';
import { wfIdOf } from './useWorkflowProgress';

/**
 * 提交审批抽屉逻辑（OSC-26090347f1 T8c，IA §3.1）：
 * 定义下拉仅 Enable+Published+TypePath 匹配；N>100 截断告警；提交调 workflow.start；
 * 可选附件在发起成功后挂到实例（Attachment / Category=WorkflowComment）。
 *
 * 注意：typePath / ids 必须用 getter 或 ref 传入，禁止在 setup 里拍平 props 快照，
 * 否则打开抽屉后仍读到初始空 ids（表现为「已选 0 条」）。
 */

export const WF_SUBMIT_MAX = 100;

/** 与后端 AutomationPaths.NormalizeTypePath / 列表 typePath 对齐：去掉前导斜杠 */
export function normalizeWfTypePath(typePath: string | null | undefined): string {
  return String(typePath ?? '').trim().replace(/^\/+/, '');
}

/** 提交抽屉步骤预览行（design §4.4：只读，starterPick 行内选人） */
export interface WfSubmitStep {
  id: string;
  type: string;
  title: string;
  /** 六种选人类型（oa.xor 为空） */
  kind: string;
  mode: string;
  /** starterPick：提交人是否可多选 */
  multiple: boolean;
  /** oa.xor：第一个条件命中时的目标节点标题 */
  targetTitle: string;
  /** oa.xor：都不命中时的「其他情况」目标节点标题 */
  elseTitle: string;
}

/**
 * 从定义图解析步骤预览（只读）：跳过 start/end，保留审批/办理/知会/条件分流；
 * XOR 带第一个条件目标与「其他情况」目标，供「条件按第一条记录判断」预告。
 */
export function buildSubmitSteps(graphJson: string | null | undefined): WfSubmitStep[] {
  if (!graphJson) return [];
  let graph;
  try {
    graph = parseGraph(graphJson);
  } catch {
    return [];
  }
  if (!graph?.nodes?.length) return [];
  const byId = new Map(graph.nodes.map((n) => [n.id, n] as const));
  const nameOf = (id: string) => {
    const t = id ? byId.get(id) : undefined;
    const name = String(t?.data?.name ?? '').trim();
    return name || id;
  };
  const steps: WfSubmitStep[] = [];
  for (const n of topoChain(graph)) {
    if (n.type === 'oa.start' || n.type === 'oa.end') continue;
    const name = String(n.data?.name ?? '').trim();
    if (n.type === 'oa.xor' || n.type === 'oa.parallel') {
      const cases = xorCaseList(n.data);
      const defTarget = String(n.data?.defaultTarget ?? '').trim();
      steps.push({
        id: n.id,
        type: n.type,
        title: name || (n.type === 'oa.parallel' ? '并行分支' : '条件分流'),
        kind: '',
        mode: '',
        multiple: false,
        targetTitle: nameOf(cases[0]?.target ?? ''),
        elseTitle: nameOf(defTarget),
      });
      continue;
    }
    const to = n.data?.to as Record<string, unknown> | undefined;
    steps.push({
      id: n.id,
      type: n.type,
      title: name || (n.type === 'oa.handle' ? '办理' : n.type === 'oa.cc' ? '知会' : '审批'),
      kind: wfToKindOf(to),
      mode: String(n.data?.mode ?? 'or'),
      multiple: to?.multiple === true,
      targetTitle: '',
      elseTitle: '',
    });
  }
  const seen = new Set(steps.map((s) => s.id));
  for (const n of graph.nodes) {
    if (seen.has(n.id)) continue;
    if (n.type !== 'oa.approve' && n.type !== 'oa.handle' && n.type !== 'oa.cc') continue;
    const name = String(n.data?.name ?? '').trim();
    const to = n.data?.to as Record<string, unknown> | undefined;
    steps.push({
      id: n.id,
      type: n.type,
      title: name || (n.type === 'oa.handle' ? '办理' : n.type === 'oa.cc' ? '知会' : '审批'),
      kind: wfToKindOf(to),
      mode: String(n.data?.mode ?? 'or'),
      multiple: to?.multiple === true,
      targetTitle: '',
      elseTitle: '',
    });
  }
  return steps;
}

/** 提交人自选是否选齐：multiple=false 须恰好 1 人，true 至少 1 人（design §3.4） */
export function picksReady(steps: WfSubmitStep[], picks: Record<string, number[]>): boolean {
  return steps
    .filter((s) => s.kind === 'starterPick')
    .every((s) => {
      const ids = (picks[s.id] ?? []).filter((n) => n > 0);
      return s.multiple ? ids.length > 0 : ids.length === 1;
    });
}

/** XOR 预告；有并行分支时补一句同时进入 */
export function buildXorHint(steps: WfSubmitStep[]): string {
  const xor = steps.find((s) => s.type === 'oa.xor');
  const parallel = steps.find((s) => s.type === 'oa.parallel');
  const parts: string[] = [];
  if (xor) {
    parts.push('条件按第一条记录判断');
    if (xor.targetTitle) parts.push(`将走：${xor.targetTitle}`);
    if (xor.elseTitle) parts.push(`都不命中走：${xor.elseTitle}`);
  }
  if (parallel) parts.push('并行分支同时进入命中的分支，全部完成后汇合');
  return parts.join('，');
}

/** 步骤行标签（展示）：条件分流/知会/提交人自选/办理/会签/依次/或签 */
export function stepTag(s: WfSubmitStep): string {
  if (s.type === 'oa.xor') return '条件分流';
  if (s.type === 'oa.parallel') return '并行分支';
  if (s.type === 'oa.cc') return '知会';
  if (s.kind === 'starterPick') return '提交人自选';
  if (s.type === 'oa.handle') return s.mode === 'and' ? '会签' : '或签';
  return s.mode === 'and' ? '会签' : s.mode === 'sequence' ? '依次' : '或签';
}

/**
 * 雪花定义 Id 字符串透传（禁止 Number()：7501276170837360640 → 7501276170837361000）。
 * a-select 偶发把数字形字符串转成 number，这里统一收回 string。
 */
export function wfDefinitionIdOf(id: unknown): string {
  if (id == null || id === '') return '';
  if (typeof id === 'string') return id.trim();
  if (typeof id === 'number' && Number.isFinite(id)) return String(Math.trunc(id));
  return String(id).trim();
}

/**
 * Arco Upload 的 change 第一个参数才是文件列表，第二个是当前项。
 * 读错参数时界面上能看到文件，提交时 files 仍为空，附件不会上传。
 */
export function filesFromUploadChange(fileList: unknown, current?: unknown): File[] {
  const list = Array.isArray(fileList) ? fileList : Array.isArray(current) ? current : [];
  const files: File[] = [];
  for (const item of list) {
    if (item instanceof File) {
      files.push(item);
      continue;
    }
    const file = item && typeof item === 'object' ? (item as { file?: unknown }).file : undefined;
    if (file instanceof File) files.push(file);
  }
  return files;
}

export function useSubmitApproval(input: {
  typePath: MaybeRefOrGetter<string>;
  ids: MaybeRefOrGetter<(string | number)[]>;
  visible: Ref<boolean> | boolean;
}) {
  const definitions = ref<WorkflowDefinitionItem[]>([]);
  const definitionsLoading = ref(false);
  /** 定义雪花 Id：全程 string，禁止 number */
  const definitionId = ref<string | null>(null);
  const summary = ref('');
  const title = ref('');
  /** 发起附件（本地待上传，提交成功后挂实例） */
  const files = ref<File[]>([]);
  const saving = ref(false);
  /** 已截断的原始条数（>100 时仅提交前 100） */
  const truncated = ref(0);

  const liveIds = computed(() => {
    const raw = toValue(input.ids);
    return Array.isArray(raw) ? raw : [];
  });
  const liveTypePath = computed(() => String(toValue(input.typePath) ?? ''));

  const selectedCount = computed(() => liveIds.value.length);
  /** 可提交条数（>100 截断） */
  const submitCount = computed(() => Math.min(liveIds.value.length, WF_SUBMIT_MAX));
  /** 归一化实体路径（去掉前导斜杠，与后端存储/GetTypeBlock 一致） */
  const normalizedTypePath = computed(() => normalizeWfTypePath(liveTypePath.value));
  /** 可用定义（Enable && Published && TypePath 匹配；双方均归一化，防定义偶发带 /） */
  const usableDefinitions = computed(() =>
    definitions.value.filter(
      (d) =>
        d.enable === true &&
        d.published === true &&
        normalizeWfTypePath(d.typePath) === normalizedTypePath.value,
    ),
  );
  /** 当前选中定义（步骤预览数据源） */
  const selectedDefinition = computed(
    () => usableDefinitions.value.find((d) => wfDefinitionIdOf(d.id) === wfDefinitionIdOf(definitionId.value)) ?? null,
  );
  /** 步骤预览（同抽屉只读，design §4.4）；starterPick 行内选人 */
  const steps = computed(() => buildSubmitSteps(selectedDefinition.value?.graphJson));
  /** 提交人自选：节点 Id → 用户 Id */
  const picks = ref<Record<string, number[]>>({});
  /** XOR 预告行（无 XOR 为空） */
  const xorHint = computed(() => buildXorHint(steps.value));
  const picksSatisfied = computed(() => picksReady(steps.value, picks.value));
  const canSubmit = computed(
    () =>
      submitCount.value > 0 &&
      !!wfDefinitionIdOf(definitionId.value) &&
      usableDefinitions.value.length > 0 &&
      picksSatisfied.value,
  );

  /** 步骤行内选人：写入 picks（multiple=false 只留 1 人） */
  function setPick(nodeId: string, ids: number[]): void {
    const step = steps.value.find((s) => s.id === nodeId);
    const clean = (ids ?? []).filter((n) => Number.isFinite(n) && n > 0);
    picks.value = { ...picks.value, [nodeId]: step && !step.multiple ? clean.slice(0, 1) : clean };
  }

  async function loadDefinitions() {
    definitionsLoading.value = true;
    try {
      const res = await cubeApi.workflow.definitions({ typePath: normalizedTypePath.value });
      definitions.value = res.data ?? [];
      // 仅一条可用定义时自动选中，减少「选了却因 Id 精度丢了」的操作面
      const usable = usableDefinitions.value;
      if (usable.length === 1) definitionId.value = wfDefinitionIdOf(usable[0].id);
    } catch (err) {
      definitions.value = [];
      Message.error(formatApiError(err, '加载流程定义失败'));
    } finally {
      definitionsLoading.value = false;
    }
  }

  watch(
    () => unref(input.visible),
    (v) => {
      if (!v) return;
      truncated.value =
        liveIds.value.length > WF_SUBMIT_MAX ? liveIds.value.length - WF_SUBMIT_MAX : 0;
      definitionId.value = null;
      summary.value = '';
      title.value = '';
      files.value = [];
      picks.value = {};
      void loadDefinitions();
    },
  );

  // 切换定义时清空提交人自选（步骤集变化）
  watch(definitionId, () => {
    picks.value = {};
  });

  async function submit(): Promise<boolean> {
    const defId = wfDefinitionIdOf(definitionId.value);
    if (!canSubmit.value || !defId) return false;
    saving.value = true;
    try {
      const keys = liveIds.value.slice(0, WF_SUBMIT_MAX).map(String);
      const picksPayload: Record<string, number[]> = {};
      for (const s of steps.value) {
        if (s.kind !== 'starterPick') continue;
        const ids = (picks.value[s.id] ?? []).filter((n) => n > 0);
        if (ids.length) picksPayload[s.id] = ids;
      }
      const res = await cubeApi.workflow.start({
        typePath: normalizedTypePath.value,
        keys,
        definitionId: defId,
        summary: summary.value?.trim() || undefined,
        title: title.value?.trim() || undefined,
        picks: Object.keys(picksPayload).length ? picksPayload : undefined,
      });
      const instanceId = wfIdOf(res.data?.instanceId);
      if (instanceId && files.value.length) {
        for (const file of files.value) {
          try {
            await cubeApi.workflow.uploadAttachment(file, { instanceId });
          } catch (err) {
            Message.warning(formatApiError(err, `附件 ${file.name} 上传失败`));
          }
        }
      }
      if (truncated.value > 0) {
        Message.warning(`已提交前 ${WF_SUBMIT_MAX} 条，其余 ${truncated.value} 条未纳入本次审批`);
      }
      Message.success('已提交审批');
      return true;
    } catch (err) {
      Message.error(formatApiError(err, '提交失败'));
      return false;
    } finally {
      saving.value = false;
    }
  }

  return {
    definitions,
    definitionsLoading,
    definitionId,
    summary,
    title,
    files,
    saving,
    truncated,
    selectedCount,
    submitCount,
    usableDefinitions,
    selectedDefinition,
    steps,
    picks,
    setPick,
    xorHint,
    picksSatisfied,
    canSubmit,
    loadDefinitions,
    submit,
    wfDefinitionIdOf,
  };
}
