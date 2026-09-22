/**
 * FlowGram.AI 固定布局画布封装（OSC-26090347f1 T8e-FlowGram）。
 * React 组件（FlowGram 为 React 库），经 Vue react-dom 桥接挂载。
 *
 * - 结构编辑（在选中节点后插入/删除/拖拽重排）由 FlowGram 承担
 * - 节点卡片只读展示 type 徽标 + 名称（名称来自外部 labels，避免依赖 data 存储语义）
 * - 每次结构/数据变化经 history onApply → onDocChanged(业务序 doc)，由 Vue 镜像重建 GraphJson
 * - 保存权威在后端 GraphJson；本组件不执行流程
 */
import { forwardRef, useEffect, useImperativeHandle, useMemo, useRef, useState } from 'react';

import '@flowgram.ai/fixed-layout-editor/index.css';

import {
  FixedLayoutEditor,
  FlowNodeEntity,
  FlowNodeRegistry,
  useNodeRender,
} from '@flowgram.ai/fixed-layout-editor';
import { defaultFixedSemiMaterials } from '@flowgram.ai/fixed-semi-materials';

import type { FlowDoc } from './flowgramGraph';
import { defaultNodeDataFor, isBusinessNodeType, xorSplitBlocks } from './flowgramGraph';
import type { WfNodeCardInfo } from '../wfNodeCard';

/** 桥接 API：Vue 侧可调用 */
export interface FlowGramApi {
  /** 整树加载（切换定义/保存后重载） */
  load(doc: FlowDoc): void;
  /** 在指定节点后插入业务节点 */
  addAfter(fromId: string, type: string): void;
  /** 删除节点 */
  removeNode(id: string): void;
}

export interface FlowGramDesignerProps {
  doc: FlowDoc;
  readonly: boolean;
  /** id → 卡片名称（data.name 由外部权威维护） */
  labels: Record<string, string>;
  cards?: Record<string, WfNodeCardInfo>;
  onSelectNode(id: string): void;
  onDocChanged(doc: FlowDoc): void;
  onInsertAfter?(fromId: string, type: string): void;
}

function flowTitleOf(type: string, labels: Record<string, string>, id: string): string {
  const base =
    type === 'start' || type === 'oa.start'
      ? '开始'
      : type === 'end' || type === 'oa.end'
        ? '结束'
        : type === 'oa.approve'
          ? '审批'
          : type === 'oa.cc'
            ? '知会'
            : type === 'oa.xor'
              ? '条件分流'
              : type;
  const name = labels[id];
  return name ? `${base} · ${name}` : base;
}

const TYPE_COLOR: Record<string, string> = {
  start: '#0fc6c2',
  'oa.start': '#0fc6c2',
  end: '#f53f3f',
  'oa.end': '#f53f3f',
  'oa.approve': '#165dff',
  'oa.cc': '#722ed1',
  'oa.xor': '#fa8c16',
};

/** 节点卡片（全部业务节点经 renderDefaultNode 使用） */
function WorkflowNodeCard(props: {
  node: FlowNodeEntity;
  labels: Record<string, string>;
  cards?: Record<string, WfNodeCardInfo>;
  readonly: boolean;
  onSelect(id: string): void;
  onInsertAfter?: (fromId: string, type: string) => void;
}) {
  const { node, labels, cards, readonly, onSelect, onInsertAfter } = props;
  const { type, activated, isBlockIcon, isBlockOrderIcon, deleteNode } = useNodeRender();
  const [menu, setMenu] = useState(false);
  if (isBlockOrderIcon) return null;
  if (isBlockIcon) {
    const id = String(node.id);
    const elseBranch = id.endsWith('__else');
    const color = elseBranch ? '#f53f3f' : '#00b42a';
    return (
      <div
        style={{
          fontSize: 12,
          fontWeight: 600,
          color,
          lineHeight: '18px',
          padding: '0 2px',
          whiteSpace: 'nowrap',
          fontFamily: 'sans-serif',
        }}
      >
        {elseBranch ? '不满足' : '满足'}
      </div>
    );
  }

  const isXor = String(type) === 'oa.xor';
  const isEnd = String(type) === 'end' || String(type) === 'oa.end';
  const color = TYPE_COLOR[String(type)] ?? '#86909c';
  const info = cards?.[String(node.id)];
  const title = info?.title || flowTitleOf(String(type), labels, String(node.id));
  const warn = !!info?.warning;
  const border = activated ? color : warn ? '#ff7d00' : 'rgba(6,7,9,0.15)';

  return (
    <div
      onClick={(e) => {
        e.stopPropagation();
        onSelect(String(node.id));
      }}
      onMouseDown={(e) => e.stopPropagation()}
      style={{
        width: 200,
        minHeight: 44,
        boxSizing: 'border-box',
        display: 'flex',
        flexDirection: 'column',
        gap: 2,
        padding: '6px 10px 14px',
        background: '#fff',
        border: `1.5px solid ${border}`,
        borderRadius: 6,
        boxShadow: activated ? `0 0 0 2px ${color}33` : '0 1px 3px rgba(0,0,0,0.04)',
        cursor: 'pointer',
        position: 'relative',
        fontFamily: 'sans-serif',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
        <span
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            minWidth: 16,
            height: 16,
            padding: '0 4px',
            borderRadius: 8,
            background: `${color}1f`,
            color,
            fontSize: 10,
            fontWeight: 600,
          }}
        >
          {flowTitleOf(String(type), labels, String(node.id)).split(' · ')[0]}
        </span>
        <span style={{ flex: 1, fontSize: 12, color: '#1d2129', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
          {title}
        </span>
        {!readonly && !isEnd && (
          <span
            role="button"
            onClick={(e) => {
              e.stopPropagation();
              deleteNode();
            }}
            style={{
              width: 16,
              height: 16,
              borderRadius: '50%',
              background: '#f2f3f5',
              color: '#86909c',
              fontSize: 11,
              lineHeight: '16px',
              textAlign: 'center',
              cursor: 'pointer',
            }}
          >
            ×
          </span>
        )}
      </div>
      {!!info?.badges?.length && (
        <div style={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
          {info.badges.map((b) => (
            <span
              key={b}
              style={{
                fontSize: 10,
                color,
                background: `${color}14`,
                borderRadius: 3,
                padding: '0 4px',
                lineHeight: '16px',
              }}
            >
              {b}
            </span>
          ))}
        </div>
      )}
      {!!info?.subtitle && (
        <div style={{ fontSize: 10, color: warn ? '#ff7d00' : '#86909c', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
          {info.subtitle}
        </div>
      )}
      {!readonly && !isEnd && !isXor && onInsertAfter && (
        <div style={{ position: 'absolute', left: '50%', bottom: -12, transform: 'translateX(-50%)', zIndex: 3 }}>
          {menu && (
            <div
              style={{
                position: 'absolute',
                bottom: 22,
                left: '50%',
                transform: 'translateX(-50%)',
                background: '#fff',
                border: '1px solid #e5e6eb',
                borderRadius: 6,
                padding: 2,
                display: 'flex',
                flexDirection: 'column',
                minWidth: 128,
              }}
              onClick={(e) => e.stopPropagation()}
            >
              {[
                ['oa.approve', '审批（选人或签）'],
                ['oa.cc', '知会（选接收人）'],
                ['oa.xor', '条件分流'],
              ].map(([t, lab]) => (
                <button
                  key={t}
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    setMenu(false);
                    onInsertAfter(String(node.id), t);
                  }}
                  style={{
                    border: 0,
                    background: 'transparent',
                    textAlign: 'left',
                    padding: '6px 10px',
                    fontSize: 12,
                    cursor: 'pointer',
                    whiteSpace: 'nowrap',
                  }}
                >
                  {lab}
                </button>
              ))}
            </div>
          )}
          <span
            role="button"
            title="在后方插入节点"
            onClick={(e) => {
              e.stopPropagation();
              setMenu((v) => !v);
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
        </div>
      )}
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
  const xor: FlowNodeRegistry = {
    type: 'oa.xor',
    extend: 'dynamicSplit',
    meta: { draggable: false, deleteDisable: false },
    onAdd: () => {
      const id = `n${Date.now()}`;
      return {
        id,
        type: 'oa.xor',
        data: defaultNodeDataFor('oa.xor'),
        blocks: xorSplitBlocks(id),
      };
    },
  };
  return [make('oa.approve'), make('oa.cc'), xor];
}

/** 文档业务节点导出（过滤内部虚拟 icon/block 节点） */
function businessDocOf(ctx: { document: { toJSON(): unknown } }): FlowDoc {
  const json = ctx.document.toJSON() as { nodes?: FlowDocNodeRaw[] };
  return { version: 1, nodes: keepCanvasNodes(json?.nodes ?? []) };
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
          components: { ...defaultFixedSemiMaterials },
          renderDefaultNode: (n: { node: FlowNodeEntity }) => (
            <WorkflowNodeCard
              node={n.node}
              labels={propsRef.current.labels}
              cards={propsRef.current.cards}
              readonly={propsRef.current.readonly}
              onSelect={propsRef.current.onSelectNode}
              onInsertAfter={propsRef.current.onInsertAfter}
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
        if (type === 'oa.xor') json.blocks = xorSplitBlocks(id);
        ctx.operation.addFromNode(fromNode, json);
      },
      removeNode(id: string) {
        const ctx = editorRef.current as {
          document?: { getNode(id: string): FlowNodeEntity | undefined };
          operation?: { deleteNode(node: FlowNodeEntity): unknown };
        } | null;
        if (!ctx?.document || !ctx.operation) return;
        const node = ctx.document.getNode(id);
        if (node) ctx.operation.deleteNode(node);
      },
    }));

    return (
      <div className="wf-flowcanvas" style={{ width: '100%', height: '100%' }}>
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
