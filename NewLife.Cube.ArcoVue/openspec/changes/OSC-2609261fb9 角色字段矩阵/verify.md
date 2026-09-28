# Verify

验收命令：

- `dotnet test` 中筛选 `FieldGrantFilterTests`
- `web` 目录 `npx vitest run src/views/crud/fieldGrant.spec.ts`
- `dotnet build`（NewLife.Cube）
- `npx vue-tsc --noEmit`（web）

## AC

- [ ] AC1 角色对某实体零行 FieldGrant 时，GetPage 的 list 字段名与配置前一致。
- [ ] AC2 仅一行且只读：该字段在 detail，不在 editForm；PUT 原值不变，响应不是 400。
- [ ] AC3 两个非系统角色分别授权不同字段时，用户同时具备两字段的读权。
- [ ] AC4 系统角色即使写了限制行，GetPage 仍不裁剪。
- [ ] AC5 PUT 空数组后，该角色+TypePath 零行，列表字段恢复。
- [ ] AC6 `CanWrite=true` 且请求里 `CanRead=false` 时，落库 CanRead 为 true。
- [ ] AC7 导出列集合等于可读字段集合，不含不可读列。
- [ ] AC8 未登录 GetPage 为 401。
- [ ] AC9 新增角色尚未保存时，字段权限区块禁用。
- [ ] AC10 系统角色抽屉中字段权限只读，不出现可保存的复选框变更。
- [ ] AC11 有效 embed 令牌仍能打开列表；若失败，只允许修令牌用户注入，GetPage 不得恢复 `[AllowAnonymous]`。

## 必须保留

- 无 FieldGrant 数据时，现有实体页字段与导出列不变。
- `MaskSensitiveFields` 仍在输出前执行。
- ViewProfile 藏列不写入 FieldGrant。
