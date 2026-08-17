# OSC-2608178bdb Design — 列表自定义链接分流放置

## 0. 适用框架与官方资料

| 场景 | 框架/资料 | 说明 |
| --- | --- | --- |
| 设计系统 / Dropdown / Link / Message | Arco Design Vue | https://arco.design/vue/docs/start ；Dropdown / Link / Message 文档须在实现前查阅，禁止凭印象造 props |
| 多维表 / 操作列 customLayout | VisActor VTable | 教程：https://arco.design/vue/docs/start ；配置：https://visactor.com/vtable/option/ListTable ；接口：https://visactor.com/vtable/api/Methods |
| 经典对照 | Metronic8 `_List_Data_Action_Adv.cshtml` | Url/DataAction 剔除数据列 → 行「更多」 |
| 后端元数据 | `ListField.Url` / `DataAction` / `Target` | `NewLife.CubeNC/ViewModels/ListField.cs`；GetPage → `data.list` |
| Url 模板 | `@cube/page-utils` `resolveUrl` | `{Id}` / 字段名占位；本号接入，不改算法除非发现 `{page:}` 缺口（见 §10） |

## 1. 目标与契约边界

在不改 GetPage 后端契约的前提下，前端按**方案 E**消费 `list[]` 中 Url 非空字段：

| 输入 | 放置 | 交互 |
| --- | --- | --- |
| `url` 非空 + 原始 `typeName` 非空 + `dataAction` 空 | **数据列单元格链接** | 显示单元格值（无值则 DisplayName）；点击导航 |
| `url` 非空 + 原始 `typeName` 空（合成列）+ `dataAction` 空 | **`__ops` 导航项** | 文案=DisplayName/Header；点击导航 |
| `dataAction` 非空（不论 typeName） | **`__ops` 动作项** | 文案同上；点击 AJAX，成功刷新列表 |
| `url` 空 | 普通列 | 本号不改 |

**与既有机制职责分离：**

| 机制 | 归属 | 本号关系 |
| --- | --- | --- |
| CRUD `__ops` | `opsAction.ts` `buildOpsParts` | 扩展 parts；CRUD 顺序不变 |
| 自动化 button | OSC-260815fa86 `useListAutomation` | 继续 `auto:{id}`；直出上限仍 3；排在自定义直出之后 |
| 工具栏「高级」 | OSC-0007 | **不**放入行级 Url |
| RecordDrawer | 详情 | calendar/gantt 自定义链接次级入口可挂详情头「更多」，table 主路径仍是 `__ops` |
| Map 自动 Url | 服务端 `ListField.GetUrl` | GetPage 若未写出 url，本号不前端补造 |

## 2. 文件级改动地图

### 2.1 类型与归一

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `packages/api-core/src/types.ts` | 确认 `DataField.dataAction?` 已存在（只读契约，不删） | 其余 DTO |
| `web/src/core/types/field.ts` | `FieldMeta` 增加 `dataAction?: string`；增加 `hasTypeName?: boolean`（或等价：`typeNameSource: 'api' \| 'fallback'`）标记后端是否给出 TypeName | 其它字段 |
| `web/src/core/utils/fieldNormalize.ts` | `toFieldMeta`：拷贝 `dataAction`（含 PascalCase `DataAction`）；在 `pickTypeName` 回落 `"String"` **之前**记录 `hasTypeName = !!(raw typeName)` | description/dataSource 既有逻辑 |

### 2.2 分流纯函数（新增）

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/core/utils/listLinkFields.ts`（新增） | `ListLinkKind = 'none' \| 'cell' \| 'opsNav' \| 'opsAction'`；`classifyListLink(field)`；`partitionListFields(fields) => { dataFields, opsLinks }`；`OPS_LINK_INLINE_MAX = 2` | — |
| `web/src/core/utils/listLinkFields.spec.ts`（新增） | 覆盖 §3 真值表全部行 | — |

**`classifyListLink` 规则（锁定）：**

```
if (!url?.trim()) → none
if (dataAction?.trim()) → opsAction
if (hasTypeName === true) → cell
else → opsNav
```

### 2.3 操作列拼装

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/core/utils/opsAction.ts` | 扩展 `buildOpsParts` 或新增 `buildOpsPartsWithLinks`：入参增加 `opsLinks: OpsCustomLink[]`；顺序见 §4；返回 parts + `overflowLinks`；自定义 key 形如 `link:{fieldName}` | CRUD 颜色表、`opsAutoKey`、`OPS_LINK_COLOR` |
| `web/src/core/utils/opsAction.spec.ts`（新增或扩展） | 顺序、直出截断、与 automation 共存 | — |

```ts
export interface OpsCustomLink {
  name: string;          // field.name
  label: string;         // displayName
  url: string;           // 模板，点击时 resolve
  target?: string;
  dataAction?: string;   // 'action' | undefined
}
```

### 2.4 列表查询与表格

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/views/crud/useListQuery.ts` | GetPage 后对 `listFields` 调用 `partitionListFields`；对外暴露 `dataListFields` + `opsCustomLinks`（或写入 listContext） | 搜索/分页/setting |
| `web/src/views/crud/listContext.ts` | 类型增加 `opsCustomLinks` | 既有 context 字段 |
| `web/src/features/vtable/useListTable.ts` | 列定义只用 `dataListFields`；`__ops` customLayout：直出链接 +「更多」触发（见 §4 UI）；点击 `link:*` 回调父级 | 分组/冻结/勾选 |
| `web/src/features/vtable/ListTable.vue` | props 透传 opsLinks / onOpsLink；薄脚本 | — |
| `web/src/views/crud/useListCrud.ts` 或新建 `useListOpsLinks.ts` | `onOpsLink(row, link)`：`resolveUrl` → 若 dataAction 则 `cubeApi`/fetch action（与项目现有 action POST 约定对齐，实现前在代码库定位一处经典 action 调用并复用）；否则 `router.push` 或 `window.open`（target） | CRUD delete/edit |
| `web/src/views/crud/DefaultList.vue` | 接线：partition 结果 → ListTable / Card；不把链接放进 topbar | 筛选/搜索/高级 |

### 2.5 单元格链接

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/core/utils/fieldFormat.ts` 或 VTable 单元格渲染路径 | `classify === 'cell'` 时输出可点击样式/事件（VTable 用 link 样式 + click 打开）；文本仍 `formatFieldValue`，空值回落 `displayName` | 非链接列格式化 |
| 实现点以当前 `useListTable` 单元格点击/customLayout 为准 | 禁止整列变成裸 `<a href>` 导致丢 SPA 路由（站内路径优先 vue-router） | — |

### 2.6 多视图

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `web/src/features/views/RecordCard.vue`（及 kanban 卡片） | 底栏操作与 `buildOpsPartsWithLinks` 同源；溢出用 `a-dropdown` | 卡片布局/字段映射 |
| calendar / gantt 视图宿主 | **不**在事件条上渲染自定义链接；打开 RecordDrawer 后，在详情头或操作区提供同一 `opsCustomLinks` 的「更多」入口（最小：详情抽屉内链接列表） | 甘特缩放/映射 |

### 2.7 SFC 职责

业务 TS 进 `useListOpsLinks.ts` / `listLinkFields.ts`；`.vue` 只保留薄 script（符合 openspec README SFC 分离）。

## 3. 分流真值表

| url | dataAction | hasTypeName | kind | 数据列 | 操作列 |
| --- | --- | --- | --- | --- | --- |
| 空 | * | * | none | 是（普通） | 否 |
| 非空 | 非空 | true | opsAction | **否** | 是（AJAX） |
| 非空 | 非空 | false | opsAction | **否** | 是（AJAX） |
| 非空 | 空 | true | cell | 是（可点） | 否 |
| 非空 | 空 | false | opsNav | **否** | 是（导航） |

**状态唯一来源：** `listFields`（归一后）→ `partitionListFields` 一次产出 `dataFields` + `opsLinks`；禁止在 VTable 与 Card 各自再猜一遍。

## 4. 操作列 UI 契约

### 4.1 视觉顺序（左→右）

```
[详情] [编辑] [删除] [自定义1] [自定义2] [自动1..3] [更多▾]
```

- `OPS_LINK_INLINE_MAX = 2`：仅统计 **opsNav + opsAction** 自定义链接直出数。
- 自动化仍 `slice(0, 3)`，全部直出（与 OSC-260815fa86 一致）；本号不把自动化塞进「更多」，除非后续另号统一溢出策略。
- 「更多」仅当 `opsLinks.length > OPS_LINK_INLINE_MAX` 时显示；菜单项为超出的自定义链接。

### 4.2 交互

| 动作 | 行为 |
| --- | --- |
| opsNav / cell 站内路径 | `vue-router` push（去掉 origin，保留 query）；失败则 `location.assign` |
| `target=_blank` 或外链 | `window.open(url, '_blank', 'noopener,noreferrer')` |
| opsAction | POST/约定 method 调解析后 Url；`Message.success/error`；成功 `reload list` |
| disabled | 无行数据或 url 解析后为空：该项不渲染或 disabled |
| 权限 | 自定义链接**不**额外要求 Detail 权限（Metronic 曾包在 Has Detail 下）；本号锁定为：**有列表可见即可点**（与飞书行按钮一致）。若产品后续要权限码，另号。 |

### 4.3 VTable 实现约束

- 继续 customLayout 独立链接文本，配色：`opsActionColor` / `OPS_LINK_COLOR`。
- 「更多」：canvas 内放「更多」文本链 → 回调打开挂载在表格外的 `a-dropdown`/`a-trigger`（记录 anchor 坐标），或使用 Arco 在行上的 teleport 菜单；**禁止**在 canvas 内自绘完整菜单 DOM。实现时选一种并在 verify 冒烟。

## 5. 核心文档影响

| 文档 | 动作 |
| --- | --- |
| `NewLife.Cube.ArcoVue/web/README.md` | 增补：ListField Url 分流（单元格 vs `__ops`） |
| `Doc/功能清单.md` | 列表自定义链接列 → ArcoVue 已支持分流放置 |
| `ArcoVue企业中后台迁移方案.md` | 能力矩阵补一行：ListField.Url/dataAction \| 操作列+单元格分流 \| ✅（本号后） |
| OSC-0018 design | 可选交叉引用本号为 L0 消费实现（不改 0018 交付物义务） |

## 6. 测试设计

| 用例 | 期望 |
| --- | --- |
| 合成字段 url + 无 typeName | opsNav；不在 dataFields |
| Name + url + typeName=String | cell；在 dataFields |
| Execute + dataAction=action | opsAction；不在 dataFields |
| 5 条 opsLinks | 直出 2 + 更多含 3 |
| buildOpsParts 含 2 CRUD + 2 link + 2 auto | 顺序符合 §4.1 |
| toFieldMeta 含 DataAction 与 hasTypeName=false | 字段齐全 |
| resolveUrl 点击构造 | `{Id}` 替换且 encode |

## 7. 多视图差异矩阵

| 视图 | 数据列单元格链接 | 自定义 ops 链接 |
| --- | --- | --- |
| table / tree | 是 | `__ops` 直出+更多 |
| card / kanban | 卡片标题/主字段若为 cell 种类则可点 | 底栏同源 parts + dropdown |
| calendar | 否（条上） | 打开详情后展示链接列表 |
| gantt | 否（条上） | 同 calendar |

## 8. 空数据与非法输入

| 输入 | 行为 |
| --- | --- |
| url 仅空白 | 视同无 url |
| 解析后 url 为空串 | 不导航；Message.warning 可选 |
| 未知 target | 按 `_self` SPA |
| dataAction 非 `action` 的其它值 | 本号仍当 AJAX 动作（与后端字符串兼容）；文档注明当前仅识别非空即动作 |

## 9. 旧数据 / 兼容

- 无 url 的旧页面：行为不变。
- 曾显示为空列的合成链接：升级后列消失、进入操作列（**预期行为变更**，须在 README 写明）。

## 10. 风险与降级

| 风险 | 降级 |
| --- | --- |
| VTable「更多」菜单难做 | 全部自定义链接直出（可临时提高 INLINE_MAX），或一律进 Dropdown 触发器列 |
| `resolveUrl` 不支持 `{page:xxx}` | 本号先支持 `{Field}`；遇 page 占位保留原文并记 verify 已知限制 |
| action POST CSRF/路由差异 | 对齐 Cube 现有前端若已有 action 调用；若无则用 `fetch` + credentials same-origin，路径用解析后绝对或相对 Url |

## 11. 方案评比摘要（决策依据）

| 方案 | 结论 |
| --- | --- |
| A 全单元格 | 合成列噪点高，否 |
| B 全进更多 | 误伤字段挂链接，否作唯一策略 |
| C 工具栏 | 行级 Url 无上下文，否 |
| D 仅抽屉 | 低频，否作主通道 |
| **E 分流** | **本号锁定** |
