/**
 * 审批动作（同意 / 驳回 / 已办理 / 已阅 / 撤回 / 转交 / 加签 / 知会 / 回退）。
 * 待办列表与进度面板共用，避免两处各写一套提交。
 */
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { useAppStore } from '@/stores/app';
import type { RecipientKind } from './recipient';
import { wfIdOf, type WfId } from './useWorkflowProgress';

export type OpinionOp = 'approve' | 'reject' | 'handle' | 'read' | 'withdraw';
export type MoreOp = 'transfer' | 'addSign' | 'rollback' | 'cc';

function refreshMeta() {
  void useAppStore().refreshWorkflowMeta();
}

async function claimIfNeeded(taskId: string, userId: number | undefined, assigneeId?: number, candidate?: number[]) {
  if (userId && assigneeId !== userId && (candidate ?? []).includes(userId)) {
    try {
      await cubeApi.workflow.claim(taskId);
    } catch {
      /* 未认领也可直接办理时忽略 */
    }
  }
}

async function uploadFiles(files: File[], instanceId: WfId | undefined, taskId: string): Promise<string[]> {
  const iid = wfIdOf(instanceId);
  const attachmentIds: string[] = [];
  if (!iid) return attachmentIds;
  for (const f of files) {
    try {
      const up = await cubeApi.workflow.uploadAttachment(f, { instanceId: iid, taskId });
      const aid = wfIdOf((up.data as { id?: string | number } | undefined)?.id);
      if (aid) attachmentIds.push(aid);
    } catch (err) {
      Message.warning(formatApiError(err, `附件 ${f.name} 上传失败`));
    }
  }
  return attachmentIds;
}

/** 同意 / 驳回 / 已办理 / 已阅：先上传附件再提交意见 */
export async function submitTaskOpinion(input: {
  taskId: WfId | null | undefined;
  instanceId?: WfId;
  userId?: number;
  assigneeId?: number;
  candidate?: number[];
  kind: Exclude<OpinionOp, 'withdraw'>;
  text: string;
  files: File[];
}): Promise<boolean> {
  const taskId = wfIdOf(input.taskId);
  if (!taskId) return false;
  if (input.kind === 'reject' && !input.text.trim()) {
    Message.warning('驳回需填写意见');
    return false;
  }
  try {
    await claimIfNeeded(taskId, input.userId, input.assigneeId, input.candidate);
    const attachmentIds = await uploadFiles(input.files, input.instanceId, taskId);
    const body = {
      comment: input.text.trim() || undefined,
      attachmentIds: attachmentIds.length ? attachmentIds : undefined,
    };
    if (input.kind === 'reject') await cubeApi.workflow.reject(taskId, body);
    else await cubeApi.workflow.approve(taskId, body);
    Message.success(
      input.kind === 'reject' ? '已驳回' : input.kind === 'handle' ? '已办理' : input.kind === 'read' ? '已阅' : '已同意',
    );
    refreshMeta();
    return true;
  } catch (err) {
    Message.error(formatApiError(err, '操作失败'));
    return false;
  }
}

/** 发起人撤回（按实例） */
export async function submitWithdraw(instanceId: WfId | null | undefined, text: string): Promise<boolean> {
  const id = wfIdOf(instanceId);
  if (!id) return false;
  try {
    await cubeApi.workflow.withdraw(id, { comment: text.trim() || undefined });
    Message.success('已撤回');
    refreshMeta();
    return true;
  } catch (err) {
    Message.error(formatApiError(err, '撤回失败'));
    return false;
  }
}

/** 转交 / 加签 / 知会 / 回退 */
export async function submitTaskMore(input: {
  taskId: WfId | null | undefined;
  userId?: number;
  assigneeId?: number;
  candidate?: number[];
  mode: MoreOp;
  before: boolean;
  text: string;
  recipientKind: RecipientKind;
  recipients: number[];
  rollbackNode: string;
}): Promise<boolean> {
  const taskId = wfIdOf(input.taskId);
  if (!taskId) return false;
  if (input.mode === 'rollback' && !input.rollbackNode) {
    Message.warning('请选择回退到哪一步');
    return false;
  }
  if (input.mode !== 'rollback' && input.recipients.length === 0) {
    Message.warning('请选择接收人');
    return false;
  }
  try {
    await claimIfNeeded(taskId, input.userId, input.assigneeId, input.candidate);
    const comment = input.text.trim() || undefined;
    if (input.mode === 'rollback') {
      await cubeApi.workflow.rollback(taskId, { targetNodeId: input.rollbackNode, comment });
    } else {
      const to = { kind: input.recipientKind, users: [], roles: [], departments: [] } as Record<string, unknown>;
      to[input.recipientKind] = input.recipients;
      const body = { to: to as never, comment, ...(input.mode === 'addSign' ? { before: input.before } : {}) };
      const fn =
        input.mode === 'addSign'
          ? cubeApi.workflow.addSign
          : input.mode === 'transfer'
            ? cubeApi.workflow.transfer
            : cubeApi.workflow.cc;
      await fn(taskId, body);
    }
    Message.success('操作成功');
    refreshMeta();
    return true;
  } catch (err) {
    Message.error(formatApiError(err, '操作失败'));
    return false;
  }
}
