# Retro

## 概述

列表行内动作按元数据的 HTTP 方法执行：`dataAction` 确认后 POST，`/api/` 单元格确认后 GET 且不导航。系统角色在用户抽屉可清空密码、吊销令牌。用户在线增加「强制下线」合成列，无删除权限不显示。验收无实现缺口。浏览器点击未做，按 proposal 的单测与构建门禁通过。

## 目标达成（对照 proposal §1）

| 目标 | 结论 | 证据 |
| --- | --- | --- |
| 1 dataAction 以 POST 调用，马上执行成功后提示并刷新 | 达成 | `opsHttpMethod`；`invokeOpsLink`；`runOpsCustomLink` 成功才 `onDone` |
| 2 `/api/` 单元格发 GET，不把地址当路由 | 达成 | `isApiActionUrl`；`runCellFieldLink` |
| 3 系统角色在非新增用户抽屉可清空密码、吊销令牌 | 达成 | `showUserSecurityActions`；`RecordDrawer.vue` 底部两个按钮 |
| 4 在线列表出现强制下线，确认后 POST Kick；无 Delete 不显示 | 达成 | `UserOnlineController` 静态构造；`visibleOpsLinks`；`Osc2610011cd6OpsTests` |

## 测试与构建

- Vitest 12 通过：`opsRequest.spec.ts` 4，`listLinkFields.spec.ts` 8。
- XUnit 2 通过：`Osc2610011cd6OpsTests`。
- `vue-tsc -b` 无错误。`dotnet build NewLife.Cube -f net10.0` 0 错误。
- 未在浏览器点选马上执行、解封、清空密码、吊销令牌、强制下线。

## 实际完成范围

计划内 T1–T4。会话小任务 T5：`useFieldInput` 上传响应断言经 `unknown`，只为通过 vue-tsc。验收至复盘无新增。

## 过程中的坑

- 计划模式只能改 Markdown。第一次批准后状态已是 Implementing，业务文件要等执行模式才能写入。
- `Modal.confirm` 的类型把 `content` 标成必填。确认句放在 `title`，`content` 用空字符串才能过 vue-tsc。
- 强制下线不能在前端写死按钮，但无 Delete 时又不能显示。用 `visibleOpsLinks` 按 URL 裁掉 `/UserOnline/Kick`，其它 dataAction 不动。
- `ListFields` 是泛型基类上的受保护静态属性。测试要 `RunClassConstructor`，再沿 `BaseType` 用 `DeclaredOnly` 查找。
