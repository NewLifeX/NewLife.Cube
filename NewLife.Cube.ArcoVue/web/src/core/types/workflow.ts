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

/** 读取行实例 Id（雪花：全程 string，禁止 Number() 丢精度；0/空=无实例） */
export function wfRowInstanceId(row: Record<string, unknown> | null | undefined): string {
  const v = row?.[WF_ROW_FIELD.instanceId];
  if (v == null || v === '' || v === 0) return '';
  if (typeof v === 'string') {
    const s = v.trim();
    return s && s !== '0' ? s : '';
  }
  // 兼容后端偶发 number（已丢精度时仍尽量透传，正确路径应由 JSON 字符串下发）
  if (typeof v === 'number' && Number.isFinite(v) && v > 0) return String(Math.trunc(v));
  const s = String(v).trim();
  return s && s !== '0' ? s : '';
}

/** 是否有关联流程实例 */
export function wfRowHasInstance(row: Record<string, unknown> | null | undefined): boolean {
  return !!wfRowInstanceId(row);
}

/** 读取行级可发起标记（后端已综合 running/approved/Detail/StartFilter；缺省 false 保守处理） */
export function wfRowCanStart(row: Record<string, unknown> | null | undefined): boolean {
  return row?.[WF_ROW_FIELD.canStart] === true;
}

/** 审批中：实体禁止编辑/删除（含所有视图；写锁由后端拦截器兜底） */
export function wfRowEditLocked(row: Record<string, unknown> | null | undefined): boolean {
  return wfRowStatus(row) === 'running';
}

/** 当前用户在本节点可写的业务字段名（GetDetail 注入 __wfWritable；缺省空） */
export function wfRowWritable(row: Record<string, unknown> | null | undefined): string[] {
  const v = row?.[WF_ROW_FIELD.writable];
  if (!Array.isArray(v)) return [];
  return v.map(String).filter(Boolean);
}

/** 审批中且有可写字段：允许走 Patch 通道改指定字段 */
export function wfRowCanPatchWritable(row: Record<string, unknown> | null | undefined): boolean {
  return wfRowEditLocked(row) && wfRowWritable(row).length > 0;
}

/** 审批中或已通过：禁止再次发起 */
export function wfRowRestartBlocked(row: Record<string, unknown> | null | undefined): boolean {
  const s = wfRowStatus(row);
  return s === 'running' || s === 'approved';
}

/**
 * 以下为设计器 GraphJson 类型（design §4）：节点 data 语义由后端 WorkflowGraph 解析，
 * 前端只做编辑与序列化（禁止浏览器执行引擎）。
 */

/** 图节点 */
export interface WfGraphNodeData {
  id: string;
  type: string;
  data: Record<string, unknown>;
}

/** 图边 */
export interface WfGraphEdgeData {
  source: string;
  target: string;
}

/** GraphJson（design §4） */
export interface WfGraphData {
  version?: number;
  nodes: WfGraphNodeData[];
  edges: WfGraphEdgeData[];
  /** 流程根：不纳入效率统计（design §6.2；节点级另有 data.excludeStats） */
  excludeStats?: boolean;
}

/** 节点 data 字段常量（与后端 WorkflowNode 解析对齐） */
export const WF_NODE_DATA_KEY = {
  name: 'name',
  mode: 'mode',
  to: 'to',
  fields: 'fields',
  timeoutHours: 'timeoutHours',
  timeoutAction: 'timeoutAction',
  allowAddSign: 'allowAddSign',
  allowRollback: 'allowRollback',
  allowTransfer: 'allowTransfer',
  cases: 'cases',
  defaultTarget: 'defaultTarget',
} as const;

/** 节点类型白名单（design §4.1：仅 oa.*；oa.handle 为办理节点） */
export const WF_NODE_TYPES = ['oa.start', 'oa.approve', 'oa.handle', 'oa.cc', 'oa.xor', 'oa.parallel', 'oa.end'] as const;
export type WfNodeType = (typeof WF_NODE_TYPES)[number];

/** 解析节点类型中文名 */
export function wfNodeTypeLabel(type: string): string {
  switch (type) {
    case 'oa.start':
      return '开始';
    case 'oa.approve':
      return '审批';
    case 'oa.handle':
      return '办理';
    case 'oa.cc':
      return '知会';
    case 'oa.xor':
      return '条件分流';
    case 'oa.parallel':
      return '并行分支';
    case 'oa.end':
      return '结束';
    default:
      return type || '未知';
  }
}

/** 是否 oa.* 白名单节点 */
export function isWfNodeType(type: string): boolean {
  return (WF_NODE_TYPES as readonly string[]).includes(type);
}

/** 规范 `to` 接收人：读取 kind 对应 id 数组；缺省 users */
export function wfRecipientTo(to: unknown): { kind: string; ids: number[] } {
  const t = (to ?? {}) as Record<string, unknown>;
  const kindMap: Record<string, 'users' | 'roles' | 'departments'> = {
    users: 'users',
    roles: 'roles',
    departments: 'departments',
    user: 'users',
    role: 'roles',
    department: 'departments',
  };
  const kind = kindMap[String(t.kind ?? 'users').toLowerCase()] ?? 'users';
  const arr = Array.isArray(t[kind]) ? (t[kind] as unknown[]) : [];
  const ids = arr.map(Number).filter((n) => Number.isFinite(n) && n > 0);
  return { kind, ids };
}

/** 序列化 `to`（仅保留当前 kind 的 id 数组） */
export function wfRecipientToJson(to: unknown, ids: number[], kind: string): Record<string, unknown> {
  const k = kind === 'roles' ? 'roles' : kind === 'departments' ? 'departments' : 'users';
  const base: Record<string, unknown> = { kind: k, users: [], roles: [], departments: [] };
  if (ids.length) base[k] = ids;
  return { ...(to && typeof to === 'object' ? to : {}), ...base };
}

/**
 * 设计器选人类型（六种，与后端 to.kind 对齐，design §3.1）：
 * 指定成员 / 角色 / 部门 / 部门负责人 / 提交人自选 / 表单人员。
 */
export type WfToKind = 'users' | 'roles' | 'departments' | 'manager' | 'starterPick' | 'field';

/** 六种选人类型中文名 */
export const WF_TO_KIND_LABEL: Record<WfToKind, string> = {
  users: '指定成员',
  roles: '角色',
  departments: '部门',
  manager: '部门负责人',
  starterPick: '提交人自选',
  field: '表单人员',
};

/** 读取 to.kind（六种；旧图无 kind 时按 users→roles→departments 推导，均空回 users） */
export function wfToKindOf(to: unknown): WfToKind {
  const t = (to ?? {}) as Record<string, unknown>;
  const kind = String(t.kind ?? '').trim();
  if (kind === 'roles' || kind === 'departments' || kind === 'manager' || kind === 'starterPick' || kind === 'field') return kind;
  if (kind === 'users') return 'users';
  const has = (k: string) => Array.isArray(t[k]) && (t[k] as unknown[]).length > 0;
  if (has('users')) return 'users';
  if (has('roles')) return 'roles';
  if (has('departments')) return 'departments';
  return 'users';
}

/** 构造六种 `to` JSON（manager/starterPick/field 无 Id；extra 覆盖 scope/multiple/field/fieldAs） */
export function wfToJsonForKind(kind: WfToKind, ids: number[], extra?: Record<string, unknown>): Record<string, unknown> {
  switch (kind) {
    case 'manager':
      return { kind: 'manager' };
    case 'starterPick':
      return { kind: 'starterPick', scope: 'all', multiple: false, ...(extra ?? {}) };
    case 'field':
      return { kind: 'field', field: '', fieldAs: 'user', ...(extra ?? {}) };
    default:
      return wfRecipientToJson(undefined, ids, kind);
  }
}

/** 需要 Id 搜索的三种选人类型 */
export function isSearchToKind(kind: WfToKind): boolean {
  return kind === 'users' || kind === 'roles' || kind === 'departments';
}

