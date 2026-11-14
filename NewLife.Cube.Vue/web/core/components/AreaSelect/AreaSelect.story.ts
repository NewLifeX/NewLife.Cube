import AreaSelect from './index.vue';

// story = 一个渲染变体。只描述「传什么 props」，不手写任何 demo 页。
// 数据面：ct/mocks/request.ts 的 Area 路由表供真数据（北京市→市辖区→东城区）。
// 每个 story 上方用「测试状态：」注释标明该变体要验证的运行时状态。

export const stories = [
  // 测试状态：懒加载、未选值，渲染关闭态输入框；展开面板应经根层(北京市)→五区真渲染
  // （复现组合缺陷的断言在 AreaSelect.ct.spec.ts，缺陷态=面板「暂无数据」）
  { id: 'AreaSelect/LazyRoot', component: AreaSelect, props: {} },

  // 测试状态：modelValue=110101 回显，经 /Cube/AreaParents 桩链回显「北京市 / 市辖区 / 东城区」
  { id: 'AreaSelect/Echo', component: AreaSelect, props: { modelValue: 110101 } },
];
