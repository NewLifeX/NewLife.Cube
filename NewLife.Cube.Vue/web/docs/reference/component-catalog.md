# 组件与扩展目录

> 本页是高频入口索引，不替代源码。具体 props、事件和类型以组件定义为准。

## 默认页面

| 位置                           | 用途                         |
| ------------------------------ | ---------------------------- |
| `core/pages/DefaultEntity.vue` | catch-all 菜单页面解析       |
| `core/views/index.vue`         | 默认列表页                   |
| `core/views/form.vue`          | 默认新增/编辑表单            |
| `core/views/components/`       | 列表/表单 Section 的默认实现 |
| `core/views/Loading.vue`       | 微应用路由加载页             |

## 共享组件

| 位置                                                   | 用途         |
| ------------------------------------------------------ | ------------ |
| `core/components/LovSelect.vue` / `LovSelectTable.vue` | LOV 值集选择 |
| `core/components/Uploader.vue`                         | 上传         |
| `core/components/RichEditor.vue`                       | 富文本编辑   |
| `core/components/JsonEditor.vue`                       | JSON 编辑    |
| `core/components/IconSelector.vue`                     | 图标选择     |
| `core/components/Notification.ts`                      | 统一通知入口 |

`CbTable`、`CubeSearch`、`CubePager` 等旧封装目前未接入默认页面渲染链，不应为新业务页面直接选用。列表、搜索和分页优先使用默认页面及其 `core/views/components/` Section；只有完成独立迁移决策后，才能重新将这些旧组件列为可复用入口。

## 扩展入口

- 页面局部替换：Section 覆盖，见 [customize-page.md](../guides/customize-page.md)。
- 对象关联选择：使用 `LovSelect`/`LovSelectTable`，对应开发技能 `../../skills/cube-lov/`。
- 布局：`core/layouts/` 与 `core/composables/useLayout.ts`，对应开发技能 `../../skills/cube-layout/`。
- 命令式弹窗：遵循 `../../skills/modal-organize/`。
