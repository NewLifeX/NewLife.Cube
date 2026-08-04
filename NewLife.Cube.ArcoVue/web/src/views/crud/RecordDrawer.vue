<template>
  <a-drawer
    :visible="visible"
    :width="width"
    unmount-on-close
    placement="right"
    class="record-drawer"
    @update:visible="(v: boolean) => emit('update:visible', v)"
  >
    <template #title>
      <div class="drawer-title">
        <a-space v-if="showNav" :size="2" class="drawer-nav">
          <a-tooltip content="上一条">
            <a-button type="text" size="mini" :disabled="!canPrev" @click="emit('prev')">
              <template #icon><IconUp /></template>
            </a-button>
          </a-tooltip>
          <a-tooltip content="下一条">
            <a-button type="text" size="mini" :disabled="!canNext" @click="emit('next')">
              <template #icon><IconDown /></template>
            </a-button>
          </a-tooltip>
        </a-space>
        <span class="drawer-title__text">{{ title }}</span>
      </div>
    </template>

    <!-- 新建 / 只读实体：不展示历史、评论 Tab -->
    <template v-if="!showSideTabs">
      <FormContent
        v-if="mode !== 'detail'"
        ref="formRef"
        :fields="fields"
        :model="model"
        :type-path="typePath"
        :mode="mode === 'add' ? 'add' : 'edit'"
      />
      <div v-else class="detail-grouped" :style="detailLabelCssVars">
        <section
          v-for="group in detailGroups"
          :key="group.category || '__default'"
          class="detail-group"
        >
          <div v-if="group.title" class="detail-group__title">{{ group.title }}</div>
          <div class="detail-fields">
            <div v-for="field in group.fields" :key="field.name" class="detail-field">
              <div class="detail-field__label" :style="detailLabelStyle">
                {{ field.displayName || field.name }}
              </div>
              <div class="detail-field__value">{{ formatDetail(field) }}</div>
            </div>
          </div>
        </section>
      </div>
    </template>

    <a-tabs v-else v-model:active-key="activeTab">
      <a-tab-pane key="form" :title="mode === 'edit' ? '编辑' : '详情'">
        <FormContent
          v-if="mode !== 'detail'"
          ref="formRef"
          :fields="fields"
          :model="model"
          :type-path="typePath"
          mode="edit"
        />
        <div v-else class="detail-grouped" :style="detailLabelCssVars">
          <section
            v-for="group in detailGroups"
            :key="group.category || '__default'"
            class="detail-group"
          >
            <div v-if="group.title" class="detail-group__title">{{ group.title }}</div>
            <div class="detail-fields">
              <div v-for="field in group.fields" :key="field.name" class="detail-field">
                <div class="detail-field__label" :style="detailLabelStyle">
                  {{ field.displayName || field.name }}
                </div>
                <div class="detail-field__value">{{ formatDetail(field) }}</div>
              </div>
            </div>
          </section>
        </div>
      </a-tab-pane>
      <a-tab-pane key="history" title="历史">
        <div class="side-tab-toolbar">
          <a-select v-model="historyAction" size="small" style="width: 110px" @change="onHistoryActionChange">
            <a-option v-for="a in historyActionOptions" :key="a.value" :value="a.value">
              {{ a.label }}
            </a-option>
          </a-select>
        </div>
        <a-spin :loading="historyLoading" style="width: 100%">
          <a-empty v-if="!historyRows.length" description="暂无历史记录" />
          <a-timeline v-else>
            <a-timeline-item v-for="(row, idx) in historyRows" :key="idx">
              <div class="history-time">{{ formatDateTime(row.createTime ?? row.CreateTime) }}</div>
              <div class="history-meta">
                <a-tag :color="historySuccess(row) ? 'green' : 'red'" size="small">
                  {{ historyActionLabel(row.action ?? row.Action) }}
                </a-tag>
                <span class="history-user">{{ row.createUser ?? row.CreateUser }}</span>
              </div>
              <div class="history-remark">{{ row.remark ?? row.Remark }}</div>
            </a-timeline-item>
          </a-timeline>
          <a-pagination
            v-if="historyTotal > 0"
            class="side-tab-pager"
            :current="historyPage"
            :page-size="20"
            :total="historyTotal"
            @change="onHistoryPageChange"
          />
        </a-spin>
      </a-tab-pane>
      <a-tab-pane key="comment" title="讨论">
        <!-- 新评论：仅顶层；回复在对应评论内部进行 -->
        <div class="comment-box">
          <a-textarea
            v-model="commentText"
            placeholder="写下你的讨论内容…"
            :max-length="500"
            allow-clear
          />
          <div class="comment-actions">
            <a-button
              size="mini"
              type="primary"
              :disabled="!commentText.trim()"
              :loading="commentSaving"
              @click="submitComment"
            >
              发送
            </a-button>
          </div>
        </div>
        <a-spin :loading="commentLoading" style="width: 100%">
          <a-empty v-if="!commentTree.length" description="暂无评论" />
          <div v-else class="comment-list">
            <a-comment
              v-for="top in commentTree"
              :key="String(top.id ?? '')"
              class="comment-card"
            >
              <template #avatar>
                <UserAvatar :name="top.createUser" :avatar="avatarOf(top)" />
              </template>
              <template #author>
                <span class="comment-user">{{ top.createUser ?? '-' }}</span>
              </template>
              <template #datetime>
                <span class="comment-time">{{ formatDateTime(top.createTime) }}</span>
              </template>
              <template #content>
                <div class="comment-content">{{ top.content }}</div>
              </template>
              <template #actions>
                <a-button type="text" size="mini" @click="startCommentReply(top)">回复</a-button>
                <a-button
                  v-if="canDeleteComment(top)"
                  type="text"
                  size="mini"
                  status="danger"
                  @click="removeComment(top)"
                >
                  删除
                </a-button>
              </template>

              <!-- 回复列表 -->
              <a-comment
                v-for="reply in top.children"
                :key="String(reply.id ?? '')"
                class="comment-card comment-card--reply"
              >
                <template #avatar>
                  <UserAvatar :name="reply.createUser" :avatar="avatarOf(reply)" />
                </template>
                <template #author>
                  <span class="comment-user">{{ reply.createUser ?? '-' }}</span>
                </template>
                <template #datetime>
                  <span class="comment-time">{{ formatDateTime(reply.createTime) }}</span>
                </template>
                <template #content>
                  <div class="comment-content">
                    <span v-if="reply.replyUser" class="comment-at">@{{ reply.replyUser }}</span>
                    {{ reply.content }}
                  </div>
                </template>
                <template #actions>
                  <a-button type="text" size="mini" @click="startCommentReply(reply)">回复</a-button>
                  <a-button
                    v-if="canDeleteComment(reply)"
                    type="text"
                    size="mini"
                    status="danger"
                    @click="removeComment(reply)"
                  >
                    删除
                  </a-button>
                </template>

                <!-- 回复的回复（第 2 级，最深）：不再允许继续回复 -->
                <a-comment
                  v-for="sub in reply.children"
                  :key="String(sub.id ?? '')"
                  class="comment-card comment-card--reply"
                >
                  <template #avatar>
                    <UserAvatar :name="sub.createUser" :avatar="avatarOf(sub)" />
                  </template>
                  <template #author>
                    <span class="comment-user">{{ sub.createUser ?? '-' }}</span>
                  </template>
                  <template #datetime>
                    <span class="comment-time">{{ formatDateTime(sub.createTime) }}</span>
                  </template>
                  <template #content>
                    <div class="comment-content">
                      <span v-if="sub.replyUser" class="comment-at">@{{ sub.replyUser }}</span>
                      {{ sub.content }}
                    </div>
                  </template>
                  <template #actions>
                    <a-button
                      v-if="canDeleteComment(sub)"
                      type="text"
                      size="mini"
                      status="danger"
                      @click="removeComment(sub)"
                    >
                      删除
                    </a-button>
                  </template>
                </a-comment>

                <!-- 回复编辑框：内嵌于被回复的评论内部 -->
                <CommentReplyEditor
                  v-if="isReplyTarget(reply)"
                  :target="commentReplyTarget"
                  v-model="commentReplyText"
                  :saving="commentSaving"
                  @submit="submitReply"
                  @cancel="cancelCommentReply"
                />
              </a-comment>

              <!-- 回复编辑框：内嵌于被回复的顶层评论内部 -->
              <CommentReplyEditor
                v-if="isReplyTarget(top)"
                :target="commentReplyTarget"
                v-model="commentReplyText"
                :saving="commentSaving"
                @submit="submitReply"
                @cancel="cancelCommentReply"
              />
            </a-comment>
          </div>
        </a-spin>
      </a-tab-pane>
    </a-tabs>

    <template #footer>
      <a-space>
        <a-button @click="emit('update:visible', false)">取消</a-button>
        <a-button
          v-if="mode !== 'detail'"
          type="primary"
          :loading="saving"
          @click="onSave"
        >
          保存
        </a-button>
        <a-button v-else-if="canEdit" type="primary" @click="emit('edit')">编辑</a-button>
      </a-space>
    </template>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { Message, Modal } from '@arco-design/web-vue';
import { IconDown, IconUp } from '@arco-design/web-vue/es/icon';
import type { EntityCommentModel } from '@cube/api-core';
import type { FieldMeta } from '@/core/types/field';
import { getValueByKey } from '@/core/utils/url';
import {
  estimateDetailLabelWidth,
  groupFieldsByCategory,
} from '@/core/utils/fieldGroups';
import { formatApiError } from '@/core/utils/apiError';
import { formatDateTime } from '@/core/utils/datetime';
import { useUserStore } from '@/stores/user';
import cubeApi from '@/api';
import CommentReplyEditor from '@/components/CommentReplyEditor.vue';
import UserAvatar from '@/components/UserAvatar.vue';
import FormContent from './FormContent.vue';

const props = withDefaults(
  defineProps<{
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
  }>(),
  { showHistoryTabs: true, canPrev: false, canNext: false },
);

const emit = defineEmits<{
  'update:visible': [boolean];
  save: [];
  edit: [];
  prev: [];
  next: [];
}>();

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

const detailGroups = computed(() => groupFieldsByCategory(props.fields));

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

function formatDetail(field: FieldMeta) {
  const v = getValueByKey(props.model, field.name);
  if (v == null || v === '') return '-';
  if (field.dataSource && field.dataSource[String(v)] != null) {
    return field.dataSource[String(v)];
  }
  if (typeof v === 'boolean') return v ? '是' : '否';
  return String(v);
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

defineExpose({ validate: () => formRef.value?.validate() });
</script>

<style scoped>
.drawer-title {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}
.drawer-nav :deep(.arco-btn) {
  padding: 0 6px;
  color: var(--color-text-2);
}
.drawer-nav :deep(.arco-btn:not(.arco-btn-disabled):hover) {
  color: rgb(var(--primary-6));
}
.drawer-title__text {
  font-weight: 500;
}

.detail-grouped {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin: -4px -4px 0;
  padding: 4px;
  background: var(--color-fill-2);
  border-radius: 8px;
}
.detail-group {
  padding: 16px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
}
.detail-group__title {
  margin-bottom: 12px;
  font-size: 14px;
  font-weight: 600;
  line-height: 22px;
  color: var(--color-text-1);
}
.detail-fields {
  display: flex;
  flex-direction: column;
  gap: 0;
  border: 1px solid var(--color-border-2);
  border-radius: var(--border-radius-small);
  overflow: hidden;
}
.detail-field {
  display: flex;
  align-items: stretch;
  gap: 0;
  min-height: 36px;
  border-bottom: 1px solid var(--color-border-2);
  background: var(--color-bg-2);
}
.detail-field:last-child {
  border-bottom: none;
}
.detail-field__label {
  flex: 0 0 auto;
  display: flex;
  align-items: flex-start;
  padding: 8px 12px;
  background-color: var(--color-fill-3);
  color: var(--color-text-2);
  text-align: left;
  line-height: 22px;
  box-sizing: border-box;
}
.detail-field__value {
  flex: 1;
  min-width: 0;
  padding: 8px 12px;
  background-color: var(--color-bg-2);
  color: var(--color-text-1);
  line-height: 22px;
  word-break: break-word;
}

/* 历史 Tab */
.side-tab-toolbar {
  margin-bottom: 12px;
}
.side-tab-pager {
  margin-top: 12px;
  display: flex;
  justify-content: flex-end;
}
.history-time {
  font-size: var(--cube-font-size-meta);
  color: var(--color-text-3);
}
.history-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 2px 0 4px;
}
.history-user {
  font-size: var(--cube-font-size-meta);
  color: var(--color-text-2);
}
.history-remark {
  font-size: var(--cube-font-size-body);
  color: var(--color-text-1);
  white-space: pre-wrap;
  word-break: break-word;
}

/* 评论 Tab */
.comment-box {
  margin-bottom: 16px;
}
.comment-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 8px;
}
.comment-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.comment-list > .comment-card {
  margin-top: 0;
}
.comment-card {
  padding: 12px;
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  background: var(--color-bg-2);
}
.comment-card--reply {
  background: var(--color-fill-1);
  border-color: var(--color-border);
}
.comment-card :deep(.arco-comment-inner-content) {
  min-width: 0;
}
.comment-card :deep(.arco-comment-inner-comment) {
  margin-top: 8px;
}
.comment-card :deep(.arco-comment-actions) {
  margin-top: 4px;
}
.comment-user {
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
}
.comment-time {
  font-size: var(--cube-font-size-meta);
  color: var(--color-text-3);
}
.comment-content {
  font-size: var(--cube-font-size-body);
  color: var(--color-text-1);
  line-height: 22px;
  word-break: break-word;
  white-space: pre-wrap;
}
.comment-at {
  color: rgb(var(--primary-6));
  margin-right: 4px;
}
</style>

<style>
/* drawer 内容挂到 body，用全局类铺灰底以衬托分组卡片 */
.record-drawer .arco-drawer-body {
  background: var(--color-fill-2);
}
</style>
