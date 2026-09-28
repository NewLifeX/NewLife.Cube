# OSC-2609261fb9 Design — 角色字段矩阵

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）。角色抽屉用现有 `a-drawer` / 表单分区，不新开页面。`.vue` 薄 script，授权编辑进 `useFieldGrantEditor.ts`。不改 VTable 列模型以外的「后端没下发的字段自然不出现」。

## 0. 状态唯一来源

| 状态 | 来源 | 禁止 |
| --- | --- | --- |
| 某角色对某实体的字段授权 | 表 `FieldGrant` | ViewProfile、`ColumnsJson`、`DataField.Authority` |
| 当前用户最终可读/可写 | `FieldGrantFilter` 对用户全部角色的并集 | 前端再算一套隐藏 |
| 未配置 | 该角色+TypePath **零行** | 把零行当成「全部拒绝」 |

## 1. 存储

在 `NewLife.Cube/Entity/Cube.xml` 增加 `FieldGrant`，按现有 XCode 生成路径出实体与 Biz 空 partial。业务判断放 `FieldGrantFilter`，不堆进生成类。

| 列 | 类型 | 约束 |
| --- | --- | --- |
| Id | Int64 | 主键 |
| RoleId | Int32 | 必填 |
| TypePath | String(200) | 如 `Admin/User`，存无前导 `/` 的路径，大小写不敏感比较 |
| Field | String(64) | 物理字段名 |
| CanRead | Boolean | |
| CanWrite | Boolean | 为 true 时 CanRead 必须为 true |

唯一索引：`(RoleId, TypePath, Field)`。

保存归一化顺序：

1. TypePath 去掉前导 `/`，trim。空路径拒绝 400。
2. Field trim。空字段名丢弃该条。
3. `CanWrite=true` → `CanRead=true`。
4. 同一字段重复出现时，可读/可写取或。
5. 整包替换该 RoleId+TypePath 的旧行。传入空数组 = 删除这些行 = 回到「不裁剪」。
6. 未知 JSON 字段忽略，不写入。

## 2. 判定矩阵

用户角色集合记为 R。系统角色（`Role.IsSystem`）短路线：不裁剪，忽略其 FieldGrant。

对其余每个角色 r、实体 P：

| r 对 P 的行数 | r 的贡献 |
| --- | --- |
| 0 | 不裁剪（该角色放行全部 GetPage 字段） |
| ≥1 | 白名单：行内 CanRead/CanWrite |

用户最终结果：

| 条件 | 可读 | 可写 |
| --- | --- | --- |
| 任一角色是系统角色，或任一非系统角色对 P 零行 | 全部 | 全部 |
| 否则 | 任一角色 CanRead | 任一角色 CanRead 且 CanWrite |

可写蕴含可读。不可读则不可写。

五分区裁剪：

| 分区 | 不可读 | 可读不可写 | 可读可写 |
| --- | --- | --- | --- |
| list / search / detail | 移除 | 保留，`ReadOnly=true` | 保留 |
| addForm / editForm | 移除 | 移除 | 保留 |

行 JSON（列表、详情）：序列化前去掉不可读属性。脱敏在裁剪之后，只处理仍保留的敏感列。

写入：Insert/Update 在 `CopyFrom` 之后，把不可写属性恢复为插入默认值或更新前原值，再保存。多出来的键不导致 400。

导出：`BuildExportFields` 的结果与可读字段名求交（大小写不敏感）。`OnExportExcel` / `Csv` / `Json` / `Xml` 都走这个列表。

## 3. 文件

| 文件 | 改动 | 不动 |
| --- | --- | --- |
| `NewLife.Cube/Entity/Cube.xml` | 增加 FieldGrant | 其他表 |
| 生成的 `FieldGrant.cs` | 仅生成 | 不手改生成文件 |
| `NewLife.Cube/Common/FieldGrantFilter.cs` | 并集、裁剪、剥离 | — |
| `NewLife.Cube/Common/ReadOnlyEntityController.cs` | 去掉 GetPage、GetFields 的 `[AllowAnonymous]`；返回前裁剪 | 登录以外的 MFA 匿名接口 |
| `NewLife.Cube/Common/ReadOnlyEntityController2.cs` | 列表材料化后去掉不可读属性 | `SearchData` 的 Where |
| `NewLife.Cube/Common/EntityController.cs` | Insert/Update 剥离不可写 | `CopyFrom` 签名 |
| `NewLife.Cube/Areas/Admin/Controllers/RoleController.cs` | `GET/PUT FieldGrant` | 菜单权限树 |
| `XUnitTest/FieldGrantFilterTests.cs` | 矩阵与剥离 | — |
| `web/src/views/crud/useFieldGrantEditor.ts` | 加载、归一化、保存 | — |
| `web/src/views/crud/FieldGrantEditor.vue` | 薄壳 | — |
| `web/src/views/crud/RecordDrawer.vue` | 仅当 TypePath 为角色实体时挂编辑器 | 其他实体表单 |
| `packages/api-core/src/api.ts` | `fieldGrant.get/put` | — |
| `web/src/views/crud/fieldGrant.spec.ts` | 归一化纯函数 | — |

`GET /Admin/Role/FieldGrant?roleId=&typePath=` 需要角色 Detail。`PUT` 需要角色 Update。roleId 必须等于正在编辑的角色，不能为 0。TypePath 必须是当前用户自己有 Detail 的实体菜单，避免借角色页探测未知实体字段名；字段列表来自该实体 GetPage 的 list∪addForm∪editForm∪detail∪search 并集（调用时已按**操作者**身份取元数据，不是按被编辑角色）。操作者看不到的字段不能出现在授权表里，也不能被写成授权行（保存时丢掉这些字段名）。

## 4. 界面

角色新增/编辑抽屉底部，菜单权限树之下，增加「字段权限」：

1. 实体下拉：当前用户菜单里有 Detail 的实体，显示菜单名。
2. 选中实体后出字段表：每行「可读」「可写」两个复选框。可写勾上时可读自动勾上；取消可读时可写取消。
3. 未加载到任何行：复选框全选，旁注「未限制」。用户改动任一格后视为进入白名单编辑。提供「清除限制」按钮，保存空数组。
4. 系统角色：区块只读，文案「系统角色不裁剪字段」，不发 PUT。
5. 保存按钮与角色表单保存分开。字段权限用区块自己的保存，成功提示「字段权限已保存」。角色主表单未保存时仍可保存字段权限（只依赖 roleId，因此新增角色须先保存得到 Id，否则区块禁用并提示先保存角色）。

断点：抽屉宽度沿用记录抽屉，字段表在窄宽下纵向堆叠复选框，不出现横向滚动条要求。

## 5. GetPage 匿名与分享

去掉 `[AllowAnonymous]` 后未登录为 401。分享 embed 若现有中间件已设置当前用户，裁剪按该用户。验收若 embed 变 401，只修令牌注入用户的那一处，不恢复匿名。

## 6. 核心文档影响

| 文档 | 影响 |
| --- | --- |
| 迁移方案 §8.6 BE-B2 / BE-B3 / BE-E1、§10.4 #23 | 完成后标明本号已做裁剪与导出求交；值集旁路仍归 OSC-260926c2b8 |
| 竞品报告 §4 权限粒度、§6.2 #6 | 同步 |
| `Doc/功能清单.md` | PERM 字段能力补一行实现/测试 |

## 7. 测试设计

- 零行 → 字段集合与裁剪前相同。
- 一行只读 → 该字段在 detail，不在 editForm；Update 后值不变。
- 两角色一并集。
- 系统角色有限制行仍不裁剪。
- 空数组 PUT 后恢复零行。
- 导出列名集合等于可读集合。
- 匿名 GetPage 401。

## 8. 明确保留

`MaskSensitiveFields` 继续存在。`IFieldScope` 不改成矩阵。无 FieldGrant 数据时线上行为与本号之前一致。
