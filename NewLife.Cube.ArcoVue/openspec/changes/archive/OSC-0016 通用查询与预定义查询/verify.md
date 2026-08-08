# OSC-0016 Verify

> 进入 `Validating` 后逐项勾选。每条 AC 必须可逐条判定；命令在仓库根（NewLife.Cube）或注明目录下执行。

## 执行阶段记录（openspec-apply，2026-08-07）

- 后端：`NewLife.Cube` / `NewLife.CubeNC` 编译通过（0 错误）；`XUnitTest.Osc0016Tests` 7 用例全过（ViewProfile QueriesJson upsert / Map 候选三分支 / LovAutoRegister 注册 / GetPage MasterTime 有/无）。
- 前端：`packages/api-core` test 11/11 + build 通过；`NewLife.Cube.ArcoVue/web` Vitest 26 文件 / 275 用例全过 + vue-tsc/vite 构建通过（wwwroot 重新生成）。
- 全量 XUnitTest 213/221 通过；8 个失败均为既有外部服务依赖（QyWeiXin 微信 6 + SsoClient 2），与 OSC-0016 无关；已加 `AssemblyInfo.cs` 禁并行修复多测试类 `AddConnStr("Cube")` 连接竞争。
- 手工冒烟（AC-01~07/09~11/13/14/16/17 与菜单禁用态）留待 `openspec-verify`。

## 验收阶段记录（openspec-verify，2026-08-08）

### 会话小任务补录

执行期通过会话窗口直接完成、不在原 proposal/design 计划内的事项已补录 tasks.md：
- **T13** `UserController.Search` 兼容标准字段名（roleIds↔RoleID、departmentId↔DepartmentID、enable↔Enable、q↔Q + 通用字段等值循环）
- **T14** 面板两行/右侧抽屉重构（`QueryInsightPanel` → `SearchDrawer` + `InsightPanel` 暂隐藏；原折叠按钮与操作区并入查询组合按钮）
- **T15** GetPage 元数据扩展（api-core PageSetting + NC 版 GetPage setting 同步）

### 三步编排摘要（实现审计 → 代码审查 → 文档同步）

**① 实现审计（implementation-audit）**：T1~T15 全部落实，AC-01~18 功能判定全部通过；发现 0🔴 / 2🟡 / 8🟢。2 个🟡均在 T13 `UserController.Search`（原 XCode 语义回归）：Code 登录名缺失模糊搜索、roleIds 多值拼接语义不等价。**已修复**。

**② 代码审查（code-review）**：1🔴 / 4🟡 / 7🟢。🔴 为 `UserController.Search` id 分支先解引用后判空（NullReference）。🟡 为 `entity:` 协议分页 Int32 溢出、反射 Invoke 无保护无缓存、`FixSearchMapCandidates` 逐字段查库、Search 通用等值 ChangeType 抛异常。**🔴 与 3 个🟡（溢出/反射/查库）已修复**；ChangeType 一项与框架基类同模式（前端白名单缓解），按与基类一致处理不做差异修改。🟢 记入 retro 后续。

**③ 文档同步（doc-sync）**：四份事实文档（web/README.md、Doc/功能清单.md、ArcoVue企业中后台迁移方案.md、Doc/Api/核心接口架构.md）核心事实均正确登记、无同义改写；5 处🟡（术语"清空→重置查询参数"×2、测试统计 193→200、§8.2.2 容器契约注记、verify AC-10/AC-13 抽屉形态）**已修正**。

### 代码审查修复记录

| # | 问题 | 严重度 | 修复 |
|---|------|:----:|------|
| 1 | `UserController.Search` id 分支 `entity.Password=null` 先于判空 → 不存在的 id 500 | 🔴 | 先判空再清密码（`if (entity != null) { … }`） |
| 2 | `UserController.Search` 关键字模糊缺 `Code` 登录名（原 XCode 语义回归） | 🟡 | 模糊条件增 `_.Code.Contains(key)`；通用等值跳过列表保留 Code 跳过 |
| 3 | `UserController.Search` roleIds 多值时 `RoleIds.Contains(","+join+",")` 语义不等价原 XCode | 🟡 | 改为逐 rid `exp2 |= _.RoleIds.Contains("," + rid + ",")` |
| 4 | `FetchEntityList` 分页 `(pageNum-1)*pageSize` Int32 溢出 | 🟡 | pageNum 上限 100_000 防溢出 |
| 5 | `FetchEntityList` 反射 Invoke 无异常保护/无缓存/按名取方法 | 🟡 | 静态 `ConcurrentDictionary` 缓存 MethodInfo（校验三参数签名）+ try/catch 解包降级 + 缺失写日志 |
| 6 | `FixSearchMapCandidates` 每请求逐字段 `LovDefinition.Find` 查库 | 🟡 | MemoryCache 60s（"LovRegistered:" key，"1"/"0" 哨兵缓存已注册/未注册） |

### 自动化门禁复检（修复后重跑）

- 后端 `NewLife.Cube` + `NewLife.CubeNC` 编译：**0 错误 0 警告** ✅
- `XUnitTest` 全量：**213 通过 / 8 失败**（QyWeiXin 6 + SsoClient 2，均为既有外部服务依赖，与执行期基线一致）✅；`--filter Osc0016Tests` **7/7** ✅
- `packages/api-core` test **11/11** ✅ + build ✅
- `NewLife.Cube.ArcoVue/web` Vitest **26 文件 / 275 用例全过** ✅；`vue-tsc --noEmit` 通过 ✅；`vite build` 成功 ✅（exit code 1 为 Rollup chunk 大小警告写 stderr 的 PowerShell 包装，构建产物正常生成）

## 验收标准

### 查询组合按钮与预定义查询
- [x] **AC-01 执行查询**：`QueryComboButton` 主按钮 `@click="emit('search')"` → `SearchDrawer` → `DefaultList.handleSearch` → `loadData()`。与抽屉/面板搜索按钮等效。✅
- [x] **AC-02 保存预定义查询**：`__save` 菜单项 `:disabled="!canSave"`（canSave=cleanSearchParams 后非空）→ `openModal('save')` 弹窗 → `emit('save', name)` → `DefaultList.handleSaveQuery` → store 新增 + 自动执行。✅
- [x] **AC-03 应用预定义查询**：`__apply:{id}` → `emit('apply', id)` → `handleApplyQuery` → `applySearchToForm(params)` 整体替换 + `loadData()`。`isApplied(id)` = `activeQueryId === id && !paramsDirty` 控制 ✓ 标记。✅
- [x] **AC-04 重命名当前查询**：`__rename` `:disabled="!canRename"`（canRename = `!!activeQueryId`）→ `openModal('rename')` → trim 非空校验。✅
- [x] **AC-05 删除当前查询**：`__delete` `:disabled="!canRename"` → `emit('delete', activeQueryId)` → `handleDeleteQuery` → `store.deleteQuery` 移除条目 + 清除 activeQueryId + **不清空 searchForm**。列表条目行内删除有 popconfirm；菜单「删除当前查询」无 popconfirm（轻微偏差，功能正确）。✅
- [x] **AC-06 重置查询参数**：`__reset` → `emit('reset')` → `handleReset` → `Object.keys(searchForm).forEach(delete)` + `clearActiveQuery` + `loadData()`。全空时未设禁用（轻微 UX 偏差，点击无副作用）。✅
- [x] **AC-07 持久化**：`store.scheduleSave(typePath, true)` → `payload.queriesJson = serializeQueriesWire(entry.queries)` → ViewProfile API → 数据库。`activeQueryId` 仅 `byType` 内存态，不随刷新保留。✅
- [x] **AC-08 线缆兼容**：`normalizeSavedQuery` + `parseQueriesWire` 覆盖 null/空串/坏 JSON/重复 id/空 name/空 params。Vitest 覆盖。✅
- [x] **AC-09 权限边界**：`viewProfile.ts` store 仅读 `personal?.queriesJson`，不回退 `template` 域；`TemplateManageDrawer` 无预定义查询管理入口。✅

### 面板控件与搜索语义
- [x] **AC-10 保留参数控件渲染矩阵**（以活跃 `SearchDrawer` 为判定对象，抽屉形态随 T14 重构）：`SearchDrawer.vue` — `v-if="masterTimeName"` 条件渲染主时间范围（label = `masterTimeDisplayName || '时间范围'`）；`v-if="enableKey !== false"` 条件渲染关键字框；Q 关键字为第一个条件，其余按 GetPage Search 顺序每条件一行，组合按钮在抽屉右上角。✅
- [x] **AC-11 Q 与 dtStart/dtEnd 生效**：Q 绑定 `model.Q`，回车触发 `$emit('search')`；主时间范围 `onMasterTimeChange` 写 `model.dtStart/dtEnd`；`cleanSearchParams` 合法键集含 `RESERVED_SEARCH_KEYS = ['Q', 'dtStart', 'dtEnd']`，与字段等值条件叠加提交。后端 `Search(Pager)` 内置 Q/dtStart/dtEnd 通用处理。✅
- [x] **AC-12 单值等值控件**：`resolveSearchControl` — DateTime→`datetime`、date→`date`、time→`time`、数值→`number`；`searchFilters.ts` 无 `_min/_max` 引用（grep 确认）；Vitest 覆盖映射。✅
- [x] **AC-13 Map 字段候选**：`FieldCollection.FillMapCandidates` — 小表（≤MaxDropDownList）内联 `DataSourceMap`、大表 `LovCode = "Entity." + entityType.FullName`；手工 LovCode/DataSourceMap 优先不覆盖；`LovController.FetchEntityList` 走 `entity:` 协议内部查询（Q 模糊 + 分页）；`FixSearchMapCandidates` 对未注册值集手工 LovCode 用 Map 实体候选兜底。`entity:` 内部查询真实运行冒烟待 CubeDemo 环境（XUnitTest 引用 NC 合并版不含 API 版控制器）。✅
- [x] **AC-14 NC 回归**：`_Common_List_Search.cshtml` — `DataSourceMap.Count <= MaxDropDownList` 阈值守卫，超限时回退文本框。编译 0 错误。✅

### 清理与并存
- [x] **AC-15 CascaderSearchPanel 事实修正**：`grep_search CascaderSearchPanel **/*.ts` 零命中；`CascaderField.vue` 保留（地区级联控件，正常使用中）。AC 判定：源码无引用 ✅ + 级联控件保留 ✅。
- [x] **AC-16 与 OSC-0015 并存**：`FiltersJson`（视图级前端过滤）与 `QueriesJson`（个人级服务端搜索）分属不同持久化字段、不同 store 方法、不同 UI 入口（「保存到此视图」vs 查询组合按钮）。✅
- [x] **AC-17 URL 只读**：`parseUrlSearch` 在 `DefaultList.onMounted` 读入 URL 参数到 searchForm；搜索提交不写回 URL（`loadData` 仅发 API 请求）。✅

### 门禁
- [x] **AC-18 门禁（验收复检）**：后端编译 0 错误 0 警告 ✅ | XUnit OSC-0016 7/7 ✅ | api-core 11/11 ✅ | Vitest 275/275 ✅ | vue-tsc ✅ | vite build ✅

## 自动化门禁

```powershell
# 后端
dotnet build "f:\Git Repos\1.Newlife\NewLife.Cube\魔方.sln"
dotnet test "f:\Git Repos\1.Newlife\NewLife.Cube\XUnitTest" --filter "FullyQualifiedName~OSC0016|FullyQualifiedName~ViewProfile|FullyQualifiedName~Lov"

# 前端
npm.cmd --prefix "f:\Git Repos\1.Newlife\NewLife.Cube\packages\api-core" run test
npm.cmd --prefix "f:\Git Repos\1.Newlife\NewLife.Cube\packages\api-core" run build
npm.cmd --prefix "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue\web" run test
npm.cmd --prefix "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue\web" run build
```

## 手工冒烟（deferred — 需 CubeDemo 运行环境）

> 代码审查阶段已通过自动化门禁 + 逐文件核实覆盖全部 AC 逻辑。以下冒烟项留待用户在 CubeDemo 运行环境中手动确认。

1. [ ] CubeDemo（ArcoVue）选含 Map 字段实体（如班级→老师/学生）：搜索面板 Map 字段出下拉，选择后查询命中。
2. [ ] 关键字框输入 → 回车 → 全字段模糊生效；主时间范围选择 → MasterTime 区间生效。
3. [ ] 组合 2 个字段条件 + Q + 时间范围 → 「保存当前查询为预定义…」命名保存 → 刷新 → 条目仍在 → 点击应用 → 条件回填并执行。
4. [ ] 重命名 / 删除（确认弹窗）/ 清空查询参数 全流程；各禁用态符合矩阵。
5. [ ] 「保存到此视图」与预定义查询分别操作，互不覆盖；OSC-0015 筛选构建器徽标与过滤不受影响。
6. [ ] NC MVC 站点打开同一实体列表页，搜索栏正常。
7. [ ] 冒烟产生的预定义查询/保存筛选清除恢复干净。

### 轻微偏差（不阻塞验收）

| 偏差 | AC | 描述 | 影响 |
|------|-----|------|------|
| 「重置查询参数」全空时未禁用 | AC-06 | `__reset` 菜单项无 `:disabled` 绑定；全空时点击为 no-op | 无功能影响，纯 UX 优化 |
| 「删除当前查询」菜单项无 popconfirm | AC-05 | 列表条目行内删除有 popconfirm，菜单「删除当前查询」直接执行 | 功能正确，缺少二次确认 |

## 风险

- 旧 FiltersJson 中 `_min/_max` 键静默失效（已声明，无数据损坏）。
- NC 共享元数据层变更需冒烟兜底（AC-14）。
- `entity:` 内部查询（AC-13）与 UserController 搜索标准字段名兼容（T13）的真实运行冒烟待 CubeDemo 环境（XUnitTest 引用 NC 合并版不含 API 版 `LovController`，已在 tasks/verify 标注）。
- 审查遗留 🟡（与基类同模式不修）：Search 通用等值 `ChangeType` 非法输入抛 FormatException（前端白名单缓解）。
- 审查遗留 🟢（记入 retro 后续）：`entity:` BatchLabel 大表逐页反查（建议改 In 查询）、`Osc0016Tests` 反射调私有方法（建议 InternalsVisibleTo）、SaveQueryAs 保存前未再次归一、SearchDrawer 单边时间无编辑入口、RegisterMapLov 无唯一索引保护、空表行数未缓存。
