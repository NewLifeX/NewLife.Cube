# OSC-260902ef43 UI — 命名工作台

框架：Arco Design Vue（https://arco.design/vue/docs/start）。`▾` 弹出菜单结构与视觉对齐 `web/src/features/search/QueryComboButton.vue`（OSC-260830a1b2 查询簇第三键）：`a-dropdown trigger="click"`、内容用 `a-doption`、分组标题用 `.qcb-group-title` 风格 div、分隔用 `a-divider`、当前项左侧 `icon-park type="check"` 打勾。不涉及 VTable 新配置、不涉及 FlowGram。`.vue` 薄 script，全部操作与对话框状态进 `views/home/useWorkbench.ts`。

## 工作台标题栏（Workbench.vue banner）

右侧操作区自左向右：

```
[编辑(左)] [▾(右)] [全屏] [恢复默认]
```

- 编辑左键：icon `setting-config`（编辑态 `check`）；tooltip「自定义工作台 / 完成」——保持现状。
- `▾` 右键：仅 `showNamedMenu`（系统角色或有命名列表）渲染；icon `down`。
- 恢复默认：仅默认工作台（`!isNamed && canRestore`）渲染（现状条件加 `!isNamed`）。
- 命名工作台页：hello 下 `.wb-hello-meta` 行为 `[命名 chip（`workbench` 图标 + 名称） + 日期]`，chip 与日期同一 flex 行、`align-items:center` **水平对齐**（日期无上下偏移、chip 内 svg `display:block`、行高统一）。

## 左侧导航中的命名工作台（菜单置顶）

- 发布/更新时后端把 `工作台` 顶级分组（显示名=`系统看板`）`Sort` 顶置为根级最大 `Sort+1`（XCode `BigSort=true`，`Childs` 按 Sort 降序）→ `系统看板 / 命名工作台1…N` 在左侧导航**永远显示在第一组**，不随其它顶级菜单沉底。
- `/home` 默认工作台不产生菜单行；菜单第一组的「系统看板」组是命名工作台的收纳分组。
- 「系统看板」顶级组菜单图标固定 `workbench`（IconPark）：`iconRegistry.MENU_NAME_ICONS` 显示名精确命中（与「魔方管理→cube-three」同款），先于 fa 映射/关键词/默认兜底；兼容旧版本地仍叫「工作台」的分组；命名子菜单仍按各自标题走关键词/默认，不误命。

## ▾ 弹出菜单内容（自上而下）

| 项 | value | 显隐/禁用 |
| --- | --- | --- |
| 发布… | `__publish` | `canPublish`（系统角色；无 slug=另存为弹窗，有 slug=直接更新发布） |
| 重命名… | `__rename` | `canRename`（系统角色**且命名**；默认工作台恒禁用） |
| 删除… | `__delete` | `canDelete`（系统角色**且命名**；默认工作台恒禁用；danger 红色） |
| `a-divider` | — | 恒有 |
| 组标题「切换工作台」 | — | 恒有 |
| 默认工作台 | `__default` | 恒有；当前为默认时打勾 |
| 命名工作台 1…N | `__named:{slug}` | `namedList` 各一项；当前 slug 打勾 |

发布对话框（无 slug 时）：标题输入（必填 ≤40）+ slug 输入（必填，预填 `wb-<4位hex>`；非法或**已被占用**（对照命名列表即时红字）不可确认）→ 确认「发布并挂载菜单」；后端 `create` 语义 409 兑底防覆盖既有看板。
重命名对话框：标题输入（必填 ≤40，预填当前标题）。
删除：确认对话框文案含「将同时移除对应系统菜单项」。

## 硬规则：默认工作台（无 slug）

- 默认工作台 = 个人墙（user>role>system），**禁止重命名与删除**：`▾` 中 `重命名…/删除…` 恒禁用；后端无默认工作台重命名/删除端点。
- 默认工作台仅可编辑个人墙并「发布…」（另存为命名工作台）。

## 只读命名工作台（普通用户）

- 命名页编辑按钮**渲染但禁用**（`canEditToggle=false`，tooltip 提示），`▾` 不渲染（仅系统角色）；无「+添加部件」空态入口。
- `config:null` 空槽：居中空态文案「该命名工作台尚未配置或为空」，无添加入口。

## 不做

- 不做命名工作台个人覆盖开关 / 角色多选发布框（可见性交给授权树）。
- 不做菜单排序/图标自定义对话框（发布默认 `fa-th-large`，改动走菜单管理）。
- 不在 banner 复制「返回/刷新」第二层头部（沿用现状单行 banner）。
