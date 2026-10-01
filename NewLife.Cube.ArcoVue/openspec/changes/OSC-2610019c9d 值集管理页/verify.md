# OSC-2610019c9d Verify — 值集管理页

## AC-1 能打开

- [ ] AC1.1 已登录访问 `/Admin/Lov` 渲染值集表格，不出现「无法识别页面类型」。
- [ ] AC1.2 菜单 url `/Admin/Lov` 与静态路由打开同一 `DynamicPage` 分支。

## AC-2 定义

- [ ] AC2.1 新增 ENUM 定义后列表出现该编码。编辑时编码不可改。
- [ ] AC2.2 删除经确认后该行消失。取消确认不发 DELETE。
- [ ] AC2.3 列表接口失败时出现警告，页面不白屏。空列表文案为「暂无手工值集」。

## AC-3 配置

- [ ] AC3.1 ENUM 保存只提交剔除空 value 后的 `enumItems`。
- [ ] AC3.2 LIST 保存含 `listConfig`、`searchFields`、`tableColumns`。
- [ ] AC3.3 非 ENUM/LIST 时保存禁用。

## AC-4 权限

- [ ] AC4.1 权限对象仅有 Detail 时，新增、编辑、删除均不渲染。
- [ ] AC4.2 无权限对象时三个操作都渲染。

## AC-5 回归

- [ ] AC5.1 实体表单上的值集下拉仍走 Meta/ListData，本号不改这些调用点。

## 命令

```powershell
cd NewLife.Cube.ArcoVue/web
pnpm exec vitest run src/core/utils/lovAdmin.spec.ts
pnpm exec vue-tsc -b
```

预期：vitest 全部通过；vue-tsc 无错误输出。
