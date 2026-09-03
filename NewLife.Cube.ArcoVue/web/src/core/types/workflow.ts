/**
 * OA 审批流程共享类型（OSC-26090347f1）。
 *
 * 状态唯一来源为服务端：GetPage 返回类型级 `workflow` 块，GetList/GetDetail 行携带
 * `__wfStatus` / `__wfInstanceId` / `__wfCanStart`（详情另加 `__wfWritable`）。
 * 前端禁止再查实例表拼状态（design §6.3 / IA §4）。
 */

/** GetPage.workflow 类型级能力块（后端 WorkflowPageOverlay.GetTypeBlock） */
export interface WorkflowPageBlock {
  /** 该类型是否挂载已发布流程定义（未启用时整体不渲染审批入口） */
  enabled: boolean;
  /** 可用定义数（仅登录用户下发） */
  definitionCount?: number;
  /** 写锁策略：full / nodeFields（仅登录用户下发） */
  lockPolicy?: string;
  /** 当前账号是否可发起审批（Detail 权限，仅登录用户下发；类型级开关） */
  canStart?: boolean;
}

/** 行级 __wfStatus 取值（服务端 MapStatus 小写映射） */
export type WfRowStatus =
  /** 无实例 */
  | 'none'
  /** 审批中 */
  | 'running'
  /** 已通过 */
  | 'approved'
  /** 已驳回 */
  | 'rejected'
  /** 已撤回 */
  | 'withdrawn'
  /** 后端新增终态透传 */
  | (string & {});

/** 行级覆盖字段名（与后端 SetItem 键一致，平铺在行 JSON） */
export const WF_ROW_FIELD = {
  status: '__wfStatus',
  instanceId: '__wfInstanceId',
  canStart: '__wfCanStart',
  writable: '__wfWritable',
} as const;

/** 读取行 workflow 状态（缺省 none） */
export function wfRowStatus(row: Record<string, unknown> | null | undefined): WfRowStatus {
  const v = row?.[WF_ROW_FIELD.status];
  if (typeof v === 'string' && v) return v as WfRowStatus;
  return 'none';
}

/** 读取行实例 Id（0=无实例；进度入口可见性依据） */
export function wfRowInstanceId(row: Record<string, unknown> | null | undefined): number {
  const v = row?.[WF_ROW_FIELD.instanceId];
  if (typeof v === 'number') return v;
  const n = Number(v);
  return Number.isFinite(n) && n > 0 ? n : 0;
}

/** 读取行级可发起标记（后端已综合 running/Detail/StartFilter；缺省 false 保守处理） */
export function wfRowCanStart(row: Record<string, unknown> | null | undefined): boolean {
  return row?.[WF_ROW_FIELD.canStart] === true;
}
