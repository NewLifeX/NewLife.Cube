import { describe, expect, it } from 'vitest';
import {
  resolveWfToolbarSubmit,
  resolveWfRowActions,
  wfStatusBadge,
} from './useWorkflowList';
import { WF_ROW_FIELD, type WorkflowPageBlock } from '@/core/types/workflow';

/** 行覆盖构造：按后端 SetItem 键平铺 */
function rowOf(partial: Partial<Record<keyof typeof WF_ROW_FIELD, unknown>> = {}) {
  return {
    id: 1,
    name: 'x',
    ...Object.fromEntries(
      Object.entries(partial).map(([k, v]) => [WF_ROW_FIELD[k as keyof typeof WF_ROW_FIELD], v]),
    ),
  } as Record<string, unknown>;
}

const disabledBlock: WorkflowPageBlock = { enabled: true, canStart: true };

describe('resolveWfToolbarSubmit 工具栏「批量提交」（IA §4）', () => {
  it('embed 分享：整体不渲染（IA §5）', () => {
    expect(resolveWfToolbarSubmit({ enabled: true, canStart: true }, 3, true)).toEqual({
      visible: false,
      disabled: true,
      tooltip: '',
    });
  });

  it('类型未启用 / 无块：不渲染', () => {
    expect(resolveWfToolbarSubmit({ enabled: false }, 3)).toMatchObject({ visible: false });
    expect(resolveWfToolbarSubmit(null, 3)).toMatchObject({ visible: false });
    expect(resolveWfToolbarSubmit(undefined, 3)).toMatchObject({ visible: false });
  });

  it('enabled + canStart=false：渲染但禁用（无发起权限）', () => {
    expect(resolveWfToolbarSubmit({ enabled: true, canStart: false }, 3)).toEqual({
      visible: true,
      disabled: true,
      tooltip: '当前账号无发起审批权限',
    });
  });

  it('canStart 缺省按可用处理：无勾选时禁用提示勾选', () => {
    expect(resolveWfToolbarSubmit(disabledBlock, 0)).toEqual({
      visible: true,
      disabled: true,
      tooltip: '请先勾选要提交的记录',
    });
  });

  it('enabled + canStart + 有勾选：可用', () => {
    expect(resolveWfToolbarSubmit(disabledBlock, 2)).toEqual({
      visible: true,
      disabled: false,
      tooltip: '',
    });
  });
});

describe('resolveWfRowActions 行「提交/进度」矩阵（IA §4）', () => {
  it('embed / 类型未启用 / 无行：整行不渲染', () => {
    expect(resolveWfRowActions({ enabled: true }, rowOf(), true)).toMatchObject({
      submitVisible: false,
      progressVisible: false,
    });
    expect(resolveWfRowActions({ enabled: false }, rowOf())).toMatchObject({
      submitVisible: false,
      progressVisible: false,
    });
    expect(resolveWfRowActions(null, rowOf())).toMatchObject({ submitVisible: false });
    expect(resolveWfRowActions(disabledBlock, null)).toMatchObject({ submitVisible: false });
  });

  it('none + 可发起：行提交可用；无实例隐藏进度', () => {
    const a = resolveWfRowActions(disabledBlock, rowOf({ status: 'none', canStart: true }));
    expect(a).toMatchObject({ submitVisible: true, submitDisabled: false, submitTooltip: '' });
    expect(a.progressVisible).toBe(false);
  });

  it('running：行提交禁用 + tooltip「审批中」；进度显示', () => {
    const a = resolveWfRowActions(
      disabledBlock,
      rowOf({ status: 'running', canStart: false, instanceId: 9 }),
    );
    expect(a).toMatchObject({
      submitVisible: true,
      submitDisabled: true,
      submitTooltip: '审批中，不可重复提交',
      progressVisible: true,
    });
  });

  it('approved + 有实例：行提交禁用（不可再发起）+ 进度显示', () => {
    const a = resolveWfRowActions(
      disabledBlock,
      rowOf({ status: 'approved', canStart: false, instanceId: 7 }),
    );
    expect(a).toMatchObject({
      submitVisible: true,
      submitDisabled: true,
      submitTooltip: '已通过，不可再次发起',
      progressVisible: true,
    });
  });

  it('approved 即便 canStart 误为 true：仍禁提交（前端兜底）', () => {
    const a = resolveWfRowActions(
      disabledBlock,
      rowOf({ status: 'approved', canStart: true, instanceId: 7 }),
    );
    expect(a).toMatchObject({
      submitVisible: true,
      submitDisabled: true,
      submitTooltip: '已通过，不可再次发起',
      progressVisible: true,
    });
  });

  it('rejected + 无实例：可提交 + 进度隐藏（无实例则隐藏进度）', () => {
    const a = resolveWfRowActions(disabledBlock, rowOf({ status: 'rejected', canStart: true }));
    expect(a).toMatchObject({ submitVisible: true, submitDisabled: false });
    expect(a.progressVisible).toBe(false);
  });
});

describe('wfStatusBadge 行状态徽章', () => {
  it('五态文案/色板/图标/Tooltip', () => {
    expect(wfStatusBadge('running')).toMatchObject({
      text: '审批中',
      color: 'orange',
      icon: '◎',
      tooltip: '审批进行中',
    });
    expect(wfStatusBadge('approved')).toMatchObject({ text: '已通过', color: 'green', icon: '✓' });
    expect(wfStatusBadge('rejected')).toMatchObject({ text: '已驳回', color: 'red', icon: '✕' });
    expect(wfStatusBadge('withdrawn')).toMatchObject({ text: '已撤回', color: 'gray', icon: '↺' });
    expect(wfStatusBadge('none')).toMatchObject({ text: '未发起', color: 'gray', icon: '○' });
    expect(wfStatusBadge('cancelled')).toMatchObject({ text: 'cancelled', color: 'gray' });
  });
});
