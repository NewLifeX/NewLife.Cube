# OSC-0002 Retro

> 复盘时间：2026-08-01  
> 终态：Done

## 做得好的

- 建模路径清晰：Cube.xml 一次三表 → xcode 生成 → Biz/API，避免手写实体骨架。
- 三套 `/Cube` API 语义与迁移方案 §5.3 对齐；Cube + CubeNC 同步，csproj Link 齐全。
- 实体层业务可单测：内存 SQLite 隔离 `ProfileCommentEntityTests`（4）覆盖绑定、隔离、回复、删权。
- 验收重跑单测 + build，门禁可执行；文档 SPA-17/18/19 与核心接口架构同步落地。

## 偏差与根因

- design 写「无 Token → 401」XUnit，实现落在控制器层，测试只覆盖实体 `userId<=0`——根因：项目缺轻量 API 宿主测样板，优先保实体契约可测。记入 lessons，不回退 Implementing。
- proposal 成功标准 checkbox 未在执行期勾选——状态以 tasks/verify 为准。

## 测试与质量

- 新增：`XUnitTest/ProfileCommentEntityTests.cs`（4）验收全过；`dotnet build` NewLife.Cube / CubeNC net10.0 无 error。
- 残余：HTTP 401、Unique 冲突插入未直接断言；删评论不级联需前端容错。

## 后续 OSC 建议

- OSC-0003/0004：可并行开发；合并与消费顺序仍 0002 优先。
- OSC-0004：对接 UserProfile；联调时验证 GET/PUT 登录态。
- OSC-0005 / OSC-0008：分别消费 EntityViewProfile / EntityComment；评论 UI 处理已删父节点。
- 可选：补 CubeController Profile/Comment 的轻量 API 测试（401/400）。
