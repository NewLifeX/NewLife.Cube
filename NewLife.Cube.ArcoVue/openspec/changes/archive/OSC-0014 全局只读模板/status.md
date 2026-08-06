# Status

- id: OSC-0014
- state: Done
- updated: 2026-08-06T09:35:00+08:00
- approvedBy: openspec-approve
- trigger: "批准并执行 OSC-0014。"
- checklist: passed
- executor: openspec-apply
- tests: |
  - dotnet test XUnitTest --filter ProfileCommentEntityTests → 13 passed（含全局模板生命周期）
  - npm.cmd --prefix packages/api-core run test → 11 passed（含 template API）
  - npm.cmd --prefix NewLife.Cube.ArcoVue\web run test → 219 passed（含模板域解析/materialize/恢复）
  - dotnet build NewLife.CubeNC -f net10.0 → 0 错误
  - npm.cmd --prefix packages/api-core run build → 成功
  - npm.cmd --prefix NewLife.Cube.ArcoVue\web run build → 成功
- note: T1–T4.2/T4.4 完成；T4.3 手工冒烟留待验收阶段执行；表单域全局唯一（OSC-0013）仅回归；执行期迭代：视图域不显示独立来源徽标、恢复由视图菜单「恢复默认」承接；模板抽屉文案筛选→搜索（「当前视图存为模板」「当前搜索存为模板」）；发布模板 405 为运行后端（CubeDemo:5000）旧 DLL 未含新 API，已重编译 CubeNC 至 Bin，重启后端后生效
