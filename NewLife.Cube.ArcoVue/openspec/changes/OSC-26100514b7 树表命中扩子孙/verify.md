# OSC-26100514b7 Verify — 树表命中扩子孙

## AC-1 门控与探测

- [ ] AC1.1 无 `viewKind` 或非 `tree`：不扩子孙，行为与今日一致。
- [ ] AC1.2 `viewKind=tree` 且 `ParentID`/主键同型（或 `IEntityTree`）：对命中扩子孙。
- [ ] AC1.3 非树实体 + `viewKind=tree`：不扩、不报错。
- [ ] AC1.4 `EntityTreeController`（如菜单）：短路，不双扩。
- [ ] AC1.5 `GetPage.setting.isTreeEntity` 与探测一致。

## AC-2 扩子孙语义

- [ ] AC2.1 种子命中父、子不匹配 Q/viewFilter：响应仍含全部可见子孙。
- [ ] AC2.2 不强制返回祖先；无父命中可作根展示。
- [ ] AC2.3 旁支未命中且非命中子孙：不返回。
- [ ] AC2.4 扩入行经行权过滤；不可见子孙不出现。

## AC-3 分页与封顶

- [ ] AC3.1 `TotalCount` = 命中行总数（扩前语义）。
- [ ] AC3.2 `Data.Count` 可大于 `pageSize`，且 ≤ **100000**。
- [ ] AC3.3 超过 100000：截断、记日志、HTTP 非 5xx。

## AC-4 前端

- [ ] AC4.1 树表 GetList 带 `viewKind=tree`；表格等不带。
- [ ] AC4.2 请求签名含 `viewKind`；切换视图不误复用。
- [ ] AC4.3 树表客户端复核不裁掉扩入子孙；hierarchy/`buildTree` 可展开下级。

## AC-5 回归与文档

- [ ] AC5.1 新增 XUnit / 相关 Vitest 全过；`vue-tsc` / 后端 build 无错误。
- [ ] AC5.2 迁移方案与功能清单已写明封顶 100000 与 TotalCount/Data 口径。

## 命令（执行/验收期）

```powershell
# 后端（路径以仓库为准）
dotnet test NewLife.Cube.Tests/NewLife.Cube.Tests.csproj --filter "FullyQualifiedName~TreeDescendantExpand"
dotnet build NewLife.Cube -f net10.0

# 前端
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/core/utils/listRequestSignature.spec.ts src/core/utils/viewMapping.spec.ts
pnpm exec vue-tsc -b
```

## 执行期测试记录（2026-10-05）

- XUnit `TreeDescendantExpand`：**7/7 通过**
- Vitest `listRequestSignature` + `viewMapping`：**58 通过**
- `vue-tsc -b`：**0 错误**（已重建 `@newlifex/api-core` dist）
- 后端测试工程编译含本号改动，无 error

## 手工冒烟（有登录环境）

1. 部门（或其它 ParentID 树实体）建树表视图。
2. 关键字命中某一父节点 → Network 见 `viewKind=tree` → 展开可见其全部下级。
3. 切回普通表格 → 无 `viewKind`，分页与今日一致。
