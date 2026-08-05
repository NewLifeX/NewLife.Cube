# OSC-0013 Retro

## 结果摘要

- 状态：已完成（验收通过）。
- 计划：以 FormJson 提供 add/edit/detail 独立的受限显示布局。
- 不做：字段类型/控件/默认值/校验/权限/公式/条件显示与模板发布。

## 实际结果

- **代码范围**：
  - 数据模型：`Cube.xml` 生成 `ViewProfile.FormJson` 字段。
  - 后端：`NewLife.Cube/Entity/视图配置.Biz.cs`（`FindGlobal`/`SaveGlobalFormJson`/`DeleteGlobalFormJson`/`IsEmptyFormJson`，全局唯一 `UserId=0`；`UpsertForUser` 拒绝 `UserId<=0`）、`CubeController.cs`（FormJson 管理员写全局、普通用户只读/403）、`CubeService.cs` + `NewLife.CubeNC/CubeService.cs`（`PropertyNameCaseInsensitive = true` camelCase 绑定修复）。
  - 前端：`FormLayoutDrawer.vue`（字段设置：顺序/显隐/Category 折叠、眼睛显隐、恢复默认布局、取消/保存、watch immediate 加载时序）、`viewProfile.ts`（formJson 读写与空壳删除）、`DefaultList.vue`（管理员「高级」子菜单入口）。
- **偏差**：范围经用户决策收敛为「表单全局唯一」——个人表单个性化被移除，每实体全局一份 FormJson、管理员定义、所有用户共享读取。属**范围变更**而非缺陷，已通过 AskQuestions 对齐并补录 tasks.md。
- **测试证据**：后端 `ProfileCommentEntityTests` 9→13 passed（含 camelCase 绑定 + Upsert 持久化 + 全局生命周期用例）；web 210→219；api-core 11；api-core/web 构建成功；`NewLife.CubeNC -f net10.0` 构建 0 错误。
- **文档**：`Doc/附录C_实体参考.md`（FormJson 全局唯一语义、UserId=0）、`Doc/Api/核心接口架构.md`（ViewProfile FormJson body）已登记。

## 经验沉淀候选

- 持久化字段从 Cube.xml 出发，生成代码不承载业务改动。
- 展示布局和字段元数据/提交规则必须严格分层。
- **`SystemJson.Apply(options, true)` 第二参数是 `web` 不是 camelCase**，不设 `PropertyNameCaseInsensitive`——MVC `[FromBody]` 默认大小写敏感，前端 camelCase 线缆会静默绑定失败（详见 harness/lessons.md OSC-0013 条目）。
- 全局唯一配置（`UserId=0`）是「管理员统一定义 + 全员共享」场景的最简契约：写入权限收口到系统角色、读取无鉴权差异、空壳即删除，避免模板分层复杂度。
