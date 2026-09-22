import { computed, ref } from 'vue';
import { isEmbedMode } from '@/core/utils/embedMode';
import {
  wfRowCanStart,
  wfRowInstanceId,
  wfRowRestartBlocked,
  wfRowStatus,
  type WfRowStatus,
  type WorkflowPageBlock,
} from '@/core/types/workflow';
import type { ListContext } from './listContext';

/**
 * 实体列表审批入口状态（OSC-26090347f1 T8b，IA §4 按钮矩阵）。
 *
 * 纯函数 resolve* 承载矩阵决策（可单测），useWorkflowList 组装 ctx 状态供 DefaultList 消费：
 * - 工具栏「批量提交」：类型级 canStart + 勾选数（仅列表视图渲染）
 * - 行操作「提交/进度」：行 __wfStatus / __wfInstanceId / __wfCanStart（仅列表视图）
 * - embed（?embed=1）整体隐藏（IA §5）
 */

/** 行状态徽章（列表列 / Tooltip）：文案 + 色板 + 图标 + 提示 */
export interface WfStatusBadgeMeta {
  text: string;
  /** Arco Tag 色名（兼容旧用法） */
  color: string;
  /** 徽标前景色 */
  textColor: string;
  /** 徽标浅底色 */
  bgColor: string;
  /** 简短图标字符（VTable canvas 可用） */
  icon: string;
  /** hover Tooltip */
  tooltip: string;
}

export const WF_STATUS_BADGE: Readonly<Record<string, WfStatusBadgeMeta>> = {
  none: {
    text: '未发起',
    color: 'gray',
    textColor: '#86909c',
    bgColor: '#f2f3f5',
    icon: '○',
    tooltip: '尚未发起审批',
  },
  running: {
    text: '审批中',
    color: 'orange',
    textColor: '#ff7d00',
    bgColor: '#fff7e8',
    icon: '◎',
    tooltip: '审批进行中',
  },
  approved: {
    text: '已通过',
    color: 'green',
    textColor: '#00b42a',
    bgColor: '#e8ffea',
    icon: '✓',
    tooltip: '审批已通过',
  },
  rejected: {
    text: '已驳回',
    color: 'red',
    textColor: '#f53f3f',
    bgColor: '#ffece8',
    icon: '✕',
    tooltip: '审批已驳回',
  },
  withdrawn: {
    text: '已撤回',
    color: 'gray',
    textColor: '#4e5969',
    bgColor: '#f2f3f5',
    icon: '↺',
    tooltip: '流程已撤回',
  },
};

/** 状态徽章元数据（未知状态回落灰显原文） */
export function wfStatusBadge(status: WfRowStatus | string): WfStatusBadgeMeta {
  const hit = WF_STATUS_BADGE[status];
  if (hit) return hit;
  const text = status || '未发起';
  return {
    text,
    color: 'gray',
    textColor: '#86909c',
    bgColor: '#f2f3f5',
    icon: '·',
    tooltip: text,
  };
}

/** 工具栏「批量提交」按钮态 */
export interface WfToolbarSubmit {
  /** 是否渲染（类型启用且非 embed） */
  visible: boolean;
  /** 是否禁用 */
  disabled: boolean;
  /** 禁用提示 */
  tooltip: string;
}

/**
 * 工具栏「批量提交」矩阵（IA §4 前两列）
 * @param block GetPage.workflow 类型块
 * @param selectedCount 已勾选记录数
 * @param embed 分享 embed 模式
 */
export function resolveWfToolbarSubmit(
  block: WorkflowPageBlock | null | undefined,
  selectedCount: number,
  embed = false,
): WfToolbarSubmit {
  if (embed || !block?.enabled) return { visible: false, disabled: true, tooltip: '' };
  // 类型级 canStart=false → 整按钮禁用（当前账号无发起权限）
  if (block.canStart === false)
    return { visible: true, disabled: true, tooltip: '当前账号无发起审批权限' };
  // 无勾选 → 禁用（T8c §3.1 无选中：禁用按钮）
  if (!(selectedCount > 0))
    return { visible: true, disabled: true, tooltip: '请先勾选要提交的记录' };
  return { visible: true, disabled: false, tooltip: '' };
}

/** 行操作按钮态 */
export interface WfRowActions {
  /** 行「提交」是否渲染 */
  submitVisible: boolean;
  /** 行「提交」是否禁用 */
  submitDisabled: boolean;
  /** 行「提交」禁用提示 */
  submitTooltip: string;
  /** 行「进度」是否渲染（有实例才显示，IA §4） */
  progressVisible: boolean;
}

/**
 * 行操作按钮矩阵（IA §4 后三列）。行状态唯一来源 __wfStatus，禁止前端拼实例。
 * 审批中 / 已通过：禁再次发起；驳回 / 撤回 / 未发起：可发起。
 * @param block GetPage.workflow 类型块
 * @param row 列表行（含 __wf* 覆盖）
 * @param embed 分享 embed 模式
 */
export function resolveWfRowActions(
  block: WorkflowPageBlock | null | undefined,
  row: Record<string, unknown> | null | undefined,
  embed = false,
): WfRowActions {
  if (embed || !block?.enabled || !row)
    return { submitVisible: false, submitDisabled: true, submitTooltip: '', progressVisible: false };

  const status = wfRowStatus(row);
  const rowCanStart = wfRowCanStart(row);
  const hasInstance = !!wfRowInstanceId(row);

  // 审批中 / 已通过：禁提交
  if (wfRowRestartBlocked(row))
    return {
      submitVisible: true,
      submitDisabled: true,
      submitTooltip: status === 'approved' ? '已通过，不可再次发起' : '审批中，不可重复提交',
      progressVisible: hasInstance,
    };
  if (!rowCanStart)
    return {
      submitVisible: true,
      submitDisabled: true,
      submitTooltip: '当前账号无发起审批权限',
      progressVisible: hasInstance,
    };
  // 到达此处：非 running/approved 且行级可发起（none/rejected/withdrawn）→ 可提交新流程
  return {
    submitVisible: true,
    submitDisabled: false,
    submitTooltip: '',
    progressVisible: hasInstance,
  };
}

/** DefaultList 消费的状态：workflow 块 + 工具栏/行矩阵 + 提交抽屉开关 */
export function useWorkflowList(ctx: ListContext) {
  const embed = isEmbedMode();
  const workflowBlock = computed(() => ctx.workflowBlock.value);

  /** 类型级启用且非 embed：整个审批入口是否可见 */
  const wfEnabled = computed(() => !embed && workflowBlock.value?.enabled === true);

  /** 工具栏「批量提交」按钮态（勾选数来自 ctx.selectedKeys） */
  const toolbarSubmit = computed(() =>
    resolveWfToolbarSubmit(workflowBlock.value, ctx.selectedKeys.value.length, embed),
  );

  /** 提交确认抽屉开关（T8d 由工具栏按钮触发） */
  const submitDrawerVisible = ref(false);
  function openSubmitDrawer() {
    if (!toolbarSubmit.value.disabled) submitDrawerVisible.value = true;
  }

  /** 行操作按钮态（T8d 行按钮消费） */
  function rowActions(row: Record<string, unknown> | null | undefined): WfRowActions {
    return resolveWfRowActions(workflowBlock.value, row, embed);
  }

  return {
    workflowBlock,
    wfEnabled,
    toolbarSubmit,
    submitDrawerVisible,
    openSubmitDrawer,
    rowActions,
  };
}
