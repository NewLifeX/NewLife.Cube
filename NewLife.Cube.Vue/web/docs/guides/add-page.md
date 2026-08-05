# 新增页面

## 强制选择扩展层级

1. 后端已有标准实体控制器、菜单和字段元数据且默认 CRUD 足够：只配置后端能力并验证，前端不新建页面。
2. 默认 CRUD 只差搜索栏、工具栏、表格或表单等局部能力：用 `CubeTable` 具名插槽覆盖，见 [customize-page.md](./customize-page.md)。
3. 需求需要独特的信息架构、连续工作流、复杂可视化或跨实体协调：在目标应用建立 `index.vue` 完整页面（可 `useCubeEngine({ routePath })` + `:context` 自定义引擎）。
4. 需求会让多个控制器或应用受益：优先评估回流 `core/components/CubeTable/`、`core/engine/`、共享组件或字段元数据契约，不要复制多个业务页面。

不要先手写 `router.addRoute()`；菜单动态路由与页面解析已在框架处理。
完整决策树见 [愿景与路线图](../product/vision-and-roadmap.md)；AI 的执行顺序见 [ai-iteration.md](./ai-iteration.md)。

## 新建自定义页面

1. 确认目标应用已列入 `configs/microAppConfig.json`，并有 `apps/<app>/src/main.ts` 导出 `routes`。
2. 按菜单路径在 `apps/<app>/src/views/` 下新建目录和 `index.vue`。例如 `/Admin/User` 对应 `apps/cube-admin/src/views/admin/user/index.vue`。
3. 页面结构按 [UI 规范](../standards/ui-spec.md) 选择类型和组件：页面级布局用 Tailwind，交互组件优先 Element Plus。
4. 后端菜单配置该路径；用户登录后菜单加载会触发动态路由注册。
5. 在开发环境验证菜单可见、首次直达、刷新后仍可命中和无权限行为。

## 视图解析规则

菜单动态路由会在 `apps/*/src/views/**/index.vue` 查找匹配目录，兼容 PascalCase、kebab-case 和小写路径。业务应用视图优先于框架内置视图；未匹配时回退默认列表页。

## 验收

- [ ] 页面只在有菜单权限时可访问。
- [ ] 首次访问和刷新都能解析到正确组件。
- [ ] 不存在同一路径的多份业务视图。
- [ ] 页面遵守 UI、API 与测试规范。
