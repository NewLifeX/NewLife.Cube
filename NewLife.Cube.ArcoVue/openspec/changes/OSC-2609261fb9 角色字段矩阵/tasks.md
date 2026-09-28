# Tasks

- [ ] T1 `Cube.xml` 增加 `FieldGrant` 并生成实体；唯一索引 `(RoleId, TypePath, Field)`。不手改生成类。
- [ ] T2 `FieldGrantFilter`：零行不裁剪、有行白名单、多角色并集、系统角色短路线、`CanWrite` 蕴含 `CanRead`。
- [ ] T3 去掉 `GetPage` 与 `GetFields` 的 `[AllowAnonymous]`；未登录 401；返回前按五分区裁剪。
- [ ] T4 列表/详情 JSON 去掉不可读属性；脱敏仍在裁剪之后。
- [ ] T5 `EntityController` Insert/Update 在 `CopyFrom` 后恢复不可写属性；不因此 400。
- [ ] T6 `BuildExportFields` 与可读字段求交，四种导出格式共用。
- [ ] T7 `RoleController` `GET/PUT FieldGrant`：Detail/Update；空数组删除该角色+TypePath 的行；操作者自己不可见的字段名丢弃。
- [ ] T8 `XUnitTest/FieldGrantFilterTests.cs` 覆盖 design §7。
- [ ] T9 `fieldGrant.ts` 归一化纯函数 + spec；`useFieldGrantEditor` + 薄 `FieldGrantEditor.vue`。
- [ ] T10 角色抽屉：未保存无 Id 时禁用；系统角色只读；「清除限制」提交空数组；`api-core` 增加 get/put。
- [ ] T11 跑新增单测；`dotnet build` 与 `vue-tsc` 无错误。
- [ ] T12 手工：未配置角色字段不变；白名单后列表/详情/导出缺列且提交不改不可写列；匿名 GetPage 401；embed 若 401 则只修令牌用户注入。
- [ ] T13 回写迁移方案 §8.6 BE-B2/B3/E1、§10.4 #23 与竞品报告权限行。
