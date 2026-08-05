# 默认页面引擎参考

## 目标

默认页面引擎让后端实体元数据驱动前端列表、搜索和编辑表单。标准实体页面无需为每个控制器新建 Vue 页面。

## 目标流程（ADR 0005，逐步落地）

```text
菜单路径
  -> DefaultEntity.vue（菜单匹配 catch-all）
  -> core/views/index.vue（目标态：单个 <CubeTable />）
       -> useCubeEngine() 按 route.path 推导 /Area/Controller，provide 上下文
       -> createCubeEngine(deps) 拉 GetPage 元数据、初始化模型、自动首查
       -> CubeTable 整合 搜索/工具栏/表格/分页/表单弹窗，开放具名插槽
  -> 业务定制：CubeTable 插槽（取代旧 Section 覆盖）
```

默认页面在 `core/views/`；业务页面与定制放在 `apps/<app>/src/views/`。具体路径发现规则见 [route-conventions.md](./route-conventions.md)。

## 扩展顺序

1. 优先修改后端字段元数据或菜单配置。
2. 仅需改变区块时，用 `CubeTable` **具名插槽**覆盖（见 [customize-page.md](../guides/customize-page.md)）。
3. 默认页面引擎确实无法承载的交互，才创建完整 `index.vue` 页面（可 `useCubeEngine({ routePath })` + `:context` 自定义引擎）。
4. 若所有应用都需要新默认能力，修改 `core/components/CubeTable/` 或 `core/engine/` 并增加测试。

## 相关入口

| 目标                | 位置                                            |
| ------------------- | ----------------------------------------------- |
| catch-all 页面解析  | `core/pages/DefaultEntity.vue`                  |
| 默认列表页          | `core/views/index.vue`（目标态 = `<CubeTable />`） |
| 页面引擎（纯工厂）  | `core/engine/createCubeEngine.ts`               |
| 引擎 composable     | `core/engine/useCubeEngine.ts`（`provide`/`inject`） |
| 集成组件 + 插槽     | `core/components/CubeTable/`                   |
| 数据层底座          | `core/dataset/data-set/DataSet.ts`              |
| 元数据/LOV 门面     | `core/composables/useCubeApi.ts`                |
| 字段契约唯一真理源  | `core/types/field.ts`（`FieldMeta`/`ControlType`） |
| 视图解析            | `core/utils/menuRoutes.ts`                      |

> **遗留（将被废除）**：旧实现中的 `core/views/components/`（Section 组合）、`core/composables/useSections.ts`（`SectionKeyMap`）、命令式 `openListFormDialog` 为 ADR 0005 前的机制。`core/views/form.vue` 默认表单页由 `CubeTableFormDialog` 取代。存量 Section 覆盖页面在迁移完成前仍可用（兼容桥），最终随 `architecture/cube-engine.md` §7 的 P6 移除。
