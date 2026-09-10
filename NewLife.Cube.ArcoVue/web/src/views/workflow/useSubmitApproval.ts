import { computed, ref, toValue, unref, watch, type MaybeRefOrGetter, type Ref } from 'vue';
import type { WorkflowDefinitionItem } from '@cube/api-core';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
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

export function useSubmitApproval(input: {
  typePath: MaybeRefOrGetter<string>;
  ids: MaybeRefOrGetter<(string | number)[]>;
  visible: Ref<boolean> | boolean;
}) {
  const definitions = ref<WorkflowDefinitionItem[]>([]);
  const definitionsLoading = ref(false);
  /** 定义雪花 Id：全程 string，禁止 number */
  const definitionId = ref<string | null>(null);
  const comment = ref('');
  const summary = ref('');
  const title = ref('');
  /** 发起附件（本地待上传，提交成功后挂实例） */
  const files = ref<File[]>([]);
  const phrases = ref<string[]>([]);
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
  const canSubmit = computed(
    () =>
      submitCount.value > 0 &&
      !!wfDefinitionIdOf(definitionId.value) &&
      usableDefinitions.value.length > 0,
  );

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

  async function loadPhrases() {
    try {
      const res = await cubeApi.workflow.phrases();
      phrases.value = (res.data ?? []).map((p) => p.text);
    } catch {
      phrases.value = [];
    }
  }

  watch(
    () => unref(input.visible),
    (v) => {
      if (!v) return;
      truncated.value =
        liveIds.value.length > WF_SUBMIT_MAX ? liveIds.value.length - WF_SUBMIT_MAX : 0;
      definitionId.value = null;
      comment.value = '';
      summary.value = '';
      title.value = '';
      files.value = [];
      void loadDefinitions();
      void loadPhrases();
    },
  );

  async function submit(): Promise<boolean> {
    const defId = wfDefinitionIdOf(definitionId.value);
    if (!canSubmit.value || !defId) return false;
    saving.value = true;
    try {
      const keys = liveIds.value.slice(0, WF_SUBMIT_MAX).map(String);
      const res = await cubeApi.workflow.start({
        typePath: normalizedTypePath.value,
        keys,
        definitionId: defId,
        comment: comment.value || undefined,
        summary: summary.value?.trim() || undefined,
        title: title.value?.trim() || undefined,
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
    comment,
    summary,
    title,
    files,
    phrases,
    saving,
    truncated,
    selectedCount,
    submitCount,
    usableDefinitions,
    canSubmit,
    loadDefinitions,
    submit,
    wfDefinitionIdOf,
  };
}
