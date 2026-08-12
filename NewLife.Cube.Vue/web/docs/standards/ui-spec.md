# UI 规范

> **状态：当前。** 这是前端 UI 的唯一权威规范；设计方法论与 Element Plus / Tailwind 分工见 [ADR 0003](../decisions/0003-element-plus-tailwind-design-system.md)。

## 系统边界

- Element Plus 负责组件外观、状态和可访问性基础能力。
- Tailwind 负责页面级 `flex`、`grid`、间距和响应式布局。
- `src/theme/tailwind.css` 将 Tailwind 语义类映射到 Element Plus `--el-*` 变量；主题切换时 UI 必须自动跟随。
- 框架已有业务组件优先于 Element Plus，Element Plus 优先于新建自定义组件。
- 组件定制（如 `CubeTable` 插槽）同样必须遵守上述令牌规则；详见 [CubeTable 引擎](../architecture/cube-engine.md) 与 [页面定制](../guides/customize-page.md)。

页面级布局禁止同时使用 Tailwind 栅格与 `el-row`/`el-col`；后者仅适用于 `el-form` 内的字段网格。理由见 [ADR 0003](../decisions/0003-element-plus-tailwind-design-system.md)。

## 页面类型

| 类型       | 目的                   | 结构                               |
| ---------- | ---------------------- | ---------------------------------- |
| 详情/设置  | 查看或编辑单个对象     | `max-w-[800px]`，卡片纵向堆叠      |
| 列表       | 管理一批对象           | `max-w-[1200px]`，筛选、表格、分页 |
| 仪表盘     | 概览与监控             | 统计卡片与图表/列表网格            |
| 全屏工作区 | 画布、编辑器等连续操作 | 不限宽，自由布局                   |

列表页先判断默认 `DefaultEntity` 与 Section 覆盖能否满足；能满足时不得重写整页。

## 颜色与主题

- 仅使用 `--el-*` 语义变量或其 Tailwind 语义映射；禁止硬编码主题颜色和 Tailwind 默认调色板（例如 `bg-red-500`）。
- `--el-color-primary` 表示主操作；`success`、`warning`、`danger`、`info` 只承载对应语义。
- 当前主题族在 `core/themes/`：`cyber`、`forest`、`aurora`、`industrial`；切换逻辑在 `core/composables/useTheme.ts`。
- 新主题只覆盖 Element Plus 语义变量，并在 `core/themes/index.ts` 与 `useTheme.ts` 注册；不要新增 `--app-*` 或其他平行色彩 token。

常用 Tailwind 映射：`bg-primary`、`bg-bg-page`、`bg-bg-overlay`、`bg-fill`、`text-text-primary`、`text-text-regular`、`text-text-secondary`、`border-border`。

## 间距、圆角与动效

- 使用 Tailwind 4px 间距阶梯：4/8/12/16/20/24/32。
- 圆角不超过 12px；使用 `rounded-sm`、`rounded-md`、`rounded-lg`。
- `el-card` 使用 `shadow="never"`，用边框区分层级。
- hover 仅改变背景或边框；禁止 `translateY`、缩放和装饰性弹跳。

## 组件与交互

| 场景             | 规则                                                  |
| ---------------- | ----------------------------------------------------- |
| 首次加载         | `el-skeleton`，避免布局跳动                           |
| 刷新、翻页、筛选 | 局部 `v-loading`；表格优先复用 `CbTable`/默认页面能力 |
| 初始空态         | `el-empty` + 创建入口                                 |
| 筛选空态         | `el-empty` + 清除筛选入口                             |
| 页面级错误       | `el-alert` + 重试                                     |
| 操作级反馈       | `ElMessage` 或统一 `Notification`                     |
| 单一轻量确认     | `el-popconfirm`                                       |
| 不可逆或批量操作 | `el-dialog` 或 `ElMessageBox.confirm`                 |
| 图标             | 统一 `@element-plus/icons-vue`                        |

表单使用 `el-form` / `el-form-item`：默认 label 在上方，失焦和提交时校验，错误信息保留在字段下方。图标按钮必须有 `aria-label` 或 `el-tooltip`。

## 交付检查

- [ ] 页面属于四种页面类型之一，或已新增 ADR/规范。
- [ ] 复用了默认页面、Section 或框架组件；没有无理由复制整页。
- [ ] 没有硬编码主题色、第二套图标库或第二套页面栅格。
- [ ] 加载、空态、错误、确认遵守本页预设模式。
- [ ] 浅色与深色主题均已检查。
- [ ] 新的稳定规则已更新本文件；取舍已新增 ADR。
