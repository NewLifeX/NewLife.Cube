# OSC-0013 Design — 受限表单布局

## 1. 目标与契约边界

本号新增 `ViewProfile.FormJson`，它只改变 ArcoVue 表单/详情的显示顺序、显示开关和既有 Category 折叠状态。字段元数据、字段分区、权限、校验、控件决策和提交载荷仍由 `GetPage`、`resolveFieldsForKind`、`FormContent` 与 `prepareSubmitPayload` 权威决定。布局不是低代码表单定义。

数据库结构以 `NewLife.Cube/Entity/Cube.xml` 为唯一事实源：先增列，再运行 xcode 生成实体和 Models，业务扩展只写在 `视图配置.Biz.cs` 或控制器，禁止手改生成文件。

## 2. 文件级改动地图

| 文件 | 计划改动 | 保留不动 |
| --- | --- | --- |
| `NewLife.Cube/Entity/Cube.xml` | ViewProfile 增 `FormJson` 长文本列与说明 | `(UserId, TypePath)` 唯一索引及既有 JSON 列 |
| `NewLife.Cube/Entity/视图配置.cs`、`Models/ViewProfileModel.cs` | 由 xcode 自动生成 FormJson | 禁止人工业务逻辑改动 |
| `NewLife.Cube/Entity/视图配置.Biz.cs` | 个人 upsert 的 FormJson 白名单、旧值兼容 | `userId <= 0` 拒绝规则 |
| `NewLife.Cube/Controllers/CubeController.cs` | 将 FormJson 透传到个人 profile GET/PUT 模型 | 既有 URL、权限、个人 endpoint 语义 |
| `packages/api-core/src/types.ts` | ViewProfileModel 加 formJson | 既有可选 JSON 字段兼容 |
| `web/src/core/utils/viewProfile.ts` | FormJson schema/解析/serialize 与 layout resolver | 其他 view config 的未知字段保留 |
| `web/src/stores/viewProfile.ts` | 当前模式 FormJson 更新/恢复与失败回滚 | debounce/committedState 机制 |
| `web/src/views/crud/RecordDrawer.vue` | 传入 resolved layout，按元数据过滤、排序和 Category 折叠 | add/edit/detail mode、历史/评论 tabs |
| `web/src/components/FormContent.vue`、详情分组组件 | 消费已解析字段序列与折叠状态 | 控件、rules、提交和 FieldErrors 契约 |
| `web/src/views/crud/*Config*.vue` | 模式切换、排序、显隐、折叠和恢复默认 UI | 命名视图列/排序配置 |
| `NewLife.Cube/**Tests.cs`、`web/src/**/*.spec.ts` | 后端/前端/组件测试 | 既有元数据行为测试 |

## 3. FormJson schema 与归一化

```ts
interface FormLayout {
  order: string[]
  hidden: string[]
  collapsedCategories: string[]
}
interface FormJsonWire {
  version: 1
  add?: FormLayout
  edit?: FormLayout
  detail?: FormLayout
}
```

- 字段名均以 GetPage `FieldMeta.name` 为 canonical key；读取时按已有 Pascal/camel 规则匹配，写回 canonical name。
- `order` 去重，仅保留当前 mode 的字段；未列字段按 GetPage 原始顺序追加。
- `hidden` 去重，仅保留当前字段；不可隐藏的系统关键字段若现有产品定义，则以既有规则为准，未定义时不新造例外。
- `collapsedCategories` 只保留当前字段集中实际存在的非空 Category；同名 Category 全组折叠。
- 缺失、空串、数组、无效 JSON 或 version 不支持：归一到空布局，不抛前端错误。
- 写入采用完整 `FormJsonWire` merge：只替换当前模式，保留另两个模式和未知顶层字段。旧记录无 FormJson 时行为与 OSC-0009 完全一致。

## 4. 显示与提交矩阵

| 条件 | 显示字段 | 提交字段 | Category |
| --- | --- | --- | --- |
| 配置正常且字段未隐藏 | `order` 后的 metadata fields | 既有 `resolveFieldsForKind` 全部可提交字段 | 按配置折叠 |
| 配置隐藏字段 | UI 不渲染 | 不因隐藏而删除 model/提交字段；仍按既有 payload 规则 | 原分组保留其余字段 |
| 已删除/无权限字段 | 静默忽略 | 既有权限/字段过滤决定 | 空组不显示 |
| 新增元数据字段 | 原 order 后追加、默认显示 | 既有行为 | 进入其原 Category |
| 非法 JSON | GetPage 原序全部显示 | 既有行为 | 默认展开 |

“隐藏”是展示偏好，不是字段权限，也不能使必填字段绕过验证。若隐藏字段导致现有后端/前端必填规则无法完成，表单保持既有验证错误并在配置 UI 明确提示该字段不能通过布局规避；实施前以真实 FormContent 规则验证，不能猜测 API。

## 5. UI 契约

配置入口仅在有现有编辑权限/配置入口的个人 profile 场景出现。抽屉按 `新增 | 编辑 | 详情` 分段：

1. 标题与当前模式；
2. 当前 mode 的可排序字段列表（拖动把手、字段标题、显隐开关）；
3. Category 折叠清单；
4. 底部“恢复本模式默认布局”与“保存”。

props：`fields`、`mode`、`modelValue: FormLayout`、`canConfigure`；emits：`update:modelValue`、`save`、`reset`、`close`。窄屏 `<768px` 抽屉全宽；空字段显示空态；无权限不渲染配置入口。不得在该 UI 中出现控件类型、默认值、校验表达式或公式编辑器。

## 6. 后端与迁移

实施顺序：

1. 修改 `Cube.xml`；
2. 运行仓库现有 xcode 生成命令，审阅生成 diff；
3. 在 Biz/Controller/Model DTO 接入 FormJson；
4. 对现存库执行项目既有迁移/建表流程，验证列存在；
5. 再实现 api-core/ArcoVue。

个人 endpoint 继续调用 `UpsertForUser`，其 `userId <= 0` 拒绝不得删除。本号不增加 `Revision`；并发沿用现有 profile 最后成功保存与失败回滚行为，不能声称提供乐观锁。

## 7. 适用框架与官方资料

- 设计系统、抽屉、表单、折叠与拖拽交互：[Arco Design Vue](https://arco.design/vue/docs/start)。
- 无 VTable 行为改动；若实现涉及多维视图适配，先查 VisActor VTable [配置](https://visactor.com/vtable/option/ListTable)与[实例接口](https://visactor.com/vtable/api/Methods)。

## 8. 核心文档影响

| 文档路径 | 影响 | 说明 |
| --- | --- | --- |
| `Doc/附录C_实体参考.md` | 修改 | ViewProfile FormJson 字段事实说明 |
| `Doc/Api/核心接口架构.md` | 修改 | ViewProfile API 的 FormJson 契约 |
| `Doc/功能清单.md` | 修改 | DATA/SPA 对应能力状态 |
| `Doc/Api/ArcoVue企业中后台迁移方案.md` | 修改 | OSC-0013 和受限布局边界 |
| `NewLife.Cube.ArcoVue/web/README.md` | 修改 | 个人布局配置说明 |

## 9. 测试设计与风险

| 目标/风险 | 证据/缓解 |
| --- | --- |
| XCode 生成漂移 | Cube.xml 与生成文件编译、Model 字段断言 |
| 旧 profile | null/空/损坏 FormJson 回退 metadata 原序 |
| 三模式污染 | add/edit/detail 独立读写与恢复用例 |
| 元数据演进 | 删除字段忽略、新字段追加、大小写 key 兼容 |
| 权限/必填绕过 | 隐藏字段仍由既有 payload/validation 规则处理的集成测试 |
| 保存失败 | store rollback 与 UI 提示组件测试 |
