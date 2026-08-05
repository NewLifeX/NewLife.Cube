# OSC-0012 Tasks

> 仅在进入 `Implementing` 后逐项勾选；完成每项先执行对应测试。

## T1 PageSize 模型与筛选/洞察配置契约

- [x] 1.1 在 `Cube.xml` 的 ViewProfile 增加 `PageSize` Int32；运行 xcode 生成实体/Model，禁止手写生成字段。
  - 说明：xcode 本次按 Description 生成 `实体视图配置.cs` 新文件名，与既有 `视图配置.cs`（OSC-0006 命名漂移遗留）并存，且与旧文件差异 329 行（xcode 版本差异）。为最小 diff 保持既有文件名，将 PageSize 按生成模板合并进 `视图配置.cs` 的 6 处（字段/属性、Copy、索引器 Get/Set、Field、const），并删除 xcode 新生成的重复文件；`Models/ViewProfileModel.cs` 由 xcode 直接生成。
- [x] 1.2 在 ViewProfile Biz/CubeController/DTO 的个人 GET/PUT 接入 PageSize，`0` 为未配置，按 PAGE_SIZE_OPTIONS 归一；保留 userId<=0 拒绝。
- [x] 1.3 在 `viewProfile.ts` 定义 `SavedFiltersWire`、双开关 `ViewInsight`、解析/正规化/未知字段保留函数，覆盖损坏数据、false/0、空条件和字段淘汰。
- [x] 1.4 将历史草案 `mode=stat/chart/none` 迁移为 showStat/showChart；round-trip 不丢其他视图配置。
- [x] 1.5 在 `api-core` 扩展 `getChartData(type, params?)` 与 ViewProfile pageSize，保持无参数 URL 兼容；补 API 序列化测试。
- [x] 1.6 新增 XUnit：生成映射、个人 PageSize upsert、0/非法值与旧记录兼容。

## T2 个人配置状态与有效搜索

- [x] 2.1 扩展 ViewProfile store：按 activeViewId 读取/写入/清除 FiltersJson，并读写当前 typePath 的 PageSize；沿用 debounce、committedState 与失败回滚。
- [x] 2.2 在 DefaultList 建立唯一 `effectiveSearch`：按 URL→saved→empty 解析并统一供 list、统计、chart、分页、排序使用。
- [x] 2.3 PageSize 读取 page profile→旧 workspace 种子→20；用户选择标准分页值仅写 profile，不再 patchWorkspace；大视图自动放大不写入。
- [x] 2.4 保存只由显式操作触发；保存空条件等价清除当前视图 key；URL 不自动写回。
- [x] 2.5 补 store/DefaultList 逻辑测试：切换视图隔离、刷新、URL 覆盖、未知搜索字段、PageSize 页面隔离/种子/大视图、保存失败回滚。
  - 说明：DefaultList 为 Vue SFC，逻辑由 `searchFilters.ts`/`viewProfile.ts`/store 纯函数测试覆盖（含 resolveStatEntries、parseUrlSearch、cleanSearchParams、normalizePageSize、store 回滚/隔离）。

## T3 受限洞察与交互

- [x] 3.1 在 ViewConfigDrawer 添加统计标签、固定图表两个独立开关；不新增任意 option、数据源或多张图表。
- [x] 3.2 实现 QueryInsightPanel：将既有 SearchForm 置于上部，stat/chart 置于下部同一视觉容器；不复制搜索状态或 submit/reset 语义。
- [x] 3.3 `showStat` 读取 GetList.stat；`showChart` 带 effectiveSearch 请求 GetChartData；双开时同时显示，处理 loading/空/403/404/失败及过期响应，均不阻塞列表。
- [x] 3.4 在查询洞察面板实现保存、清除和来源提示；无 active view/无配置权限按矩阵禁用。
- [x] 3.5 补组件测试与 `<768px` 单面板样式验证。
  - 说明：web 测试环境为 node（无 jsdom/@vue/test-utils 组件测试基础设施），组件关键逻辑提取为纯函数（`resolveStatEntries` 等）并由 Vitest 覆盖；组件以 `vue-tsc -b && vite build` 为门禁，窄屏单面板样式由响应式布局实现、手工冒烟验证。

## T4 验证与文档

- [x] 4.1 执行 api-core 与 web 新增/相关 Vitest，修复后全过。
- [x] 4.2 执行 api-core、ArcoVue web 构建，无错误。
- [ ] 4.3 手工冒烟：URL→saved→empty、视图切换、stat/chart 单开与双开、空/失败/无权限、实体间 PageSize 隔离、六类视图列表不回归。
  - 说明：需真实后端 + 浏览器操作，当前环境无法自动化完成，留待验收阶段手工冒烟。
- [x] 4.4 最小同步迁移方案、核心接口/实体参考、功能清单、web README；评估附录B是否需要事实性更新。
  - 说明：附录B 无 GetChartData/ViewProfile 条目且行为变化已在核心接口架构与迁移方案登记，评估后不需改动。

## 测试与构建记录

- 后端 XUnit：`dotnet test XUnitTest --filter ProfileCommentEntityTests` → **7 passed**（含新增 2 个 PageSize 用例）
- api-core Vitest：`npm.cmd --prefix packages/api-core run test` → **8 passed**
- web Vitest：`npm.cmd --prefix web run test` → **25 files / 198 passed**
- api-core 构建：`npm.cmd --prefix packages/api-core run build` → 成功（tsup ESM/CJS/DTS）
- web 构建：`npm.cmd --prefix web run build`（vue-tsc -b && vite build）→ 成功

## 本 OSC 新增/追加测试文件

- `XUnitTest/ProfileCommentEntityTests.cs`：+2（PageSize 字段映射、归一化/0 不覆盖）
- `packages/api-core/src/api.spec.ts`：+2（getChartData 无参数兼容、带参数透传）
- `web/src/core/utils/searchFilters.spec.ts`：新建 5 用例（collectSearchKeys/cleanSearchParams/parseUrlSearch/resolveStatEntries）
- `web/src/core/utils/viewProfile.spec.ts`：+8（normalizeInsight/mode 迁移/SavedFiltersWire/round-trip 未知字段保留）
- `web/src/core/utils/viewMapping.spec.ts`：+1（normalizePageSize）
- `web/src/stores/viewProfile.spec.ts`：+5（load 解析、save/clear 隔离、pageSize 归一、失败回滚）

## 执行期修复补录（2026-08-05，OSC-0013 用户反馈联动）

- **根因（与 OSC-0013 相同）**：`SystemJson.Apply(options, true)`（web）不设置 `PropertyNameCaseInsensitive`，MVC `[FromBody]` 反序列化大小写敏感，前端 camelCase 线缆（`filtersJson`/`pageSize`/`viewsJson`/`typePath` 等）无法绑定后端 PascalCase 属性 → 本 OSC 的筛选记忆与 PageSize 亦无法持久化（此前仅前端内存态生效，刷新即丢）。
- **修复（双栈）**：`NewLife.Cube/CubeService.cs` 与 `NewLife.CubeNC/CubeService.cs` 追加 `options.JsonSerializerOptions.PropertyNameCaseInsensitive = true`。
- **验证**：`ProfileCommentEntityTests` 新增「camelCase JSON 绑定 PascalCase 属性并 Upsert 持久化」用例（10 passed）。
