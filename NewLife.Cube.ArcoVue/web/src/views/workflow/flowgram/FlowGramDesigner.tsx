/**
 * FlowGram.AI 固定布局画布封装（OSC-26090347f1 T8e-FlowGram）。
 * React 组件（FlowGram 为 React 库），经 Vue react-dom 桥接挂载。
 *
 * - 结构编辑（在选中节点后插入/删除/拖拽重排）由 FlowGram 承担
 * - 节点卡片单独标题栏：类型徽标、签核方式，右侧垃圾桶删除；内容区显示接收人
 * - 每次结构/数据变化经 history onApply → onDocChanged(业务序 doc)，由 Vue 镜像重建 GraphJson
 * - 保存权威在后端 GraphJson；本组件不执行流程
 */
import { forwardRef, useEffect, useImperativeHandle, useMemo, useRef, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';

import '@flowgram.ai/fixed-layout-editor/index.css';

import {
  FixedLayoutEditor,
  FlowNodeEntity,
  FlowNodeRegistry,
  useNodeRender,
} from '@flowgram.ai/fixed-layout-editor';
import { defaultFixedSemiMaterials } from '@flowgram.ai/fixed-semi-materials';

import type { FlowDoc } from './flowgramGraph';
import { defaultNodeDataFor, isBusinessNodeType, nextCaseBlockId, xorSplitBlocks } from './flowgramGraph';
import type { WfBranchInfo, WfNodeCardInfo } from '../wfNodeCard';

/** 桥接 API：Vue 侧可调用 */
export interface FlowGramApi {
  /** 整树加载（切换定义/保存后重载） */
  load(doc: FlowDoc): void;
  /** 在指定节点后插入业务节点 */
  addAfter(fromId: string, type: string): void;
  /** 删除节点 */
  removeNode(id: string): void;
  /** 更新分支块数据（条件编辑；进入 onDocChanged 镜像） */
  patchBranch(blockId: string, data: Record<string, unknown>): void;
  /** 删除分支块（条件删除；走 history 操作） */
  removeBranch(blockId: string): void;
}

export interface FlowGramDesignerProps {
  doc: FlowDoc;
  readonly: boolean;
  /** id → 卡片名称（data.name 由外部权威维护） */
  labels: Record<string, string>;
  cards?: Record<string, WfNodeCardInfo>;
  /** splitId → 分支摘要（按块物理顺序：条件1…条件N、其他情况） */
  branchInfos?: Record<string, WfBranchInfo[]>;
  /** 当前选中（含条件分支块 id） */
  selectedId?: string;
  /** 只读模式节点状态：nodeId → 已完成/当前/未处理（卡片标题栏右侧显示状态标签，替代编辑操作） */
  nodeStates?: Record<string, WfNodeState>;
  onSelectNode(id: string): void;
  onDocChanged(doc: FlowDoc): void;
  onInsertAfter?(fromId: string, type: string): void;
}

const TYPE_BADGE: Record<string, string> = {
  start: '开始',
  'oa.start': '开始',
  end: '结束',
  'oa.end': '结束',
  'oa.approve': '审批',
  'oa.handle': '办理',
  'oa.cc': '知会',
  'oa.xor': '条件',
  'oa.parallel': '并行',
};

/** 只读模式节点状态（实例流程画布） */
export type WfNodeState = 'done' | 'current' | 'waiting';

/** 状态标签文案与配色 */
const STATE_LABEL: Record<string, string> = { done: '已完成', current: '当前', waiting: '未处理' };
const STATE_STYLE: Record<string, { background: string; color: string }> = {
  done: { background: '#e8ffea', color: '#00b42a' },
  current: { background: '#fff7e8', color: '#ff7d00' },
  waiting: { background: '#f2f3f5', color: '#86909c' },
};

/** 并行分支入口开关：暂不开放（代码与引擎保留，存量并行节点仍可打开/运行；恢复时改 true） */
const ENABLE_PARALLEL_BRANCH = false;

/** 连线菜单。后一组是分支，前面用分隔条隔开。icon 名与 iconComponents 登记一致 */
const INSERT_GROUPS: { type: string; label: string; icon: string }[][] = [
  [
    { type: 'oa.approve', label: '审批人', icon: 'audit' },
    { type: 'oa.handle', label: '办理人', icon: 'checklist' },
    { type: 'oa.cc', label: '抄送人', icon: 'send' },
  ],
  [
    { type: 'oa.xor', label: '条件分支', icon: 'branch' },
    ...(ENABLE_PARALLEL_BRANCH
      ? [{ type: 'oa.parallel', label: '并行分支', icon: 'parallel-gateway' }]
      : []),
  ],
];

/** 画布是 React，不能挂 Vue 的 icon-park。这里只画已在 iconComponents 登记的那几个图标 */
function ParkIcon(props: { name: string; size?: number }) {
  const s = props.size ?? 14;
  const common = {
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: 4,
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
  };
  const body = { ...common, fill: 'currentColor', fillOpacity: 0.15 };
  let children: ReactNode = null;
  if (props.name === 'audit') {
    children = (
      <>
        <path
          d="M8 36L8.00461 28.0426C8.00551 27.4906 8.45313 27.0432 9.00519 27.0426C12.3391 27.0426 15.6731 27.0426 19.0071 27.0426C19.9286 27.0426 19.9237 26.2252 19.9237 24.2792C19.9237 22.3332 15.0221 20.6941 15.0221 13.8528C15.0221 7.01151 20.0999 5 24.32 5C28.5401 5 33.1366 7.01151 33.1366 13.8528C33.1366 20.6941 28.2607 21.7818 28.2607 24.2792C28.2607 26.7765 28.2607 27.0426 29.0413 27.0426C32.3609 27.0426 35.6806 27.0426 39.0003 27.0426C39.5525 27.0426 40.0003 27.4904 40.0003 28.0426V36H8Z"
          {...body}
        />
        <path d="M8 42H40" {...common} />
      </>
    );
  } else if (props.name === 'checklist') {
    children = (
      <>
        <path d="M34 10L42 18" {...common} />
        <path d="M42 10L34 18" {...common} />
        <path d="M44 30L37 38L33 34" {...common} />
        <path d="M26 10H4V18H26V10Z" {...body} />
        <path d="M26 30H4V38H26V30Z" {...body} />
      </>
    );
  } else if (props.name === 'send') {
    children = (
      <>
        <path d="M43 5L29.7 43L22.1 25.9L5 18.3L43 5Z" {...common} />
        <path d="M43.0001 5L22.1001 25.9" {...common} />
      </>
    );
  } else if (props.name === 'branch') {
    children = (
      <>
        <path d="M40 28C42.2091 28 44 26.2091 44 24C44 21.7909 42.2091 20 40 20C37.7909 20 36 21.7909 36 24C36 26.2091 37.7909 28 40 28Z" {...body} />
        <path d="M9 12C11.2091 12 13 10.2091 13 8C13 5.79086 11.2091 4 9 4C6.79086 4 5 5.79086 5 8C5 10.2091 6.79086 12 9 12Z" {...body} />
        <path d="M9 44C11.2091 44 13 42.2091 13 40C13 37.7909 11.2091 36 9 36C6.79086 36 5 37.7909 5 40C5 42.2091 6.79086 44 9 44Z" {...body} />
        <path d="M9 12V36V24.0083H36" {...common} />
      </>
    );
  } else if (props.name === 'parallel-gateway') {
    children = (
      <>
        <path
          d="M22.7992 4.20102L4.4144 22.5858C3.63336 23.3668 3.63335 24.6332 4.4144 25.4142L22.7992 43.799C23.5802 44.58 24.8466 44.58 25.6276 43.799L44.0124 25.4142C44.7934 24.6332 44.7934 23.3668 44.0124 22.5858L25.6276 4.20102C24.8466 3.41997 23.5802 3.41997 22.7992 4.20102Z"
          {...body}
        />
        <path d="M24.043 15.5342V32.4778" {...common} />
        <path d="M15.5713 24.0059H32.5149" {...common} />
      </>
    );
  } else if (props.name === 'delete') {
    children = (
      <>
        <path d="M9 10V44H39V10H9Z" {...body} />
        <path d="M20 20V33" {...common} />
        <path d="M28 20V33" {...common} />
        <path d="M4 10H44" {...common} />
        <path d="M16 10L19.289 4H28.7771L32 10H16Z" {...body} />
      </>
    );
  }
  return (
    <svg width={s} height={s} viewBox="0 0 48 48" aria-hidden style={{ flex: 'none', display: 'block' }}>
      {children}
    </svg>
  );
}

const canvasDataRef: {
  current: {
    cards?: Record<string, WfNodeCardInfo>;
    branchInfos?: Record<string, WfBranchInfo[]>;
    selectedId?: string;
    nodeStates?: Record<string, WfNodeState>;
  };
} = { current: {} };
const canvasCardSubs = new Set<() => void>();

function publishCanvasData(data: {
  cards?: Record<string, WfNodeCardInfo>;
  branchInfos?: Record<string, WfBranchInfo[]>;
  selectedId?: string;
  nodeStates?: Record<string, WfNodeState>;
}) {
  canvasDataRef.current = data;
  canvasCardSubs.forEach((fn) => fn());
}

/** 订阅画布数据（cards/branchInfos/selectedId）：props 变化时驱动卡片重渲染 */
function useCanvasData() {
  const [, setTick] = useState(0);
  useEffect(() => {
    const fn = () => setTick((n) => n + 1);
    canvasCardSubs.add(fn);
    return () => {
      canvasCardSubs.delete(fn);
    };
  }, []);
  return canvasDataRef.current;
}

function useCanvasCard(id: string): WfNodeCardInfo | undefined {
  return useCanvasData().cards?.[id];
}
const lineInsertRef: { current?: (fromId: string, type: string) => void } = {};
/** 分支块数据补丁（条件编辑）：onDocChanged 镜像时并入块 data，实现 Vue 与画布一致 */
const branchPatchRef: { current: Record<string, Record<string, unknown>> } = { current: {} };
/** 画布上「添加条件」入口：由 FlowGramDesigner 注入（readonly 时置空） */
const branchAddRef: { current?: (split: FlowNodeEntity) => void } = {};
/** 分支卡片垃圾桶：删除指定条件块（由 FlowGramDesigner 注入，readonly 时置空） */
const branchRemoveRef: { current?: (blockId: string) => void } = {};

/** 替换连线上默认「+」弹出的通用节点菜单，走现有插入动作。key 与 FlowRendererKey.ADDER（'adder'）一致 */
function WfLineAdder(props: { from?: { id?: string; flowNodeType?: { type?: string } }; hoverActivated?: boolean }) {
  const [open, setOpen] = useState(false);
  const [box, setBox] = useState<{ top: number; left: number } | null>(null);
  const anchorRef = useRef<HTMLSpanElement>(null);
  const fromId = String(props.from?.id ?? '');
  const fromType = String(props.from?.flowNodeType?.type ?? '');
  const hidden = !fromId || !lineInsertRef.current || fromType === 'end' || fromType === 'oa.end';

  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      const t = e.target as HTMLElement | null;
      if (anchorRef.current?.contains(t)) return;
      if (t?.closest('[data-wf-line-menu]')) return;
      setOpen(false);
    };
    document.addEventListener('mousedown', onDown, true);
    return () => document.removeEventListener('mousedown', onDown, true);
  }, [open]);

  if (hidden) return null;

  const place = () => {
    const r = anchorRef.current?.getBoundingClientRect();
    if (!r) return;
    const menuW = 168;
    const itemCount = INSERT_GROUPS.reduce((n, g) => n + g.length, 0);
    const menuH = itemCount * 32 + (INSERT_GROUPS.length - 1) * 9 + 8;
    let left = r.right + 8;
    if (left + menuW > window.innerWidth - 8) left = Math.max(8, r.left - menuW - 8);
    let top = r.top + r.height / 2 - menuH / 2;
    top = Math.max(8, Math.min(top, window.innerHeight - menuH - 8));
    setBox({ top, left });
    setOpen(true);
  };

  return (
    <div
      style={{ position: 'relative', width: 18, height: 18 }}
      onMouseDown={(e) => e.stopPropagation()}
      onClick={(e) => e.stopPropagation()}
    >
      <span
        ref={anchorRef}
        role="button"
        title="在后方插入节点"
        onClick={(e) => {
          e.stopPropagation();
          if (open) setOpen(false);
          else place();
        }}
        style={{
          display: 'block',
          width: 18,
          height: 18,
          borderRadius: '50%',
          background: '#165dff',
          color: '#fff',
          fontSize: 14,
          lineHeight: '16px',
          textAlign: 'center',
          cursor: 'pointer',
        }}
      >
        +
      </span>
      {open &&
        box &&
        createPortal(
          <div
            data-wf-line-menu
            style={{
              position: 'fixed',
              top: box.top,
              left: box.left,
              background: 'var(--color-bg-popup)',
              border: '1px solid var(--color-border-2)',
              borderRadius: 6,
              padding: 2,
              display: 'flex',
              flexDirection: 'column',
              minWidth: 160,
              zIndex: 4000,
              boxShadow: '0 8px 24px rgba(0,0,0,0.12)',
            }}
          >
            {INSERT_GROUPS.map((group, gi) => (
              <div key={group[0]?.type ?? gi}>
                {gi > 0 && <div style={{ height: 1, margin: '4px 8px', background: 'var(--color-border-2)' }} />}
                {group.map((act) => (
                  <button
                    key={act.type}
                    type="button"
                    onMouseDown={(e) => e.stopPropagation()}
                    onClick={(e) => {
                      e.stopPropagation();
                      setOpen(false);
                      lineInsertRef.current?.(fromId, act.type);
                    }}
                    style={{
                      border: 0,
                      background: 'transparent',
                      textAlign: 'left',
                      padding: '6px 10px',
                      fontSize: 12,
                      cursor: 'pointer',
                      whiteSpace: 'nowrap',
                      display: 'flex',
                      alignItems: 'center',
                      gap: 8,
                      width: '100%',
                      color: 'var(--color-text-1)',
                      borderRadius: 4,
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.background = 'var(--color-fill-2)';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.background = 'transparent';
                    }}
                  >
                    <ParkIcon name={act.icon} />
                    {act.label}
                  </button>
                ))}
              </div>
            ))}
          </div>,
          document.body,
        )}
    </div>
  );
}

const TYPE_COLOR: Record<string, string> = {
  start: '#0fc6c2',
  'oa.start': '#0fc6c2',
  end: '#f53f3f',
  'oa.end': '#f53f3f',
  'oa.approve': '#165dff',
  'oa.handle': '#00b42a',
  'oa.cc': '#722ed1',
  'oa.xor': '#fa8c16',
  'oa.parallel': '#3491fa',
};

/** 替换内置 branch-adder：只在分叉容器处显示，点击在「其他情况」前增加一个条件分支 */
function WfBranchAdder(props: { activated?: boolean; node?: FlowNodeEntity; transform?: { entity?: FlowNodeEntity } }) {
  const split = props.node;
  const container = props.transform?.entity;
  const isContainer = !!split && !!container && String(container.flowNodeType) === 'inlineBlocks';
  if (!split || !isContainer || !branchAddRef.current) return null;
  return (
    <button
      type="button"
      title="添加条件分支"
      onMouseDown={(e) => e.stopPropagation()}
      onClick={(e) => {
        e.stopPropagation();
        branchAddRef.current?.(split);
      }}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        height: 20,
        padding: '0 8px',
        borderRadius: 10,
        border: '1px solid #00b42a66',
        background: props.activated ? '#e8ffea' : '#f7f8fa',
        color: '#00b42a',
        fontSize: 11,
        lineHeight: '18px',
        cursor: 'pointer',
        whiteSpace: 'nowrap',
        fontFamily: 'sans-serif',
      }}
    >
      ＋ 添加条件
    </button>
  );
}

/** 分支条件卡片（每个条件/其他情况一块，点击右侧抽屉编辑该条件） */
function BranchCard(props: {
  blockId: string;
  index: number;
  total: number;
  info?: WfBranchInfo;
  selected: boolean;
  readonly: boolean;
  onSelect(id: string): void;
}) {
  const { blockId, index, total, info, selected, readonly, onSelect } = props;
  const isElse = index >= total - 1;
  const label = isElse ? '其他情况' : info?.title || `条件${index + 1}`;
  const color = isElse ? '#f53f3f' : '#00b42a';
  const summary = info?.subtitle || (isElse ? '都不命中时走这里' : '未设置条件');
  const warn = !!info?.warning;
  return (
    <div
      onClick={(e) => {
        e.stopPropagation();
        onSelect(blockId);
      }}
      onMouseDown={(e) => e.stopPropagation()}
      style={{
        width: 200,
        minHeight: 54,
        boxSizing: 'border-box',
        background: 'var(--color-bg-2)',
        border: `1.5px ${isElse ? 'dashed' : 'solid'} ${selected ? color : warn ? '#ff7d00' : 'var(--color-border-2)'}`,
        borderRadius: 6,
        overflow: 'hidden',
        cursor: 'pointer',
        boxShadow: selected ? `0 0 0 2px ${color}33` : '0 1px 3px rgba(0,0,0,0.04)',
        fontFamily: 'sans-serif',
      }}
      data-wf-node
    >
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 6,
          height: 24,
          flex: 'none',
          padding: '0 8px',
          background: `${color}14`,
          borderBottom: '1px solid var(--color-border-1)',
        }}
      >
        <span
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            minWidth: 16,
            height: 16,
            padding: '0 4px',
            borderRadius: 8,
            background: 'var(--color-bg-2)',
            color,
            fontSize: 10,
            fontWeight: 600,
          }}
        >
          {isElse ? '默认' : '条件'}
        </span>
        <span style={{ fontSize: 12, fontWeight: 600, color: 'var(--color-text-1)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
          {label}
        </span>
        {!isElse && !readonly && (
          <span
            role="button"
            title="删除该条件"
            onClick={(e) => {
              e.stopPropagation();
              branchRemoveRef.current?.(blockId);
            }}
            style={{
              marginLeft: 'auto',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: 18,
              height: 18,
              flex: 'none',
              color: 'var(--color-text-3)',
              cursor: 'pointer',
            }}
          >
            <ParkIcon name="delete" size={14} />
          </span>
        )}
      </div>
      <div
        style={{
          padding: '5px 8px',
          fontSize: 12,
          lineHeight: '18px',
          color: warn ? '#ff7d00' : 'var(--color-text-2)',
          overflow: 'hidden',
          whiteSpace: 'nowrap',
          textOverflow: 'ellipsis',
        }}
      >
        {summary}
      </div>
    </div>
  );
}

/** 节点卡片（全部业务节点经 renderDefaultNode 使用） */
function WorkflowNodeCard(props: {
  node: FlowNodeEntity;
  cards?: Record<string, WfNodeCardInfo>;
  readonly: boolean;
  onSelect(id: string): void;
}) {
  const { node, cards, readonly, onSelect } = props;
  const { type, activated, isBlockIcon, isBlockOrderIcon, deleteNode } = useNodeRender();
  const canvasData = useCanvasData();
  if (isBlockOrderIcon) {
    const block = node.parent;
    const split = node.originParent;
    const blockId = String(block?.id ?? '');
    // 物理块序号（分行容器 children 中的位置）：与后端/镜像的 cases 顺序一致
    const branches = ((block?.parent?.children ?? []) as FlowNodeEntity[]).filter(
      (c) => String(c.flowNodeType) === 'block',
    );
    const index = block ? branches.indexOf(block) : -1;
    const total = branches.length || 1;
    const info = split ? canvasData.branchInfos?.[String(split.id)]?.[Math.max(0, index)] : undefined;
    const selected = !!canvasData.selectedId && canvasData.selectedId === blockId;
    return (
      <BranchCard
        blockId={blockId}
        index={Math.max(0, index)}
        total={total}
        info={info}
        selected={selected}
        readonly={readonly}
        onSelect={onSelect}
      />
    );
  }
  if (isBlockIcon) {
    // 分流节点图标（条件信息由各分支卡片呈现，不再在此显示误导性的「条件1」）
    return null;
  }

  const isEnd = String(type) === 'end' || String(type) === 'oa.end';
  const isStart = String(type) === 'start' || String(type) === 'oa.start';
  const color = TYPE_COLOR[String(type)] ?? '#86909c';
  const info = useCanvasCard(String(node.id)) ?? cards?.[String(node.id)];
  const nodeState = canvasData.nodeStates?.[String(node.id)];
  const badge = TYPE_BADGE[String(type)] ?? String(type);
  const modeLabel = info?.modeLabel || '';
  const people =
    String(type) === 'oa.approve' || String(type) === 'oa.handle' || String(type) === 'oa.cc'
      ? info?.assignee || ''
      : '';
  const branch =
    String(type) === 'oa.xor' || String(type) === 'oa.parallel' ? info?.branchLine || info?.subtitle || '' : '';
  const warn = !!info?.warning;
  const border = activated ? color : warn ? '#ff7d00' : 'var(--color-border-2)';

  return (
    <div
      onClick={(e) => {
        e.stopPropagation();
        onSelect(String(node.id));
      }}
      onMouseDown={(e) => e.stopPropagation()}
      style={{
        width: 200,
        height: 64,
        boxSizing: 'border-box',
        display: 'flex',
        flexDirection: 'column',
        background: 'var(--color-bg-2)',
        border: `1.5px solid ${border}`,
        borderRadius: 6,
        overflow: 'hidden',
        boxShadow: activated ? `0 0 0 2px ${color}33` : '0 1px 3px rgba(0,0,0,0.04)',
        cursor: 'pointer',
        position: 'relative',
        fontFamily: 'sans-serif',
      }}
      data-wf-node
    >
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 6,
          height: 28,
          flex: 'none',
          padding: '0 8px',
          background: `${color}14`,
          borderBottom: '1px solid var(--color-border-1)',
        }}
      >
        <span
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            minWidth: 16,
            height: 16,
            padding: '0 4px',
            borderRadius: 8,
            background: 'var(--color-bg-2)',
            color,
            fontSize: 10,
            fontWeight: 600,
          }}
        >
          {badge}
        </span>
        {!!modeLabel && <span style={{ fontSize: 12, color: 'var(--color-text-1)' }}>{modeLabel}</span>}
        {readonly && nodeState && (
          <span
            style={{
              marginLeft: 'auto',
              display: 'inline-flex',
              alignItems: 'center',
              height: 16,
              padding: '0 6px',
              borderRadius: 8,
              fontSize: 10,
              fontWeight: 600,
              flex: 'none',
              background: STATE_STYLE[nodeState]?.background,
              color: STATE_STYLE[nodeState]?.color,
            }}
          >
            {STATE_LABEL[nodeState]}
          </span>
        )}
        {!readonly && !isEnd && !isStart && (
          <span
            role="button"
            title="删除节点"
            onClick={(e) => {
              e.stopPropagation();
              deleteNode();
            }}
            style={{
              marginLeft: 'auto',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: 18,
              height: 18,
              color: 'var(--color-text-3)',
              cursor: 'pointer',
            }}
          >
            <ParkIcon name="delete" size={14} />
          </span>
        )}
      </div>
      <div
        style={{
          flex: 1,
          minHeight: 0,
          padding: '6px 8px',
          fontSize: 12,
          lineHeight: '18px',
          color: warn ? '#ff7d00' : 'var(--color-text-2)',
          overflow: 'hidden',
          whiteSpace: 'nowrap',
          textOverflow: 'ellipsis',
        }}
      >
        {people || branch}
      </div>
    </div>
  );
}

/** 注册业务节点（start/end 由 FlowGram 内置类型承担） */
function buildRegistries(): FlowNodeRegistry[] {
  const make = (type: string): FlowNodeRegistry => ({
    type,
    meta: { draggable: false, deleteDisable: false },
    onAdd: () => ({ id: `n${Date.now()}`, type, data: defaultNodeDataFor(type) }),
  });
  const split = (type: 'oa.xor' | 'oa.parallel'): FlowNodeRegistry => ({
    type,
    extend: 'dynamicSplit',
    meta: { draggable: false, deleteDisable: false },
    onAdd: () => {
      const id = `n${Date.now()}`;
      return {
        id,
        type,
        data: defaultNodeDataFor(type),
        blocks: xorSplitBlocks(id),
      };
    },
  });
  // 覆盖内置分流图标占位：默认占位 250x84 且保留间距，会在分叉节点上方留出大片空白
  const iconShim: FlowNodeRegistry = {
    type: 'blockIcon',
    meta: { size: { width: 0, height: 0 }, spacing: 0 },
  };
  return [iconShim, make('oa.approve'), make('oa.handle'), make('oa.cc'), split('oa.xor'), split('oa.parallel')];
}

/** 文档业务节点导出（过滤内部虚拟 icon/block 节点） */
function businessDocOf(ctx: { document: { toJSON(): unknown } }): FlowDoc {
  const json = ctx.document.toJSON() as { nodes?: FlowDocNodeRaw[] };
  const nodes = applyBranchPatches(keepCanvasNodes(json?.nodes ?? []), branchPatchRef.current);
  return { version: 1, nodes };
}

/** 分支块数据补丁并入（条件编辑先在 Vue 侧生效，下一次结构镜像时合并） */
function applyBranchPatches(
  nodes: FlowDoc['nodes'],
  patches: Record<string, Record<string, unknown>>,
): FlowDoc['nodes'] {
  if (!Object.keys(patches).length) return nodes;
  return nodes.map((n) => {
    const blocks = n.blocks ? applyBranchPatches(n.blocks, patches) : undefined;
    const p = patches[n.id];
    return {
      ...n,
      data: p ? { ...(n.data ?? {}), ...p } : n.data,
      ...(blocks ? { blocks } : {}),
    };
  });
}

type FlowDocNodeRaw = {
  id: string;
  type?: string | number;
  data?: Record<string, unknown>;
  blocks?: FlowDocNodeRaw[];
};

function keepCanvasNodes(nodes: FlowDocNodeRaw[]): FlowDoc['nodes'] {
  const out: FlowDoc['nodes'] = [];
  for (const n of nodes) {
    const type = String(n.type ?? '');
    if (!type || type.startsWith('$')) continue;
    const blocks = n.blocks?.length ? keepCanvasNodes(n.blocks) : undefined;
    if (type === 'block' || isBusinessNodeType(type)) {
      out.push({ id: n.id, type, data: n.data ?? {}, ...(blocks ? { blocks } : {}) });
    } else if (blocks?.length) {
      out.push(...blocks);
    }
  }
  return out;
}

export const FlowGramDesigner = forwardRef<FlowGramApi, FlowGramDesignerProps>(
  function FlowGramDesigner(props, ref) {
    const { doc, readonly, onDocChanged } = props;
    const editorRef = useRef<unknown>(null);
    const propsRef = useRef(props);
    propsRef.current = props;
    const disposed = useRef(false);

    // 首个数据渲染一次（后续切换定义走 api.load）
    const initialDoc = useMemo(() => ({ ...doc }), []);
    const nodeRegistries = useMemo(() => buildRegistries(), []);

    useEffect(
      () => () => {
        disposed.current = true;
      },
      [],
    );

    lineInsertRef.current = readonly ? undefined : props.onInsertAfter;

    /** 画布上「＋ 添加条件」：在「其他情况」前插入条件分支块（id 按 __caseN 递增） */
    branchAddRef.current = readonly
      ? undefined
      : (split: FlowNodeEntity) => {
          const ctx = editorRef.current as {
            document?: {
              getNode(id: string): FlowNodeEntity | undefined;
              moveChildNodes?(o: { toParentId: string; toIndex: number; nodeIds: string[] }): unknown;
            };
            operation?: {
              addBlock?(target: FlowNodeEntity, json: unknown, config?: { index?: number }): FlowNodeEntity | undefined;
            };
          } | null;
          // 分支块挂在 $inlineBlocks$<splitId> 容器下（split.blocks 是 icon/容器本身）
          const containerId = `$inlineBlocks$${split.id}`;
          const container = ctx?.document?.getNode(containerId);
          const branches = ((container?.children ?? []) as FlowNodeEntity[]).filter(
            (c) => String(c.flowNodeType) === 'block',
          );
          const id = nextCaseBlockId(String(split.id), branches.map((b) => String(b.id)));
          // FlowGram 的 addBlock 插入位不可靠且返回值非节点：追加后按 id 反查，再挪到「其他情况」之前
          ctx?.operation?.addBlock?.(split, { id, type: 'block' }, {});
          const created = ctx?.document?.getNode(id);
          if (!created) return;
          const list = ((container?.children ?? []) as FlowNodeEntity[]).filter(
            (c) => String(c.flowNodeType) === 'block',
          );
          const ordered = list.filter((b) => String(b.id) !== String(created.id));
          const elseIdx = ordered.findIndex((b) => String(b.id).endsWith('__else'));
          const toIndex = elseIdx >= 0 ? elseIdx : ordered.length;
          ctx?.document?.moveChildNodes?.({ toParentId: containerId, toIndex, nodeIds: [String(created.id)] });
          // 手动移动不在 history 里，主动镜像一次保证 Vue 侧 cases 顺序一致
          if (ctx) propsRef.current.onDocChanged(businessDocOf(ctx as never));
        };

    /** 分支卡片垃圾桶：删除该条件块（其余分支不动；Vue 侧随后按镜像结果重排条件序号） */
    branchRemoveRef.current = readonly
      ? undefined
      : (blockId: string) => {
          const ctx = editorRef.current as {
            document?: { getNode(id: string): FlowNodeEntity | undefined };
            operation?: { deleteNode(node: FlowNodeEntity): unknown };
          } | null;
          const node = ctx?.document?.getNode(blockId);
          if (!node || !ctx?.operation) return;
          delete branchPatchRef.current[blockId];
          ctx.operation.deleteNode(node);
        };

    useEffect(() => {
      publishCanvasData({
        cards: props.cards,
        branchInfos: props.branchInfos,
        selectedId: props.selectedId,
        nodeStates: props.nodeStates,
      });
    }, [props.cards, props.branchInfos, props.selectedId, props.nodeStates]);

    const editorProps = useMemo(
      () => ({
        readonly,
        background: true,
        nodeRegistries,
        initialData: initialDoc,
        // 启用历史（撤销/重做 + onApply 变更镜像）
        history: { enable: true },
        materials: {
          // 必须合并官方默认材料：内部渲染（drag-node 拖拽占位、Adder 等）依赖这些 key，
          // 缺失会抛 Unknown render key 导致 PlaygroundReactRenderer 整树卸载（画布空白）
          components: { ...defaultFixedSemiMaterials, adder: WfLineAdder, 'branch-adder': WfBranchAdder },
          renderDefaultNode: (n: { node: FlowNodeEntity }) => (
            <WorkflowNodeCard
              node={n.node}
              cards={propsRef.current.cards}
              readonly={propsRef.current.readonly}
              onSelect={propsRef.current.onSelectNode}
            />
          ),
        },
        onAllLayersRendered: (ctx: unknown) => {
          setTimeout(() => {
            const c = ctx as { playground?: { config?: { fitView?(b: unknown, e?: boolean, p?: number): Promise<void> } }; document?: { root?: { bounds?: unknown } } };
            c.playground?.config?.fitView?.(c.document?.root?.bounds, false, 30);
          }, 10);
        },
      }),
      [readonly, nodeRegistries, initialDoc],
    );

    useImperativeHandle(ref, () => ({
      load(nextDoc: FlowDoc) {
        branchPatchRef.current = {};
        const ctx = editorRef.current as { document?: { fromJSON(d: unknown): void } } | null;
        ctx?.document?.fromJSON(nextDoc);
      },
      addAfter(fromId: string, type: string) {
        const ctx = editorRef.current as {
          document?: { getNode(id: string): FlowNodeEntity | undefined; getAllNodes(): FlowNodeEntity[] };
          operation?: { addFromNode(node: FlowNodeEntity, json: unknown): unknown };
        } | null;
        if (!ctx?.document || !ctx.operation) return;
        const fromNode = ctx.document.getNode(fromId);
        if (!fromNode) return;
        const used = new Set(ctx.document.getAllNodes().map((n) => n.id));
        let i = 1;
        while (used.has(`n${i}`)) i++;
        const id = `n${i}`;
        const json: { id: string; type: string; data: Record<string, unknown>; blocks?: ReturnType<typeof xorSplitBlocks> } = {
          id,
          type,
          data: defaultNodeDataFor(type),
        };
        if (type === 'oa.xor' || type === 'oa.parallel') json.blocks = xorSplitBlocks(id);
        ctx.operation.addFromNode(fromNode, json);
      },
      removeNode(id: string) {
        const ctx = editorRef.current as {
          document?: { getNode(id: string): FlowNodeEntity | undefined };
          operation?: { deleteNode(node: FlowNodeEntity): unknown };
        } | null;
        if (!ctx?.document || !ctx.operation) return;
        const node = ctx.document.getNode(id);
        if (!node) return;
        // 开始/结束节点不允许删除（卡片按钮已隐藏，命令式入口也拦截）
        const t = String((node as unknown as { flowNodeType?: string; type?: string }).flowNodeType ?? '');
        if (t === 'start' || t === 'oa.start' || t === 'end' || t === 'oa.end' || id === 'start' || id === 'end') return;
        ctx.operation.deleteNode(node);
      },
      patchBranch(blockId: string, data: Record<string, unknown>) {
        branchPatchRef.current[blockId] = { ...(branchPatchRef.current[blockId] ?? {}), ...data };
        const ctx = editorRef.current as { document?: { toJSON(): unknown } } | null;
        if (ctx) onDocChanged(businessDocOf(ctx as never));
      },
      removeBranch(blockId: string) {
        const ctx = editorRef.current as {
          document?: { getNode(id: string): FlowNodeEntity | undefined };
          operation?: { deleteNode(node: FlowNodeEntity): unknown };
        } | null;
        if (!ctx?.document || !ctx.operation) return;
        const node = ctx.document.getNode(blockId);
        if (!node) return;
        delete branchPatchRef.current[blockId];
        ctx.operation.deleteNode(node);
      },
    }));

    return (
      <div
        className="wf-flowcanvas"
        style={{ width: '100%', height: '100%' }}
        onClick={(e) => {
          const t = e.target as HTMLElement | null;
          if (t?.closest('[data-wf-node], [data-wf-line-menu], [title="在后方插入节点"]')) return;
          props.onSelectNode('');
        }}
      >
        <FixedLayoutEditor
          ref={(c) => {
            editorRef.current = c;
            if (c && !disposed.current) {
              const ctx = c as {
                history?: { onApply(fn: () => void): { dispose(): void } };
                document?: { toJSON(): unknown };
              };
              // 画布结构/数据变更（添加/删除/拖拽/表单）后把业务序回传 Vue
              ctx.history?.onApply(() => {
                if (!disposed.current) onDocChanged(businessDocOf(ctx as never));
              });
            }
          }}
          {...editorProps}
        />
      </div>
    );
  },
);
