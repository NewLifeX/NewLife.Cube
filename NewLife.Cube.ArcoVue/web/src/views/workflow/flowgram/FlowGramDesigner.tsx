/**
 * FlowGram.AI 固定布局画布封装（OSC-26090347f1 T8e-FlowGram）。
 * React 组件（FlowGram 为 React 库），经 Vue react-dom 桥接挂载。
 *
 * - 结构编辑（在选中节点后插入/删除/拖拽重排）由 FlowGram 承担
 * - 节点卡片只读展示 type 徽标 + 名称（名称来自外部 labels，避免依赖 data 存储语义）
 * - 每次结构/数据变化经 history onApply → onDocChanged(业务序 doc)，由 Vue 镜像重建 GraphJson
 * - 保存权威在后端 GraphJson；本组件不执行流程
 */
import { forwardRef, useEffect, useImperativeHandle, useMemo, useRef } from 'react';

import '@flowgram.ai/fixed-layout-editor/index.css';

import {
  FixedLayoutEditor,
  FlowNodeEntity,
  FlowNodeRegistry,
  useNodeRender,
} from '@flowgram.ai/fixed-layout-editor';
import { defaultFixedSemiMaterials } from '@flowgram.ai/fixed-semi-materials';

import type { FlowDoc } from './flowgramGraph';
import { defaultNodeDataFor } from './flowgramGraph';

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
  onSelectNode(id: string): void;
  onDocChanged(doc: FlowDoc): void;
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
function WorkflowNodeCard(props: { node: FlowNodeEntity; labels: Record<string, string>; readonly: boolean; onSelect(id: string): void }) {
  const { node, labels, readonly, onSelect } = props;
  const { type, activated, isBlockIcon, isBlockOrderIcon, deleteNode } = useNodeRender();
  if (isBlockIcon || isBlockOrderIcon) return null;

  const isEnd = String(type) === 'end' || String(type) === 'oa.end';
  const color = TYPE_COLOR[String(type)] ?? '#86909c';

  return (
    <div
      onClick={(e) => {
        e.stopPropagation();
        onSelect(String(node.id));
      }}
      onMouseDown={(e) => e.stopPropagation()}
      style={{
        width: 260,
        minHeight: 64,
        boxSizing: 'border-box',
        display: 'flex',
        alignItems: 'center',
        gap: 10,
        padding: '10px 14px',
        background: '#fff',
        border: `1.5px solid ${activated ? color : 'rgba(6,7,9,0.15)'}`,
        borderRadius: 8,
        boxShadow: activated ? `0 0 0 2px ${color}33` : '0 1px 4px rgba(0,0,0,0.04)',
        cursor: 'pointer',
        position: 'relative',
        fontFamily: 'sans-serif',
      }}
    >
      <span
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
          minWidth: 20,
          height: 20,
          padding: '0 6px',
          borderRadius: 10,
          background: `${color}1f`,
          color,
          fontSize: 12,
          fontWeight: 600,
        }}
      >
        {flowTitleOf(String(type), labels, String(node.id)).split(' · ')[0]}
      </span>
      <span style={{ flex: 1, fontSize: 13, color: '#1d2129', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
        {flowTitleOf(String(type), labels, String(node.id))}
      </span>
      <span style={{ fontSize: 11, color: '#c9cdd4' }}>{String(node.id)}</span>
      {!readonly && !isEnd && (
        <span
          role="button"
          onClick={(e) => {
            e.stopPropagation();
            deleteNode();
          }}
          style={{
            position: 'absolute',
            top: 4,
            right: 4,
            width: 18,
            height: 18,
            borderRadius: '50%',
            background: '#f2f3f5',
            color: '#86909c',
            fontSize: 12,
            lineHeight: '18px',
            textAlign: 'center',
            cursor: 'pointer',
          }}
        >
          ×
        </span>
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
  return [make('oa.approve'), make('oa.cc'), make('oa.xor')];
}

/** 文档业务节点导出（过滤内部虚拟 icon/block 节点） */
function businessDocOf(ctx: { document: { toJSON(): unknown } }): FlowDoc {
  const json = ctx.document.toJSON() as { nodes?: Array<{ id: string; type: string | number; data?: unknown }> };
  const nodes = (json?.nodes ?? []).filter(
    (n) => typeof n.type === 'string' && /^(oa\.|start$|end$)/.test(n.type),
  );
  return { version: 1, nodes: nodes.map((n) => ({ id: n.id, type: n.type as string })) };
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
        ctx.operation.addFromNode(fromNode, { id, type, data: defaultNodeDataFor(type) });
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
