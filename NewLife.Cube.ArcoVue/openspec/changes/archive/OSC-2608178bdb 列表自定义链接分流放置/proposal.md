# OSC-2608178bdb — 列表自定义链接分流放置

## 1. 目标愿景

让 GetPage `list[]` 中 **Url 非空** 的自定义链接在 ArcoVue 多维列表中按语义正确出现、可点击、可执行，表干净、行操作可发现，与飞书多维表 / Arco 中后台操作列范式一致。

- 目标 1：实体字段挂 `Url`（有后端 `TypeName`）保留为**数据列单元格链接**（值可点，支持 `target`）。
- 目标 2：合成 `AddListField` 导航链接与任意 `dataAction=action` **不占空洞数据列**，并入 `__ops`（直出前 N 个 +「更多」溢出）。
- 目标 3：`dataAction` 经归一进入 `FieldMeta`；点击后按占位符解析 Url，AJAX 成功刷新列表，导航走 SPA/`_blank`。
- 目标 4：table/tree 与 card/kanban 动作清单一致；calendar/gantt 不在条上堆操作，自定义链接进详情区/更多。

## 2. 为何做

后端控制器普遍通过 `ListFields.AddListField(...).Url = "...{Id}"` 或给已有字段设 `Url`/`DataAction` 扩展行级能力（日志、马上执行、关联页等）。GetPage 已把这些字段序列进 `data.list`。

ArcoVue 现状缺口：

1. Url 列当普通文本渲染，不可点；`packages/page-utils` 的 `resolveUrl` 未接入列表。
2. `fieldNormalize` **未拷贝 `dataAction`**，`FieldMeta` 无该字段。
3. `__ops` 仅有详情/编辑/删除 + 最多 3 个自动化按钮，无 ListField 自定义链接通道。
4. 合成链接列在表中形成空值/噪点列，与迁移方案「工具条干净、列是数据」及 Metronic8「Url 进更多」演进不一致。

## 3. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | 采用**方案 E 分流**：字段挂链接 → 单元格；合成导航链接 + 任意 `dataAction` → 操作列。 |
| 2 | **分流判定**：`dataAction` 非空 → 操作列动作；`url` 非空且后端原始 `typeName` 非空 → 单元格链接；`url` 非空且原始 `typeName` 为空（合成列）→ 操作列导航。归一时不得用默认 `"String"` 抹掉「无 TypeName」信号（须保留可判定空 TypeName 的信息）。 |
| 3 | 操作列顺序：`详情 \| 编辑 \| 删除 \| 自定义直出(≤OPS_LINK_INLINE_MAX=2) \| 自动化(≤3) \| 更多▾`；「更多」收纳超出直出的自定义链接（及可选自动化溢出，本号以自定义链接溢出为主）。 |
| 4 | 自定义链接配色复用既有 `OPS_LINK_COLOR`（链接色）；不改 CRUD 配色。 |
| 5 | **工具栏不承载**行级 `{Id}` Url（页级批量启用/禁用仍属迁移矩阵 P2，本号不做）。 |
| 6 | **后端零改** ListFields / GetPage 契约；仅前端消费与 UI。不实现 `DataVisible` 服务端按行隐藏（API 未物化到 SPA 则本号不做）。 |
| 7 | MapProvider 服务端运行时补 Url：若 GetPage JSON 中 `url` 仍为空，本号不前端臆造 Map 链接（与经典 GetLink 服务端渲染差异登记为已知限制，另立号若需）。 |

## 4. 做什么

1. 扩展 `FieldMeta` / `toFieldMeta`：保留 `dataAction`；保留「原始 typeName 是否为空」供分流（见 design）。
2. 新增纯函数：`classifyListLink` / `partitionListFields`（数据列 vs 操作链接）。
3. 列表列构建：合成 Url/`dataAction` 字段从 VTable/卡片数据列剔除。
4. 单元格：字段挂链接渲染可点（resolveUrl + target）。
5. `__ops`：拼装自定义链接；直出 + Arco Dropdown「更多」；点击分流导航 / AJAX。
6. card/kanban 同步动作；calendar/gantt：自定义链接仅出现在打开详情后的次级入口（见 ui IA）。
7. Vitest + 手工冒烟（User 链接、CronJob 日志/马上执行、Demo Class 若可用）。

## 5. 不做什么

- 不把行级 Url 做成 list-topbar /「高级」菜单按钮。
- 不改后端 `AddListField` / `ListField.GetLink` / GetPage 载荷形状。
- 不做 OSC-0018 设计文档正文；本号是可执行前端能力。
- 不做页级批量「启用/禁用」工具条（迁移 P2）。
- 不实现 FlowGram / 审批类动作。
- 不把 Metronic「全部 Url 进更多」作为唯一策略（会误伤字段挂链接）。

## 6. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0003 | DefaultList / GetPage / FieldMeta |
| OSC-0005 | `__ops` 右固定列 |
| OSC-0006 | card/kanban 底栏操作 |
| OSC-0007 | 工具栏精简契约（本号不往工具栏塞行链接） |
| OSC-260815fa86 | 自动化 button 已占 `__ops`；本号与之并存，直出配额分离 |
| OSC-0018 | 文档层引用 Url/dataAction；本号实现消费，不阻塞 0018 |
| `@cube/page-utils` `resolveUrl` | 占位符替换 |

## 7. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| Vitest | 是 | classify/partition、ops 拼装顺序与溢出、normalize 保留 dataAction/空 typeName、resolveUrl 点击参数构造 |
| 构建 | 是 | `pnpm --filter @cube/arco-vue test` + `build`（或 web 目录等价命令） |
| 手工冒烟 | 是 | Admin/User「链接」、Cube/CronJob「日志」「马上执行」；字段挂 Url 实体若有则验单元格可点 |

## 8. 成功标准

- [ ] 合成 Url 列不再以空洞数据列出现在 table/tree。
- [ ] 字段挂 Url：列显示实体值且可点，`target=_blank` 新开。
- [ ] `dataAction=action`：点击发请求（与现有 action 约定一致），成功后刷新当前列表。
- [ ] `__ops` 含自定义链接；超过直出上限进入「更多」；与自动化按钮共存且不互相覆盖配额语义。
- [ ] card/kanban 动作清单与 table 一致（含更多）。
- [ ] 新增单测全过；前端构建无错误；`web/README.md` / 功能清单最小回写。
