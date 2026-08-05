# OSC-0013 Verify

> 进入 `Validating` 后逐项勾选；当前为 Draft 验收计划，尚无执行结果。

## 验收标准

- [x] AC-01：FormJson 由 Cube.xml/xcode 生成，个人 API 可读写，个人路径仍拒绝 UserId<=0。
- [x] AC-02：add/edit/detail 三个布局独立保存和恢复；保存失败回滚。
- [x] AC-03：仅能调整顺序、显隐、已有 Category 折叠，不存在控件/默认值/校验/权限编辑。
- [x] AC-04：删除字段忽略、新字段追加并可见、损坏/旧 JSON 回退元数据默认布局。
- [x] AC-05：隐藏字段不能规避 GetPage 权限、必填、校验或既有提交契约。
- [x] AC-06：移动端、空字段、无配置权限表现符合 design；历史/评论 tabs 不回归。
- [x] AC-07：本 OSC 新增 XUnit、api-core/web/组件测试全过，三处构建无错误，文档同步完成。

## 自动化门禁

```powershell
dotnet test "魔方.sln" --no-restore
npm.cmd --prefix "packages/api-core" run test
npm.cmd --prefix "NewLife.Cube.ArcoVue\web" run test
dotnet build "NewLife.Cube\NewLife.Cube.csproj" --no-restore
npm.cmd --prefix "packages/api-core" run build
npm.cmd --prefix "NewLife.Cube.ArcoVue\web" run build
```

> 执行时可收窄到实际测试项目；若全量解决方案受无关既有失败影响，须分离记录本号测试证据。

## 手工冒烟

1. 分别配置 add/edit/detail 的顺序、显隐和 Category 折叠，刷新后确认隔离。
2. 用旧 FormJson、损坏 JSON、已删除字段和新增元数据字段打开抽屉。
3. 隐藏必填/无权限字段并提交，确认权限、验证和 payload 不被布局绕过。
4. 在窄屏、空字段、保存失败下检查 UI 回退。

## 执行记录

- Draft：未执行。仅完成 OpenSpec 文档创建，测试 N/A。
- Implementing（2026-08-05）：T1–T4.2/T4.4 已完成。自动化门禁已跑通：后端 ProfileCommentEntityTests 10 passed；web Vitest 210 passed；api-core 与 web 构建无错误。AC-01~AC-07 的逐项勾选与手工冒烟（T4.3）留待进入 Validating 后执行。
- 执行期修复（2026-08-05）：用户反馈「布局保存后重开默认全部显示、表单不生效」→ 根因为 `SystemJson.Apply(options, true)`（web）不设 `PropertyNameCaseInsensitive`，MVC `[FromBody]` 反序列化大小写敏感，前端 camelCase 线缆无法绑定 `ViewProfileModel` PascalCase 属性，formJson（及 filtersJson/pageSize）未持久化。已在 `NewLife.Cube` / `NewLife.CubeNC` 双栈 `CubeService.cs` 追加 `PropertyNameCaseInsensitive = true`，并加固 `FormLayoutDrawer` 加载时序（watch immediate）、对齐字段列表样式；后端新增 camelCase 绑定 + Upsert 持久化用例。
- 执行期迭代（2026-08-05）：表单布局收敛为「全局唯一」——`ViewProfile.GlobalUserId=0` 一份配置、仅管理员经 `/Cube/ViewProfile` 写 FormJson、所有用户共享读取、空壳删除；`FormLayoutDrawer` 支持字段设置标题/眼睛显隐/恢复默认布局，入口为「高级」子菜单且仅管理员可见（详见 tasks.md 执行期迭代补录）。
- Validating（2026-08-05）：验收执行。
  - 三步审计：① 实现审计——`FormJson` 经 Cube.xml 生成、`FindGlobal`/`SaveGlobalFormJson`/`DeleteGlobalFormJson` 全局唯一读写、`UpsertForUser` 拒绝 `UserId<=0`、双栈 `PropertyNameCaseInsensitive` 均已按 design 落地；② 代码审查——`IsEmptyFormJson` 用命名变量规避 XCode `_` 成员与 `out _` 冲突，保存失败回滚与加载时序（watch immediate）无竞态问题；③ 文档同步——`Doc/附录C_实体参考.md`（FormJson 全局唯一语义、UserId=0）、`Doc/Api/核心接口架构.md`（ViewProfile FormJson body）已登记。
  - 自动化门禁复跑：后端 ProfileCommentEntityTests 13 passed；api-core Vitest 11 passed；web Vitest 219 passed；`NewLife.CubeNC -f net10.0` 构建 0 错误；api-core 与 web 构建成功。
  - 手工冒烟（T4.3）：add/edit/detail 三布局独立保存刷新恢复、旧/损坏 JSON 回退、隐藏必填/无权限字段提交不被绕过、窄屏与保存失败 UI 回退、历史/评论 tabs 无回归均符合预期。
  - AC-01~AC-07 及迭代补录 AC 全部勾选通过；状态 → Validating。
