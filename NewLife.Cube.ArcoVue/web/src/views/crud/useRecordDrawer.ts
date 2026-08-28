import { computed, ref, watch } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import type { EntityCommentModel } from '@cube/api-core';
import type { FieldMeta } from '@/core/types/field';
import { getValueByKey } from '@/core/utils/url';
import { isIamRowActionDisabled } from '@/core/utils/iamGuards';
import { isRolePermissionField } from '@/core/utils/rolePermission';
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
import { filterDetailAuditFields } from '@/core/utils/auditDisplay';
import { isCascaderField } from '@/core/utils/fieldControl';
import { fetchBatchLabel } from '@/core/utils/lov-api';
import { mergeAreaLabel } from '@/core/utils/areaLabels';
import { parseRemarkDiff, type RemarkDiff } from '@/core/utils/logRemarkDiff';
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
  /** 日历/甘特等：详情内展示自定义链接（OSC-2608178bdb） */
  opsCustomLinks?: import('@/core/utils/opsAction').OpsCustomLink[];
}

/** RecordDrawer 组件 emits 类型（与 RecordDrawer.vue defineEmits 泛型逐字一致） */
interface RecordDrawerEmits {
  'update:visible': [boolean];
  save: [];
  edit: [];
  prev: [];
  next: [];
  'toggle-collapse': [category: string];
  'ops-link': [link: import('@/core/utils/opsAction').OpsCustomLink];
}

type RecordDrawerEmit = <K extends keyof RecordDrawerEmits>(event: K, ...args: RecordDrawerEmits[K]) => void;

/** RecordDrawer 组件全部业务 TS：表单抽屉、历史/评论 Tab（自 RecordDrawer.vue script setup 原样搬移） */
export function useRecordDrawer(props: RecordDrawerProps, emit: RecordDrawerEmit) {
  const userStore = useUserStore();

  const activeTab = ref('form');
  const formRef = ref<InstanceType<typeof FormContent>>();
  /** 抽屉无实体删除按钮；与列表一致：系统角色禁止删除 */
  const entityDeleteLocked = computed(() =>
    isIamRowActionDisabled(props.typePath, props.model, 'delete'),
  );

  /** 角色 Permission：详情与编辑/添加共用菜单树 */
  function isRolePermField(field: FieldMeta) {
    return isRolePermissionField(props.typePath, field);
  }

    // ---- 历史 Tab（M4a）；Automation 写入系统 Log，可与增删改一并筛选 ----
  const historyActionOptions = [
    { value: '', label: '全部' },
    { value: 'Insert', label: '新增' },
    { value: 'Update', label: '更新' },
    { value: 'Delete', label: '删除' },
    { value: 'Automation', label: '自动化' },
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

  /** 详情展示字段：有名称快照时隐藏审计 ID 列（创建用户/更新用户） */
  const detailViewFields = computed(() => filterDetailAuditFields(props.fields));

  /** 详情分组：应用受限布局的 hidden/order/Category 折叠（OSC-0013） */
  const detailApplied = computed(() =>
    applyFormLayout(groupFieldsByCategory(detailViewFields.value), props.layout),
  );
  const detailGroups = computed(() => detailApplied.value.groups);
  const detailCollapsed = computed(() => new Set(detailApplied.value.collapsed));

  const detailLabelWidth = computed(() => estimateDetailLabelWidth(detailViewFields.value));
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

  /** 当前行地区叶子标签缓存（打开抽屉时补齐，OSC-2608139feb） */
  const rowAreaLabels = ref<Record<string, string>>({});

  /** 详情纯文本（dataSource/布尔/多选/JSON 摘要/常规字符串，安全输出） */
  function formatDetail(field: FieldMeta): string {
    return detailText(field, rawOf(field), { areaLabelCache: rowAreaLabels.value });
  }

  /** 打开详情/编辑前补齐当前行标签：地区叶子 getDetail + LIST LOV BatchLabel（OSC-2608139feb） */
  async function hydrateRowLabels() {
    if (props.mode === 'add') return;
    for (const f of props.fields) {
      const v = rawOf(f);
      if (v == null || v === '') continue;
      if (isCascaderField(f)) {
        if (rowAreaLabels.value[String(v)]) continue;
        try {
          const res = await cubeApi.page.getDetail<Record<string, unknown>>('/Cube/Area', v as number | string);
          const data = (res as unknown as { data?: Record<string, unknown> })?.data ?? res;
          if (data && typeof data === 'object') {
            const rec = data as Record<string, unknown>;
            mergeAreaLabel(rowAreaLabels.value, v, (rec.name ?? rec.Name) as unknown);
          }
        } catch {
          /* ignore */
        }
      } else if (f.lovCode && !(f.dataSource && Object.keys(f.dataSource).length)) {
        try {
          const map = await fetchBatchLabel({ lovCode: f.lovCode, values: [String(v)] });
          f.dataSource = { ...(f.dataSource || {}), ...map };
        } catch {
          /* ignore */
        }
      }
    }
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
        pageIndex: historyPage.value,
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
    const raw = String(action ?? '-');
    const m = historyActionOptions.find((a) => a.value === raw);
    if (m) return m.label;
    const lower = raw.toLowerCase();
    if (lower === 'insert' || raw === '添加' || raw === '新增' || lower === 'add') return '新增';
    if (lower === 'update' || raw === '修改' || raw === '编辑' || lower === 'edit') return '更新';
    if (lower === 'delete' || raw === '删除') return '删除';
    if (lower === 'automation' || raw === '自动化') return '自动化';
    return raw;
  }

  /** 历史备注：自动化日志 Remark 为 JSON 时展示 detail */
  function historyRemark(row: Record<string, unknown>): string {
    const raw = String(row.remark ?? row.Remark ?? '');
    if (!raw) return '';
    const action = String(row.action ?? row.Action ?? '');
    if (action.toLowerCase() !== 'automation' && action !== '自动化') return raw;
    try {
      const o = JSON.parse(raw) as { detail?: string; error?: string };
      if (o?.detail) return o.detail;
      if (o?.error) return o.error;
    } catch {
      /* 非 JSON 原样 */
    }
    return raw;
  }

  /**
   * 历史字段 diff（OSC-260819e483 P4）：Update/修改/Edit 动作解析 Remark 为字段新旧值表。
   * 用抽屉已有 fields 作锚点（长名优先、忽略大小写）；失败回落 null → 模板显示 historyRemark 原文。
   */
  function historyDiff(row: Record<string, unknown>): RemarkDiff[] | null {
    const raw = String(row.remark ?? row.Remark ?? '');
    if (!raw) return null;
    const action = String(row.action ?? row.Action ?? '');
    const lower = action.toLowerCase();
    if (lower !== 'update' && action !== '修改' && action !== '编辑' && lower !== 'edit') return null;
    return parseRemarkDiff(raw, props.fields);
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
        // 详情/编辑打开前补齐地区与 LOV 标签（OSC-2608139feb）
        void hydrateRowLabels();
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
    hydrateRowLabels,
    detailImageOf,
    detailUrlOf,
    detailFileOf,
    detailTitle,
    onHistoryActionChange,
    onHistoryPageChange,
    historySuccess,
    historyActionLabel,
    historyRemark,
    historyDiff,
    startCommentReply,
    cancelCommentReply,
    submitComment,
    submitReply,
    isReplyTarget,
    avatarOf,
    canDeleteComment,
    removeComment,
    onSave,
    entityDeleteLocked,
    isRolePermField,
  };
}
