# @newlifex/cube-vue（Cube 默认前端模板）

> NewLife.Cube 的 Vue 3 默认前端模板 / 微前端框架。**元数据驱动**：后端 `EntityController` 下发字段元数据（`GetPage` / `GetFields`），前端自动渲染动态列表、动态搜索、动态表单，无需为每个实体手写页面。

## 目录真相（务必先读）

| 目录 | 角色 | 说明 |
|------|------|------|
| `web/core/` | **框架引擎（默认模板本体）** | 所有动态列表/表单/布局/通用组件都在这里。**改默认模板逻辑只动这里。** |
| `web/src/` | 遗留演示壳（**已废弃**） | 旧版代码，**并未 import `@newlifex/cube-vue/core`**，不是默认模板。不要在此修改动态模板。 |
| `web/apps/`（cube-admin / cube-cube / cube-v1） | 消费 core 的应用壳 | 微前端子应用，运行时引用 `core`。 |
| `web/skills/` | 开发流程说明 | 见下方「开发流程 / 快捷指令」。 |

包名 `@newlifex/cube-vue`，通过 pnpm workspace 别名引用；`vite.config.ts` 中 `'@newlifex/cube-vue' → web/`。

## 架构概览（web/core）

| 目录/文件 | 职责 |
|-----------|------|
| `views/index.vue` | **动态列表页**（DefaultListPage）：表格 + 动态搜索 + 增删改入口 |
| `views/form.vue` | **动态新增/编辑表单页**（FormPage），被列表页弹窗调用 |
| `views/components/` | 列表/表单分段件：`ListTableContent` / `ListSearchBar` / `ListToolbar` / `FormContent`（字段渲染链）等 |
| `pages/DefaultEntity.vue` | catch-all 路由：按后端菜单匹配并渲染 `views/index.vue` |
| `components/` | 布局/原子 UI：`SearchBar` / `LovSelect` / `MenuItem` / `UserProfile` 等（`CbTable`/`CubeSearch` 等已孤立，勿用） |
| `composables/` | `useSections`（页面区块覆盖）、`useCubeApi`、`useModal`、`useLayout`、`useTheme` |
| `configure/` | `UIConfig` 默认配置（`defaultConfig`） |
| `dataset/` | `DataSet` 数据容器 |
| `stores/` | `menu` / `tabs` / `user` 状态 |
| `layouts/` | `Main` / `Cyber`（默认）/ `TopMenu` / `Root` 布局 |
| `plugin/` | `cubeFront()` Vite 插件：自动注册页面 / Section、扫描应用 |
| `router/` `routes/` | 核心路由 |
| `types/` `utils/` `i18n/` `themes/` | 类型、工具、国际化、主题 |
| `initApp.ts` / `main.ts` / `App.vue` / `microAppRouter.ts` | 应用初始化与微前端路由 |

## 动态默认模板引擎（核心）

### 渲染流程
1. 路由由**后端菜单驱动**：`pages/DefaultEntity.vue` 匹配当前菜单路径，渲染 `views/index.vue`。
2. 列表页请求 `GetPage` 拿到 `setting / list / search / addForm / editForm / detail` 五区字段元数据。
3. `backendFieldsToFormFields()` 把后端字段转成前端 `FormField[]`。
4. 表单弹窗调用 `views/form.vue`（FormPage）→ `FormContent.vue` 按 `field.type` 渲染控件。

### 字段类型 → 控件映射（两段式）

**第一段：后端 CLR `typeName` → 内部 `type`**

列表页 `views/index.vue`：
```ts
// TYPE_TO_FORM_TYPE（index.vue L170-178）
const TYPE_TO_FORM_TYPE = {
  String: 'text', Int32: 'text', Int64: 'text', Decimal: 'text', Double: 'text',
  Boolean: 'select', DateTime: 'text',
};
```
表单页 `views/form.vue`：
```ts
// TYPE_TO_FORM_TYPE（form.vue L94-102）
const TYPE_TO_FORM_TYPE = {
  String: 'text', Int32: 'text', Int64: 'text', Decimal: 'text', Double: 'text',
  Boolean: 'switch', DateTime: 'datetime',
};
```
> ⚠️ **已知不一致（待修复）**：列表弹窗里 `Boolean→select`、`DateTime→text`；表单页里 `Boolean→switch`、`DateTime→datetime`。两处应统一。
>
> 搜索区 `TYPE_TO_SEARCH_TYPE`（index.vue L46-53）仅覆盖 `String/Int32/Int64/Decimal/Double→text`、`Boolean→select`，**无日期 / 枚举 / 数值范围**。

**第二段：内部 `type` → 实际控件**（`views/components/FormContent.vue` L68-135 的 `v-if/else` 链）：

| `field.type` | 渲染控件 |
|--------------|----------|
| `textarea` | `<textarea>`（长度 > 50 的文本自动转此） |
| `select` | 原生 `<select>` + `options` |
| `radio` | 原生单选组 |
| `switch` | `<el-switch>` |
| `datetime` | `<el-date-picker type="datetime">` |
| 其它（text/email/tel/number…） | 原生 `<input :type="field.type">` |

> 表单**无字段级组件注册表**，控件靠 `FormContent.vue` 的 `v-if/else` 链决定；新增控件类型需扩展该链（或引入注册表）。

### 当前支持矩阵与缺口

| 后端类型 / ItemType | 当前渲染 | 状态 |
|---------------------|----------|------|
| String（短） | text 输入 | ✅ |
| 长文本（length > 50） | textarea | ✅（仅表单；列表仍按普通文本） |
| Int32/64/Decimal/Double 等数值 | 原生 text 输入 | ⚠️ 应改为 `number` / `inputNumber` |
| Boolean | select（列表）/ switch（表单） | ⚠️ 两处不一致 |
| DateTime | datetime 选择器（表单）/ text（列表） | ⚠️ 列表未用日期控件 |
| 枚举 | select（需 LOV 提供 options） | ⚠️ options 未自动填充 |
| Guid | text | ❌ 主键通常被过滤；非主键会异常 |
| TimeSpan（时间） | — | ❌ 无 timePicker |
| 附件（Byte[] / ItemType=file） | — | ❌ 无上传控件 |
| ItemType=image | — | ❌ 无图片上传 / 预览（列表仅加宽列） |
| ItemType=json | text | ❌ 无 Json 编辑器 |
| ItemType=color | — | ❌ 无颜色选择器 |
| ItemType=icon | — | ❌ 无图标选择器 |
| ItemType=html / markdown | — | ❌ 无富文本编辑器 |
| ItemType=singleSelect / multipleSelect | select / — | ⚠️ 多选未实现 |
| 搜索区日期范围 / 数值范围 | — | ❌ 仅 text / select |

> **完善默认模板**（让每种字段类型都能正常渲染 / 编辑 / 提交）即补齐上表缺口，并统一列表 / 表单两处映射。LOV 值集系统（见下）已可用于枚举 / 列表型下拉。

### LOV 值集（枚举 / 列表下拉）
值集由后端 `LovController` + 前端 `LovSelect` 组件支撑：后端为字段配置 `LovCode`（`Enum.{FullName}` 或 `List.xxx`），`GetPage` 元数据携带 `lovCode`，前端自动渲染下拉并翻译列值。详见 `web/skills/cube-lov/SKILL.md`。

## 开发

### 环境要求
- Node.js ≥ 24，pnpm ≥ 9（初始化细节见 `skills/cube-init`）。

### 常用命令（在 `web/` 目录）
```sh
pnpm install
pnpm dev            # 开发服务器，端口 5187；代理 /Admin /Cube /Sso /api → http://localhost:5000
pnpm build          # 生产构建，产物输出到 ../wwwroot（由 CubeDemo 的 UseVue() 托管）
pnpm test:unit      # Vitest 单元测试
pnpm test:e2e:dev   # Cypress E2E（针对 dev server）
pnpm test:e2e       # Cypress E2E（针对生产构建，适合 CI）
pnpm lint           # ESLint
```

### 后端联调
- `web/.env.development` 的 `VITE_API_URL` 配置后端地址；`vite.config.ts` 的 `server.proxy` 将 `/Admin`、`/Cube`、`/Sso`、`/api` 转发到本地后端（默认 `:5000`）。
- 后端 demo 见 `NewLife.Cube/CubeDemo`（SQLite 本地库，已接入本前端默认模板）。

## 开发流程 / 快捷指令（web/skills）

`web/skills/` 下为可复用的流程说明，AI 助手按场景调用：

| Skill | 用途 | 触发场景 |
|-------|------|----------|
| `cube-init` | 用 `@newlifex/cube-vue` 初始化新前端项目（布局 / 状态 / 路由 / API / 多语言 / 容器化） | 初始化项目、搭建前端 |
| `cube-add-app` | 在微前端架构中新增子应用 / 模块（约 4 步） | 新增应用、新模块 |
| `cube-add-page` | 按后端 Controller 在前端创建页面（默认 CRUD 无需建文件） | 新增页面、给模块加页面 |
| `cube-add-api` | **已迁移**至 `cube-add-page`，请勿再用 | — |
| `cube-layout` | 新增 / 注册 / 切换页面布局（`registerLayout` + CSS Token 规范） | 自定义布局、切换布局 |
| `cube-lov` | LOV 值集（枚举 / 列表下拉）全链路配置 | 配置值集、枚举下拉 |
| `cube-page-override` | Section 机制覆盖 / 扩展框架组件（搜索栏 / 表格 / 工具栏 / 表单字段…） | 覆盖页面区块、自定义组件 |
| `modal-organize` | 命令式弹窗组织规范（`.vue` + `openXxx.ts` 成对放置） | 弹窗放哪、弹窗结构 |

> 详细步骤见各 `SKILL.md`。

## 扩展机制
- **Section 覆盖**：在应用 `views/<area>/<controller>/` 下放置大写开头的 `.vue`（如 `ListSearchBar.vue`、`FormFields.vue`），由 `cubeFront` 插件自动扫描注册，覆盖框架默认区块，无需改框架源码。
- **弹窗**：遵循 `modal-organize`，每个弹窗一个文件夹（组件 + `openXxx.ts`）。
- **布局**：`registerLayout()` 注册，详见 `cube-layout`。

## 测试建议
- **组件测试（推荐锁死「字段类型渲染」）**：挂载 `FormContent` / 列表页，喂入不同 `typeName` / `itemType` 的字段元数据，断言渲染出正确控件（`inputNumber` / `switch` / `datePicker` / `timePicker` / `select` / `upload` / `colorPicker` / `iconSelector` / `textarea` / `jsonEditor`…）。正好覆盖「每种字段类型都能正常渲染」。
- **E2E（锁死「完整 CRUD」）**：Cypress 启动真实 `CubeDemo` + `web` dev，走 新增 → 列表 → 编辑 → 提交 → 删除。
