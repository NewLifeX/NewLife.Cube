# Vue、TypeScript 与样式约定

## 代码边界

- 默认模板和共享能力修改在 `core/`；业务页面和覆盖组件修改在 `apps/<app>/`。
- 根 `src/` 是遗留壳，不是当前默认模板的实现位置。
- 优先扩展已有 composable、业务组件和 Section；不要为局部页面复制框架逻辑。

## Vue 与 TypeScript

- 使用 Vue 3 Composition API 与 `<script setup lang="ts">`。
- Props、emits、请求结果和跨模块数据必须有显式 TypeScript 类型。
- 类型导入使用 `import type`。
- 组件使用 PascalCase；业务视图目录按路由约定，入口文件为 `index.vue`。
- 自动发现的 Section 文件首字母大写，且名称必须存在于 `SectionKeyMap`。

## 状态与副作用

- 组件局部状态留在组件/composable；跨页面共享状态使用 Pinia store。
- 网络请求统一走 `core/utils/request.ts`；不要在页面中另建 Axios 实例。
- 标准 API 响应、分页和错误处理遵守 [api-contract.md](./api-contract.md)。
- 异步代码使用 `async`/`await`，边界处处理异常；不要吞掉错误后继续假装成功。

## 样式

- 页面级排版用 Tailwind；交互组件优先 Element Plus。
- 样式必须遵守 [ui-spec.md](./ui-spec.md)，主题相关值通过 `--el-*` 或映射的 Tailwind 语义类获取。
- 组件私有样式只处理 Tailwind/Element Plus 无法表达的局部结构，不写主题色硬编码。
- 不添加新的全局 token、全局类或覆盖规则，除非先有 ADR 说明其共享价值。

## 格式与检查

在 `web/` 执行：

```powershell
pnpm run type-check
pnpm run lint:eslint
pnpm run test:unit
```

全量 `check` 会顺序执行类型检查和 ESLint；提交前至少运行与改动范围匹配的命令。
