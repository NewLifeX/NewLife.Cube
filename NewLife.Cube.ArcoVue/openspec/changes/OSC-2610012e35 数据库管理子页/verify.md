# OSC-2610012e35 Verify — 数据库管理子页

## AC-1 接口

- [x] AC1.1 连接名不在配置中时四个动作 code 非 0，响应体不是 HTML。（`Osc2610012e35DbTests`）
- [x] AC1.2 ShowTables / ShowEntities / ModelDiff 的 data 含 design §2 的数组字段。无差异时 tables 为空数组。（实现审阅 + `ShowTables` SQLite 测试）
- [x] AC1.3 Compact 成功 message 为「压缩完成」。异常时 code 为 1 且 message 非空。（实现审阅）
- [x] AC1.4 无 Detail 调用前三个被拒绝；无 Update 调用 Compact 被拒绝（沿用 EntityAuthorize，测试可反射特性）。（实现审阅）

## AC-2 页面

- [x] AC2.1 卡片在下载架构之后有表、实体、差异、压缩。（实现审阅）
- [x] AC2.2 表与实体抽屉列出名称、表名、行数。差异多列展成多行。无差异显示「无差异」。（`dbPage.spec.ts` + 实现审阅）
- [x] AC2.3 压缩有确认；取消不发请求。（实现审阅；请求仅在 `Modal.confirm.onOk` 中发起）
- [x] AC2.4 权限对象只有 Update 时不显示表/实体/差异，显示压缩。无权限对象时四个都显示。（`dbPage.spec.ts`）

## AC-3 回归

- [x] AC3.1 备份、备份并压缩、下载架构仍可用。（实现审阅；既有调用保持不变）
- [x] AC3.2 CubeNC 的 Razor `Db/Index.cshtml` 未被本号删除或改成 JSON。（变更审阅）

## 命令

```powershell
dotnet test "NewLife.Cube.Tests/NewLife.Cube.Tests.csproj" --filter Osc2610012e35
dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/core/utils/dbPage.spec.ts
pnpm exec vue-tsc -b
cd ../../packages/api-core
pnpm exec vitest run src/api.spec.ts
```

预期：过滤测试、dbPage spec、api.spec 通过；build 与 vue-tsc 无错误。csproj 路径以仓库实际文件名为准。

## 执行记录

- `dotnet test NewLife.Cube.Tests/NewLife.Cube.Tests.csproj --filter Osc2610012e35`：9/9 通过（含显式 API 路由回归）。
- `dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0`：0 错误。
- `pnpm exec vitest run src/core/utils/dbPage.spec.ts`：2/2 通过；`pnpm exec vitest run src/api.spec.ts`：40/40 通过。
- `pnpm exec vue-tsc -b`：通过。
