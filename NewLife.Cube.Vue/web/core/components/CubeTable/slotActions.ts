/**
 * `#row-actions` 插槽动作节点的归一化
 *
 * 插槽返回值是 vnode 树：模板里的 `v-if` 会产出 Comment、换行会产出空白 Text、
 * 片段（Fragment）会把多个按钮包成一层。计量与收起都要求「一个节点 = 一个操作」，
 * 所以先把这些结构性节点摊平，只留真正要上屏的操作。
 */
import { Comment, Fragment, Text, type VNode, type VNodeArrayChildren } from 'vue';

/**
 * 把插槽返回值摊平成「一个节点 = 一个操作」的列表。
 *
 * @param nodes 插槽函数返回值（可能是单个 vnode、数组或片段）
 * @returns 可上屏的操作节点，顺序与原插槽一致
 */
export function toActionNodes(nodes: unknown): VNode[] {
  const out: VNode[] = [];

  const walk = (value: unknown) => {
    if (Array.isArray(value)) {
      value.forEach(walk);
      return;
    }
    if (!value || typeof value !== 'object') return;

    const node = value as VNode;
    if (!('type' in node)) return;
    // 片段没有自己的 DOM，摊平后才好按「第几个按钮」收起
    if (node.type === Fragment) {
      walk(node.children as VNodeArrayChildren);
      return;
    }
    if (node.type === Comment) return;
    if (node.type === Text && !String(node.children ?? '').trim()) return;

    out.push(node);
  };

  walk(nodes);
  return out;
}
