# 构建与发布

## 本地构建

在 `web/` 目录执行：

```powershell
pnpm build
```

Vite 默认将产物写入 `../wwwroot`，并生成 source map。发布前建议顺序执行：

```powershell
pnpm run type-check
pnpm run lint:eslint
pnpm run test:unit
pnpm build
```

## CI/CD 卡点

为保证 [前端可测试渐进式开发与测试规范](../standards/frontend-testable-development.md) 不被破坏，流水线加入强制卡点（工具尚未安装，首次落地需 `pnpm add -D husky lint-staged @vitest/coverage-v8`）。

1. **提交前检查（husky + lint-staged）**：`pre-commit` 运行 ESLint（`pnpm run lint:eslint` 作用于改动文件）与相关单元测试；不通过禁止提交。
2. **PR 准入检查**：CI 执行 `pnpm run check && pnpm run test:coverage`（类型检查 + 主题 token 检查 + ESLint + 覆盖率）。**硬性规则**：新增代码覆盖率低于 `vitest.config.unit.ts` 设定的门槛（statements 80 / branches 75 / functions 80 / lines 80），或导致整体覆盖率下降，PR 自动打回。**前置：`test:coverage` 脚本与 `@vitest/coverage-v8` 需先按上条（安装工具）落地；当前未装，本门禁暂不可用。**
3. **定期审查**：每个迭代结束，Tech Lead 审查覆盖率报告，识别薄弱模块并补充测试。

> 本地可等价运行 `pnpm run check`（类型检查 + 主题检查 + ESLint）预检，避免 PR 被打回。`pnpm run test:coverage` 需待上条「安装工具」落地后可用。

## 运行时配置

构建时读取 `configs/config.production.ts`。`cubeFront()` 插件会把该文件中的 `BUILD_*` 占位符注入 HTML，部署环境可写入 `window._CUBE_CONFIG_` 覆盖运行时配置。

运行时覆盖只用于公开配置，例如 API 基地址；不得把密钥、令牌或私有凭据打入前端资源。

## 微应用清单

发布前确认 `configs/microAppConfig.json` 只包含需要发布的应用，且每个应用包路径可解析并导出 `routes`。错误清单会导致微应用路由初始化失败，用户被转到 `/loading`。
