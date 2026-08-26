import { computed, nextTick, ref, watch } from 'vue';
import cubeApi from '@/api';
import type { FieldMeta } from '@/core/types/field';
import type { ViewKind } from '@/core/utils/viewMapping';
import { normalizeDataSource } from '@/core/utils/viewMapping';
import type { FormatApply, ViewFilterOp, ViewFormatRule } from '@/core/utils/viewProfile';
import {
  FILTER_OPS_BY_KIND,
  FILTER_OP_LABELS,
  opNeedsValue,
  resetCondForField,
  resolveFieldFilterKind,
} from '@/core/utils/filterBuilder';
import {
  DEFAULT_FORMAT_COLOR,
  FORMAT_APPLY_LABELS,
  FORMAT_PRESET_COLORS,
  formatApplyOptions,
  formatRuleNeedsCondition,
  moveFormatRule,
  newFormatRule,
  seedFormatRulesOnOpen,
} from '@/core/utils/viewFormat';

const MAX_RULES = 50;

interface FormatPopoverProps {
  visible: boolean;
  fields: FieldMeta[];
  modelValue: ViewFormatRule[];
  viewKind: ViewKind;
}

interface FormatPopoverEmits {
  'update:visible': [v: boolean];
  change: [rules: ViewFormatRule[]];
}

type FormatPopoverEmit = <K extends keyof FormatPopoverEmits>(
  event: K,
  ...args: FormatPopoverEmits[K]
) => void;

export function useFormatPopover(props: FormatPopoverProps, emit: FormatPopoverEmit) {
  const allowedApply = computed(() => formatApplyOptions(props.viewKind));
  const applyLabels = FORMAT_APPLY_LABELS;
  const opLabels = FILTER_OP_LABELS;

  const rules = ref<ViewFormatRule[]>([]);
  const dragFrom = ref(-1);
  const openColorIdx = ref(-1);

  watch(
    () => props.visible,
    () => {
      openColorIdx.value = -1;
    },
  );

  function syncFromProps() {
    rules.value = (props.modelValue || []).map((r) => ({ ...r }));
  }

  watch(
    () => props.modelValue,
    () => syncFromProps(),
    { immediate: true, deep: true },
  );

  function commit(next: ViewFormatRule[]) {
    rules.value = next;
    emit('change', next.map((r) => ({ ...r })));
  }

  function condFieldOf(name: string): FieldMeta | undefined {
    return props.fields.find((f) => f.name === name);
  }

  function kindOfName(name: string) {
    const f = condFieldOf(name);
    return f ? resolveFieldFilterKind(f) : 'string';
  }

  function opsOf(rule: ViewFormatRule): readonly ViewFilterOp[] {
    return rule.field ? FILTER_OPS_BY_KIND[kindOfName(rule.field)] : ['eq'];
  }

  function enumOptionsOf(rule: ViewFormatRule): { value: string; label: string }[] {
    const f = rule.field ? condFieldOf(rule.field) : undefined;
    if (f?.dataSource && Object.keys(f.dataSource).length) {
      return normalizeDataSource(f.dataSource).options;
    }
    return [];
  }

  const fieldCandidates = computed(() => props.fields.filter((f) => !!f.name));

  const userOptions = ref<{ value: string; label: string }[]>([]);
  const userLoading = ref(false);
  async function ensureUserOptions() {
    if (userOptions.value.length || userLoading.value) return;
    userLoading.value = true;
    try {
      const res = await cubeApi.page.getList('/Admin/User', { pageIndex: 0, pageSize: 500 });
      const rows = (res.data as Record<string, unknown>[]) || [];
      userOptions.value = rows.map((u) => {
        const id = u.id ?? u.Id;
        const name = u.name ?? u.Name ?? u.account ?? u.Account;
        return { value: String(id), label: String(name ?? id) };
      });
    } catch {
      /* ignore */
    } finally {
      userLoading.value = false;
    }
  }

  function applyChoices(rule: ViewFormatRule): FormatApply[] {
    const opts = allowedApply.value;
    if (opts.includes(rule.apply)) return opts;
    return [rule.apply, ...opts];
  }

  function canChangeApply(next: FormatApply): boolean {
    return allowedApply.value.includes(next);
  }

  function addRule() {
    if (rules.value.length >= MAX_RULES) return;
    const first = fieldCandidates.value[0]?.name || '';
    const apply = allowedApply.value[0] || 'cell';
    commit([...rules.value, newFormatRule({ apply, field: first })]);
  }

  function removeRule(i: number) {
    const next = rules.value.filter((_, idx) => idx !== i);
    commit(next);
  }

  function patch(i: number, partial: Partial<ViewFormatRule>) {
    const next = rules.value.map((r, idx) => (idx === i ? { ...r, ...partial } : r));
    commit(next);
  }

  function onFieldChange(i: number, field: string) {
    const rule = { ...rules.value[i], field };
    resetCondForField(rule as { field: string; op: ViewFilterOp; value?: unknown; value2?: unknown }, kindOfName(field));
    if (kindOfName(field) === 'person') void ensureUserOptions();
    patch(i, { field: rule.field, op: rule.op, value: rule.value });
  }

  function onApplyChange(i: number, apply: FormatApply) {
    if (!canChangeApply(apply)) return;
    patch(i, { apply });
  }

  function onOpChange(i: number, op: ViewFilterOp) {
    patch(i, { op, value: undefined });
  }

  function onColorChange(i: number, color: string) {
    const hex = typeof color === 'string' ? color.trim() : '';
    patch(i, { color: /^#[0-9A-Fa-f]{6}$/.test(hex) ? hex : DEFAULT_FORMAT_COLOR });
  }

  function onBoldChange(i: number, bold: boolean) {
    patch(i, { bold: bold ? true : undefined });
  }

  function isPresetSelected(ruleColor: string, preset: string) {
    return ruleColor.toUpperCase() === preset.toUpperCase();
  }

  function onDragStart(idx: number, e: DragEvent) {
    dragFrom.value = idx;
    e.dataTransfer?.setData('text/plain', String(idx));
  }

  function onDrop(toIdx: number) {
    const from = dragFrom.value;
    dragFrom.value = -1;
    if (from < 0 || from === toIdx) return;
    commit(moveFormatRule(rules.value, from, toIdx));
  }

  function onVisibleChange(v: boolean) {
    emit('update:visible', v);
    if (!v) return;
    // 延后种默认规则，避免与 Arco popup-visible-change 同拍提交触发递归更新
    void nextTick(() => {
      if (!props.visible) return;
      const seeded = seedFormatRulesOnOpen(rules.value, {
        firstField: fieldCandidates.value[0]?.name || '',
        apply: allowedApply.value[0] || 'cell',
      });
      if (seeded) commit(seeded);
    });
  }

  const addDisabled = computed(() => rules.value.length >= MAX_RULES);

  return {
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
  };
}

export { MAX_RULES };
