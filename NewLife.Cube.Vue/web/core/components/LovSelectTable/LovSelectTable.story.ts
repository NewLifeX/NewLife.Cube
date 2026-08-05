import LovSelectTable from './index.vue';

// story = 一个渲染变体。只描述「传什么 props」，不手写任何 demo 页。
// 新增变体只需往数组里加一项；gallery 自动收集。
//
// 每个 story 上方用「测试状态：」注释标明该变体要验证的运行时状态，
// 使 story 与 CT 截图测试、README 的「功能↔测试」表一一对应（见 README.md）。

const baseLovMeta = {
  name: '角色',
  valueField: 'id',
  tableColumns: [
    { field: 'id', title: '编号' },
    { field: 'name', title: '名称' },
  ],
  listConfig: { pageable: false },
  searchFields: [] as unknown[],
};

// 分页型 meta：listConfig.pageable=true → 渲染 el-pagination（CT mock 对 code 含 'Paged' 返回 23 条）
const pagedLovMeta = {
  ...baseLovMeta,
  listConfig: { pageable: true },
};

const base = {
  dialogVisible: false, // 由 spec 通过 setStoryProps 打开，触发真实 watch 加载数据
  lovCode: 'List.CubeDemo.Role',
  lovMeta: baseLovMeta,
  inlineEnums: {},
};

const pagedBase = {
  dialogVisible: false,
  lovCode: 'List.Paged.Role', // code 含 'Paged' → CT mock 返回 23 条、可分 2 页
  lovMeta: pagedLovMeta,
  inlineEnums: {},
};

export const stories = [
  // 测试状态：单选 + 弹窗打开，表格行已加载（左侧单选 radio 列）
  { id: 'LovSelectTable/SingleOpen', component: LovSelectTable, props: { ...base, multiple: false } },

  // 测试状态：多选 + 弹窗打开，表格行已加载（左侧复选框列 + 底部「确定」按钮）
  { id: 'LovSelectTable/MultiOpen', component: LovSelectTable, props: { ...base, multiple: true } },

  // 测试状态：单选 + 已通过 modelValue=1 回显，已选行高亮
  { id: 'LovSelectTable/SingleEcho', component: LovSelectTable, props: { ...base, multiple: false, modelValue: 1 } },

  // 测试状态：多选 + 已通过 modelValue=[1,2] 回显，对应行勾选 + 底部「已选 2 项」
  { id: 'LovSelectTable/MultiEcho', component: LovSelectTable, props: { ...base, multiple: true, modelValue: [1, 2] } },

  // 测试状态：多选 + 分页（23 条/2 页）+ 跨页回显已选（modelValue=[1,2,21,22] 横跨两页），
  // 验证「点击回显时已选不正确 / 翻页后已选不正确 / 翻页后勾选才更新」根因 C2 已修复：
  // 打开后统计「已选 4 项」、翻到第 2 页勾选与统计保持、翻回第 1 页保持。
  { id: 'LovSelectTable/PagedMultiEcho', component: LovSelectTable, props: { ...pagedBase, multiple: true, modelValue: [1, 2, 21, 22] } },

  // 测试状态：多选 + 分页（23 条/2 页）+ 跨页选中，底部「已选 N 项」统计（翻页左下角统计要正常）
  { id: 'LovSelectTable/PagedMultiSelection', component: LovSelectTable, props: { ...pagedBase, multiple: true } },
];
