# 测试改动

## 选择测试层

| 改动                                 | 首选验证               |
| ------------------------------------ | ---------------------- |
| 字段类型、分页、响应处理、工具函数   | Vitest 单元测试        |
| 默认页面/Section 组件渲染            | Vitest 组件测试        |
| 组件 UI（template、CSS、props 行为） | Playwright CT 截图对比 |
| 登录、菜单、路由、完整 CRUD          | Playwright E2E         |
| Vue/TypeScript 类型或导入            | `pnpm run type-check`  |
| 代码风格                             | `pnpm run lint:eslint` |

## 命令

```powershell
# Vitest 单元/组件测试
pnpm run test:unit

# Playwright 组件视觉测试（CT）
pnpm run ct:server          # 启动 gallery，手动浏览器预览组件
pnpm run test:ct            # 无头模式，截图对比基线
pnpm run test:ct:headed     # 有头模式
pnpm run test:ct:update     # 更新基线截图
pnpm run test:ct:dev        # 开发循环：有头 + 截图存档

# Playwright E2E
pnpm run test:e2e

# 静态检查
pnpm run type-check
pnpm run lint:eslint
```

Vitest 使用 `vitest.config.unit.ts` 的最小配置，测试文件放在 `core/__tests__/` 或相邻 `*.spec.ts`/`*.test.ts`。  
Playwright CT 使用 `playwright-ct.config.ts`，测试文件为 `*.ct.spec.ts`，通过 gallery 模式渲染组件。  
Playwright E2E 默认前端地址为 `http://localhost:5187`，后端地址由 `PLAYWRIGHT_API_URL` 或配置文件默认值控制。

## 编写原则

- 测试行为与边界，不测试实现细节。
- 修复 bug 时优先增加能稳定复现的最小测试。
- E2E 不使用固定等待；等待具体请求、路由或可观察 UI 状态。
- CT 截图测试开发期用 `--update-snapshots` 只拍照不对比，确认效果后用 `pnpm test:ct:update` 确立基线。
- 环境不可用导致无法跑测试时，说明阻塞条件和已执行的最窄静态检查。
