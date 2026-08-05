# Status

- id: OSC-0009
- state: Implementing
- updated: 2026-08-04
- note: 实现完成：后端 BatchLabel LIST 权威反查 + ArcoVue 表单/详情/搜索/六视图元数据治理；142 Vitest 通过、web 与 NewLife.Cube 构建成功；手工冒烟待真实 MVC 环境
- note2: 补充迭代（搜索/校验/级联/日期）：搜索框角色 LIST 单选改下拉直显且去编辑光标；手机/电话/邮件/邮箱/网址字段校验；User.AreaId 使用 Arco Cascader（后端 MVC 补 ItemType=area4）；日期/时间/日期时间按 itemType 推断组件 + 壁钟时间避免时区漂移；列表/卡片/看板/详情同步 formatFieldValue。172 Vitest 通过、web 与 NewLife.Cube 构建成功
- note3: 补充迭代（徽标交互）：Enable 徽标可点击调后端 SetEnable（列表/树/卡片/看板全视图）；非 Enable 状态/枚举/值集徽标悬停光标不变；卡片/看板状态字段渲染为徽标；卡片高度按字段自动伸缩、操作区固定左下。web 173 tests + api-core 5 tests 全过、构建成功
- note4: 补充迭代（回退与等高）：撤销自定义 SetEnable，改用既有 EnableOrDisableSelect 暴露的 EnableSelect/DisableSelect（与 NC 对齐）；看板徽标与卡片一致核验；卡片视图所有卡片高度统一为全量对象最高者（min-height 下发）、操作区固定左下。web 173 tests + api-core 6 tests 全过、构建成功
- note5: 补充迭代（徽标宽度自适应）：看板/卡片徽标 `align-self:flex-start` + nowrap，宽度按文案自适应（修复 vertical flex stretch 拉伸）；纯样式，构建成功
- note6: 补充迭代（横向徽标居中）：卡片横向排版（fieldOrientation=horizontal）下徽标 `align-self:center`，与前方标签垂直居中对齐不下沉；并入 tasks T9，纯样式，构建成功
