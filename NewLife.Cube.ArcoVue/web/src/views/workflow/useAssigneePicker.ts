import { computed } from 'vue';
import type { WfToKind } from '@/core/types/workflow';
import { WF_TO_KIND_LABEL, isSearchToKind } from '@/core/types/workflow';
import type { RecipientKind } from './recipient';

/**
 * 设计器六种选人（design §3.1/§4.1）：指定成员/角色/部门（需 Id 搜索），
 * 部门负责人/提交人自选/表单人员（无 Id，带 scope/multiple/field/fieldAs 参数）。
 * 纯绑定逻辑：把 kind 与各参数透传为 v-model 事件，SFC 只做渲染。
 */

/** 六种选人顺序 */
export const ASSIGNEE_KIND_ORDER: WfToKind[] = [
  'users',
  'roles',
  'departments',
  'manager',
  'starterPick',
  'field',
];

/** 各类型一句话说明 */
export const ASSIGNEE_KIND_HINT: Record<WfToKind, string> = {
  users: '指定具体成员，可多选',
  roles: '角色下全部启用成员',
  departments: '部门下全部启用成员',
  manager: '发起人主部门的负责人（仅直属一级）',
  starterPick: '发起时由提交人现场选择',
  field: '取表单上已有的用户/部门字段',
};

/** 提交人自选范围 */
export const STARTER_SCOPE_OPTIONS = [
  { value: 'all', label: '全员' },
  { value: 'roles', label: '仅角色' },
  { value: 'departments', label: '仅部门' },
];

export interface AssigneePickerProps {
  kind?: WfToKind;
  modelValue?: number[];
  /** starterPick：可选范围 */
  scope?: string;
  /** starterPick：提交人是否可多选 */
  starterMultiple?: boolean;
  /** 搜索型（用户/角色/部门）是否多选（由节点签核模式决定） */
  multiSelect?: boolean;
  /** field：字段名 */
  field?: string;
  /** field：user=用户字段；manager=部门字段取负责人 */
  fieldAs?: string;
  /** field 可选字段 */
  fields?: { name: string; label: string }[];
}

export interface AssigneePickerEmits {
  (e: 'update:kind', kind: WfToKind): void;
  (e: 'update:modelValue', ids: number[]): void;
  (e: 'update:labels', labels: string[]): void;
  (e: 'update:scope', scope: string): void;
  (e: 'update:starterMultiple', multiple: boolean): void;
  (e: 'update:field', field: string): void;
  (e: 'update:fieldAs', fieldAs: string): void;
}

export function useAssigneePicker(props: AssigneePickerProps, emit: AssigneePickerEmits) {
  const currentKind = computed<WfToKind>(() => props.kind ?? 'users');
  /** 需要 Id 搜索的三种；其余三种无 Id */
  const isSearchKind = computed(() => isSearchToKind(currentKind.value));
  const kindLabel = (k: WfToKind) => WF_TO_KIND_LABEL[k];
  const kindHint = computed(() => ASSIGNEE_KIND_HINT[currentKind.value] ?? '');
  const fieldOptions = computed(() => (props.fields ?? []).map((f) => ({ value: f.name, label: f.label })));
  /** 子选择器（用户/角色/部门搜索）的类别视图 */
  const searchKind = computed(() => currentKind.value as RecipientKind);

  function onKindChange(v: unknown) {
    const k = String(v) as WfToKind;
    if (!ASSIGNEE_KIND_ORDER.includes(k) || k === currentKind.value) return;
    emit('update:kind', k);
  }

  function onIds(ids: number[]) {
    emit('update:modelValue', ids);
  }

  function onLabels(labels: string[]) {
    emit('update:labels', labels);
  }

  function onScope(v: unknown) {
    emit('update:scope', String(v ?? 'all'));
  }

  function onStarterMultiple(v: unknown) {
    emit('update:starterMultiple', v === true);
  }

  function onField(v: unknown) {
    emit('update:field', String(v ?? ''));
  }

  function onFieldAs(v: unknown) {
    emit('update:fieldAs', String(v ?? 'user'));
  }

  return {
    currentKind,
    isSearchKind,
    kindLabel,
    kindHint,
    fieldOptions,
    searchKind,
    onKindChange,
    onIds,
    onLabels,
    onScope,
    onStarterMultiple,
    onField,
    onFieldAs,
  };
}
