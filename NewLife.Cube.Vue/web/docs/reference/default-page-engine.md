# 默认页面引擎参考

## 目标

默认页面引擎让后端实体元数据驱动前端列表、搜索和编辑表单。标准实体页面无需为每个控制器新建 Vue 页面。

## 当前流程

```text
菜单路径
  -> DefaultEntity.vue（菜单匹配）
  -> core/views/index.vue（默认列表）
  -> 后端页面/字段元数据
  -> 字段转换与列表/搜索/表单 Section
  -> core/views/form.vue（新增/编辑）
```

默认页面在 `core/views/`；自定义路径页面和 Section 放在 `apps/<app>/src/views/`。具体路径发现规则见 [route-conventions.md](./route-conventions.md)。

## 扩展顺序

1. 优先修改后端字段元数据或菜单配置。
2. 仅需改变区块时添加 Section 覆盖。
3. 默认页面引擎确实无法承载的交互才添加完整 `index.vue` 页面。
4. 若所有应用都需要新默认能力，修改 `core/views/` 并增加测试。

## 相关入口

| 目标                | 位置                              |
| ------------------- | --------------------------------- |
| catch-all 页面解析  | `core/pages/DefaultEntity.vue`    |
| 默认列表页          | `core/views/index.vue`            |
| 默认表单页          | `core/views/form.vue`             |
| 可覆盖 Section 名称 | `core/composables/useSections.ts` |
| 视图解析            | `core/utils/menuRoutes.ts`        |
| Section 注册        | `core/utils/pageSections.ts`      |
