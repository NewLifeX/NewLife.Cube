<template>
  <a-drawer
    :visible="visible"
    :width="width"
    unmount-on-close
    placement="right"
    class="record-drawer"
    :class="{ 'record-drawer--no-entity-delete': entityDeleteLocked }"
    @update:visible="(v: boolean) => emit('update:visible', v)"
  >
    <template #title>
      <div class="drawer-title">
        <a-space v-if="showNav" :size="2" class="drawer-nav">
          <a-tooltip content="上一条">
            <a-button type="text" size="mini" :disabled="!canPrev" @click="emit('prev')">
              <template #icon><icon-park type="up" /></template>
            </a-button>
          </a-tooltip>
          <a-tooltip content="下一条">
            <a-button type="text" size="mini" :disabled="!canNext" @click="emit('next')">
              <template #icon><icon-park type="down" /></template>
            </a-button>
          </a-tooltip>
        </a-space>
        <span class="drawer-title__text">{{ title }}</span>
        <a-space v-if="mode === 'detail' && opsCustomLinks.length" :size="8" class="drawer-ops-links">
          <a-link
            v-for="link in opsCustomLinks"
            :key="link.name"
            @click="emit('ops-link', link)"
          >
            {{ link.label }}
          </a-link>
        </a-space>
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
        :field-errors="fieldErrors"
        :layout="layout"
        @toggle-collapse="emit('toggle-collapse', $event)"
      />
      <div v-else class="detail-grouped" :style="detailLabelCssVars">
        <section
          v-for="group in detailGroups"
          :key="group.category || '__default'"
          class="detail-group"
        >
          <button
            v-if="group.title"
            type="button"
            class="detail-group__title detail-group__collapse"
            :aria-expanded="!detailCollapsed.has(group.category)"
            @click="emit('toggle-collapse', group.category)"
          >
            <span>{{ group.title }}</span>
            <icon-park type="down" class="detail-group__caret" :class="{ open: !detailCollapsed.has(group.category) }" />
          </button>
            <div v-show="!detailCollapsed.has(group.category)" class="detail-fields">
            <div
              v-for="field in group.fields"
              :key="field.name"
              class="detail-field"
              :class="{ 'detail-field--perm': isRolePermField(field) }"
            >
              <div class="detail-field__label" :style="isRolePermField(field) ? undefined : detailLabelStyle">
                <icon-park :type="fieldIcon(field)" class="detail-field__icon" />
                {{ field.displayName || field.name }}
              </div>
              <div
                class="detail-field__value"
                :class="{ 'detail-field__value--perm': isRolePermField(field) }"
              >
                <RolePermTree
                  v-if="isRolePermField(field)"
                  :model-value="model[field.name]"
                  disabled
                />
                <img
                  v-else-if="detailImageOf(field)"
                  :src="detailImageOf(field)!.href"
                  class="detail-image"
                  :alt="detailImageOf(field)!.text"
                />
                <a-link
                  v-else-if="detailUrlOf(field)"
                  :href="detailUrlOf(field)!.href"
                  target="_blank"
                  :disabled="!detailUrlOf(field)!.safe"
                >
                  {{ detailUrlOf(field)!.text }}
                </a-link>
                <a-link
                  v-else-if="detailFileOf(field)"
                  :href="detailFileOf(field)!.href"
                  target="_blank"
                  :disabled="!detailFileOf(field)!.safe"
                >
                  {{ detailFileOf(field)!.text }}
                </a-link>
                <span v-else class="detail-json" :title="detailTitle(field)">
                  {{ formatDetail(field) }}
                </span>
              </div>
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
          :field-errors="fieldErrors"
          :layout="layout"
          @toggle-collapse="emit('toggle-collapse', $event)"
        />
        <div v-else class="detail-grouped" :style="detailLabelCssVars">
          <section
            v-for="group in detailGroups"
            :key="group.category || '__default'"
            class="detail-group"
          >
            <button
              v-if="group.title"
              type="button"
              class="detail-group__title detail-group__collapse"
              :aria-expanded="!detailCollapsed.has(group.category)"
              @click="emit('toggle-collapse', group.category)"
            >
              <span>{{ group.title }}</span>
              <icon-park type="down" class="detail-group__caret" :class="{ open: !detailCollapsed.has(group.category) }" />
            </button>
            <div v-show="!detailCollapsed.has(group.category)" class="detail-fields">
              <div
                v-for="field in group.fields"
                :key="field.name"
                class="detail-field"
                :class="{ 'detail-field--perm': isRolePermField(field) }"
              >
                <div class="detail-field__label" :style="isRolePermField(field) ? undefined : detailLabelStyle">
                  <icon-park :type="fieldIcon(field)" class="detail-field__icon" />
                  {{ field.displayName || field.name }}
                </div>
                <div
                  class="detail-field__value"
                  :class="{ 'detail-field__value--perm': isRolePermField(field) }"
                >
                  <RolePermTree
                    v-if="isRolePermField(field)"
                    :model-value="model[field.name]"
                    disabled
                  />
                  <img
                    v-else-if="detailImageOf(field)"
                    :src="detailImageOf(field)!.href"
                    class="detail-image"
                    :alt="detailImageOf(field)!.text"
                  />
                  <a-link
                    v-else-if="detailUrlOf(field)"
                    :href="detailUrlOf(field)!.href"
                    target="_blank"
                    :disabled="!detailUrlOf(field)!.safe"
                  >
                    {{ detailUrlOf(field)!.text }}
                  </a-link>
                  <a-link
                    v-else-if="detailFileOf(field)"
                    :href="detailFileOf(field)!.href"
                    target="_blank"
                    :disabled="!detailFileOf(field)!.safe"
                  >
                    {{ detailFileOf(field)!.text }}
                  </a-link>
                  <span v-else class="detail-json" :title="detailTitle(field)">
                    {{ formatDetail(field) }}
                  </span>
                </div>
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
              <template v-if="historyDiff(row)?.length">
                <div class="history-diff">
                  <div v-for="(d, di) in historyDiff(row)!" :key="di" class="history-diff-item">
                    <span class="history-diff-field">{{ d.displayName }}</span>
                    <span class="history-diff-old">{{ d.oldValue }}</span>
                    <span class="history-diff-arrow">→</span>
                    <span class="history-diff-new">{{ d.newValue }}</span>
                  </div>
                </div>
              </template>
              <div v-else class="history-remark">{{ historyRemark(row) }}</div>
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
import type { FieldMeta } from '@/core/types/field';
import { fieldIcon } from '@/core/utils/iconRegistry';
import type { FormLayout } from '@/core/utils/viewProfile';
import type { OpsCustomLink } from '@/core/utils/opsAction';
import { formatDateTime } from '@/core/utils/datetime';
import CommentReplyEditor from '@/components/CommentReplyEditor.vue';
import UserAvatar from '@/components/UserAvatar.vue';
import FormContent from './FormContent.vue';
import RolePermTree from './RolePermTree.vue';
import { useRecordDrawer } from './useRecordDrawer';

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
    /** 后端字段级错误（FieldErrors），映射到表单对应字段（OSC-0009） */
    fieldErrors?: { field: string; message: string }[];
    /** 受限表单布局（OSC-0013）：当前 mode 的字段排序/显隐/Category 折叠 */
    layout?: FormLayout | null;
    /** 日历/甘特等：详情内展示自定义链接（OSC-2608178bdb） */
    opsCustomLinks?: OpsCustomLink[];
  }>(),
  {
    showHistoryTabs: true,
    canPrev: false,
    canNext: false,
    fieldErrors: () => [],
    layout: null,
    opsCustomLinks: () => [],
  },
);

const emit = defineEmits<{
  'update:visible': [boolean];
  save: [];
  edit: [];
  prev: [];
  next: [];
  'toggle-collapse': [category: string];
  'ops-link': [link: OpsCustomLink];
}>();

const {
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
  title,
  width,
  showSideTabs,
  showNav,
  detailGroups,
  detailCollapsed,
  detailLabelStyle,
  detailLabelCssVars,
  commentTree,
  formatDetail,
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
} = useRecordDrawer(props, emit);

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
.drawer-ops-links {
  flex-shrink: 0;
  margin-left: 4px;
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
/* 详情分组标题可点击折叠（OSC-0013） */
.detail-group__collapse {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  border: none;
  background: transparent;
  padding: 0;
  cursor: pointer;
  text-align: left;
  font: inherit;
  color: inherit;
}
.detail-group__caret {
  color: var(--color-text-3);
  transition: transform 0.2s;
  font-size: 12px;
}
.detail-group__caret.open {
  transform: rotate(180deg);
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
 
.detail-image {
  max-width: 160px;
  max-height: 120px;
  object-fit: contain;
  border-radius: 4px;
  display: block;
}
.detail-json {
  font-family: var(--font-family, inherit);
  white-space: pre-wrap;
} gap: 0;
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
  align-items: center;
  gap: 6px;
  padding: 8px 12px;
  background-color: var(--color-primary-light-1);
  color: var(--color-text-2);
  text-align: left;
  line-height: 22px;
  white-space: nowrap;
  box-sizing: border-box;
  border-right: 1px solid var(--color-border-2);
}
.detail-field__icon {
  flex: 0 0 auto;
  display: inline-flex;
  align-items: center;
  font-size: 14px;
  line-height: 22px;
  opacity: 0.65;
  color: var(--color-text-3);
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

.detail-field--perm {
  display: flex;
  flex-direction: column;
  align-items: stretch;
}
.detail-field--perm .detail-field__label {
  width: 100%;
  min-width: 0;
  max-width: none;
  border-right: none;
  border-bottom: 1px solid var(--color-border-2);
}
.detail-field__value--perm {
  padding: 12px;
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
/* 历史字段 diff（OSC-260819e483 P4） */
.history-diff {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.history-diff-item {
  display: flex;
  align-items: baseline;
  gap: 6px;
  font-size: var(--cube-font-size-body);
  line-height: 20px;
  flex-wrap: wrap;
}
.history-diff-field {
  color: var(--color-text-2);
  min-width: 0;
}
.history-diff-old {
  color: var(--color-text-3);
  text-decoration: line-through;
  word-break: break-word;
}
.history-diff-arrow {
  color: var(--color-text-4);
}
.history-diff-new {
  color: var(--color-text-1);
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
