# OSC-0016 Retro

> 复盘在 `Validating` 阶段（checklist passed）由 openspec-retro 填写；归档后状态置 `Done`。

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | 18/18 通过（2 项轻微 UX 偏差不阻塞） |
| 三步编排 | 实现审计（0🔴/2🟡已修）→ 代码审查（1🔴+3🟡已修）→ 文档同步（5🟡已修） |
| 自动化门禁 | 后端 0 错误 0 警告 · XUnit 213 通过/8 外部依赖失败（基线一致）· Osc0016Tests 7/7 · api-core 11/11 · Vitest 275/275 · vue-tsc ✅ · vite build ✅ |
| 代码质量 | 后端元数据层扩展干净（FillMapCandidates / entity: 协议），前端无状态组件设计合理（QueryComboButton 纯事件上抛） |
| 工期 | 15 个 Task（T1~T12 计划 + T13~T15 会话补录）全部完成，执行 + 验收两阶段 |
| 手工冒烟 | deferred（需 CubeDemo 运行环境；`entity:` 内部查询与 UserController 标准字段兼容尤需真实运行验证） |

## 做得好的

1. **无状态组件设计**：`QueryComboButton` 纯 props + emits，全部状态由 `SearchDrawer` / `DefaultList` / `viewProfile store` 持有，组件可复用于抽屉与面板容器。
2. **entity: 协议内部值集**：`LovController.FetchEntityList` 直接走 EntityFactory 分页 + Q 模糊，不经 HTTP 外环，性能与安全边界清晰；修复后含方法缓存与异常降级。
3. **Map 候选自动填充三分支**：小表内联 / 大表 Entity. 值集 / 手工优先不覆盖，逻辑清晰且有 MemoryCache 60s 防抖；`FixSearchMapCandidates` 对未注册值集手工 LovCode 兜底。
4. **线缆兼容防御**：`normalizeSavedQuery` + `parseQueriesWire` 覆盖 null/空串/坏 JSON/重复 id/空 name/空 params 六种异常输入，Vitest 全覆盖。
5. **NC 回归守卫**：`_Common_List_Search.cshtml` 加 `DataSourceMap.Count <= MaxDropDownList` 阈值，防止大表候选灌入 MVC 下拉。
6. **与 OSC-0015 清晰分层**：FiltersJson（视图级前端过滤）vs QueriesJson（个人级服务端搜索），持久化字段、store 方法、UI 入口完全独立。

## 待改进

1. **会话小任务未即时补录**：T13/T14/T15（UserController 兼容、面板抽屉重构、GetPage 元数据扩展）在执行期直接完成，未即时登记 tasks.md，直到验收补录。**教训**：执行期每完成一件计划外事项应立即补录 tasks.md + status.md，避免验收期补课与 review 才发现语义回归（UserController 的 Code 缺失/roleIds 语义问题正是因补录晚发现）。
2. **CascaderSearchPanel 清理误判**：design §7 将 Arco Design Vue 库内部组件 `cascader-search-panel.js` 误判为项目孤儿组件。执行阶段发现后及时修正为 AC-15 事实修正。**教训**：清理前应区分 `node_modules` / 库产物与项目源码。
3. **UserController.Search 重写引入 3 处回归**（🔴 空引用 + 🟡 Code 缺失 + 🟡 roleIds 多值语义），均被代码审查兜住。**教训**：重写"对齐原语义"的搜索方法时，必须逐项对照原 XCode 实现（Code 是否含、多值拼接方式、null 判空顺序），并补针对性测试；此类业务重写最易出现静默语义漂移。
4. **菜单「删除当前查询」缺少 popconfirm**：列表条目行内删除有 popconfirm，但菜单项直接执行。**建议**：后续补 `Modal.confirm`。
5. **「重置查询参数」全空时未禁用**：`__reset` 菜单项无 `:disabled` 绑定，全空时点击为 no-op。**建议**：增加 `canReset` computed。
6. **CubeDemo 进程锁定 DLL**：`dotnet build 魔方.sln` 因 CubeDemo 进程锁定 DLL 导致 MSB3027 复制失败。**运维提示**：长时间运行的 Demo 进程应在 CI/验收流程前自动清理。

## 遗留与后续

- 旧 FiltersJson 中 `_min/_max` 键随本号静默失效，是否需要在某版本做一次性迁移提示（当前决策：不做，文档标注）。
- 服务端通用操作符/范围/IN 多值查询能力（若未来需要）另立 OSC。
- `entity:` BatchLabel 大表逐页反查 → 建议改按 value 集合 `In` 查询（🟢）。
- `Osc0016Tests` 反射调私有 `FillMapCandidates` → 建议 `internal` + `InternalsVisibleTo`（🟢）。
- `saveQueryAs` 保存前未再次归一、SearchDrawer 单边时间无编辑入口、`RegisterMapLov` 无唯一索引保护、空表行数未缓存（🟢）。
- 待改进 #4/#5 可作为 micro-OSC 或后续迭代直接修复（影响面小，不阻塞发版）。
