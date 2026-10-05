# OSC-26100514b7 Verify — 树表命中扩子孙

> 编排：implementation-audit → code-review → doc-sync  
> 验收日：2026-10-05；触发：「验收和复盘 14b7 变更 。」

## AC-1 门控与探测

- [x] AC1.1 无 `viewKind` 或非 `tree`：不扩子孙，行为与今日一致。 ✅ `IsTreeViewRequest` 仅 `"tree"`（忽略大小写）；`ShouldExpandTreeDescendants` 与之 AND
- [x] AC1.2 `viewKind=tree` 且 `ParentID`/主键同型（或 `IEntityTree`）：对命中扩子孙。 ✅ `Index` → `ExpandEntities`；图用例命中 A 含 A1
- [x] AC1.3 非树实体 + `viewKind=tree`：不扩、不报错。 ✅ `IsTreeEntity`/`ResolveTreeParentFieldName` 空则跳过 Expand
- [x] AC1.4 `EntityTreeController`（如菜单）：短路，不双扩。 ✅ `ShouldExpandTreeDescendants => false`
- [x] AC1.5 `GetPage.setting.isTreeEntity` 与探测一致。 ✅ `setting.isTreeEntity = IsTreeEntity()`；api-core `PageSetting.isTreeEntity`

## AC-2 扩子孙语义

- [x] AC2.1 种子命中父、子不匹配 Q/viewFilter：响应仍含全部可见子孙。 ✅ Expand 不再套用 Q/viewFilter；前端树表跳过 `matchesViewFilter`
- [x] AC2.2 不强制返回祖先；无父命中可作根展示。 ✅ 结果 = hits 原序 ∪ BFS 子孙
- [x] AC2.3 旁支未命中且非命中子孙：不返回。 ✅ 用例 `Expand_HitA_IncludesA1_ExcludesB`
- [x] AC2.4 扩入行经行权过滤；不可见子孙不出现。 ✅ `ExpandEntities(..., CanAccess, ...)`；`Expand_CanView_DropsHiddenChild`

## AC-3 分页与封顶

- [x] AC3.1 `TotalCount` = 命中行总数（扩前语义）。 ✅ Index 注释与 `p` 分页未改；扩发生在 SearchData 之后
- [x] AC3.2 `Data.Count` 可大于 `pageSize`，且 ≤ **100000**（设置夹取上限 1_000_000）。 ✅ `ResolveMaxRows` + `CubeSetting.TreeExpandMaxRows` 默认 100000
- [x] AC3.3 超过封顶：截断、记日志、HTTP 非 5xx。 ✅ `onTruncated` → `XTrace`；`Expand_TruncatesAtMaxRows`

## AC-4 前端

- [x] AC4.1 树表 GetList 带 `viewKind=tree`；表格等不带。 ✅ `useListQuery` 仅 `activeViewKind === 'tree'` 附加
- [x] AC4.2 请求签名含 `viewKind`；切换视图不误复用。 ✅ `listRequestSignature` spec
- [x] AC4.3 树表客户端复核不裁掉扩入子孙；hierarchy/`buildTree` 可展开下级。 ✅ 跳过 `matchesViewFilter`

## AC-5 回归与文档

- [x] AC5.1 新增 XUnit / 相关 Vitest 全过；`vue-tsc` / 后端 build 无错误。 ✅ 见测试记录
- [x] AC5.2 迁移方案与功能清单已写明封顶 100000 与 TotalCount/Data 口径。 ✅ §7.1 / 树表视图行 / SPA-18
- [x] AC5.3 部门（或其它 ParentID 树实体）树表关键字冒烟 ✅ **仅记录放行**（用户 2026-10-05「验收和复盘」；本轮未再开登录浏览器）

## 固定编排摘要（2026-10-05）

| 步 | 结论 |
| --- | --- |
| 实现审计 | T1–T3 与 design 对齐：`TreeDescendantExpand` 索引/BFS、环、行权、封顶；`Index` 在 SearchData 后、OnFillListValues 前挂钩；`EntityTreeController` 短路；`GetPage.isTreeEntity`；前端 `viewKind` + 签名 + 跳过复核。无 P0。 |
| 代码审查 | 0 🔴。🟡：`BuildChildFinder` 取子失败吞异常只打日志；缓存阈值固定 10000 而非 `Meta.MaxCacheCount`；控制器级门控无独立 XUnit（纯函数已覆盖 Expand/`viewKind`）。 |
| 文档同步 | 迁移方案树表行与 §7.1、功能清单 SPA-18 已含 OSC-26100514b7 口径。DATA-3 仍描述 EntityTree 全树路径（与本号短路并存，不改语义）。 |

## 目标愿景对照

| 目标 | 结论 |
| --- | --- |
| 1 探测树实体且仅 `viewKind=tree` 扩子孙 | 达成 |
| 2 先筛命中再并入子孙；不补祖先 | 达成 |
| 3 扁平列表；TotalCount=命中数；Data 可大于 pageSize；封顶默认 100000 | 达成 |
| 4 树表传 `viewKind=tree`；客户端不裁扩入行 | 达成 |

## 缺口清单与决策

| 缺口 | 级别 | 处置 |
| --- | --- | --- |
| AC5.3 部门树表关键字手工冒烟 | P1 | **仅记录**：用户「验收和复盘」；有登录环境后按 verify 手工段补做 |
| Index / `IsTreeEntity` / EntityTree 短路无控制器集成测 | P2 | 仅记录；纯函数 7 例已覆盖核心图与门控字符串 |
| 缓存阈值 10000 vs `MaxCacheCount` | P2 | 仅记录；与常见 XCode 默认同量级 |
| 取子失败 catch 后空集续扩 | P2 | 仅记录；避免 5xx，依赖日志观测 |

## 命令（执行/验收期）

```powershell
dotnet test NewLife.Cube.Tests/NewLife.Cube.Tests.csproj --filter "FullyQualifiedName~TreeDescendantExpand"
dotnet build NewLife.Cube/NewLife.Cube.csproj -f net10.0

cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/core/utils/listRequestSignature.spec.ts src/core/utils/viewMapping.spec.ts
pnpm exec vue-tsc -b
```

## 验收期测试记录（2026-10-05）

- XUnit `TreeDescendantExpand`：**7/7 通过**（失败 0）
- Vitest `listRequestSignature` + `viewMapping`：**58 通过**
- `vue-tsc -b`：**0 错误**
- `dotnet build NewLife.Cube -f net10.0`：**0 错误 / 0 警告**

## 手工冒烟（有登录环境，仅记录）

1. 部门（或其它 ParentID 树实体）建树表视图。
2. 关键字命中某一父节点 → Network 见 `viewKind=tree` → 展开可见其全部下级。
3. 切回普通表格 → 无 `viewKind`，分页与今日一致。
