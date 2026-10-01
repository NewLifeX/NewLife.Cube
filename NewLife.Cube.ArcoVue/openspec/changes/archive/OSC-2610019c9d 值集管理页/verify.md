# OSC-2610019c9d Verify — 值集管理页

## AC-1 能打开

- [x] AC1.1 已登录访问 `/Admin/Lov` 渲染值集表格，不出现「无法识别页面类型」。（`CUSTOM_TYPES` + `isLovPage` + 静态路由；执行期浏览器已打开）
- [x] AC1.2 菜单 url `/Admin/Lov` 与静态路由打开同一 `DynamicPage` 分支。

## AC-2 定义

- [x] AC2.1 新增 ENUM 定义后列表出现该编码。编辑时编码不可改。（`useLovPage` + 定义抽屉）
- [x] AC2.2 删除经确认后该行消失。取消确认不发 DELETE。（`Modal.confirm`）
- [x] AC2.3 列表接口失败时出现警告，页面不白屏。空列表文案为「暂无自定义值集」。
- [x] AC2.4 列表请求路径为 `/api/Admin/Lov`（type 带前导 `/`），不再拼成 `/apiAdmin/Lov`。

## AC-3 配置

- [x] AC3.1 ENUM 保存只提交剔除空 value 后的 `enumItems`。（`buildEnumSaveBody` 单测）
- [x] AC3.2 LIST 保存含完整 `listConfig`（method/proxy/pageable/路径等）、`searchFields`、`tableColumns`；与 Cube.Vue 字段 1:1。
- [x] AC3.3 非 ENUM/LIST 时保存禁用。（`canSaveConfig` + 抽屉 empty）
- [x] AC3.4 同源 `/` 请求地址强制 `proxyRequest=false`。

## AC-4 权限

- [x] AC4.1 权限对象仅有 Detail 时，新增、编辑、删除均不渲染。（`lovPermFlags` 单测；配置入口可读、保存禁用）
- [x] AC4.2 无权限对象时三个操作都渲染。

## AC-5 回归

- [x] AC5.1 实体表单上的值集下拉仍走 Meta/ListData，本号不改这些调用点。

## UI

- [x] 工具栏「+ 添加值集」居左，对齐实体列表「添加记录」。
- [x] 类型下拉与列表展示为「枚举 / 自定义列表」。
- [x] 空态「暂无自定义值集」。
- [x] 表头/表面样式对齐实体列表（fill-2 表头、list-panel）。
- [x] 系统启动或打开列表时幂等写入样例：是否/性别/优先级 ENUM + 用户 LIST。
- [x] 列表行高压缩；配置说明在标签下左对齐；搜索字段/表格列宽防折行。

## 命令

```powershell
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/core/utils/lovAdmin.spec.ts src/core/utils/pageKind.spec.ts
pnpm exec vue-tsc -b
dotnet test NewLife.Cube.Tests/NewLife.Cube.Tests.csproj --filter "FullyQualifiedName~LovSampleSeeds"
dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0
```

预期：vitest 全部通过；vue-tsc 无错误；LovSampleSeeds 单测通过；dotnet build 0 错误。

## 执行记录（2026-10-01）

- `lovAdmin.spec.ts` + `pageKind.spec.ts`：初版 17/17；T5 后 lovAdmin 9/9
- `vue-tsc -b`：0 错误
- T5：LIST 配置对齐 Cube.Vue；类型文案「枚举/自定义列表」
- T6：列表 API 路径补前导 `/` 修复 404；空态「暂无自定义值集」；按钮「+ 添加值集」
- T7：表头样式；`LovSampleSeeds` 四条样例
- T8：行高 / 说明文 / 列宽

## 验收阶段记录（openspec-verify，2026-10-01）

> 触发：按 OpenSpec 对 9c9d 进行验收和复盘。

### 会话小任务补录

T5–T8 已在 tasks/status 注明「已补录」；验收核对无新增。

### 实现审计

对照 proposal 目标 1–4：

| 目标 | 结论 | 证据 |
| --- | --- | --- |
| 1 `/Admin/Lov` 可开、可筛、增删改 | 达成 | 静态路由 + `isLovPage` + `useLovPage`；`LOV_TYPE='/Admin/Lov'` |
| 2 ENUM/LIST 配置走 GetConfig/SaveConfig | 达成 | `useLovConfig` + LIST 三 Tab 与 Cube.Vue 字段 1:1 |
| 3 无权限隐藏按钮；失败告警不白屏 | 达成 | `lovPermFlags`；列表/配置 alert + empty |
| 4 Meta/ListData/BatchLabel 不变 | 达成 | 本号未改这些路径 |

无 P0/P1 实现缺口。执行期扩展的样例种子已落入 T7 与 design 文件地图（doc-sync 已对齐）。

### 代码审查

无 🔴。注意：列表 type 须带前导 `/`（否则 `resolveRequestUrl` → `/apiAdmin/Lov` 404）；同源 LIST 强制 `proxyRequest=false`；配置按钮可读、保存受 `canEdit`。

### 文档同步

- 迁移方案功能对照已含「值集管理页 /Admin/Lov」。
- `web/README.md` custom 行已含 `Admin/Lov`。
- proposal §7 / design §1 已补录 `LovSampleSeeds`（执行期用户决策，非初稿范围）。

### 测试与构建（验收重跑）

```text
vitest: lovAdmin 9 + pageKind 9 = 18 passed
vue-tsc -b: exit 0
dotnet test LovSampleSeeds: 1 passed
dotnet build NewLife.Cube -f net10.0: 0 error
```

### 缺口清单

无。P2 风险：本验收未再逐条手点增删改；以执行期浏览器冒烟 + 代码路径为准。

### 验收结论

**通过**（checklist: passed）。可复盘 OSC-2610019c9d。
