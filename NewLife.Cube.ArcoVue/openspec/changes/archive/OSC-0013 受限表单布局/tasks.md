# OSC-0013 Tasks

> 仅在进入 `Implementing` 后逐项勾选；先完成 schema/generation，再改前端。

## T1 模型、生成与个人 API

- [x] 1.1 在 `Cube.xml` 的 ViewProfile 增加 FormJson，使用与既有 JSON 列一致的长度/说明约定。
- [x] 1.2 运行 xcode 生成实体和 Model，审阅生成 diff；禁止手写生成字段。
  - 说明：xcode 按 Description 生成 `实体视图配置.cs` 新文件名（与 OSC-0012 相同的命名漂移），且与旧文件存在版本差异。为最小 diff 保持既有 `视图配置.cs`，将 FormJson 按生成模板合并进 6 处（字段/属性、Copy、索引器 Get/Set、Field、const）并删除新生成重复文件；`Models/ViewProfileModel.cs` 由 xcode 直接生成。
- [x] 1.3 在 ViewProfile Biz/CubeController/DTO 对个人 profile 的 GET/PUT 透传 FormJson，保留 userId<=0 拒绝。
- [x] 1.4 增加 XUnit：生成映射、个人 upsert、null/旧数据兼容与非法主体拒绝。

## T2 前端协议与解析

- [x] 2.1 api-core ViewProfileModel 增 formJson；补 wire 测试。
- [x] 2.2 viewProfile.ts 定义 FormJson V1、canonical key、order/hidden/category 归一化、未知字段保留。
- [x] 2.3 store 增当前 mode 更新/重置及失败 rollback；三 mode 互不覆盖。
- [x] 2.4 补 Vitest：损坏 JSON、删除字段、新字段追加、大小写兼容、三模式与恢复。
  - 说明：canonical key 归一化与「删除字段忽略 / 新字段追加」由 `fieldGroups.normalizeFormLayout/applyFormLayout` 承担并由 Vitest 覆盖（字段去重、未知字段/分类剔除、未列字段原序追加）。

## T3 Drawer 与受限配置 UI

- [x] 3.1 提取 layout resolver，RecordDrawer/FormContent/详情分组共同消费排序、显隐、折叠结果。
- [x] 3.2 实现新增/编辑/详情三段配置 UI：字段拖动、显隐、Category 折叠、保存与仅重置当前 mode。
- [x] 3.3 以真实 FormContent 验证隐藏字段不改变 GetPage 权限、必填、校验和 payload；补组件测试。
  - 说明：隐藏仅作用于展示（`FormContent.visibleGroups` / 详情分组过滤），model 仍保留全部字段值，提交继续走既有 `prepareSubmitPayload`（resolveFieldsForKind 全部可提交字段），不因隐藏删除/绕过必填与校验。web 测试环境为 node（无 DOM 组件测试设施），组件关键逻辑提取为纯函数（`applyFormLayout` 等）并由 Vitest 覆盖；组件以 `vue-tsc -b && vite build` 为门禁。
- [x] 3.4 实现窄屏全宽抽屉、空字段和无配置权限状态；不加入控件/公式/条件编辑。
  - 说明：`FormLayoutDrawer` 宽度 `<768px` 时 100%；空字段显示 `a-empty`；无配置权限由入口 `v-if="flags.canEdit"` + 组件内按钮 `disabled` 双重防御；未加入任何控件/公式/条件编辑。

## T4 验证与文档

- [x] 4.1 执行相关 XUnit、api-core/web Vitest，全过后记录。
- [x] 4.2 构建 NewLife.Cube、api-core、ArcoVue web，无错误。
- [ ] 4.3 手工冒烟：Admin/User 的 add/edit/detail 布局、旧 profile、新字段、提交/验证回归。
  - 说明：需真实后端 + 浏览器操作，当前环境无法自动化完成，留待验收阶段手工冒烟。
- [x] 4.4 最小同步实体/API参考、功能清单、迁移方案与 web README。
  - 说明：附录C、核心接口架构、功能清单、web README 已登记 FormJson；迁移方案 §8.2.5 对 OSC-0013 的描述与实现一致，无需改动。

## 测试与构建记录

- 后端 XUnit：`dotnet test XUnitTest --filter ProfileCommentEntityTests` → **10 passed**（含新增 FormJson 2 + camelCase 绑定修复 1）
- web Vitest：`npm.cmd --prefix web run test` → **25 files / 210 passed**（含新增 fieldGroups 5、viewProfile FormJson 3、store 4）
- api-core 构建：`npm.cmd --prefix packages/api-core run build` → 成功（tsup ESM/CJS/DTS）
- web 构建：`npm.cmd --prefix web run build`（vue-tsc -b && vite build）→ 成功
- NewLife.Cube / NewLife.CubeNC 编译：无错误

## 本 OSC 新增/追加测试文件

- `XUnitTest/ProfileCommentEntityTests.cs`：+3（FormJson 字段映射、个人 upsert/null 不覆盖、camelCase 绑定 + Upsert 持久化）
- `web/src/core/utils/fieldGroups.spec.ts`：+5（normalizeFormLayout 字段/分类归一、applyFormLayout 排序/隐藏/折叠/空组）
- `web/src/core/utils/viewProfile.spec.ts`：+3（FormJson 解析/损坏回退、三模式 set/clear round-trip）
- `web/src/stores/viewProfile.spec.ts`：+4（load 解析、update 仅当前模式、reset 删 key、失败回滚）

## 执行期修复补录（用户反馈：布局保存后重开默认全部显示、表单不生效）

- **根因（后端）**：`SystemJson.Apply(options, true)` 第二参数为 `web`，**不设置** `PropertyNameCaseInsensitive` 与命名策略；MVC `[FromBody]` 反序列化默认大小写敏感，前端 api-core 的 camelCase 线缆（`typePath`/`formJson`/`filtersJson`/`pageSize`/`viewsJson`）**无法绑定**后端 PascalCase 的 `ViewProfileModel` 属性 → `typePath=null`（400）、`FormJson=null` → 布局/筛选/PageSize 全部无法持久化（重新打开显示默认、表单不生效）。
- **修复（双栈）**：`NewLife.Cube/CubeService.cs` 与 `NewLife.CubeNC/CubeService.cs` 在 `SystemJson.Apply` 后追加 `options.JsonSerializerOptions.PropertyNameCaseInsensitive = true`（ASP.NET Core 标准 web 实践，兼容 camelCase/PascalCase，不影响 OSC-0008 枚举数值归一化）。
- **前端加固**：`FormLayoutDrawer` 的 `watch(visible)` 加 `{ immediate: true }`，防御组件挂载时 visible 已为 true（页面加载后立即打开）导致 localLayout 空、显示全部字段。
- **样式一致（问题 1）**：`FormLayoutDrawer` 字段列表样式对齐 `ViewConfigDrawer` 字段配置（`.field-list` 边框容器 + max-height + 滚动、`.field-item` border-bottom 分隔、`.drag-handle`、隐藏字段名 `muted` 变灰而非整行 opacity）。
- **影响**：该根因同时影响 OSC-0012 的筛选记忆/PageSize 持久化（同一 ViewProfile API），已在 OSC-0012 tasks 补录。

## 执行期迭代补录（2026-08-05，用户反馈：勾选可见性不应自动保存、Tab 切换不应回到默认、底部需取消/保存）

- **改为手动保存模型**：`FormLayoutDrawer` 三模式各自维护本地编辑态（`localLayouts`），勾选可见性/拖动排序/分组折叠**只改本地、不触发保存**；`FormJson` 仅在点击底部「保存」时经 `store.setFormJson` 一次性提交三模式（`buildFormJsonWire` 过滤全空布局，未修改模式不落库）。
- **Tab 切换保留未保存修改**：移除 `watch(activeMode, loadMode)` 的重载行为——「新增 / 编辑 / 详情」切换只改当前编辑目标，不丢弃各 Tab 未保存的编辑；重新打开抽屉时（`loadAll`）才从 store 重新加载。
- **底部按钮**：新增「取消」（关闭抽屉并丢弃未保存修改）、「保存」（提交后关闭）；「恢复本模式默认布局」改为仅本地清空当前模式（保存时才提交删除）。
- **store**：新增 `setFormJson(typePath, wire, immediate)` 整体替换 FormJson 线缆；既有 `updateFormLayout`/`resetFormLayout` 保留（表单内 Category 折叠等运行时交互仍可自动保存）。
- **测试**：`viewProfile.spec.ts` +2（buildFormJsonWire 过滤空模式/全空返回空 wire）、`viewProfile.spec.ts`(store) +1（setFormJson 整体替换 + 持久化）；web Vitest **213 passed**，构建无错误。

## 执行期迭代补录（2026-08-05，用户反馈：管理员权限 + 高级子菜单 + 文案）

- **仅管理员可用**：`DefaultList` 新增 `isAdmin` computed（`userStore.userInfo?.roleName === '管理员'`，后端 `AuthController.Info()` 返回的 `Areas.Admin.Models.UserInfo.RoleName` 主要角色名经 camelCase 序列化得到）；`FormLayoutDrawer` 的 `:can-configure` 由 `flags.canEdit` 改为 `isAdmin`——其他角色即使打开抽屉也无法保存。
- **入口移入「高级」子菜单**：移除 topbar 独立的「表单布局」按钮；高级下拉（`a-dropdown`）内新增 `<a-doption v-if="isAdmin">表单布局</a-doption>`；`advancedVisible` 追加 `|| isAdmin.value`，确保仅管理员时下拉仍可见。
- **文案**：「恢复本模式默认布局」→「恢复默认布局」（按钮与 `Message.success` 提示一致）。
- **测试**：web Vitest **213 passed**，`vue-tsc` 构建无错误。

## 执行期迭代补录（2026-08-05，用户反馈：表单布局为系统全局唯一配置，作用于所有用户）

- **需求变更**：表单布局 FormJson 从「按用户个性化存储」改为「系统全局只有一份」——管理员定义后作用于所有用户的 新增/编辑/详情 表单；筛选/PageSize/视图配置仍为按用户偏好。
- **存储（后端）**：`ViewProfile` 表用 `UserId=0` 记录承载全局表单布局（唯一索引 `IU_ViewProfile_UserId_TypePath` 天然支持）。`视图配置.Biz.cs` 新增 `GlobalUserId`、`FindGlobal`、`SaveGlobalFormJson`、`DeleteGlobalFormJson`；空壳 FormJson（无 add/edit/detail 任何模式）等价于恢复默认，删除全局布局。
- **读取（双栈 `CubeController.ViewProfile` GET）**：返回模型时 `FormJson` 取 `UserId=0` 全局记录的值，其余字段取当前用户记录——所有用户读取同一份管理员配置。
- **写入（双栈 `CubeController.ViewProfile` PUT）**：`FormJson != null` 时校验管理员（`Roles.Any(e => e.IsSystem)`，否则 403），写入全局记录并置 `model.FormJson = null` 避免进入个人记录；内容未变不重复写，避免无谓更新。
- **前端 `viewProfile` store**：`saveNow` 仅管理员提交 `formJson`（`roleName === '管理员'`），非管理员不发送——避免把全局布局写回或触发 403；`reset`（恢复视图默认）改为删除用户记录后重新 `load`，保留全局表单布局而非清空。
- **注意**：`视图配置.Biz.cs` 中 JSON 解析不能用 `out _` 弃元（XCode 实体有 `_` 成员，编译报 CS0118），改用具名变量。
- **测试**：后端 XUnitTest +2（全局布局生命周期/删除，12 passed）；web `viewProfile.spec.ts` mock `./user` 为管理员 + 新增非管理员 saveNow 不带 formJson 测试（214 passed），`vue-tsc` 构建无错误。
