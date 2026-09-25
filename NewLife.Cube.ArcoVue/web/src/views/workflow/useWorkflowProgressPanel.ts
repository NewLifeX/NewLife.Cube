import { computed, ref, watch, type Ref } from 'vue';
import type { WorkflowPhrase } from '@newlifex/api-core';
import cubeApi from '@/api';
import { actionLabel, useWorkflowProgress, wfIdOf, type WfId } from './useWorkflowProgress';
import { nodeFlags, opinionTimeline, previewKind, rollbackTargets, splitAttachments, type ProgressAttachment } from './wfProgressFlow';

/**
 * 审批进度面板编排（OSC-26090347f1 T8c / T13；T3g 起为「意见时间线 + 查看流程只读画布」）。
 */
export function useWorkflowProgressPanel(input: {
  visible: Ref<boolean>;
  instanceId: () => WfId | null | undefined;
}) {
  const activeInstanceId = ref<string | null>(null);
  const phrases = ref<WorkflowPhrase[]>([]);

  watch(
    () => (input.visible.value ? wfIdOf(input.instanceId()) || null : null),
    (v) => {
      activeInstanceId.value = v;
    },
    { immediate: true },
  );

  const wf = useWorkflowProgress(activeInstanceId);

  /** 办理节点只有「已办理」；知会只有「已阅」；审批节点才有同意/驳回 */
  const isHandle = computed(() => wf.myTask.value?.nodeType === 'oa.handle');
  const isCc = computed(() => wf.myTask.value?.nodeType === 'oa.cc');
  const opinionEvents = computed(() => opinionTimeline(wf.detail.value));
  const preview = ref<{ title: string; url: string; kind: 'image' | 'pdf' | 'text'; text?: string } | null>(null);

  async function openAttachment(file: ProgressAttachment) {
    const kind = previewKind(file.fileName);
    if (!file.url || kind === 'none') {
      if (file.url) window.open(file.url, '_blank', 'noopener');
      return;
    }
    if (kind === 'text') {
      try {
        const res = await fetch(file.url);
        preview.value = { title: file.fileName, url: file.url, kind, text: await res.text() };
      } catch {
        window.open(file.url, '_blank', 'noopener');
      }
      return;
    }
    preview.value = { title: file.fileName, url: file.url, kind };
  }
  const flags = computed(() => nodeFlags(wf.detail.value, wf.myTask.value?.nodeId));
  const rollbackOptions = computed(() => rollbackTargets(wf.detail.value, wf.myTask.value?.nodeId));
  /** 发起附件（key=实例Id）；意见附件由节点下人员条目列出 */
  const starterAttachments = computed(
    () => splitAttachments(wf.detail.value?.attachments, String(wf.detail.value?.id ?? '')).starter,
  );
  /** 当前用户可写字段（InstanceDetail.writableFields；无待办则为空） */
  const writableFieldNames = computed(() => {
    const d = wf.detail.value as { writableFields?: unknown } | null;
    if (!d || !wf.myTask.value) return [] as string[];
    const raw = d.writableFields;
    if (!Array.isArray(raw)) return [];
    return raw.map(String).filter(Boolean);
  });

  async function loadPhrases() {
    try {
      const res = await cubeApi.workflow.phrases();
      phrases.value = res.data ?? [];
    } catch {
      phrases.value = [];
    }
  }
  void loadPhrases();

  const timelineItems = computed(() => {
    const items: {
      key: string;
      title: string;
      user: string;
      time?: string;
      content?: string;
    }[] = [];
    const d = wf.detail.value;
    if (!d) return items;
    for (const c of d.comments ?? []) {
      items.push({
        key: `c${c.id}`,
        title: actionLabel(c.action),
        user: c.createUser || `#${c.taskId ?? ''}`,
        time: c.createTime,
        content: c.content || undefined,
      });
    }
    return items;
  });

  return {
    detail: wf.detail,
    loading: wf.loading,
    error: wf.error,
    myTask: wf.myTask,
    isHandle,
    isCc,
    canWithdraw: wf.canWithdraw,
    statusMeta: wf.statusMeta,
    opinionEvents,
    preview,
    openAttachment,
    closePreview: () => {
      preview.value = null;
    },
    flags,
    rollbackOptions,
    starterAttachments,
    writableFieldNames,
    timelineItems,
    phrases,
    reload: wf.load,
  };
}
