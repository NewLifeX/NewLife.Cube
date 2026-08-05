# OSC-0014 Verify

> 进入 `Validating` 后逐项勾选；当前为 Draft 验收计划，尚无执行结果。

## 验收标准

- [ ] AC-01：模板只由专用 endpoint 管理，固定 UserId=0；个人 endpoint/Biz 路径继续拒绝 userId<=0。
- [ ] AC-02：仅系统管理员可 GET/PUT/POST/DELETE 模板；匿名和普通用户无读取管理态或写入权限。
- [ ] AC-03：发布/删除模板均记录既有 Cube 操作日志，日志不泄露完整 JSON。
- [ ] AC-04：ViewsJson、FiltersJson 各自按 personal→template→system 整体选取，不发生字段级或跨域合并；FormJson 为全局唯一一份（OSC-0013），不参与三层解析。
- [ ] AC-05：视图/筛选域个人首次保存仅 materialize 该域；模板后续更新不覆盖个人域；表单域无个人层，管理员发布即全局生效。
- [ ] AC-06：恢复视图/筛选个人域后即时回落模板/系统；删除模板后未个人化域回落系统默认；表单空壳恢复即回落系统默认布局。
- [ ] AC-07：模板不能改变 GetPage 字段/权限、数据权限或 CRUD payload；普通用户没有模板编辑入口，也没有表单布局写入口（OSC-0013）。
- [ ] AC-08：模板/个人 JSON 损坏、无模板、加载/保存失败、窄屏均安全降级。
- [ ] AC-09：本 OSC 新增 XUnit、api-core/web/组件测试全过，构建无错误，文档同步完成。

## 自动化门禁

```powershell
dotnet test "魔方.sln" --no-restore
npm.cmd --prefix "packages/api-core" run test
npm.cmd --prefix "NewLife.Cube.ArcoVue\web" run test
dotnet build "NewLife.Cube\NewLife.Cube.csproj" --no-restore
npm.cmd --prefix "packages/api-core" run build
npm.cmd --prefix "NewLife.Cube.ArcoVue\web" run build
```

> 执行阶段可收窄实际测试项目；全量既有失败须与本 OSC 新增测试明确分离。

## 手工冒烟

1. 系统管理员为一个 typePath 发布/更新/删除视图/筛选模板，确认审计记录与 API 返回。
2. 普通用户打开实体，确认视图/筛选来源和模板生效；分别保存视图、筛选个人域；表单布局为全局唯一（管理员发布后所有用户生效，普通用户只读）。
3. 管理员更新模板，确认仅未个人化域变化；用户恢复单域后回落模板；管理员更新表单布局即全局生效。
4. 删除模板，确认未个人化域回落系统默认；删除表单全局布局回落系统默认；测试匿名/普通用户直接调模板 API 的拒绝。

## 执行记录

- Draft：未执行。仅完成 OpenSpec 文档创建，测试 N/A。
