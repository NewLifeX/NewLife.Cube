<template>
  <a-popover
    :popup-visible="visible"
    position="bottom"
    trigger="click"
    content-class="format-popover"
    @popup-visible-change="onVisibleChange"
  >
    <template #content>
      <div class="format-builder">
        <div class="fmt-head">
          <span class="fmt-title">
            设置填色条件
            <a-tooltip content="背景色与行侧边各取从上到下第一条命中规则，可同时生效。整列按所选字段铺满该列，无需条件。">
              <icon-park type="info" />
            </a-tooltip>
          </span>
        </div>
        <div class="fmt-rows" @click="openColorIdx = -1">
          <div
            v-for="(rule, i) in rules"
            :key="rule.id"
            class="fmt-rule"
            @dragover.prevent
            @drop="onDrop(i)"
          >
            <div class="fmt-row">
              <span
                class="fmt-drag"
                draggable="true"
                title="拖动调整优先级"
                @dragstart="onDragStart(i, $event)"
              >
                <icon-park type="drag" />
              </span>
              <button
                type="button"
                class="fmt-color-btn"
                :style="{ backgroundColor: rule.color }"
                :title="rule.color"
                @click.stop="openColorIdx = openColorIdx === i ? -1 : i"
              />
              <a-select
                :model-value="rule.apply"
                size="small"
                :style="{ width: '88px' }"
                @change="(v: string) => onApplyChange(i, v as FormatApply)"
              >
                <a-option
                  v-for="a in applyChoices(rule)"
                  :key="a"
                  :value="a"
                  :disabled="!allowedApply.includes(a)"
                  :label="applyLabels[a]"
                />
              </a-select>
              <a-select
                :model-value="rule.field"
                placeholder="字段"
                size="small"
                :style="{ width: '110px' }"
                @change="(v: string) => onFieldChange(i, v)"
              >
                <a-option
                  v-for="f in fieldCandidates"
                  :key="f.name"
                  :value="f.name"
                  :label="f.displayName || f.name"
                />
              </a-select>
              <a-select
                v-if="formatRuleNeedsCondition(rule.apply)"
                :model-value="rule.op"
                size="small"
                :style="{ width: '104px' }"
                @change="(v: string) => onOpChange(i, v as ViewFilterOp)"
              >
                <a-option
                  v-for="op in opsOf(rule)"
                  :key="op"
                  :value="op"
                  :label="opLabels[op]"
                />
              </a-select>
              <div
                v-if="formatRuleNeedsCondition(rule.apply) && rule.field && opNeedsValue(rule.op)"
                class="fmt-value"
              >
                <a-select
                  v-if="kindOfName(rule.field) === 'person'"
                  :model-value="rule.value"
                  placeholder="请选择人员"
                  allow-clear
                  :loading="userLoading"
                  size="small"
                  style="width: 132px"
                  @update:model-value="(v: unknown) => patch(i, { value: v })"
                  @focus="ensureUserOptions"
                >
                  <a-option
                    v-for="u in userOptions"
                    :key="u.value"
                    :value="u.value"
                    :label="u.label"
                  />
                </a-select>
                <a-select
                  v-else-if="kindOfName(rule.field) === 'enum' && enumOptionsOf(rule).length"
                  :model-value="rule.value"
                  placeholder="请输入"
                  allow-clear
                  size="small"
                  style="width: 132px"
                  @update:model-value="(v: unknown) => patch(i, { value: v })"
                >
                  <a-option
                    v-for="o in enumOptionsOf(rule)"
                    :key="o.value"
                    :value="o.value"
                    :label="o.label"
                  />
                </a-select>
                <LovSelect
                  v-else-if="kindOfName(rule.field) === 'enum' && !!condFieldOf(rule.field)?.lovCode"
                  :code="condFieldOf(rule.field)!.lovCode!"
                  :model-value="rule.value as string | number | null"
                  size="small"
                  style="width: 132px"
                  @update:model-value="(v: unknown) => patch(i, { value: v })"
                />
                <a-input-number
                  v-else-if="kindOfName(rule.field) === 'number'"
                  :model-value="rule.value"
                  placeholder="请输入"
                  size="small"
                  style="width: 132px"
                  @update:model-value="(v: unknown) => patch(i, { value: v })"
                />
                <a-date-picker
                  v-else-if="kindOfName(rule.field) === 'datetime'"
                  :model-value="rule.value"
                  placeholder="请输入"
                  size="small"
                  style="width: 132px"
                  value-format="YYYY-MM-DD"
                  @update:model-value="(v: unknown) => patch(i, { value: v })"
                />
                <a-input
                  v-else
                  :model-value="rule.value as string"
                  placeholder="请输入"
                  size="small"
                  style="width: 132px"
                  @update:model-value="(v: string) => patch(i, { value: v })"
                />
              </div>
              <a-button type="text" size="mini" @click="removeRule(i)">
                <icon-park type="close" />
              </a-button>
            </div>
            <div v-if="openColorIdx === i" class="fmt-palette" @click.stop>
              <div class="fmt-palette-label">颜色</div>
              <div class="fmt-swatches">
                <button
                  v-for="c in FORMAT_PRESET_COLORS"
                  :key="c"
                  type="button"
                  class="fmt-swatch"
                  :class="{ 'is-selected': isPresetSelected(rule.color, c) }"
                  :style="{ backgroundColor: c }"
                  :title="c"
                  @click="onColorChange(i, c)"
                />
              </div>
              <a-checkbox
                :model-value="!!rule.bold"
                @update:model-value="(v: boolean) => onBoldChange(i, v === true)"
              >
                文字加粗
              </a-checkbox>
            </div>
          </div>
          <a-button type="text" size="small" :disabled="addDisabled" @click="addRule">
            + 添加条件
          </a-button>
        </div>
      </div>
    </template>
    <template #default>
      <slot />
    </template>
  </a-popover>
</template>

<script setup lang="ts">
import type { FieldMeta } from '@/core/types/field';
import type { ViewKind } from '@/core/utils/viewMapping';
import type { FormatApply, ViewFilterOp, ViewFormatRule } from '@/core/utils/viewProfile';
import LovSelect from '@/components/LovSelect.vue';
import { useFormatPopover } from './useFormatPopover';

const props = defineProps<{
  visible: boolean;
  fields: FieldMeta[];
  modelValue: ViewFormatRule[];
  viewKind: ViewKind;
}>();

const emit = defineEmits<{
  'update:visible': [v: boolean];
  change: [rules: ViewFormatRule[]];
}>();

const {
  rules,
  allowedApply,
  applyLabels,
  opLabels,
  fieldCandidates,
  userOptions,
  userLoading,
  addDisabled,
  opNeedsValue,
  kindOfName,
  opsOf,
  enumOptionsOf,
  condFieldOf,
  addRule,
  removeRule,
  patch,
  onFieldChange,
  onApplyChange,
  onOpChange,
  onColorChange,
  onBoldChange,
  isPresetSelected,
  formatRuleNeedsCondition,
  FORMAT_PRESET_COLORS,
  onDragStart,
  onDrop,
  onVisibleChange,
  applyChoices,
  ensureUserOptions,
  openColorIdx,
} = useFormatPopover(props, emit);
</script>

<style scoped>
.format-builder {
  width: max-content;
  padding: 4px 0;
}
.fmt-head {
  display: flex;
  align-items: center;
  margin-bottom: 8px;
}
.fmt-title {
  font-weight: 500;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.fmt-rule {
  margin-bottom: 8px;
}
.fmt-row {
  display: flex;
  align-items: center;
  gap: 6px;
  width: max-content;
}
.fmt-drag {
  cursor: grab;
  color: var(--color-text-3);
  display: inline-flex;
}
.fmt-color-btn {
  width: 22px;
  height: 22px;
  flex-shrink: 0;
  border: 1px solid var(--color-border-2);
  border-radius: 4px;
  padding: 0;
  cursor: pointer;
}
.fmt-palette {
  margin: 8px 0 0 28px;
  width: max-content;
  padding: 8px;
  background: var(--color-fill-1);
  border: 1px solid var(--color-border-2);
  border-radius: 6px;
}
.fmt-palette-label {
  font-size: 12px;
  color: var(--color-text-2);
  margin-bottom: 8px;
}
.fmt-swatches {
  display: grid;
  grid-template-columns: repeat(10, 20px);
  gap: 6px;
  margin-bottom: 10px;
}
.fmt-swatch {
  width: 20px;
  height: 20px;
  border: 1px solid var(--color-border-2);
  border-radius: 4px;
  cursor: pointer;
  padding: 0;
}
.fmt-swatch.is-selected {
  outline: 2px solid rgb(var(--primary-6));
  outline-offset: 1px;
}
.fmt-value {
  width: 132px;
  flex-shrink: 0;
}
</style>

<style>
.format-popover {
  width: max-content;
  max-width: none;
}
</style>
