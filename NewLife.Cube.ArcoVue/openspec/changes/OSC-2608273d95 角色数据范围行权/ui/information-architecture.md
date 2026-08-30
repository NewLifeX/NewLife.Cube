# OSC-2608273d95 UI — 角色数据范围

框架：Arco Design Vue（https://arco.design/vue/docs/start）。不涉及 VTable 新配置、不涉及 FlowGram。`.vue` 薄 script；显隐进 `core/utils/dataScopeForm.ts`。

## 页面

`Admin/Role` 新增/编辑抽屉（DefaultForm），无新路由。

## DataDepartmentIds

| DataScope 值 | 控件 |
| --- | --- |
| 0 全部 / 1 本部门及下级 / 2 本部门 / 3 仅本人 | **不渲染** DataDepartmentIds（含标签） |
| 4 自定义 / 显示名「自定义」 | 渲染多选；DataSource=启用部门；空选保存后服务端行权恒假 |

非法值归一：非 4 时提交不得带部门 ID 列表当作授权（可随表单其它字段走，服务端忽略非自定义）。

空角色列表：不出现部门多选。

## 列表 sensitive

GetPage 字段 `sensitive===true`：不进入 VTable 可见列。空表仍显示其它列。不把「列被藏」写成权限成功提示。

## 仅自己提示

`Admin/User`：仅当可判定「仅本人且仅自己一行」时显示既有文案。本部门两名同事 → 不提示。不根据角色名。

## 不做

- 不在角色表单做字段矩阵。
- 不展示 Where SQL / 部门 ID 作为「你的权限」。
- 不改 Permission 树交互（OSC-260824fc7c）。
