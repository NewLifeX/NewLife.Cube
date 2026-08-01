# OSC-0002 Verify

> 状态：通过（openspec-verify）  
> 时间：2026-08-01T07:35+08:00

## AC 对照

| AC | 结果 | 说明 |
|----|------|------|
| Cube.xml 三表 + 索引 | ✅ | `UserProfile` Unique(UserId)；`EntityViewProfile` Unique(UserId,TypePath)；`EntityComment` (Category,LinkId)+ParentId/RootId/CreateUserId |
| XCode 生成物与 xml 一致 | ✅ | `用户呈现配置*.cs` / `实体视图配置*.cs` / `实体评论*.cs` + Models；BindTable/BindIndex 与 xml 对齐；业务仅在 `.Biz.cs` |
| 三套 API 语义 | ✅ | `CubeController`（Cube + CubeNC）路径与 design 一致；CurrentUser 空→401；当前用户绑定；Comment 同表回复/删权 |
| 本 OSC 新增 XUnit 全过 | ✅ | `ProfileCommentEntityTests` 4 passed |
| dotnet build 无错误 | ✅ | NewLife.Cube + NewLife.CubeNC(net10.0) 0 error |
| 核心接口架构 / 功能清单 | ✅ | §2.2 三路径；SPA-17/18/19 已回写 |

## 测试验证记录

```text
dotnet test XUnitTest/XUnitTest.csproj --filter "FullyQualifiedName~ProfileCommentEntityTests"
已通过! - 失败: 0，通过: 4，已跳过: 0，总计: 4，持续时间: 812 ms
```

用例：UserProfile Upsert 绑定用户；EntityViewProfile typePath 隔离+Delete；EntityComment 列表/回复/删权；userId<=0 拒绝。

## 构建记录

```text
dotnet build NewLife.Cube/NewLife.Cube.csproj
已成功生成。0 个错误（既有 warning 若干）

dotnet build NewLife.CubeNC/NewLife.CubeNC.csproj -f net10.0
已成功生成。0 个错误
EXIT=0
```

## 验收三连摘要

### 1. implementation-audit

- 范围落地：Cube.xml → 生成实体/Model → `/Cube/*` API → XUnit → 文档；CubeNC 链接与 API 同步。
- 未越界：无 ArcoVue UI、无 localStorage、无 VTable/抽屉。
- 缺口（不阻塞）：无 HTTP 宿主级 401 单测（控制器有 `CurrentUser==null → Json(401)`；实体层覆盖 userId<=0）；DB Unique 由索引声明+Upsert 路径保证，未做冲突插入断言。

### 2. code-review

- 业务集中在 `.Biz`：`UpsertForUser` 强制 UserId；Comment `AddComment` 校验父评论同 category/linkId；`TryDelete` 本人或管理员、不级联。
- JSON 列 `Length=-1`，避免列布局过短。
- 低优先：`EntityViewProfile.UpsertForUser` 允许用 model.TypePath 回写键字段；当前控制器以 body.TypePath 同时作查找键，风险低，消费方可保持单字段契约。

### 3. doc-sync

- `Doc/Api/核心接口架构.md` 高级表已登记三路径。
- `Doc/功能清单.md` SPA-17/18/19 + 测试列已回写。
- 迁移方案 §5.2.1 / §5.3 字段与路径一致，无需破坏性回写；里程碑勾选在复盘时更新。

## 风险

- HTTP 401 / 全链路鉴权依赖控制器 `CurrentUser`，建议 OSC-0004 联调或后续补 API 宿主测。
- Comment 删父不级联：子回复 ParentId 可能指向已删节点，前端 OSC-0008 需容错展示。

## Checklist

- checklist: **passed**
- 可进入复盘：`复盘 OSC-0002`
