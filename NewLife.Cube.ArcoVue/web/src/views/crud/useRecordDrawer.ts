import { computed, ref, watch } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import type { EntityCommentModel } from '@cube/api-core';
import type { FieldMeta } from '@/core/types/field';
import { getValueByKey } from '@/core/utils/url';
import {
  applyFormLayout,
  estimateDetailLabelWidth,
  groupFieldsByCategory,
} from '@/core/utils/fieldGroups';
import type { FormLayout } from '@/core/utils/viewProfile';
import { formatApiError } from '@/core/utils/apiError';
import {
  detailFile,
  detailImage,
  detailText,
  detailUrl,
  jsonPreview,
} from '@/core/utils/detailFormat';
import { useUserStore } from '@/stores/user';
import cubeApi from '@/api';
import FormContent from './FormContent.vue';

/** RecordDrawer 组件 props 类型（与 RecordDrawer.vue defineProps 泛型逐字一致） */
interface RecordDrawerProps {
  visible: boolean;
  typePath: string;
  fields: FieldMeta[];
  model: Record<string, unknown>;
  mode: 'add' | 'edit' | 'detail';
  pkField: string;
  canEdit?: boolean;
  saving?: boolean;
  /** 是否显示历史/评论；新建与只读实体应为 false */
  showHistoryTabs?: boolean;
  /** 编辑/详情：当前页可见数据内上一条/下一条 */
  canPrev?: boolean;
  canNext?: boolean;
  /** 后端字段级错误（FieldErrors），映射到表单对应字段（OSC-0009） */
  fieldErrors?: { field: string; message: string }[];
  /** 受限表单布局（OSC-0013）：当前 mode 的字段排序/显隐/Category 折叠 */
  layout?: FormLayout | null;
}

/** RecordDrawer 组件 emits 类型（与 RecordDrawer.vue defineEmits 泛型逐字一致） */
interface RecordDrawerEmits {
  'update:visible': [boolean];
  save: [];
  edit: [];
  prev: [];
  next: [];
  'toggle-collapse': [category: string];
}

type RecordDrawerEmit = <K extends keyof RecordDrawerEmits>(event: K, ...args: RecordDrawerEmits[K]) => void;

/** RecordDrawer 组件全部业务 TS：表单抽屉、历史/评论 Tab（自 RecordDrawer.vue script setup 原样搬移） */
export function useRecordDrawer(props: RecordDrawerProps, emit: RecordDrawerEmit) {
  const userStore = useUserStore();

  const activeTab = ref('form');
  const formRef = ref<InstanceType<typeof FormContent>>();

  // ---- 历史 Tab（M4a） ----
  const historyActionOptions = [
    { value: '', label: '全部' },
    { value: 'Insert', label: '新增' },
    { value: 'Update', label: '更新' },
    { value: 'Delete', label: '删除' },
  ];
  const historyAction = ref('');
  const historyPage = ref(1);
  const historyTotal = ref(0);
  const historyLoading = ref(false);
  const historyRows = ref<Record<string, unknown>[]>([]);
  /** 最近一次加载历史的记录主键；记录切换时重置分页 */
  let lastHistoryId: unknown;

  // ---- 评论 Tab（M4b） ----
  type CommentNode = EntityCommentModel & {
    children?: CommentNode[];
    /** 头像地址；后端暂未返回，本人评论回落当前用户头像 */
    avatar?: string;
  };
  const commentLoading = ref(false);
  const commentSaving = ref(false);
  const commentText = ref('');
  const commentReplyTarget = ref<EntityCommentModel | null>(null);
  const commentReplyText = ref('');
  const comments = ref<EntityCommentModel[]>([]);

  const title = computed(() => {
    if (props.mode === 'add') return '新增';
    if (props.mode === 'edit') return '编辑';
    return '详情';
  });

  const width = computed(() => (props.fields.length > 10 ? 720 : 520));

  const showSideTabs = computed(
    () => props.mode !== 'add' && props.showHistoryTabs !== false,
  );

  const showNav = computed(() => props.mode === 'edit' || props.mode === 'detail');

  /** 详情分组：应用受限布局的 hidden/order/Category 折叠（OSC-0013） */
  const detailApplied = computed(() =>
    applyFormLayout(groupFieldsByCategory(props.fields), props.layout),
  );
  const detailGroups = computed(() => detailApplied.value.groups);
  const detailCollapsed = computed(() => new Set(detailApplied.value.collapsed));

  const detailLabelWidth = computed(() => estimateDetailLabelWidth(props.fields));
  const detailLabelStyle = computed(() => ({
    width: `${detailLabelWidth.value}px`,
    minWidth: `${detailLabelWidth.value}px`,
    maxWidth: `${detailLabelWidth.value}px`,
  }));
  const detailLabelCssVars = computed(() => ({
    '--detail-label-width': `${detailLabelWidth.value}px`,
  }));

  /** 评论线程树：顶层 + 最多两级回复（可对回复进行回复，最深不再展开） */
  const commentTree = computed<CommentNode[]>(() => {
    const top = comments.value.filter(
      (c) => c.parentId == null || Number(c.parentId) === 0,
    );
    const replies = comments.value.filter(
      (c) => c.parentId != null && Number(c.parentId) !== 0,
    );
    return top.map((t) => ({
      ...t,
      children: replies
        .filter((r) => String(r.parentId) === String(t.id))
        .map((r) => ({
          ...r,
          children: replies.filter((s) => String(s.parentId) === String(r.id)),
        })),
    }));
  });

  /** 详情取值统一入口 */
  function rawOf(field: FieldMeta): unknown {
    return getValueByKey(props.model, field.name);
  }

  /** 详情纯文本（dataSource/布尔/多选/JSON 摘要/常规字符串，安全输出） */
  function formatDetail(field: FieldMeta): string {
    return detailText(field, rawOf(field));
  }

  function detailImageOf(field: FieldMeta) {
    return detailImage(field, rawOf(field));
  }

  function detailUrlOf(field: FieldMeta) {
    return detailUrl(field, rawOf(field));
  }

  function detailFileOf(field: FieldMeta) {
    return detailFile(field, rawOf(field));
  }

  /** JSON 字段悬浮显示完整内容；其它字段无提示 */
  function detailTitle(field: FieldMeta): string | undefined {
    if ((field.itemType ?? '').trim().toLowerCase() === 'json') {
      return jsonPreview(rawOf(field), 4000);
    }
    return undefined;
  }

  async function loadHistory() {
    const id = getValueByKey(props.model, props.pkField);
    if (id == null || props.mode === 'add') {
      historyRows.value = [];
      historyTotal.value = 0;
      return;
    }
    // 记录切换（上一条/下一条）后，历史从第一页开始
    if (id !== lastHistoryId) {
      lastHistoryId = id;
      historyPage.value = 1;
    }
    const linkId = id as string | number;
    historyLoading.value = true;
    try {
      const res = await cubeApi.page.getList('/Admin/Log', {
        pageIndex: historyPage.value - 1,
        pageSize: 20,
        category: props.typePath.replace(/^\//, ''),
        linkId,
        ...(historyAction.value ? { action: historyAction.value } : {}),
      });
      historyRows.value = (res.data as Record<string, unknown>[]) ?? [];
      historyTotal.value = res.page?.totalCount ?? historyRows.value.length;
    } catch {
      historyRows.value = [];
      historyTotal.value = 0;
    } finally {
      historyLoading.value = false;
    }
  }

  function onHistoryActionChange() {
    historyPage.value = 1;
    loadHistory();
  }

  function onHistoryPageChange(page: number) {
    historyPage.value = page;
    loadHistory();
  }

  function historySuccess(row: Record<string, unknown>): boolean {
    const s = row.success ?? row.Success;
    return s === undefined ? true : !!s;
  }

  function historyActionLabel(action: unknown): string {
    const m = historyActionOptions.find((a) => a.value === String(action));
    return m?.label ?? String(action ?? '-');
  }

  async function loadComments() {
    const id = getValueByKey(props.model, props.pkField);
    if (id == null || props.mode === 'add') {
      comments.value = [];
      return;
    }
    const linkId = id as string | number;
    commentLoading.value = true;
    try {
      const res = await cubeApi.comment.getList({
        category: props.typePath.replace(/^\//, ''),
        linkId,
      });
      comments.value = (res.data as EntityCommentModel[]) ?? [];
    } catch {
      comments.value = [];
    } finally {
      commentLoading.value = false;
    }
  }

  function startCommentReply(c: EntityCommentModel) {
    commentReplyTarget.value = c;
    commentReplyText.value = '';
  }

  function cancelCommentReply() {
    commentReplyTarget.value = null;
    commentReplyText.value = '';
  }

  /** 顶层评论：提交新评论 */
  async function submitComment() {
    const id = getValueByKey(props.model, props.pkField);
    const content = commentText.value.trim();
    if (id == null || !content) return;
    const linkId = id as string | number;
    commentSaving.value = true;
    try {
      await cubeApi.comment.post({
        category: props.typePath.replace(/^\//, ''),
        linkId,
        content,
        parentId: 0,
      });
      Message.success('评论成功');
      commentText.value = '';
      await loadComments();
    } catch (err) {
      Message.error(formatApiError(err, '评论失败'));
    } finally {
      commentSaving.value = false;
    }
  }

  /** 回复：回复框内嵌于被回复的评论内部 */
  async function submitReply() {
    const id = getValueByKey(props.model, props.pkField);
    const content = commentReplyText.value.trim();
    const target = commentReplyTarget.value;
    if (id == null || !content || !target) return;
    const linkId = id as string | number;
    commentSaving.value = true;
    try {
      await cubeApi.comment.post({
        category: props.typePath.replace(/^\//, ''),
        linkId,
        content,
        parentId: target.id,
      });
      Message.success('回复成功');
      commentReplyText.value = '';
      commentReplyTarget.value = null;
      await loadComments();
    } catch (err) {
      Message.error(formatApiError(err, '回复失败'));
    } finally {
      commentSaving.value = false;
    }
  }

  /** 是否正在回复该评论（用于在评论内部显示回复框） */
  function isReplyTarget(c: EntityCommentModel): boolean {
    const t = commentReplyTarget.value;
    return t != null && String(t.id) === String(c.id);
  }

  /** 评论头像：本人评论回落当前用户头像；其余无头像信息，回落首字符 */
  function avatarOf(c: CommentNode): string {
    if (c.avatar) return c.avatar;
    const uid = userStore.userInfo?.id;
    const cid = c.createUserId;
    if (uid != null && cid != null && String(uid) === String(cid)) {
      return userStore.userInfo?.avatar ?? '';
    }
    return '';
  }

  /** 本人评论可删除；后端仍兜底（本人或管理员） */
  function canDeleteComment(c: EntityCommentModel): boolean {
    const uid = userStore.userInfo?.id;
    const cid = c.createUserId;
    return uid != null && cid != null && String(uid) === String(cid);
  }

  function removeComment(c: EntityCommentModel) {
    const id = c.id;
    if (id == null) return;
    Modal.confirm({
      title: '删除评论？',
      content: '删除后不可恢复',
      onOk: async () => {
        try {
          await cubeApi.comment.remove(id);
          Message.success('删除成功');
          await loadComments();
        } catch (err) {
          Message.error(formatApiError(err, '删除失败'));
        }
      },
    });
  }

  async function onSave() {
    try {
      await formRef.value?.validate();
    } catch {
      return;
    }
    emit('save');
  }

  // 记录切换（上一条/下一条）时，清空未提交的回复编辑状态
  watch(
    () => getValueByKey(props.model, props.pkField),
    () => {
      commentReplyTarget.value = null;
      commentReplyText.value = '';
    },
  );

  // 依赖记录主键：上一条/下一条切换记录时（model 原地更新、引用不变），历史/讨论同步重载
  watch(
    () =>
      [
        props.visible,
        activeTab.value,
        getValueByKey(props.model, props.pkField),
      ] as const,
    ([vis, tab]) => {
      if (!vis || !showSideTabs.value) return;
      if (tab === 'history') loadHistory();
      else if (tab === 'comment') loadComments();
    },
  );

  watch(
    () => props.visible,
    (v) => {
      if (v) {
        activeTab.value = 'form';
        commentReplyTarget.value = null;
        commentReplyText.value = '';
        lastHistoryId = null;
      }
    },
  );

  return {
    activeTab,
    formRef,
    historyActionOptions,
    historyAction,
    historyPage,
    historyTotal,
    historyLoading,
    historyRows,
    commentLoading,
    commentSaving,
    commentText,
    commentReplyTarget,
    commentReplyText,
    comments,
    title,
    width,
    showSideTabs,
    showNav,
    detailGroups,
    detailCollapsed,
    detailLabelStyle,
    detailLabelCssVars,
    commentTree,
    rawOf,
    formatDetail,
    detailImageOf,
    detailUrlOf,
    detailFileOf,
    detailTitle,
    onHistoryActionChange,
    onHistoryPageChange,
    historySuccess,
    historyActionLabel,
    startCommentReply,
    cancelCommentReply,
    submitComment,
    submitReply,
    isReplyTarget,
    avatarOf,
    canDeleteComment,
    removeComment,
    onSave,
  };
}
