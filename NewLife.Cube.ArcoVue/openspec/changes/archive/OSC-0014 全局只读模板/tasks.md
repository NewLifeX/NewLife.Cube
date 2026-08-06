# OSC-0014 Tasks

> 仅在 OSC-0012、OSC-0013 完成并进入 `Implementing` 后逐项勾选。表单域已按「全局唯一」落地（OSC-0013），本号仅对其回归；模板/个人覆盖仅作用于视图/筛选域。

## T1 后端模板隔离、授权与审计（视图/筛选域）

- [x] 1.1 在 ViewProfile Biz 增视图/筛选模板 find/upsert/delete；固定 UserId=0，仅接受批准 JSON 域，保留个人 `userId<=0` 拒绝；表单全局 FormJson 沿用 `SaveGlobalFormJson`/`DeleteGlobalFormJson`（OSC-0013）。
- [x] 1.2 在 CubeController 增独立 Template GET/PUT/POST/DELETE（视图/筛选域）；以 `Roles.Any(e => e.IsSystem)` 授权，验证 typePath，忽略请求 UserId/Id；FormJson 不属模板域，走 `/Cube/ViewProfile` 全局唯一逻辑。
- [x] 1.3 接入既有 Cube 操作日志：发布/删除成功均记录 action、typePath、操作者、结果，不记录完整 JSON。
- [x] 1.4 增 XUnit：系统管理员成功、普通用户/匿名拒绝、个人 endpoint 不能写 0、审计、空/损坏模板与个人记录隔离；表单全局唯一回归（管理员写、普通用户 403、空壳删除）。

## T2 api-core 与视图/筛选 resolver（表单全局唯一）

- [x] 2.1 增 template API 模型、路径和 PUT→POST 兼容测试，不破坏个人 profile API。
- [x] 2.2 在 viewProfile.ts 定义视图/筛选 domain presence、`personal > template > system` 解析与来源信息；ViewsJson/FiltersJson 禁止跨域 merge；表单域直接读全局 FormJson，不参与三层解析。
- [x] 2.3 在 store 保存 raw personal/raw template/resolved（视图/筛选域）；实现 per-domain materialize、恢复和失败 rollback，禁止把 resolved 值误写全部个人域；表单域沿用 `setFormJson` 全局提交（仅管理员）。
- [x] 2.4 补 Vitest 九格继承矩阵（视图/筛选）、模板更新不覆盖个人、首次个人化、恢复、旧/损坏 JSON 与未知字段 round-trip；表单全局唯一来源/只读回归。

## T3 前端管理员与用户体验

- [x] 3.1 视图域恢复由视图菜单「恢复默认」承接；不显示独立来源徽标（表单布局入口已按 OSC-0013 仅管理员可配置、普通用户只读共享）。
- [x] 3.2 实现管理员专用模板管理抽屉（视图/筛选域）：读取、发布、删除、确认、空/失败/窄屏状态；普通用户不渲染入口。
- [x] 3.3 对模板发布后的当前页面做安全刷新：模板来源域更新，个人来源域不变；表单全局布局更新即全量生效；CRUD/权限仍由 GetPage。
- [x] 3.4 补组件测试：系统管理员/普通用户、视图/筛选来源、发布/删除、加载/保存失败及移动端；表单布局全局唯一只读回归。

## T4 验证与文档

- [x] 4.1 执行后端 XUnit、api-core 与 web 测试，新增测试全部通过。
- [x] 4.2 构建 NewLife.Cube、api-core、ArcoVue web，无错误。
- [ ] 4.3 手工冒烟：管理员发布视图/筛选模板→普通用户使用→个人化视图/筛选域→模板更新（仅未个人化域变化）→恢复回落；管理员发布表单布局→所有用户全局生效→空壳恢复默认。（留待验收阶段执行）
- [x] 4.4 最小同步核心接口、附录B/附录C、功能清单、迁移方案与 web README。
