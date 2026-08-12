import LovSelect from './index.vue';

// story = 一个渲染变体。只描述「传什么 props」，不手写任何 demo 页。
// 新增变体只需往数组里加一项；gallery 自动收集。
// 注意：LovSelect 内部会按 code 调 fetchLovMeta（CT 桩按前缀返回 ENUM/LIST meta）。
//
// 每个 story 上方用「测试状态：」注释标明该变体要验证的运行时状态，
// 使 story 与 CT 截图测试、README 的「功能↔测试」表一一对应（见 README.md）。

export const stories = [
  // 测试状态：ENUM 类型、未选值，渲染单选项下拉（options 已加载，占位「请选择」）
  { id: 'LovSelect/EnumSingle', component: LovSelect, props: { code: 'Enum.Test.Status', multiple: false } },

  // 测试状态：ENUM 类型、多选，渲染多选下拉（collapse-tags 折叠标签）
  { id: 'LovSelect/EnumMulti', component: LovSelect, props: { code: 'Enum.Test.Status', multiple: true } },

  // 测试状态：ENUM 单选、已通过 modelValue='1' 回显，input 应显示「启用」（验证回显数据正常）
  { id: 'LovSelect/EnumSingleSelected', component: LovSelect, props: { code: 'Enum.Test.Status', multiple: false, modelValue: '1' } },

  // 测试状态：LIST 类型、单选、弹窗未开，渲染只读 input + 搜索按钮（点击触发弹窗）
  { id: 'LovSelect/ListSingleClosed', component: LovSelect, props: { code: 'List.Test.User', multiple: false } },

  // 测试状态：LIST 类型、多选、弹窗未开，渲染只读 input（多选占位）
  { id: 'LovSelect/ListMultiClosed', component: LovSelect, props: { code: 'List.Test.User', multiple: true } },

  // 测试状态：LIST 单选、已通过 modelValue=1 回显，只读 input 显示已选文本（displayText）
  { id: 'LovSelect/ListSingleEcho', component: LovSelect, props: { code: 'List.Test.User', multiple: false, modelValue: 1 } },
];
