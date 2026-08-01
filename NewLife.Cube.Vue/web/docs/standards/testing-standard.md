# 测试规范

## 分层

| 层             | 工具            | 适用范围                                           |
| -------------- | --------------- | -------------------------------------------------- |
| 单元/组件      | Vitest + jsdom  | 字段转换、composable、请求辅助、组件渲染和边界条件 |
| E2E            | Playwright      | 登录、菜单路由、默认 CRUD 和跨页面用户流程         |
| 类型与静态检查 | vue-tsc、ESLint | Vue/TS 类型、导入与代码质量                        |

## 最低要求

- 改变纯函数、字段映射、请求/分页规则时，新增或更新 Vitest 测试。
- 改变页面交互、路由守卫、菜单路径或默认 CRUD 流程时，覆盖对应 Playwright 场景；无法自动化时在 PR/变更说明记录人工验证步骤。
- 修复 bug 时，先补能复现该 bug 的最窄测试，除非环境确实无法建立。

## 命令

```powershell
pnpm run test:unit
pnpm run test:e2e
pnpm run type-check
pnpm run lint:eslint
```

`vitest.config.unit.ts` 使用最小 Vite 配置与 `jsdom`，避免全量框架插件干扰测试；新增单元测试放在 `core/__tests__/` 或与被测 `core` 模块相邻的 `*.spec.ts`/`*.test.ts`。
