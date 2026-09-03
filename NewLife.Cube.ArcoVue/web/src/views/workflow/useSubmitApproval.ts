import { computed, ref, unref, watch, type Ref } from 'vue';
import type { WorkflowDefinitionItem } from '@cube/api-core';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';

/**
 * 提交审批抽屉逻辑（OSC-26090347f1 T8c，IA §3.1）：
 * 定义下拉仅 Enable+Published+TypePath 匹配；N>100 截断告警；提交调 workflow.start。
 */

export const WF_SUBMIT_MAX = 100;

export function useSubmitApproval(props: {
  typePath: string;
  ids: (string | number)[];
  visible: Ref<boolean> | boolean;
}) {
  const definitions = ref<WorkflowDefinitionItem[]>([]);
  const definitionsLoading = ref(false);
  const definitionId = ref<number | null>(null);
  const comment = ref('');
  const phrases = ref<string[]>([]);
  const saving = ref(false);
  /** 已截断的原始条数（>100 时仅提交前 100） */
  const truncated = ref(0);

  const selectedCount = computed(() => props.ids.length);
  /** 可提交条数（>100 截断） */
  const submitCount = computed(() => Math.min(props.ids.length, WF_SUBMIT_MAX));
  /** 归一化实体路径（去掉前导斜杠，与后端存储/GetTypeBlock 一致） */
  const normalizedTypePath = computed(() => String(props.typePath ?? '').replace(/^\/\/+/, ''));
  /** 可用定义（Enable && Published && TypePath 匹配） */
  const usableDefinitions = computed(() =>
    definitions.value.filter(
      (d) => d.enable === true && d.published === true && (d.typePath || '') === normalizedTypePath.value,
    ),
  );
  const canSubmit = computed(
    () => submitCount.value > 0 && !!definitionId.value && usableDefinitions.value.length > 0,
  );

  async function loadDefinitions() {
    definitionsLoading.value = true;
    try {
      const res = await cubeApi.workflow.definitions({ typePath: normalizedTypePath.value });
      definitions.value = res.data ?? [];
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
    () => unref(props.visible),
    (v) => {
      if (!v) return;
      truncated.value = props.ids.length > WF_SUBMIT_MAX ? props.ids.length - WF_SUBMIT_MAX : 0;
      definitionId.value = null;
      comment.value = '';
      void loadDefinitions();
      void loadPhrases();
    },
  );

  async function submit(): Promise<boolean> {
    if (!canSubmit.value) return false;
    saving.value = true;
    try {
      const keys = props.ids.slice(0, WF_SUBMIT_MAX).map(String);
      await cubeApi.workflow.start({
        typePath: normalizedTypePath.value,
        keys,
        definitionId: definitionId.value as number,
        comment: comment.value || undefined,
      });
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
    phrases,
    saving,
    truncated,
    selectedCount,
    submitCount,
    usableDefinitions,
    canSubmit,
    loadDefinitions,
    submit,
  };
}
