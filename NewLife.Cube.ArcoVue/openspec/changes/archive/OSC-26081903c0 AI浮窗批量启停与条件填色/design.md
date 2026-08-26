# OSC-26081903c0 Design — AI 浮窗、批量启停与条件填色

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）；列表 VisActor VTable（配置 https://visactor.com/vtable/option/ListTable；接口 https://visactor.com/vtable/api/Methods）。不涉及 FlowGram。`.vue` 薄 script，业务进同目录 `useXxx.ts` 或 `core/utils` 纯函数。

图标：FAB 用已注册 IconPark `robot-one`。面板内只用已注册：`info` `more` `full-screen` `off-screen` `close` `send` `delete`。不新增图标包。

## 0. 竞品三项对照（实施勿扩范围）

| P0 | 报告现状 | 本号落地 | 明确留给别号 |
| --- | --- | --- | --- |
| #1 AI 浮窗 | ArcoVue 无；MVC/Vue 有；`/Ai/AiChat` 已具备 | 右侧停靠面板 + FAB；协议对齐 Vue | 不改工具/提示词/鉴权；无历史/附件/搭建 |
| #3 批量工具栏 | 仅删除 + 行内 Enable 徽标；e483 已加「批量修改」 | **批量启用/禁用** + 修高级菜单可见性 | 不改 GET EnableSelect；不重做 BatchUpdateFields |
| #4 条件格式 | 无；VTable 已有 `cellBgColor` | 填色弹层；含 **行侧边** 竖条；卡片 side+标题行 | 不是权限；无智能配色；不做日历/甘特 |

## 1. 状态唯一来源

| 状态 | 来源 | 禁止 |
| --- | --- | --- |
| AI 是否显示 | `GetAiConfig.data.AISwitch`（登录后拉一次；Setting 页保存后可再拉） | 前端自造管理员开关 |
| AI 配色 | `AIPrimaryColor` / `AISecondaryColor`，缺省 `#2ecc71` / `#1e8e3e` | 写死与主题脱节的第三套色（FAB 可用配置色） |
| 会话 Id | `localStorage cube-ai-session`（与 Vue 同键，便于对照） | 每条消息新建无持久 session |
| 当前用户称呼 | `userStore.displayName`（与顶栏同源） | 组件内另拉用户资料 |
| 当前页面上下文 | 路由 path → `area`/`controller`；抽屉模式由 `appStore.aiForm` | AI 组件自己猜 URL |
| 欢迎 Tab | 面板内 `welcomeTab`，仅 `messages.length===0` 时有意义 | 写入 ViewsJson / 后端 |
| 批量启停可见 | `resolveBatchEnableState` 纯函数 | 模板里散写 if |
| 填色规则 | 当前 NamedView.`format` | 平行存 chrome / localStorage |

## 2. A — AI 助手浮窗

对照竞品右侧停靠面板：**抄布局与欢迎区交互，不抄品牌与无后端能力**。协议、SSE、fill_form、run_js 仍对齐 Cube.Vue。

### 2.0 截图对照（抄什么 / 砍什么）

| 竞品截图 | 本号 | 理由 |
| --- | --- | --- |
| 右侧全高白底停靠面板，压在主内容上 | 做：`fixed` 贴 `right:0`，宽 380px，高 100vh | 比 Vue 右下小卡片更接近竞品 |
| 关闭后右下 FAB | 做；面板打开隐藏 FAB | 与 Vue 一致 |
| 标题「表哥」 | **不做**；标题固定 **「AI 助手」** | 不引入第三方品牌 |
| 标题旁问号 | 做：`info` + `a-tooltip` | 文案见 2.4b；不接外部帮助站 |
| 历史（时钟） | **不做** | 无会话历史 API；`cube-ai-session` 仍只保当前会话 Id |
| 更多（三点） | 做：下拉里 **深度推理** + **清空会话** | 深度从底栏挪走，底栏更干净 |
| 最大化 / 关闭 | 做 | `full-screen` / `off-screen` / `close` |
| 问候「Hi, 亲爱的{名}」+ 副文案 | 做：`👋 Hi，{displayName}`；无名则「管理员」 | `userStore.displayName` |
| Tab：推荐 / 提问 / 分析 / 搭建 | **三 Tab**：推荐、提问、分析。**不做「搭建」** | 无搭建/生成应用工具 |
| 推荐区可点行（雷达图等） | 做**列表行**形态；文案走 §2.5 现有快捷指令 | 不发「生成雷达图」等无工具消息 |
| 底栏附件回形针 | **不做** | AiChat 无上传 |
| 纸飞机发送 + Enter/Shift+Enter 提示 | 做 | |
| 底栏「深度」勾选 | 挪到「更多」 | 视觉对齐截图底栏 |

实施对照 Cube.Vue `AiAssistant.vue`：Arco 组件替换 Element Plus；**不要**复制 `el-button` round 芯片作欢迎区。

### 2.1 协议（对齐 Cube.Vue，路径一律绝对）

| 方法 | 路径 | 权限 | 说明 |
| --- | --- | --- | --- |
| GET | `/Cube/GetAiConfig` | 登录即可（现有 CubeController） | `{ AISwitch, AIPrimaryColor, AISecondaryColor }` |
| POST | `/Ai/AiChat` | 全局 `EntityAuthorize(Detail)`；实体目标再 `CheckEntityPermission` | body JSON；响应 **SSE** `data: {type, ...}` |
| POST | `/Ai/OperationResult` | 同上 | `{ checkpointId, result }`；`result` 字符串 ≤8192 |

**禁止**相对路径 `Ai/OperationResult`（Vue 在深层路由会打错）。开发代理：`/Ai/...` 已被 `DEV_PROXY_AREA_PATTERN` 覆盖，不必改 `devProxy.ts`。

SSE **不要走 axios**（难读流）。用 `fetch`，头与 `cubeApi` 一致：

- `Authorization`: `cubeApi.tokenManager.getToken()`（ArcoVue 默认无 `Bearer ` 前缀，与 `createApiClient` 相同）
- 租户：`resolveTenantHeader(sessionStorage cube.tenant.code)` 有则带上
- `Content-Type: application/json`
- **不加** `X-Cube-Field-Validation`（读/对话不是实体写）

AiChat body（字段名与 `AiChatRequest` 一致）：

```json
{
  "sessionId": "s…",
  "message": "…",
  "page": "list|form|detail|object|home|custom",
  "mode": "add|edit",
  "id": 0,
  "query": "",
  "area": "Admin",
  "controller": "User",
  "think": false
}
```

`area`/`controller`：路由 path `Admin/User` → area=`Admin`，controller=`User`；单段 `Home` → area 空串，controller=`Home`。`query`：当前列表 Search 参数 JSON 再 Base64（无列表则 `""`）。编码函数抽纯函数并单测；失败则传空串，不阻断发送。

SSE 行：`trim` 后须 `data: ` 前缀，JSON.parse 失败跳过。`type`：

| type | 行为 |
| --- | --- |
| `text` | 追加 `content`，整段 Markdown 渲染 |
| `tool` | 工具卡片 start/done/error（字段名随现码 `handleTool`，实施时对照 Vue） |
| `run_js` | `new Function(script)`，结果 JSON 截断 8192 后 POST OperationResult |
| `error` | 助手气泡展示 `message`，停止流 |
| 未知 | 忽略 |

HTTP 非 2xx：解析 `message`，气泡「请求失败」+ 提示检查 AISwitch。

### 2.2 Markdown 与 XSS

新增依赖 `marked`（与 Cube.Vue 相同）。`renderAiMarkdown(text)`：

- `gfm: true`，`breaks: true`
- `renderer.html` → `escapeHtml`
- 代码块内容 `escapeHtml`
- 输入空 → `''`

禁止 `v-html` 未经过该函数的模型原文。

### 2.3 文件地图

| 文件 | 计划 | 冻结 |
| --- | --- | --- |
| `packages/api-core/src/api.ts` `createConfigApi` | 加 `getAiConfig(): GET /Cube/GetAiConfig` | 不改 Setting 读写 |
| `packages/api-core/src/api.spec.ts` | 断言 URL 无 `/api` 前缀 | |
| `web/package.json` | 加 `marked` | 不引入 element-plus |
| `web/src/views/ai/AiAssistant.vue` | 薄模板：FAB + 面板 | 无业务 fetch |
| `web/src/views/ai/useAiAssistant.ts` | 配置/SSE/会话/快捷指令 | |
| `web/src/core/utils/aiMarkdown.ts` + spec | 渲染 | |
| `web/src/core/utils/aiSse.ts` + spec | 拆 `data:` 行 | |
| `web/src/core/utils/aiChatContext.ts` + spec | path→area/controller；query Base64 | |
| `web/src/core/utils/aiWelcome.ts` + spec | 问候文案、Tab 项、快捷行 | |
| `web/src/stores/app.ts` | `aiContext: { page, mode, id, typePath, queryB64, applyFill }`；列表 `loadData` 后更新 `queryB64` | 不把消息列表放 store |
| `web/src/layouts/RootLayout.vue` | 登录壳内挂载 `<AiAssistant />` | 不挂登录路由 |
| `web/src/views/crud/useDefaultList.ts` 或 `useRecordDrawer.ts` | 抽屉打开 add/edit 时登记 `aiForm`，关闭清空 | |
| `web/src/views/object/useDefaultObject.ts` | 对象页登记 `page=object`，可 fill 当前表单 | |
| `NewLife.Cube/AI/*`、`AiController` | **不改** | |

### 2.4 UI 数值与结构（见 `ui/information-architecture.md`）

**壳**

- 根：`position:fixed; z-index:3000`（高于 Arco Drawer 默认档与列表全屏 900/1001）。不改主布局 padding，面板覆盖内容。
- FAB：关闭时 `right:24px; bottom:24px`；直径 48px；背景 `var(--ai-primary)`；图标 `robot-one` 白色。
- 面板默认：**右侧停靠** `top:0; right:0; bottom:0; width:380px`。背景 `var(--color-bg-1)`，左边框 `1px solid var(--color-border-2)`，左侧阴影 `-6px 0 16px rgba(0,0,0,.08)`。
- 窄屏 `max-width: 639px`：面板改 `width:100vw`（仍 overlay）。
- 最大化：`inset:20px; width:auto; height:auto; border-radius:8px`；`localStorage cube-ai-maximized`（`1`/`0`），重开恢复。
- 面板开时隐藏 FAB。
- 未登录 / `AISwitch=false` / GetAiConfig 失败：不渲染。

**面板 DOM 从上到下**

```
header 48px
  [robot-one 16] AI 助手  [info tooltip]
  弹性空白
  [more 下拉] [full-screen|off-screen] [close]
body flex:1 overflow
  空会话：问候 + a-tabs + 建议列表
  有会话：消息气泡 + 工具卡片
footer
  a-textarea（autosize minRows=1 maxRows=4）
  右下 send 按钮
  提示「Enter 发送 / Shift+Enter 换行」
```

**header 锁定**

- 标题：「AI 助手」，字号 `var(--cube-font-size-body)`，字重 medium。
- tooltip：「对话会带上当前页面与筛选上下文，不会改权限。Enter 发送，Shift+Enter 换行。」
- 更多 `a-dropdown` 两项：① `a-checkbox`「深度推理」（绑定 `think`，即 AiChat `think`）②「清空会话」（现有 `clear`，换新 `cube-ai-session`）。无「历史」。
- 最大化/还原、关闭：`a-button type=text`。关闭只藏面板，不清会话。

**空会话欢迎（`messages.length===0`）**

- 主句：`👋 Hi，{aiGreetingName(displayName)}`
- 副句按 `page`：list「我能帮你分析当前列表或检查系统状态。」；form/object「我能帮你填写当前表单或检查系统状态。」；其余「我能帮你检查系统运行状态。」
- Tab：`a-tabs` `type=rounded` 或 `line`，三项 **推荐 / 提问 / 分析**。默认「推荐」。`welcomeTab` 不持久化。
- 推荐：§2.5 该 page 全部快捷行。
- 提问：无列表；一行灰字「在下方输入问题，或切到推荐查看快捷指令。」
- 分析：仅含「分析当前列表数据」（list 才有）与「检查系统运行状态」。
- 行 UI：全宽可点、左对齐、行间 `1px var(--color-border-2)`；hover `var(--color-fill-2)`。点击 = `send(message)`。
- 有消息后欢迎+Tab 整块卸载。

**footer 锁定**

- placeholder：「输入问题…」
- 无回形针、无底栏「深度」。
- 发送：`icon-park type=send` 圆钮，空输入或 `streaming` 时 disabled。
- Enter 发送，Shift+Enter 换行；流式中禁止第二路 SSE。
- 配色：`--ai-primary` / `--ai-secondary` 驱动发送钮与 FAB，其余走 Arco 变量。

### 2.5 快捷指令矩阵

纯函数 `aiWelcome.ts`：`aiGreetingName`、`aiWelcomeSubtitle(page)`、`aiQuickItems(page)`（`{tab:'recommend'|'analyze'; label; message}`）。提问 Tab 永远不从该函数出项。

| `page` | 推荐行 label → message | 分析行 |
| --- | --- | --- |
| `list` | 分析当前数据 → `分析当前列表数据`；系统诊断 → `检查系统运行状态` | 两行都在 |
| `form` | 帮我填表 → `帮我填写当前表单`；系统诊断 → 同上 | 仅诊断 |
| `detail` | 仅系统诊断 | 仅诊断 |
| `object` | 同 form | 仅诊断 |
| `home` / `custom` | 仅系统诊断 | 仅诊断 |

`page` 来源（与 `detectPageKind` 对齐后再映射到 AiChatRequest.page）：

| detectPageKind / 路由 | 抽屉 | AiChat `page` |
| --- | --- | --- |
| entity 列表 | 关 | `list` |
| entity | add/edit 开 | `form`（mode=add\|edit，id=主键或 0） |
| entity | 详情开 | `detail` |
| object | — | `object` |
| home / `/home` | — | `home` |
| custom（Db/File） | — | `custom` |
| unknown | — | `custom` |

`DynamicPage` / `DefaultList` / `DefaultObject` 在激活时写入 `appStore.aiContext`；离开路由清空 typePath 但保留 session。

### 2.6 fill_form

SSE `type=tool` 且 `event=done` 且 `name=fill_form` 时，`json.value` 为 JSON 字符串，形状与 Cube.Vue 相同：

```json
{ "kind": "fill_form", "values": { "FieldName": "…" } }
```

解析失败静默。`appStore.aiContext.applyFill(values)`：

- 无登记或当前不是 add/edit：**忽略**，`Message.info('请先打开添加或编辑')`
- 有登记：只合并 **当前 add/edit 分区字段名**（大小写不敏感），只读/主键跳过；成功 `Message.success` 含字段个数；然后现有表单校验照旧

### 2.7 run_js

与 Vue 相同：`new Function(script)`，禁止 eval 字符串以外封装。失败回传 `{ok:false,error}`。不在本号收紧后端白名单。

## 3. B — 批量启停

### 3.1 门禁真值表 `resolveBatchEnableState`

输入：`viewKind`、`canEdit`、`setting.enableSelect`、`hasEnableField`、`selectedCount`。

`hasEnableField` = `listFields` 中 `isEnableField`（name 忽略大小写 `enable`）为真。

| viewKind | canEdit | enableSelect≠false | hasEnable | selectedCount | visible | disabled |
| --- | --- | --- | --- | --- | --- | --- |
| 非 `table` | * | * | * | * | false | true |
| table | false | * | * | * | false | true |
| table | true | false | * | * | false | true |
| table | true | true | false | * | false | true |
| table | true | true | true | 0 | **true** | **true** |
| table | true | true | true | ≥1 | true | false |
| table | true | true | true | >200 | true | **true**（点击也拦截） |

一次最多 **200** 条（GET `keys=` 查询串；超出 Message.error「一次最多启用/禁用 200 条」）。

`enableSelect` 缺省 true（与 `PageSetting.EnableSelect` 默认一致）。

### 3.2 交互

「高级」菜单顺序（有则显示）：导入 → 导出 → **批量启用** → **批量禁用** → 批量删除 → 批量修改 → 表单布局。

- 启停：`Modal.confirm` 标题「确认启用已选 N 条？」/「确认禁用已选 N 条？」；确定后调现有 API，`reason` 不传。
- 成功：`Message.success` 用接口 `Message`（如 `共启用[3]个`）；清空 `selectedKeys`；`loadData()`。
- 失败：`formatApiError`；不清空选择。
- `code=500` 且 count 语义「未找到」：Message.error，不假装成功。

### 3.3 `advancedVisible`

现码仅 `canImport \| canExport \| batchDelete.visible \| isAdmin`，导致 **只有 Update、无导入导出删除时，「批量修改」进不了菜单**。改为并上：

`batchEnableState.visible \|\| (flags.canEdit && selectedKeys 逻辑不要求已选) \|\| batchDelete…`

更精确：`batchEnableState.visible \|\| flags.canEdit \|\| 原条件`。`canEdit` 为真即显示高级（内含批量修改，空选禁用提交——e483 已如此）。

### 3.4 文件地图

| 文件 | 计划 | 冻结 |
| --- | --- | --- |
| `web/src/core/utils/viewMapping.ts` | `resolveBatchEnableState`；导出类型 | `resolveBatchDeleteState` 语义不改（仍仅 table） |
| `viewMapping.spec.ts` | 真值表 | |
| `web/src/views/crud/listContext.ts` | `batchEnableState`；改 `advancedVisible` | |
| `web/src/views/crud/useListCrud.ts` | `confirmBatchEnable(true\|false)` | 行内徽标 `onToggleEnable` 不改 |
| `web/src/views/crud/DefaultList.vue` | 两个 `a-doption` | 不把启停做成顶栏主按钮 |
| `EntityController.EnableSelect` | **不改** | 签名、GET、Find("ID") |

既有限制（本号不修，verify 记残余）：`ParseKeys` 仅 `ToLong()>0`；非数字主键批量启停 count=0。

## 4. C — 条件填色（对齐飞书「设置填色条件」）

对照截图：**不是**筛选构建器的多条件 AND/OR，也**不是**视图配置抽屉 Tab。入口、规则行、操作符与竞品弹层逐项锁定。

### 4.0 截图对照（抄什么 / 砍什么）

| 飞书截图 | 本号 | 理由 |
| --- | --- | --- |
| 工具栏「填色」+ 数字徽标（点徽标清空） | 做 | 与现有筛选/分组同构 |
| 弹层标题「设置填色条件」 | 做 | |
| 标题旁问号 | 可选 `a-tooltip`，文案见 §4.1 | 不接外部帮助站 |
| 「智能全局配色」 | **不做** | 无后端配色服务；另号 |
| 规则行：柄 + 色 + 范围 + 字段 + 操作符 + 值 + 删 | 做 | |
| 范围：单元格 / 整行 / 整列 | 做，并在整行前插入 **行侧边** | 见 §4.3 双通道 |
| 弹层顶栏自然语言「描述用什么颜色」 | **不做** | 无配色 LLM 接线；AI 浮窗另段 |
| 卡片填色 | 按钮显示；范围 **行侧边 / 整行**；侧边=卡片左缘竖条，整行=标题条 | 不涂卡片底/字段/图/操作 |
| 操作符：等于/不等于/包含/不包含/为空/不为空 | 字符串字段即这 6 项；其它类别走 `FILTER_OPS_BY_KIND` | 不自造第二套操作符 |
| `+ 添加条件` | 做；满 50 禁用 | |
| 无确定/取消页脚 | 做：改即 `updateFormat` | 与筛选弹层「应用/存为方案」不同，勿抄页脚 |
| 多维表分组头上的填色 | 不做 | 分组头不是数据行 |
| 看板/日历/甘特工具栏填色 | 按钮隐藏 | 看板可吃同源 RecordCard 的 side/title，但不提供入口 |

实施时对照现码：`FilterBuilderPopover` 有 apply/save；`FormatPopover` **禁止**复制这两枚按钮。

### 4.1 入口与互斥

工具栏顺序（table/tree/card）：

```
[筛选 n] [分组 n] [填色 n] [搜索] [自动化?] [高级]
```

- 按钮文案 **「填色」**；有规则时 `tb-act is-active` + 右上角圆形徽标数字 = `format.length`；点徽标 **清除全部规则**（同筛选徽标）。
- `chrome.showFilter` **不**控制填色。填色按钮：**table / tree / card**。kanban / calendar / gantt **不显示按钮**（`format` 仍存于 NamedView；切回表格或卡片即按该视图规则绘制）。
- 弹层：`a-popover` `position=bottom` `trigger=click`，标题 **「设置填色条件」**。与筛选/分组 **三选一互斥**：`activePopover: 'filter' | 'group' | 'format' | null`。
- **改即生效 + 持久化**：每次增删改序/改色/改条件调用 `evpStore.updateFormat`（仿 `updateFilter(..., immediate=true)`）。无「应用/保存」页脚。
- 无规则时打开弹层即用当前列表**第一字段**种一条默认规则（提升操作体验）。
- **不做**「智能全局配色」、不做弹层内自然语言配色。可选 tooltip：「背景色与行侧边各取从上到下第一条命中规则，可同时生效。」
- 图标：工具栏经 IconPark 站点确认后注册（优先 `background-color`，没有则 `platte`）；拖拽柄用已有 `drag`。
- 弹层 `width: max-content`（刚好包住规则行，不再 min-width 720px）。

### 4.1b Schema（NamedView.format，一条规则一个条件）

```ts
export type FormatApply = 'cell' | 'side' | 'row' | 'column';

export interface ViewFormatRule {
  id: string;              // `f_` + 时间戳36 + 4位随机36；非法/重复重生
  apply: FormatApply;      // 非法 → 丢弃（含未知字符串）
  color: string;           // /^#[0-9A-Fa-f]{6}$/ 否则丢弃
  field: string;           // 归一到 list canonical；空或不在列表 → 保留行但不命中
  op: ViewFilterOp;        // 不在该字段 FILTER_OPS_BY_KIND → 改为 eq
  value?: unknown;         // isNull/notNull 忽略
  bold?: boolean;          // 文字加粗；仅 true 序列化
}
```

文案：`cell` 单元格、`side` **行侧边**、`row` 整行、`column` 整列。下拉顺序固定为上述四项（卡片子集见 §4.2）。

**整行、行侧边都要选字段**（条件字段）。不要嵌套 `ViewFilter`。顶层 `format` 加入 `MANAGED_VIEW_KEYS`。最多 50，超出截断前 50。旧数据无 `format` → `[]`。`serializeNamedView`：`length>0` 才写出。误读到 `{ filter: ViewFilter }` 的旧草案形态 → **丢弃该条**。

非法值顺序：非数组→[] → 逐条校正 apply/color/op → 截断 50。缺 field 的行仍序列化。

### 4.2 规则行 UI（从左到右，与截图同构）

```
[⠿ drag] [色块] [单元格▾] [字段▾] [等于▾] [请输入] [×]
```

| 控件 | 锁定 |
| --- | --- |
| 拖拽 | 六点柄；拖动改变数组下标（上=优先）。不新增 npm。纯函数 `moveFormatRule(rules, from, to)` |
| 颜色 | 色块打开 3×10 预置色板（浅/中/饱和，含红橙绿蓝紫）；底部「文字加粗」。新建默认 `#FFF7E8` |
| 范围 | table/tree：`单元格 / 行侧边 / 整行 / 整列`（顺序锁定）。**card：仅「行侧边 / 整行」**。已有 cell/column 规则在卡片弹层仍列出，范围不可改成 cell/column，可改成 side 或 row |
| 字段 | 当前视图可见 list 列（与筛选候选同源） |
| 操作符 | `FILTER_OPS_BY_KIND` + `FILTER_OP_LABELS`（字符串即截图那 6 项；数字/日期/枚举/人员用 0015 矩阵）。**`apply=column` 时隐藏** |
| 值 | 与 `FilterBuilderPopover` **同一矩阵**。`opNeedsValue=false` 或 **`apply=column` 时隐藏** |
| 删除 | ×，立刻从数组去掉 |

底部 `+ 添加条件`；满 50 禁用。新建：table/tree `apply=cell`；**card `apply=side`**；候选第一字段、`op=eq`、`value=undefined`、`color=#FFF7E8`。切字段走 `resetCondForField`。

`formatApplyOptions(viewKind)`：`card` → `['side','row']`；table/tree → `['cell','side','row','column']`。

常量 `ROW_SIDE_WIDTH_PX = 3`。

### 4.3 命中与着色（双通道）

```ts
function ruleMatchesRow(row, rule, fields): boolean {
  if (!rule.field) return false;
  if (opNeedsValue(rule.op) && (rule.value === undefined || rule.value === '')) return false;
  return matchesViewFilter(row, {
    logic: 'all',
    conditions: [{ field: rule.field, op: rule.op, value: rule.value }],
  }, fields);
}
```

**背景通道** `resolveCellFormatColor(row, columnField, rules, fields)`：只看 `cell|row|column`，**跳过 `side`**。从上到下第一条命中范围的胜出：`column` **不看行条件**（仅字段名匹配即涂该列）；`cell`/`row` 需 `ruleMatchesRow`。

| apply | 该列是否铺背景 |
| --- | --- |
| `row` | 行命中 → 所有数据列 |
| `cell` | 行命中且 `columnField` ≡ `rule.field` |
| `column` | **无条件**铺满 `rule.field` 对应数据列（只选字段；操作符/值忽略） |
| `side` | **不进本函数** |

**侧边通道** `resolveRowSideColor(row, rules, fields)`：只看 `apply==='side'`，从上到下第一条命中的 `color`，否则 `undefined`。与背景通道独立，**可同时生效**（例如整列橙色底 + 行侧边红条）。

分组头：**不铺背景**。`apply=row`（整行）时勾选列与操作列与数据列铺同一底；`cell`/`column` 仍只涂数据列。侧边条画在实体行最左缘，见 §4.4。未命中 → `undefined`。与 OSC-0015 筛选独立。填色不是 ACL。

### 4.4 VTable / 卡片 / 其它视图

`useListTable.ts` 数据列用官方 `style`/`cellBgColor` 铺背景。

**行侧边（table/tree）**

- 仅实体数据行；分组头、空白垫行不画。
- 在该行**可视最左侧**画宽 `ROW_SIDE_WIDTH_PX`（3px）、高度=行高的实心竖条，颜色=`resolveRowSideColor`。
- 落点：有勾选/行号列时贴该 chrome 列**左缘**（与截图「行号旁竖条」同构）；无 chrome 列则贴第一数据列左缘。
- 实现优先 VTable 单元格 `borderLineWidth`/`borderColor` 的 left，或该列 `customRender` 画 3px 矩形；**禁止**为此新增一列数据字段。
- 竖条不改变勾选、行号、文字色。

**卡片**（`viewKind==='card'`）：

- `side`：`.record-card` **左缘** `border-left: 3px solid`（或绝对定位 3px 条），圆角内侧，**禁止**改整卡 `background`。
- `row`：仍只涂 `.record-card-title` 背景。
- cell/column：无视觉效果。
- `resolveCardTitleFormatColor`：仅 `apply==='row'`。
- `resolveRowSideColor` 下发为 `sideFormatColor`。
- 文件：`RecordCard` 增加可选 `titleFormatColor`、`sideFormatColor`；`CardList` 按行计算。

**看板**：不显示填色按钮。同源 `RecordCard` 可下发 side/title。日历/甘特不绘制。

### 4.5 文件地图

| 文件 | 计划 | 冻结 |
| --- | --- | --- |
| `viewProfile.ts` | 类型、`normalizeFormat`、serialize | filter/group/insight |
| `viewProfile.spec.ts` | 非法色、>50、旧 `{filter}` 丢弃 | |
| `core/utils/viewFormat.ts` + spec | `newFormatRule`、`moveFormatRule`、`formatApplyOptions`、`ruleMatchesRow`、`resolveCellFormatColor`（跳过 side）、`resolveRowSideColor`、`resolveCardTitleFormatColor` | 不改 `matchesViewFilter` |
| `stores/viewProfile.ts` + spec | `updateFormat` / `getFormat` | |
| `listContext.ts` | `viewFormat`；`activePopover` 加 `'format'` | |
| `useListViews.ts` | `onFormatChange` / `onClearFormat`；互斥 | |
| `FormatPopover.vue` + `useFormatPopover.ts` | 薄模板 + composable | **不改** ViewConfigDrawer |
| `DefaultList.vue` | 分组与搜索之间插入填色；card 也显示按钮 | |
| `useListTable.ts` | 数据列背景 + 行最左 3px 竖条；整行同时涂勾选/操作列 | 分组头不画侧边；cell/column 不涂 chrome 列 |
| `RecordCard.vue` + `useRecordCard.ts` | 标题行底 + 卡片左缘竖条 | 不涂整卡底 |
| `CardList.vue` / `useCardList.ts` | 按行下发 title/side 色 | |
| `KanbanBoard.vue` | 可选下发同一 title 色 | 不显示填色按钮 |
| 日历/甘特 | 不绘制 | |
| `iconRegistry.ts` / `iconComponents.ts` / spec | 填色工具栏图标 | 已有 `drag` |
| `FilterBuilderPopover.vue` | 值控件可抽公共片段供 Format 复用 | 筛选语义不改 |

## 5. 核心文档影响

| 文档 | 动作 |
| --- | --- |
| `web/README.md` | AI 浮窗、高级启停、工具栏填色弹层 |
| `Doc/功能清单.md` | SPA-7 说明补 AI 接线；DATA-13 不动 |
| `Doc/Api/核心接口架构.md` | 可补 GetAiConfig / AiChat 已有行（若缺失） |
| `ArcoVue企业中后台迁移方案.md` | §3.1「批量其它操作」改为已接线启停；§10.4 工具栏占位勾掉本项 |
| `竞品分析报告.md` | 文首/§6.1 #1#3#4 标注本号 |

不改 OSC-0018、不改 e483 编号。

## 6. 测试设计

### Vitest

- `aiMarkdown.spec.ts`：`<script>` 被转义；换行 breaks。
- `aiSse.spec.ts`：多行 `data:`、残缺 JSON 跳过。
- `aiChatContext.spec.ts`：`Admin/User`、`User`、空 path。
- `aiWelcome.spec.ts`：无名→管理员；list/form 快捷行与 Tab 归属；提问 Tab 无项。
- `viewMapping.spec.ts`：启停真值表含 0 选中、>200。
- `viewProfile.spec.ts`：format 单条件归一；旧 `{filter}` 丢弃。
- `viewFormat.spec.ts`：先匹配胜出；row vs cell；side 不进背景通道；背景与 side 可同时命中；空值 eq 不命中；`column` 无条件铺该列；`moveFormatRule`；`resolveCardTitleFormatColor` 忽略 cell/column/side；`formatApplyOptions('card')` 为 `side,row`。
- `stores/viewProfile.spec.ts`：`updateFormat` round-trip。
- `api-core`：`getAiConfig` → `/Cube/GetAiConfig` GET。

### 构建

```
pnpm --filter @cube/api-core test
pnpm --filter @cube/arco-vue test
pnpm --filter @cube/arco-vue build
```

不强制 `dotnet test`。若误改 Cube 工程则须 0 error。

### 手工

见 verify AC。
