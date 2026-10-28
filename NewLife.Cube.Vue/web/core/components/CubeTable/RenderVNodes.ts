/**
 * 渲染一段外部传入的 vnode
 *
 * 操作列的插槽动作要「按第几个」拆到行内与「更多」下拉两处，模板里没法直接渲染
 * vnode 数组，所以用这个极薄的渲染组件承一下。调用方负责每次渲染重新调插槽函数
 * （同一 vnode 对象不能在两处挂载）。
 */
import { defineComponent, type PropType, type VNode } from 'vue';

export default defineComponent({
  name: 'RenderVNodes',
  props: {
    /** 要渲染的 vnode 列表（通常只有一个操作节点） */
    nodes: { type: Array as PropType<VNode[]>, required: true },
  },
  setup(props) {
    return () => props.nodes;
  },
});
