<template>
  <a-spin :loading="loading" class="auto-ed-spin">
    <a-tabs v-model:active-key="mainTab" class="auto-ed-tabs">
      <a-tab-pane key="edit" title="编辑">
        <a-alert v-if="parseError" type="warning" class="auto-ed-alert">{{ parseError }}</a-alert>
        <a-alert v-if="skipPersistTrigger" type="warning" class="auto-ed-alert">
          该实体的写入由自动化自身产生（规则 / 通知 / 评论），记录增删改不会触发流程，请改用按钮、定时或 Webhook。
        </a-alert>

        <div class="auto-ed-panel">
          <a-form layout="vertical" class="auto-ed-form">
            <a-form-item label="名称" required class="auto-ed-name">
              <a-input v-model="name" :max-length="50" placeholder="流程名称" allow-clear />
            </a-form-item>
          </a-form>
          <div class="auto-ed-divider" />

          <div class="auto-flow">
            <!-- 第 1 步：触发 + 飞书式字段条件 -->
            <section class="auto-flow__col">
              <div class="auto-flow__label">当以下情况发生时：</div>
              <div class="auto-flow__step">第 1 步</div>
              <div class="auto-flow-body">
                <a-select v-model="triggerKind" size="large" class="auto-trigger-select">
                  <a-option v-for="o in TRIGGER_OPTIONS" :key="o.value" :value="o.value">{{ o.label }}</a-option>
                </a-select>

                <template v-if="triggerKind === 'dateArrive'">
                  <div class="auto-flow-body__sec">选择日期字段</div>
                  <a-select v-model="triggerConfig.field">
                    <a-option v-for="f in fields" :key="f.name" :value="f.name">{{ f.displayName || f.name }}</a-option>
                  </a-select>
                  <div class="auto-flow-body__sec">偏移分钟</div>
                  <a-input-number v-model="triggerConfig.offsetMinutes" :min="-10080" :max="10080" class="auto-ed-full" />
                </template>
                <template v-else-if="triggerKind === 'schedule'">
                  <div class="auto-flow-body__sec">Cron</div>
                  <a-input v-model="triggerConfig.cron" placeholder="0 * * * * ?" />
                </template>
                <template v-else-if="triggerKind === 'button'">
                  <div class="auto-flow-body__sec">按钮文案</div>
                  <a-input v-model="triggerConfig.label" :max-length="12" />
                  <div class="auto-flow-body__sec">所需权限</div>
                  <a-select v-model="triggerConfig.requirePermission">
                    <a-option value="detail">查看</a-option>
                    <a-option value="update">更新</a-option>
                  </a-select>
                </template>
                <template v-else-if="triggerKind === 'webhook'">
                  <div class="auto-flow-body__sec">令牌</div>
                  <a-input :model-value="hookToken" readonly>
                    <template #append>
                      <a-button size="mini" @click="copyToken">复制</a-button>
                      <a-button size="mini" @click="regenHook">重新生成</a-button>
                    </template>
                  </a-input>
                  <div v-if="hookUrl" class="hook-url">{{ hookUrl }}</div>
                  <div class="auto-flow-body__sec">校验签名</div>
                  <a-switch v-model="triggerConfig.requireSignature" />
                </template>

                <!-- 飞书式：字段勾选列表 + 展开嵌套条件 -->
                <div v-if="showConditionList" class="auto-cond">
                  <div class="auto-cond__head">
                    <span class="auto-cond__title">{{ conditionSectionTitle }}</span>
                    <a-radio-group
                      v-if="showFilterLogic"
                      v-model="filterLogic"
                      type="button"
                      size="mini"
                      class="auto-cond__logic"
                    >
                      <a-radio value="all">并且</a-radio>
                      <a-radio value="any">或者</a-radio>
                    </a-radio-group>
                    <span class="auto-cond__acts">
                      <a-button type="text" size="mini" @click="selectAllConditionFields">全选</a-button>
                      <a-button type="text" size="mini" @click="clearAllConditionFields">取消选择</a-button>
                    </span>
                  </div>
                  <div v-if="conditionFields.length" class="auto-cond__list">
                    <div
                      v-for="f in conditionFields"
                      :key="f.name"
                      class="auto-cond__item"
                      :class="{ 'auto-cond__item--on': isConditionFieldChecked(f.name) }"
                    >
                      <label class="auto-cond__row">
                        <a-checkbox
                          :model-value="isConditionFieldChecked(f.name)"
                          @update:model-value="(v: boolean | (string | number | boolean)[]) => setConditionFieldChecked(f.name, v === true)"
                        />
                        <icon-park :type="fieldIcon(f)" class="auto-cond__ico" />
                        <span class="auto-cond__name">{{ f.displayName || f.name }}</span>
                      </label>
                      <div v-if="isConditionFieldChecked(f.name)" class="auto-cond__nest">
                        <span class="auto-cond__nest-label">{{ conditionNestLabel }}</span>
                        <div class="auto-cond__nest-controls">
                          <a-select
                            v-model="conditionRowOf(f.name).cond.op"
                            size="small"
                            class="auto-cond__op"
                          >
                            <a-option
                              v-for="op in opsOf(f.name)"
                              :key="op"
                              :value="op"
                            >{{ FILTER_OP_LABELS[op] }}</a-option>
                          </a-select>
                          <a-input
                            v-if="opNeedsValue(conditionRowOf(f.name).cond.op)"
                            v-model="conditionRowOf(f.name).cond.value"
                            size="small"
                            allow-clear
                            placeholder="请输入"
                            class="auto-cond__val"
                          />
                        </div>
                      </div>
                    </div>
                  </div>
                  <a-empty v-else description="暂无可用字段，可点击下方添加条件" class="auto-cond__empty" />

                  <!-- 列表外的自由条件行 -->
                  <div v-for="item in orphanFilterRows" :key="`orphan-${item.index}`" class="auto-cond__orphan">
                    <div class="fb-cond">
                      <a-select
                        v-model="item.row.cond.field"
                        placeholder="字段"
                        size="small"
                        class="fb-cond__field"
                        allow-search
                        @change="onFilterField(item.row)"
                      >
                        <a-option v-for="f in conditionFields" :key="f.name" :value="f.name">
                          {{ f.displayName || f.name }}
                        </a-option>
                      </a-select>
                      <a-select v-model="item.row.cond.op" size="small" class="fb-cond__op">
                        <a-option v-for="op in opsOf(item.row.cond.field)" :key="op" :value="op">
                          {{ FILTER_OP_LABELS[op] }}
                        </a-option>
                      </a-select>
                      <a-input
                        v-if="opNeedsValue(item.row.cond.op)"
                        v-model="item.row.cond.value"
                        size="small"
                        placeholder="请输入"
                      />
                      <a-button size="mini" type="text" status="danger" @click="removeFilterRow(item.index)">
                        <icon-park type="delete" />
                      </a-button>
                    </div>
                  </div>

                  <a-button type="text" size="small" class="auto-cond__add" @click="addConditionField">
                    + 添加同时满足的条件
                  </a-button>
                </div>
              </div>
            </section>

            <div class="auto-flow__bridge" aria-hidden="true">
              <div class="auto-flow__rail" />
              <div class="auto-flow__badge">
                <icon-park type="right" theme="outline" :size="22" class="auto-flow__badge-ico" />
              </div>
            </div>

            <!-- 第 2 步：动作 Timeline -->
            <section class="auto-flow__col">
              <div class="auto-flow__label">就执行以下操作：</div>
              <div class="auto-flow__step">第 2 步</div>
              <div class="auto-flow-body auto-flow-body--actions">
                <a-timeline v-if="actions.length" class="auto-tl">
                  <a-timeline-item v-for="(act, i) in actions" :key="i">
                    <AutomationActionCard
                      :action="act"
                      :action-label="actionLabel(act.type)"
                      :fields="conditionFields.length ? conditionFields : fields"
                      :found-warning="foundWarningAt(i)"
                      :can-up="i > 0"
                      :can-down="i < actions.length - 1"
                      @up="moveAction(i, -1)"
                      @down="moveAction(i, 1)"
                      @remove="removeAction(i)"
                    />
                  </a-timeline-item>
                </a-timeline>
                <a-dropdown trigger="click" position="bl" content-class="auto-action-dd">
                  <a-button type="text" size="small">+ 添加动作</a-button>
                  <template #content>
                    <a-doption
                      v-for="o in ACTION_OPTIONS"
                      :key="o.value"
                      class="auto-action-dd__item"
                      @click="addAction(o.value)"
                    >
                      <icon-park :type="o.icon" class="auto-action-dd__ico" />
                      <span>{{ o.label }}</span>
                    </a-doption>
                  </template>
                </a-dropdown>
              </div>
            </section>
          </div>
        </div>
      </a-tab-pane>

      <a-tab-pane key="runs" title="运行日志" :disabled="!canViewRuns">
        <div class="auto-ed-panel">
          <a-spin :loading="runsLoading" class="auto-ed-spin">
            <a-empty v-if="!runs.length" description="暂无运行记录" />
            <a-timeline v-else class="auto-run-tl">
              <a-timeline-item
                v-for="r in runs"
                :key="r.id"
                :dot-color="runDotColor(r.status)"
              >
                <template #dot>
                  <span class="auto-run-dot" :class="`auto-run-dot--${runStatusTone(r.status)}`">
                    <icon-park :type="runStatusIcon(r.status)" />
                  </span>
                </template>
                <div class="auto-run-item">
                  <div class="auto-run-item__title">{{ runTitle(r) }}</div>
                  <div class="auto-run-item__body">
                    <div class="auto-run-item__time">{{ r.timeText }}</div>
                    <div class="auto-run-item__detail">{{ runDetailBody(r) }}</div>
                    <div v-if="r.error && !(r.detail || '').includes(r.error)" class="auto-run-item__err">{{ r.error }}</div>
                  </div>
                </div>
              </a-timeline-item>
            </a-timeline>
          </a-spin>
        </div>
      </a-tab-pane>
    </a-tabs>
  </a-spin>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import { fieldIcon } from '@/core/utils/iconRegistry';
import AutomationActionCard from './AutomationActionCard.vue';
import { useAutomationEditor } from './useAutomationEditor';

const props = defineProps<{
  typePath: string;
  fields: FieldMeta[];
  editId: number | 'new';
}>();

const emit = defineEmits<{
  saved: [];
  back: [];
  footerChange: [payload: { saving: boolean; summary: string }];
}>();

const api = useAutomationEditor(props, emit);

const {
  mainTab,
  loading,
  parseError,
  skipPersistTrigger,
  name,
  triggerKind,
  triggerConfig,
  hookToken,
  hookUrl,
  filterLogic,
  actions,
  canViewRuns,
  runs,
  runsLoading,
  FILTER_OP_LABELS,
  opNeedsValue,
  opsOf,
  conditionSectionTitle,
  conditionNestLabel,
  showFilterLogic,
  showConditionList,
  conditionFields,
  isConditionFieldChecked,
  setConditionFieldChecked,
  selectAllConditionFields,
  clearAllConditionFields,
  conditionRowOf,
  addConditionField,
  orphanFilterRows,
  removeFilterRow,
  onFilterField,
  addAction,
  removeAction,
  moveAction,
  actionLabel,
  foundWarningAt,
  copyToken,
  regenHook,
  saveOnly,
  saveAndEnable,
  runTitle,
  runDetailBody,
  runStatusTone,
  runStatusIcon,
  runDotColor,
  TRIGGER_OPTIONS,
  ACTION_OPTIONS,
} = api;

defineExpose({
  saveOnly,
  saveAndEnable,
});
</script>

<style scoped>
/* 字号 / 字重 / 圆角消费 theme/tokens.ts（OSC-0007），勿硬编码 px 字号与品牌色 */
.auto-ed-tabs :deep(.arco-tabs-content) {
  padding-top: var(--cube-density-gap);
}
.auto-ed-spin {
  display: block;
  width: 100%;
}
.auto-ed-full {
  width: 100%;
}
.auto-ed-alert {
  margin-bottom: var(--cube-density-gap);
}
/* 与 RecordDrawer.detail-group 一致：灰底抽屉上的一张大面板 */
.auto-ed-panel {
  padding: var(--cube-density-gap);
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: var(--border-radius-medium);
  box-sizing: border-box;
}
.auto-ed-name {
  margin-bottom: 0;
}
.auto-ed-name :deep(.arco-form-item-wrapper-col),
.auto-ed-name :deep(.arco-form-item-content-wrapper),
.auto-ed-name :deep(.arco-input-wrapper) {
  width: 100%;
}
.auto-ed-divider {
  height: 1px;
  margin: var(--cube-density-gap) 0;
  background: var(--color-border-2);
}
.auto-ed-logic {
  margin-left: 8px;
}
.auto-trigger-select {
  width: 100%;
}
.auto-cond {
  margin-top: 4px;
  border: 1px solid var(--color-border-2);
  border-radius: var(--border-radius-small);
  background: var(--color-bg-1);
  padding: 10px 12px;
}
.auto-cond__head {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 8px;
}
.auto-cond__title {
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
}
.auto-cond__acts {
  display: flex;
  align-items: center;
  gap: 0;
  flex-shrink: 0;
  margin-left: auto;
}
.auto-cond__logic {
  flex-shrink: 0;
}
.auto-cond__list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 360px;
  overflow: auto;
}
/* 飞书式：每个字段一张浅灰小卡片 */
.auto-cond__item {
  padding: 10px 12px;
  border-radius: var(--border-radius-medium);
  background: var(--color-fill-2);
  transition: background 0.15s ease;
}
.auto-cond__item--on {
  background: var(--color-fill-2);
}
.auto-cond__row {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  margin: 0;
  min-height: 28px;
}
.auto-cond__ico {
  color: var(--color-text-3);
  flex-shrink: 0;
  font-size: 14px;
}
.auto-cond__name {
  font-size: var(--cube-font-size-body);
  color: var(--color-text-1);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
/* 勾选后：字段名与筛选条件之间分隔线；标签 + 运算符(4/12) + 条件值(8/12) */
.auto-cond__nest {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
  padding-top: 8px;
  border-top: 1px solid var(--color-border-2);
  background: transparent;
}
.auto-cond__nest-label {
  font-size: var(--cube-font-size-meta);
  color: var(--color-text-2);
  flex-shrink: 0;
  white-space: nowrap;
}
.auto-cond__nest-controls {
  display: grid;
  grid-template-columns: 4fr 8fr;
  gap: 8px;
  flex: 1;
  min-width: 0;
  align-items: center;
}
.auto-cond__op {
  width: 100%;
  min-width: 0;
}
.auto-cond__val {
  width: 100%;
  min-width: 0;
}
/* 无需条件值时，运算符占满剩余宽度 */
.auto-cond__nest-controls:not(:has(.auto-cond__val)) {
  grid-template-columns: 1fr;
}
.auto-cond__empty {
  padding: 12px 0;
}
.auto-cond__orphan {
  margin-top: 8px;
}
.auto-cond__add {
  margin-top: 8px;
  padding-left: 0;
}
.auto-flow {
  display: grid;
  grid-template-columns: 1fr 56px 1fr;
  gap: 8px;
  align-items: start;
  min-height: 280px;
}
@media (max-width: 900px) {
  .auto-flow {
    grid-template-columns: 1fr;
  }
  .auto-flow__bridge {
    min-height: 56px;
  }
  .auto-flow__rail {
    top: 50%;
    bottom: auto;
    left: 0;
    right: 0;
    width: auto;
    height: 1px;
    transform: translateY(-50%);
  }
  .auto-flow__badge {
    transform: rotate(90deg);
  }
}
.auto-flow__col {
  align-self: stretch;
  min-width: 0;
}
.auto-flow__label {
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
  margin-bottom: 4px;
}
.auto-flow__step {
  display: inline-flex;
  align-items: center;
  margin-bottom: 8px;
  font-size: var(--cube-font-size-meta);
  font-weight: var(--cube-font-weight-normal);
  color: var(--color-text-3);
}
/* 中间桥：竖线通栏，徽标相对竖线垂直居中 */
.auto-flow__bridge {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  align-self: stretch;
  min-height: 120px;
}
.auto-flow__rail {
  position: absolute;
  top: 0;
  bottom: 0;
  left: 50%;
  width: 1px;
  transform: translateX(-50%);
  background: var(--color-border-2);
  pointer-events: none;
}
.auto-flow__badge {
  position: relative;
  z-index: 1;
  display: grid;
  place-items: center;
  flex-shrink: 0;
  width: calc(var(--cube-density-control-height) + 8px);
  height: calc(var(--cube-density-control-height) + 8px);
  border-radius: 50%;
  color: var(--color-white, #fff);
  background: rgb(var(--primary-6));
  box-shadow: 0 2px 8px rgba(var(--primary-6), 0.35);
  overflow: hidden;
}
.auto-flow__badge-ico {
  display: grid;
  place-items: center;
  line-height: 0;
  transform: translate(1px, 0.5px);
}
.auto-flow__badge-ico :deep(svg) {
  display: block;
  vertical-align: middle;
}
/* 大面板内步骤区：不再套第二层描边卡片，避免双层面板 */
.auto-flow-body {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.auto-flow-body__sec {
  display: flex;
  align-items: center;
  margin-top: 4px;
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-normal);
  color: var(--color-text-2);
}
.auto-tl {
  margin: 4px 0 0;
  padding-left: 4px;
}
.auto-tl :deep(.arco-timeline-item-content) {
  min-height: auto;
}
.fb-cond {
  display: flex;
  gap: 6px;
  align-items: center;
  flex-wrap: wrap;
}
.fb-cond__field {
  width: 110px;
}
.fb-cond__op {
  width: 100px;
}
.hook-url {
  font-size: var(--cube-font-size-meta);
  font-weight: var(--cube-font-weight-normal);
  color: var(--color-text-3);
  word-break: break-all;
}
.auto-run-tl {
  padding: 8px 4px 0;
}
.auto-run-dot {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  font-size: var(--cube-font-size-meta);
  color: var(--color-white, #fff);
  background: var(--color-text-3);
}
.auto-run-dot--ok {
  background: rgb(var(--success-6));
}
.auto-run-dot--fail {
  background: rgb(var(--danger-6));
}
.auto-run-dot--run {
  background: rgb(var(--primary-6));
}
.auto-run-dot--wait {
  background: rgb(var(--warning-6));
}
.auto-run-dot--muted {
  background: var(--color-text-3);
}
.auto-run-item__title {
  font-size: var(--cube-font-size-title);
  font-weight: var(--cube-font-weight-medium);
  color: var(--color-text-1);
  line-height: 1.4;
}
.auto-run-item__body {
  margin-top: 6px;
  padding-left: 10px;
  border-left: 1px solid var(--color-border-2);
}
.auto-run-item__time {
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-normal);
  color: var(--color-text-3);
  line-height: 1.5;
}
.auto-run-item__detail {
  margin-top: 4px;
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-normal);
  color: var(--color-text-1);
  line-height: 1.55;
  word-break: break-word;
}
.auto-run-item__err {
  margin-top: 4px;
  font-size: var(--cube-font-size-body);
  font-weight: var(--cube-font-weight-normal);
  color: rgb(var(--danger-6));
  line-height: 1.5;
}
</style>

<style>
/* dropdown 挂到 body：仅菜单项图标样式，保留 Arco 默认高度/滚动 */
.auto-action-dd__item {
  display: flex !important;
  align-items: center;
  gap: 8px;
}
.auto-action-dd__ico {
  flex-shrink: 0;
  font-size: 16px;
  color: var(--color-text-2);
}
</style>
