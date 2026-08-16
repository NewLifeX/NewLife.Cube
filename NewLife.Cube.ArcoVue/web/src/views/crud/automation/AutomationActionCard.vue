<template>
  <div v-if="action" class="act-card" :class="{ 'act-card--collapsed': collapsed }">
    <div class="act-card__head">
      <div class="act-card__title">
        <span class="act-card__type">{{ actionLabel }}</span>
      </div>
      <div class="act-card__ops" @click.stop>
        <a-dropdown trigger="click" position="br">
          <a-button type="text" size="mini" class="act-card__more" title="更多操作">
            <icon-park type="more" theme="outline" :size="18" />
          </a-button>
          <template #content>
            <a-doption :disabled="!canUp" @click="$emit('up')">
              <icon-park type="up" class="act-menu-ico" />
              上移
            </a-doption>
            <a-doption :disabled="!canDown" @click="$emit('down')">
              <icon-park type="down" class="act-menu-ico" />
              下移
            </a-doption>
            <a-doption class="act-card__del" @click="$emit('remove')">
              <icon-park type="delete" class="act-menu-ico" />
              删除
            </a-doption>
          </template>
        </a-dropdown>
        <a-button type="text" size="mini" class="act-card__fold-btn" @click="collapsed = !collapsed">
          <icon-park :type="collapsed ? 'down' : 'up'" theme="outline" :size="20" />
        </a-button>
      </div>
    </div>

    <div v-show="!collapsed" class="act-card__body">
      <a-alert v-if="foundWarning" type="warning" class="act-alert">
        {{ foundWarning }}
      </a-alert>

      <!-- 发送通知 -->
      <template v-if="action.type === 'notify'">
        <a-form-item label="渠道">
          <a-select v-model="data.channel" size="small">
            <a-option value="InApp">站内信</a-option>
            <a-option value="Mail">邮件</a-option>
            <a-option value="Sms">短信</a-option>
            <a-option value="DingTalk">钉钉</a-option>
            <a-option value="WeCom">企微</a-option>
          </a-select>
        </a-form-item>
        <a-form-item label="接收对象">
          <div class="act-recipient-wrap">
            <a-radio-group v-model="recipientKind" type="button" size="mini" class="act-kind">
              <a-radio value="users">用户</a-radio>
              <a-radio value="roles">角色</a-radio>
              <a-radio value="departments">部门</a-radio>
            </a-radio-group>
            <a-select
              :model-value="recipientIds"
              multiple
              allow-clear
              allow-search
              :filter-option="false"
              :loading="recipientLoading"
              :placeholder="recipientPlaceholder"
              size="small"
              class="act-recipient"
              @search="onRecipientSearch"
              @focus="() => searchRecipients('')"
              @popup-visible-change="(v: boolean) => v && searchRecipients('')"
              @dropdown-visible-change="(v: boolean) => v && searchRecipients('')"
              @update:model-value="(v: unknown) => onRecipientIdsUpdate(v)"
            >
              <a-option
                v-for="o in recipientOptions"
                :key="o.id"
                :value="o.id"
                :label="o.displayName || o.name || String(o.id)"
              >
                {{ o.displayName || o.name || o.id }}
              </a-option>
            </a-select>
          </div>
        </a-form-item>
        <a-form-item label="标题">
          <a-input v-model="data.title" :max-length="200" placeholder="支持 {{字段名}}" />
        </a-form-item>
        <a-form-item label="正文">
          <a-textarea v-model="data.body" :max-length="2000" placeholder="支持 {{字段名}}" />
        </a-form-item>
      </template>

      <!-- 修改记录 -->
      <template v-else-if="action.type === 'updateRecord'">
        <a-form-item label="目标">
          <a-select v-model="data.target" size="small">
            <a-option value="current">当前记录</a-option>
            <a-option value="found">查找结果</a-option>
          </a-select>
        </a-form-item>
        <p v-if="data.target === 'found' && !foundWarning" class="act-hint">
          需前置「查找记录」。0 条跳过；多条时对每条逐一赋值。
        </p>
        <a-form-item label="字段赋值">
          <div class="assign-list">
            <div v-for="(row, i) in fieldRows" :key="i" class="assign-row">
              <a-select
                :model-value="row.name"
                size="small"
                allow-search
                placeholder="字段"
                class="assign-row__field"
                @update:model-value="(v: string) => patchFieldRow(i, { name: v })"
              >
                <a-option v-for="f in writableFields" :key="f.name" :value="f.name">
                  {{ f.displayName || f.name }}
                </a-option>
              </a-select>
              <span class="assign-row__eq">=</span>
              <a-input
                :model-value="row.value"
                size="small"
                placeholder="值或 {{字段}}"
                class="assign-row__val"
                @update:model-value="(v: string) => patchFieldRow(i, { value: v })"
              />
              <a-button size="mini" type="text" status="danger" @click="removeFieldRow(i)">
                <icon-park type="delete" />
              </a-button>
            </div>
            <a-button type="text" size="small" @click="addFieldRow">+ 添加字段</a-button>
          </div>
        </a-form-item>
      </template>

      <!-- 创建记录 -->
      <template v-else-if="action.type === 'createRecord'">
        <a-form-item label="实体">
          <a-select
            v-model="data.typePath"
            size="small"
            allow-search
            allow-clear
            placeholder="选择有新增权限的实体"
          >
            <a-option
              v-for="e in insertEntities"
              :key="e.typePath"
              :value="e.typePath"
              :label="e.displayName"
            >
              {{ e.displayName }}
              <span class="act-path">{{ e.typePath }}</span>
            </a-option>
          </a-select>
        </a-form-item>
        <a-form-item label="字段赋值">
          <div class="assign-list">
            <div v-for="(row, i) in fieldRows" :key="i" class="assign-row">
              <a-select
                :model-value="row.name"
                size="small"
                allow-search
                placeholder="字段"
                class="assign-row__field"
                @update:model-value="(v: string) => patchFieldRow(i, { name: v })"
              >
                <a-option v-for="f in writableFields" :key="f.name" :value="f.name">
                  {{ f.displayName || f.name }}
                </a-option>
              </a-select>
              <span class="assign-row__eq">=</span>
              <a-input
                :model-value="row.value"
                size="small"
                placeholder="值或 {{字段}}"
                class="assign-row__val"
                @update:model-value="(v: string) => patchFieldRow(i, { value: v })"
              />
              <a-button size="mini" type="text" status="danger" @click="removeFieldRow(i)">
                <icon-park type="delete" />
              </a-button>
            </div>
            <a-button type="text" size="small" @click="addFieldRow">+ 添加字段</a-button>
          </div>
        </a-form-item>
      </template>

      <!-- 查找记录 -->
      <template v-else-if="action.type === 'findRecords'">
        <a-form-item label="实体">
          <a-select
            :model-value="data.typePath"
            size="small"
            allow-search
            allow-clear
            placeholder="选择有更新权限的实体"
            :loading="findFieldsLoading"
            @update:model-value="onFindTypePathChange"
          >
            <a-option
              v-for="e in updateEntities"
              :key="e.typePath"
              :value="e.typePath"
              :label="e.displayName"
            >
              {{ e.displayName }}
              <span class="act-path">{{ e.typePath }}</span>
            </a-option>
          </a-select>
        </a-form-item>
        <a-form-item label="条数上限">
          <a-input-number v-model="data.limit" :min="1" :max="100" />
        </a-form-item>
        <a-form-item label="筛选条件">
          <div class="find-filter">
            <a-radio-group v-model="findFilterLogic" type="button" size="mini">
              <a-radio value="all">并且</a-radio>
              <a-radio value="any">或者</a-radio>
            </a-radio-group>
            <div v-for="(row, i) in findFilterRows" :key="i" class="fb-cond">
              <a-select
                v-model="row.cond.field"
                placeholder="可搜索字段"
                size="small"
                class="fb-cond__field"
                @change="onFindFilterField(row)"
              >
                <a-option v-for="f in filterFieldSource" :key="f.name" :value="f.name">
                  {{ f.displayName || f.name }}
                </a-option>
              </a-select>
              <a-select v-model="row.cond.op" size="small" class="fb-cond__op">
                <a-option v-for="op in opsOf(row.cond.field)" :key="op" :value="op">
                  {{ FILTER_OP_LABELS[op] }}
                </a-option>
              </a-select>
              <a-input
                v-if="opNeedsValue(row.cond.op)"
                v-model="row.cond.value"
                size="small"
                placeholder="请输入"
              />
              <a-button size="mini" type="text" status="danger" @click="removeFindFilterRow(i)">
                <icon-park type="delete" />
              </a-button>
            </div>
            <a-button type="text" size="small" :disabled="!data.typePath" @click="addFindFilterRow">
              + 添加条件
            </a-button>
          </div>
        </a-form-item>
        <p class="act-hint">查找结果供后续「目标=查找结果」的修改/通知/评论使用；单条与多条均支持。</p>
      </template>

      <!-- HTTP -->
      <template v-else-if="action.type === 'httpRequest'">
        <a-form-item label="方法">
          <a-select v-model="data.method" size="small">
            <a-option value="GET">GET</a-option>
            <a-option value="POST">POST</a-option>
            <a-option value="PUT">PUT</a-option>
            <a-option value="DELETE">DELETE</a-option>
            <a-option value="PATCH">PATCH</a-option>
          </a-select>
        </a-form-item>
        <a-form-item label="URL">
          <a-input v-model="data.url" placeholder="https://... 支持 {{字段}}" />
        </a-form-item>
        <a-form-item label="请求头">
          <div class="assign-list">
            <div v-for="(row, i) in headerRows" :key="i" class="assign-row">
              <a-input
                :model-value="row.key"
                size="small"
                placeholder="Header"
                class="assign-row__field"
                @update:model-value="(v: string) => patchHeaderRow(i, { key: v })"
              />
              <span class="assign-row__eq">:</span>
              <a-input
                :model-value="row.value"
                size="small"
                placeholder="值"
                class="assign-row__val"
                @update:model-value="(v: string) => patchHeaderRow(i, { value: v })"
              />
              <a-button size="mini" type="text" status="danger" @click="removeHeaderRow(i)">
                <icon-park type="delete" />
              </a-button>
            </div>
            <a-button type="text" size="small" @click="addHeaderRow">+ 添加请求头</a-button>
          </div>
        </a-form-item>
        <a-form-item label="Body">
          <a-textarea v-model="data.body" placeholder="JSON，支持 {{字段}}" />
        </a-form-item>
      </template>

      <template v-else-if="action.type === 'delay'">
        <a-form-item label="分钟">
          <a-input-number v-model="data.minutes" :min="1" :max="10080" />
        </a-form-item>
      </template>

      <template v-else-if="action.type === 'runAutomation'">
        <a-form-item label="规则 Id">
          <a-input-number v-model="data.automationId" :min="1" />
        </a-form-item>
        <p class="act-hint">该动作已从菜单下线，仅保留历史流程展示。</p>
      </template>

      <template v-else-if="action.type === 'addComment'">
        <a-form-item label="目标">
          <a-select v-model="data.target" size="small">
            <a-option value="current">当前记录</a-option>
            <a-option value="found">查找结果</a-option>
          </a-select>
        </a-form-item>
        <p v-if="data.target === 'found' && !foundWarning" class="act-hint">
          需前置「查找记录」；多条查找结果时对每条添加评论。
        </p>
        <a-form-item label="内容">
          <a-textarea v-model="data.content" placeholder="支持 {{字段名}}" />
        </a-form-item>
      </template>

      <template v-else-if="action.type === 'aiText'">
        <a-form-item label="提示词">
          <a-textarea v-model="data.prompt" placeholder="支持 {{字段名}}" />
        </a-form-item>
        <a-form-item label="写入字段">
          <a-select v-model="data.outputField" size="small" allow-clear allow-search placeholder="选择字段">
            <a-option v-for="f in writableFields" :key="f.name" :value="f.name">
              {{ f.displayName || f.name }}
            </a-option>
          </a-select>
        </a-form-item>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { ActionDraft } from '@/core/utils/automationGraph';
import { useAutomationActionCard } from './useAutomationActionCard';

const props = defineProps<{
  action: ActionDraft;
  actionLabel: string;
  canUp: boolean;
  canDown: boolean;
  fields?: FieldMeta[];
  foundWarning?: string;
}>();

defineEmits<{ up: []; down: []; remove: [] }>();

const fieldsRef = computed(() => props.fields ?? []);

const {
  collapsed,
  data,
  recipientKind,
  recipientIds,
  recipientOptions,
  recipientLoading,
  onRecipientSearch,
  onRecipientIdsUpdate,
  searchRecipients,
  fieldRows,
  addFieldRow,
  removeFieldRow,
  patchFieldRow,
  updateEntities,
  insertEntities,
  findFieldsLoading,
  filterFieldSource,
  findFilterLogic,
  findFilterRows,
  opsOf,
  addFindFilterRow,
  removeFindFilterRow,
  onFindFilterField,
  onFindTypePathChange,
  FILTER_OP_LABELS,
  opNeedsValue,
  headerRows,
  addHeaderRow,
  removeHeaderRow,
  patchHeaderRow,
  writableFields,
} = useAutomationActionCard(
  computed(() => props.action),
  fieldsRef,
);

const recipientPlaceholder = computed(() => {
  if (recipientKind.value === 'roles') return '搜索并选择角色';
  if (recipientKind.value === 'departments') return '搜索并选择部门';
  return '搜索并选择用户';
});
</script>

<style scoped>
.act-card {
  border: 1px solid var(--color-border-2);
  border-radius: var(--border-radius-small);
  padding: 8px 12px;
  margin-bottom: 8px;
  background: var(--color-bg-2);
}
.act-card__head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
}
.act-card__title {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
  flex: 1;
}
.act-card__type {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.act-card__ops {
  display: flex;
  align-items: center;
  gap: 2px;
  flex-shrink: 0;
}
.act-card__more,
.act-card__fold-btn {
  color: var(--color-text-2);
  padding: 4px;
  height: auto;
  line-height: 1;
}
.act-card__more:hover,
.act-card__fold-btn:hover {
  color: var(--color-text-1);
}
.act-card__fold-btn {
  font-size: 20px;
}
.act-card__del {
  color: rgb(var(--red-6));
}
.act-menu-ico {
  display: inline-flex;
  align-items: center;
  margin-right: 6px;
  font-size: 14px;
  vertical-align: -2px;
}
.act-card--collapsed .act-card__head {
  margin-bottom: 0;
}
.act-card__body {
  margin-top: 8px;
}
.act-card__body :deep(.arco-form-item-label) {
  font-size: var(--cube-font-size-meta);
  font-weight: var(--cube-font-weight-normal);
  color: var(--color-text-2);
}
.act-alert {
  margin-bottom: 8px;
}
.act-hint {
  margin: 0 0 8px;
  font-size: var(--cube-font-size-meta);
  color: var(--color-text-3);
  line-height: 1.4;
}
.act-kind {
  display: inline-flex;
  width: auto;
}
.act-kind :deep(.arco-radio-group) {
  display: inline-flex;
}
.act-kind :deep(.arco-radio-button) {
  margin: 0;
}
.act-recipient-wrap {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
  width: 100%;
}
.act-recipient {
  width: 100%;
}
.act-path {
  margin-left: 8px;
  font-size: var(--cube-font-size-meta);
  color: var(--color-text-3);
}
.assign-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
}
.assign-row {
  display: flex;
  align-items: center;
  gap: 6px;
  width: 100%;
}
.assign-row__field {
  width: 140px;
  flex-shrink: 0;
}
.assign-row__eq {
  color: var(--color-text-3);
  flex-shrink: 0;
}
.assign-row__val {
  flex: 1;
  min-width: 0;
}
.fb-cond {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: center;
  margin-top: 6px;
}
.fb-cond__field {
  width: 120px;
}
.fb-cond__op {
  width: 110px;
}
.find-filter {
  width: 100%;
}
</style>
