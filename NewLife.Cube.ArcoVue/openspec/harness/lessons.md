# OpenSpec Harness Lessons

跨变更教训库。由 `openspec-retro` 追加；勿删历史条目。

## 格式

```markdown
## OSC-YYMMDDxxxx — <日期>
- …
```

历史条目仍为 `## OSC-00xx — <日期>`，勿改写。新条目必须用新 ID，并同步在顶部「条目索引」追加一行（条目名 = 正文标题原文）；索引由 `harness/verify-lessons.ps1` 双向校验。

---

## 条目索引

| 条目 | 摘要 |
|------|------|
| 流程 — 2026-07-31 | 通用门禁：测试必跑、编号命名、可执行性细节 |
| 待办 — 字体规范（Harness） | 字号 / 字重规范；布尔徽章圆角 |
| OSC-0001 — 2026-07-31 | 代理可测化；npm 镜像；Auth 冒烟 |
| OSC-0002 — 2026-08-01 | 三实体：先改 Cube.xml 再生成；双栈 Link |
| OSC-0003 — 2026-08-01 | 可见列=list；右抽屉；校验对齐后端 |
| OSC-0004 — 2026-08-01 | 壳偏好进 UserProfile；api-core 必 build |
| OSC-0005 联调 — 2026-08-01 | devProxy 通配 Area + Accept bypass |
| OSC-0005 — 2026-08-01 | 命名视图 ViewsJson；VTable 适配 |
| OSC-0006 — 2026-08-02 | 改名收敛；PUT→POST 回退；默认视图 |
| OSC-0007 — 2026-08-02 | 权限位对齐；勾选/排序；remount key |
| OSC-0008 — 2026-08-02 | 提交归一化；字段名大小写；评论层级 |
| OSC-0009 — 2026-08-05 | Enable 复用批量接口；卡片样式；归档竞态 |
| OSC-0012 — 2026-08-05 | effectiveSearch 单源；URL 一次性；JSON 兜底 |
| OSC-0013 — 2026-08-05 | SystemJson 大小写坑；配置抽屉样式 |
| OSC-0014 — 2026-08-06 | 模板/个人 API 分离；域整体选取 |
| OSC-0015 — 2026-08-06 | 前端筛选；空值比较；分组勾选；序号丢弃 |
| OSC-0016 — 2026-08-08 | 补录即时化；搜索重写对照；GetPage 缓存 |
| OSC-0017 — 2026-08-08 | 图标按需引入；双源注册表；透传剔除 |
| OSC-0019 — 2026-08-10 | Gantt 补丁固化；canvas 测试限制；控件决策 |
| OSC-260813c3e9 — 2026-08-13 | 随机编号；SFC 机械门禁；context 共享 |
| OSC-2608139feb — 2026-08-14 | 页面种类探测；Cascader 归一；E2E skip |
| OSC-260815fa86 — 2026-08-16 | 自动化队列内存化；SSRF 基线 |
| OSC-2608178bdb — 2026-08-17 | Url 占位容错；ListField 分流纯函数 |
| OSC-260813397e — 2026-08-17 | 多租户冒烟；验证码归一；切换入口 |
| OSC-26081903c0 — 2026-08-20 | SFC ref+watch；竞品截图对照；协议优先 |
| OSC-260819e483 — 2026-08-21 | 局部更新只校验提交字段；复用现码 |
| OSC-26082097c1 — 2026-08-21 | 纯文档基线；TimeSpan≠时钟；只读公式 |
| OSC-260824fc7c — 2026-08-26 | 授权菜单未裁剪；权限解析分离 |
| OSC-2608280e9e — 2026-08-28 | Widget 租户 Where；短令牌；EmbedLayout |
| OSC-26082815a1 — 2026-08-29 | 角色空域阻断；三层压测；滚动条 |
| OSC-260830a1b2 — 2026-08-30 | 响应头不可读；groupBy 顺序；monorepo build |
| OSC-260902ef43 — 2026-09-02 | 菜单顶置 Sort；路由重置；slug 单源 |
| OSC-2608273d95 — 2026-09-14 | 锁定包版本比对；Link 隐形依赖；批量旁路 |
| OSC-26090347f1 — 2026-09-22 | 菜单连接勿中途改；依次签入口与引擎分离 |
| OSC-260903e2a4 — 2026-09-22 | 候选只富化不扩充；畸形形状保存即 400；$host 仅等值解析 |
| 流程 — 2026-09-15 | 共享规则单源；status 瘦身；lessons 索引；检查表压缩 |
| OSC-260922201a — 2026-09-25 | 发布校验按 kind 分支；UI 重构同步三件套；镜像防复活；排除统计忌文案耦合 |

---

## 流程 — 2026-07-31

- 触及前后端代码时：执行阶段必须跑单元测试；验收阶段须**本 OSC 新增单测全过**且**构建无错误**。不得以「无业务逻辑 / 仅配置」跳过跑测（纯文档/纯 openspec 文案除外）。
- 变更目录命名：新号 `OSC-YYMMDDxxxx <简洁中文描述>`（进行中与 archive 相同）；禁止仅编号、英文 slug、或 `max+1` 顺序号。历史 `OSC-00xx` 豁免不改名。
- 实施型 OpenSpec 要面向**小参数模型可准确执行**：design 必须逐文件写清组件/函数/状态及冻结不动的符号；条件分支给完整矩阵；JSON/DTO 给 schema、默认/非法值归一化与旧数据策略；UI 给 props/emits、阈值、断点、空态和范围外行为。verify 的 AC 必须覆盖正常、无权限、边界/非法输入、旧数据兼容，并给出命令和可判定结果；不得用「按需」「适配」「优化」等隐含决策替代细节。

## 待办 — 字体规范（Harness）

- 后续按现代中后台常见 **组件/场景**（列表表头、单元格、表单标签、抽屉标题、徽章等）在 Harness 建立统一的 **字体 / 字号 / 字重** 规范，并替换各处临时字重（如 VTable `headerStyle.fontWeight: 400`）。
- 列表布尔徽章勿用 `borderRadius: 999`：短文案「是/否」会视觉成圆；用小矩形圆角（≈ Arco `--border-radius-small` / 4px）。

## OSC-0001 — 2026-07-31

- 首跑样板：代理可测化（`devProxy.ts` + Vitest）优于直接改 `vite.config` 难测；勿放在被 `.gitignore` 的 `[Cc]onfig/` 目录下。
- 初版用「无业务逻辑」跳过单测被纠正；以后 proposal 触及 FE/BE 代码不得写「无单元测试」。
- npm registry 超时可用 npmmirror；CI 宜缓存 vitest。
- 后端未起时 Auth 冒烟记环境受限即可，不阻塞代理/文档 AC。

## OSC-0002 — 2026-08-01

- 后端三实体：先改 Cube.xml 再 xcode 生成，Biz 只写 Upsert/列表/删权；禁止大段手写实体骨架。
- 可测性优先落在实体业务方法（内存 SQLite + `DAL.AddConnStr`）；HTTP 401 若 design 要求，须有 API 宿主测样板，否则 verify 标明「控制器已实现、宿主测缺口」。
- Cube / CubeNC 双栈：API 与 csproj `Link` 新实体/Model 必须同步，否则 net10 宿主缺类型。
- EntityComment 删父不级联：前端消费方（OSC-0008）需容错 ParentId 指向已删节点。

## OSC-0003 — 2026-08-01

- GetPage.**list 数组即可见列**；勿用 `DataField.visible` 过滤（Fill 不置 true，默认 false 会滤空整表）。
- 记录表单抽屉 **必须右侧弹出**（`placement="right"`）；已写入 `.cursor/rules/arcovue-record-drawer.mdc`。
- 保存校验对齐后端：`!Nullable` 必填；提交去自增 PK/空数值；展示 `ApiError.fieldErrors`，禁止裸「保存失败」。
- JSDoc 中勿写 `` `**/…` ``（`*/` 会截断块注释，esbuild 报怪错）。
- 加宽范围 OSC 批准后须同步改迁移方案措辞，避免 §8/§13 与实现长期不一致。

## OSC-0004 — 2026-08-01

- 壳偏好走 UserProfile（`layoutJson`/`themeJson`/`workspaceJson` 字符串列）；FE 负责 parse/merge/防抖 PUT；CRUD **禁止**读 `userProfileStore`。
- 改 `@newlifex/api-core` **src 后必须 `pnpm build` 该包**（types 入口是 `dist`），否则 ArcoVue `vue-tsc` 看不到新导出。
- TagsView + keep-alive：动态页多为同名组件，须按**路由 name 具名包装**才能用 `:include` 关签裁剪。
- 401/`onUnauthorized` 全页跳转会丢内存 store，须同步 **clear localStorage 壳偏好**，否则多用户同浏览器串布局/主题。
- `appearance=system` 要监听 `prefers-color-scheme`；仅设一次 light/dark 不够。

## OSC-0005 联调 — 2026-08-01

- Vite `devProxy` **不能只代理 /Admin|/Cube**：业务 Area（如 `/School/Class/GetPage`）未命中时会返回 `index.html`，表现为 GetPage.list / 新增表单 / 字段设置全空。
- 修复：PascalCase Area 通配代理 + `Accept: text/html` bypass 回 SPA；改代理后必须重启 `pnpm dev`。

## OSC-0005 — 2026-08-01

- ViewProfile 命名视图：`ViewsJson` 权威 + `ActiveViewId`；`columnsJson` 与活跃视图同步；默认种子名「默认列表」（兼容旧「列表」）。
- VTable 适配层单点收敛；列宽/显隐/顺序走 debounce PUT；表头排序接 `sort`/`desc`。
- `DataField` 上若有 `Boolean Nullable` 属性，禁止写 `Nullable.GetUnderlyingType`——须 `System.Nullable.GetUnderlyingType`。
- 验收与范围外抛光分列：chrome/徽章/分组等不充当 M3a 硬 AC；冻结 UI 暂禁用须在 verify 记残留。
- 打开详情手势变更（单击→双击）必须同步 README/对接指南，避免「行点」文档漂移。
- 残留补齐：左冻结前缀钉住；操作列用横向比例命中分发动作（`opsAction.ts`）；`DynamicPage`/`DefaultList` 异步加载 + vite `manualChunks` 拆 VTable。

## OSC-0006 — 2026-08-02

- `EntityViewProfile` 已统一收敛为 `ViewProfile`：后端实体 / 控制器路由、`@newlifex/api-core` 类型与方法名、ArcoVue store/工具模块、README/对接文档必须同步改名，避免旧路径残留导致接口与类型双轨并存。
- `UserProfile` / `ViewProfile` 保存接口前端需兼容 `PUT → POST` 回退；部分宿主、代理或历史环境会放行读取但拒绝 `PUT`，若不兜底会出现「加载成功、保存 405」的伪联调通过。
- 列表页默认态要消费 `workspace.defaultView` 与 `workspace.pageSize`：当用户尚未保存个人 ViewProfile 时，`seedDefaultView` 负责首视图回落，分页条数也要与工作台偏好一致，避免壳偏好和 CRUD 默认值各走各的。

## OSC-0007 — 2026-08-02

- 「高级」菜单权限勿直接用前端自拟 `Auth.EXPORT/IMPORT` 位：后端菜单常见只有 Detail/Insert/Update/Delete；导入/导出应对齐 `VIEW`/`ADD`（或真实菜单权）做兼容，否则菜单项整片消失。
- VTable 勾选列必须用 `cellType`/`headerType: 'checkbox'`（误用 `type` 会渲染成截断文本）；不要 `watch(selectedKeys)` 后全量 `updateOption`，会冲掉勾选态。
- 服务端排序接管时：`sort: false` + `showSort: true`，`sort_click` 里按业务状态自行 none→asc→desc；若 `return false` 且不自管，表头图标会卡死。
- 卡片配置类样式变更要用 **CSS 变量 + remount key**（mapping 为真源），避免 scoped/异步组件下「配置已存、视觉不变」。
- 冻结范围外能力（如列表/树拖拽排序）即使可做，也应另立 OSC；本号试做后整段撤销，避免验收范围蠕变。

## OSC-0008 — 2026-08-02

- MVC 版（NewLife.Cube WebAPI）**未注册** CubeNC 的 `EntityModelBinderProvider`，`Insert/Update(TModel)` 走 System.Text.Json 直接反序列化 JSON body → 实体；System.Text.Json **拒绝** JSON 字符串绑定 `Int32`/数值枚举属性。ArcoVue 若把枚举/Lov 值字符串化提交（`String(o.value)` + `dataSource: Record<string,string>`）会报「请求数据格式不正确」。修复：提交前按字段元数据归一化（`normalizeSubmitValue`：`"1"`→`1`）。
- **字段名大小写不一致（编辑表单空值坑）**：GetPage/GetFields 返回 `DataField.name` 为 **PascalCase**（`Name`），而 GetList/GetDetail 数据行 key 为 **camelCase**（`name`）。列表/详情用 `getValueByKey`（容错）正常，但表单 `model[field.name]` 直接索引取不到值 → 编辑窗口内容全空。修复：`normalizeKeysByFields`（url.ts）按字段元数据把数据归一化到 PascalCase key，在 `loadRecordIntoDrawer` 统一入口调用。新皮肤/新控件凡 `model[field.name]` 直取，必须先归一化或走容错取值。
- 评论 Tab（M4b）：`createCommentApi`（getList/post/remove）消费 `/Cube/EntityComment`；前端按 `parentId` 组装顶层+一层回复；删除仅本人显示、后端兜底；改 api-core src 后必须 `pnpm build`。
- **评论回复层级比 design 深**：实际实现三层嵌套（顶层 + 两级回复，最深不再展开），回复编辑框内嵌于被回复评论内部；Arco `a-comment` 嵌套子回复放 **default slot**（`.arco-comment-inner-comment` 内）。
- **头像徽章**：评论/回复/内嵌编辑框统一走 `UserAvatar` 组件；无头像回落 `avatarInitial(name)`（`Array.from` 取 Unicode 首字符；英文首字母 `toUpperCase`；空回落 `?`）。
- **watch 对象引用 vs 原地赋值**：抽屉切换记录时父组件 `Object.assign` 原地改 `formModel`，`watch(model)` 不触发；须 watch 主键值（`getValueByKey(model, pkField)`）驱动历史/评论重载。
- 抽屉「上一条/下一条」用 Arco `IconUp`/`IconDown` 图标 + `:disabled` 禁用态；切换记录同时清空未提交回复编辑状态。

## OSC-0009 — 2026-08-05

- **Enable 启停复用既有批量接口**（`EnableOrDisableSelect` 暴露 `EnableSelect/DisableSelect`）优于新造 `SetEnable`：少一条 API 路径、与 CubeNC 双栈对齐。
- **会话小任务补录固化**：验收前/复盘归档前，通过会话窗口完成但不在 OSC 计划内的事项/重构/修复，须按「独立 → 新增任务项、相似 → 并入已有任务项」补录到 `tasks.md`（已固化进 `openspec-verify`/`openspec-retro` 智能体动作）。
- 徽标在 flex column 交叉轴会被 `stretch` 拉伸成整行宽，需 `align-self:flex-start` + `max-width:100%` + `box-sizing:border-box`；横向排版需 `align-self:center` 防文本基线下沉。
- 卡片等高用「测量最大高度 → min-height 统一下发」而非 flex stretch，避免视觉拉伸；操作区以 grid 末行 + `margin-top:auto` 固定左下。
- 枚举/值集徽标悬停光标：列表 VTable badge 列 style 控制 `cursor:default`（非 Enable 不可点）；Enable 列才 `pointer` + `@click.stop` 防冒泡。
- 日期/时间前端必须按「壁钟时间」解析（忽略 `Z`/时区标识），否则 UTC 串被本地化换算造成时区漂移；按 `inferDateKind` 选 date/datetime/time 组件。
- 验收三步（实现审计 → 代码审查 → 文档同步）发现文档残留 `SetEnable` 旧接口描述：归档前必须修事实错误（附录B/实体控制器等），避免错误文档长期生效。
- 卡片间距/徽标等纯样式微调不新建任务，并入相似任务（T8/T9）即可；纯样式变更以构建成功为门禁，无需重跑全量单测。
- **归档竞态（0008/0009 两次复现）**：归档前用编辑工具修改 `retro.md`/`status.md`/`verify.md` 后执行 `Move-Item` 移动目录，编辑器文档缓冲会在旧路径重新写回这 3 个文件，导致 `changes/` 下残留重复副本（内容与 archive 哈希一致）。归档后**必须校验** `changes/` 下该变更目录已消失，若有残留直接删除。

## OSC-0012 — 2026-08-05

- **列表/统计/图表必须共用单一 `effectiveSearch`**：GetList 与 GetChartData 同源条件；过期图表响应用 `chartSeq` 序号丢弃，防止慢响应覆盖新搜索结果（快速切换筛选的经典竞态）。
- **筛选来源优先级 + 一次性 URL**：URL→当前视图保存条件→空条件；URL 只作为进入页面的一次性来源，**绝不自动持久化**，避免污染用户保存的筛选。
- **配置 JSON round-trip 必须保留未知字段**：insight 旧 `mode` 等历史字段安全迁移，未知键不丢失（删除即破坏用户配置）。
- **域解析（个人/模板/系统）用整体选取而非字段级 patch 合并**：解析函数单点演进（OSC-0012 → OSC-0014 扩展模板域），契约简单可预测，避免多套合并逻辑漂移。
- **PageSize 归属实体 ViewProfile（typePath 级）**：仅接受固定枚举（20/50/100/200/500/1000）；kanban/calendar/gantt 的「自动大页」只读本地展示，**不回写**普通页面偏好，也不写全局 workspace.pageSize——全局工作台值仅作旧配置种子。

## OSC-0013 — 2026-08-05

- **`SystemJson.Apply(options, true)` 第二参数是 `web`，不是 camelCase**：它**不设置** `PropertyNameCaseInsensitive` 与 `PropertyNamingPolicy`；MVC `[FromBody]` 反序列化默认大小写敏感。前端 api-core 的 camelCase 线缆（`typePath`/`formJson`/`filtersJson`/`pageSize`/`viewsJson`）**无法绑定**后端 PascalCase 属性 → 保存静默失败（`typePath=null` 400、其余字段 null），此前多次 OSC 的「保存成功」实际是前端内存态、刷新即丢。
- **修复**：`NewLife.Cube` / `NewLife.CubeNC` 双栈 `CubeService.cs` 在 `SystemJson.Apply` 后追加 `options.JsonSerializerOptions.PropertyNameCaseInsensitive = true`（ASP.NET Core 标准 web 实践，兼容 camelCase/PascalCase，不影响 OSC-0008 枚举数值归一化）。
- **判定技巧**：涉及 `[FromBody]` DTO 与前端 camelCase 交互时，用 XUnitTest 复刻 `SystemJson.Apply(options, true)` 反序列化 camelCase JSON 并断言属性绑定成功，避免「前端提示保存成功、后端未落库」的伪联调。
- 配置抽屉类 UI 的字段列表样式应与既有 `ViewConfigDrawer` 字段配置保持统一（`.field-list` 边框容器/max-height、`.field-item` border-bottom、`.drag-handle`、隐藏项 `muted` 变灰），避免同一产品两套视觉。

## OSC-0014 — 2026-08-06

- **模板 API 与个人 API 分离**（`/Cube/ViewProfileTemplate` vs `/Cube/ViewProfile`）：专用 endpoint + `Roles.Any(e => e.IsSystem)` 授权 + 固定 UserId=0，杜绝个人路径越权 body.userId=0 写入模板。
- **多域共存于同一 UserId=0 记录**：模板域（ViewsJson/FiltersJson）与全局唯一表单布局（FormJson，OSC-0013）共享同一条 `ViewProfile.UserId=0` 记录；删除/清空某一域时须判断 `hasContent` 保留其他域，避免误删共存数据。
- **三层解析按域整体选取**（`personal > template > system default`）：ViewsJson 与 FiltersJson 两域独立、不做跨域或字段级 merge；personal 仅 contentless 时回落 template；域整体覆盖比 JSON patch 可预测性高得多。
- **materialize 首次保存即提升为 personal**：前端 store 通过 `carryViews`/`carryFilters` 控制——仅在 source=personal 或 dirty 时才提交域 JSON；保存成功后 `viewsSource`/`filtersSource` 永久提升，后续不自动继承模板更新。

## OSC-0015 — 2026-08-06

- **筛选与搜索职责分离**：搜索 = 并入请求的关键字/字段条件（`effectiveSearch`，OSC-0012 不变）；筛选 = 对已返回数据的前端 `matchesViewFilter` 过滤（纯前端、不并发）。筛选为空时请求与基线完全一致，回归安全；对业务重写 `Search(Pager)`（如 Department 仅处理部分字段）的控制器前端兜底过滤保证生效。
- **比较运算符对空值必须显式语义**：`gte`/`lte` 用「非 lt / 非 gt」反推会把空值行（`compareValues` 返回 `'na'`）误判为命中，与 `isNull` 语义冲突；`>=`/`<=` 应显式 `==='gt'||'eq'` / `==='lt'||'eq'`，与 `gt`/`lt` 严格对称，并补空值边界单测。
- **视图门控要落到数据传递层**：UI 隐藏按钮 ≠ 状态不残留——`group-fields` 这类跨视图数据传递必须按当前视图能力（`isGrouped`）过滤，否则表格配置分组后切树视图，残留分组字段使 ListTable 进 groupedMode 跳过 hierarchy → 树结构丢失。
- **字段名转换对齐后端序列化**：前端把 PascalCase 字段名匹配后端 camelCase 数据 key 时须按 .NET `JsonNamingPolicy.CamelCase` 实现（`Type→type`、`ParentID→parentID`、`ID→id`、`URL→url`），仅首字母小写处理不了全大写缩写。
- **VTable 分组勾选**：分组场景 checkbox 必须走 `rowSeriesNumber(cellType/headerType:'checkbox')` + `groupConfig(groupBy/titleCheckbox/enableCheckboxCascade)`，勿用 tree/hierarchy 渲染分组（tree 模式会把 checkbox 列自动置为 tree 列）；级联状态读取须 `setTimeout(0)` 延后宏任务（VTable 内部级联监听在 setTimeout(0) 注册，同步读会拿到旧状态触发重置）。
- **异步请求序号 + 卸载清理**：防抖/远程搜索类组件（LOV LIST）需 `seq` 丢弃过期响应 + `onBeforeUnmount` 清理 timer，避免慢响应覆盖新结果与写已卸载组件；`watch(immediate)` 已覆盖 `onMounted` 时勿重复调用 load。
- **受控下拉误选同名选项**：冒烟脚本点击 Arco `a-select` 下拉选项时，页面可能存在多个同值选项（搜索面板/构建器的「公司」等），须作用域到**当前打开的 popup**（`.arco-trigger-popup`）精确点击。
- **验收标准须随实现演进同步**：verify.md 初版基于「筛选并入后端」方案，实现改为纯前端后若不同步更新会产生按旧标准验收失败/误判；验收前先对齐验收标准与最终实现（本号 AC-02/03/04 已重写）。
- **表单域不参与三层解析**：FormJson 为全局唯一（OSC-0013），所有人共享读取，与模板互不干扰，减少概念混淆。
- **前端 isAdmin 判定**（`roleName === '管理员'`）与后端 `Roles.Any(e => e.IsSystem)` 不完全对齐：安全关键路径在后端 403 拒绝，前端仅 UI 可见性控制；跨部署环境若角色名非"管理员"则管理入口不显示，建议后续对齐为菜单权限位判定。

## OSC-0016 — 2026-08-08

- **会话小任务必须即时补录**：T13/T14/T15（UserController 兼容、面板抽屉重构、GetPage 元数据）执行期直接完成但未即时登记 tasks.md，验收补录才发现 `UserController.Search` 重写有 3 处回归（🔴 空引用、🟡 Code 缺失、🟡 roleIds 多值语义）。执行期每完成一件计划外事项应立即补录 tasks.md + status.md。
- **重写"对齐原语义"的搜索方法必须逐项对照原实现**：XCode 原 `User.Search` 关键字模糊含 `Code`（登录名）、roleIds 多值逐 rid `Contains(","+rid+",")`、先判空后解引用——任一被"简化"都是静默语义漂移；此类业务重写应补针对性单测（本号靠代码审查兜住）。
- **`entity:` 内部值集协议**：LovController 内部实体查询不经 HTTP 外环，直走 EntityFactory 分页 + Q 模糊（`SearchWhereByKeys` 反射调用须缓存 MethodInfo + 校验参数签名 + try/catch 解包降级，勿每请求反射、勿裸 Invoke）；`Entity.` 值集 LovCode 命名（`"Entity." + FullName`）与手工 LovCode 并存时，`FixSearchMapCandidates` 用「值集是否已注册」哨兵缓存（`LovRegistered:` "1"/"0"）判定覆盖，避免逐字段查库。
- **分页偏移防溢出**：对外接口的 `(pageNum-1)*pageSize` 必须设 pageNum 上限（本号 100_000），否则 Int32 静默溢出产生异常 SQL。
- **Map 搜索候选分层**：小表（≤`CubeSetting.MaxDropDownList`）内联 `DataSourceMap`、大表注册 `Entity.` 值集走 LOV 远程搜索、手工已设 LovCode/DataSourceMap 优先不覆盖——三态决策矩阵必须在 design 写清，避免前端数字框裸输入。
- **`GetPage` 高频接口禁止逐字段查库**：值集注册判定（`LovRegistered:`）、目标表行数判定（`LovMapCount:`）均走 MemoryCache 60s；负结果（未注册/空表）也应缓存哨兵，否则高频接口每请求回源。
- **"清空查询参数" vs "重置查询参数"术语**：菜单项最终命名以代码为准（`__reset` IconRefresh「重置查询参数」），proposal/verify/README/功能清单四处文档曾残留旧名「清空」，归档前须统一术语（doc-sync 审计兜住）。

## OSC-0017 — 2026-08-08

- **图标库全量 install 会显著膨胀 bundle，须量化后按需引入**：`@icon-park/vue-next` 全量 `install(app,'icon')` 实测 +387KB gzip（主包 682→324KB）。落地：`iconComponents.ts` 唯一按需具名引入点 + `main.ts` 自定义 `<icon-park>` 聚合组件（按 `type` 查表、未命中回退 `FALLBACK_ICON` 不抛错）。design §10 预置风险后应在 T1 立即量化验证，不要等验收。
- **图标注册表双源分层 + 单测双向锁死**：`iconRegistry.ts` 只存 kebab-case 名字串（纯函数可测）+ `iconComponents.ts` 只存 名→组件 映射；`iconRegistry.spec.ts` 用 `ICON_COMPONENTS` 覆盖断言，把「名字 ↔ 组件」双向锁死，新增图标漏登记会被测试拦截。
- **IconPark 全局聚合组件需自定义注册**：`es/all` 的 `install(app, 'icon')` 只注册各 `icon-xxx` 单组件，**不注册** `<icon-park>` 聚合组件；须 `app.component('icon-park', IconPark)` 手动注册才能用动态 `type` 渲染（design 与实际包行为差异，执行期修正）。
- **动态组件透传 props 要排除内部消费键**：自定义 `<icon-park>` 把 `{...attrs, ...props}` 透传时，`type` 会被当作普通 attribute 渲染到 SVG 根元素（`<svg type="...">` 冗余 DOM）；须解构剔除仅用于查表的 `type`，只透传图标有效 props。
- **fa 图标类名兼容多 token**：后端 `Icon` 可能是 `fa fa-user`（空格多类名）或 `fas fa-user`，`icon.replace(/^fa-/,'')` 只剥离一个前缀无法命中；先 `split(/\s+/)[0]` 取首 token 再查表，兜底（`fas` miss → 关键词/默认）保证不崩。
- **批量替换后自检格式与注释**：T4 替换 Arco 图标后遗留缩进瑕疵 + 头注释残留「install(app,'icon')」旧描述，验收代码审查才捕获；替换完成后应 grep 关键注释关键词 + 检查 diff 缩进一致性。
- **涉及既有控件形态的设计先与用户对齐最终形态**：「高级」按钮设计基线（文字前 more 图标）执行期被用户改右侧 down 箭头并撤销 min-width，来回 2 次；自定义主色布局也 3 次调整（独立行→第三行→徽标在标签下方）。此类视觉微调宜 demo 时给 2~3 个候选一次性对齐，避免逐轮会话往返。
- **删除登记同步清理 spec**：T4 后 `more` 不再使用但 iconComponents 保留登记 + spec 覆盖列表含 `more`，验收审查才删；替换完成后 grep 使用点同步清理未用登记与其 spec 条目。
- **外观设置动态保存即可删除「立即保存」**：`patchLayout/patchTheme → markDirtyAndSchedule(400ms) → saveNow` 自动持久化，显式保存按钮冗余；UI 保留「恢复默认」+ 同步状态标签即可（review 确认无手动保存诉求）。

## OSC-0019 — 2026-08-10

- **VTable Gantt 源码补丁用 postinstall 幂等脚本固化**：`switchToLevel`/`setMillisecondsPerPixel`/`updateScales` 三处冗余 `refreshAll` 去掉后切级 4→1 次全量重建；脚本内置版本检查告警，升级 `@visactor/vtable-gantt` 后自动提示核对。不用 patch-package（npm 不支持 `workspace:` 协议），不用 pnpm patch（避免重构 node_modules 风险）。
- **VTable Gantt 无表格宽度调整事件**：`GANTT_EVENT_TYPE` 无 `resize_table_width`，`state-manager.ts` 的 `endResizeTableWidth` 不触发事件→需用轮询 `gantt.taskTableWidth` + 防抖兜底。
- **VTable canvas 渲染与 Playwright 合成事件不兼容**：canvas 内部状态机（mousedown→pointermove 链）不响应 Playwright 派发的合成鼠标事件，canvas 交互类 AC 只能验证代码配置正确性，视觉效果需人工冒烟。涉及 VTable canvas 交互的 OSC 应在 verify 中提前声明此限制。
- **`position: relative` 是 absolute 子元素的前提**：VTable 分割线（`verticalSplitResizeLine`）为 absolute 定位，宿主 `.gantt-host` 缺 `position: relative` 时分割线相对页面定位（被页头遮挡），导致拖拽不可用。涉及 absolute 定位子元素时，design 阶段应显式声明宿主定位上下文。
- **同步 applyZoomLevel 消除两段式渲染跳动**：VTable Gantt `new Gantt()` 后 `ZoomScaleManager` 自动设初始级别（calculateInitialMillisecondsPerPixel），若异步延迟 `setZoomPosition` 会导致先渲染自动级别、再跳变目标级别。改为同步调用（`ZoomScaleManager` 在 `new Gantt()` 返回时已完整初始化），与实例创建同一渲染帧，消除跳动。
- **单 OSC 增量增强不宜超过原始范围**：本号原始 4 项核心能力 + 9 项执行期增量，增量占比 >200%。大跨度增量应评估是否拆分为独立 OSC，保持单 OSC 范围可控、验收审查轻量。
- **控件形态决策应在 design 阶段一次性确认**：缩放控件 3 次变更（选项框→移除滚动条→−/+ 按钮）与 OSC-0017「高级按钮形态」属同一教训——涉及控件形态的 UI 决策宜 demo 时给 2~3 个候选一次性对齐。
- **ResizeObserver 回调需尺寸对比拦截**：VTable 缩放/时间轴总宽变化等内部布局变化也会触发 RO，不比较宿主尺寸直接重建会造成「重建→切级别→再重建」循环。增加 `lastHostW/H` 对比，真正变化才重建。

## OSC-260813c3e9 — 2026-08-13

- **协作编号用日期+随机 hex、中间不加连字符**：`OSC-YYMMDDxxxx` 在 `changes/` 与 `archive/` 查前缀唯一即可并行创建；禁止 `max+1` 与预留空洞号。历史 `OSC-00xx` 不改名。
- **OpenSpec 不要绑定厂商模型名**：曾为 DeepSeek Flash/Pro 写「一次 1 个 T」后又整段撤销。执行粒度用任务可勾选、design 可对照即可，模型能力变化快。
- **SFC 抽离用机械门禁收口**：`sfcThin.spec.ts` 扫 `.vue` 的 `watch(`/`onMounted(`/`cubeApi.`，allowlist 必须清空。不要用「大约够薄了」代替回归。
- **大列表页先共享 context 再拆领域**：DefaultList 用 `createListContext` + deps 注入 `loadData`，避免四个 composable 各持一份 ref。
- **编号规则变更与存量重构宜拆号**：本号同时改协作规范与 47 个 Vue，tasks 过长；以后流程 OSC 与代码 OSC 分开。

## OSC-2608139feb — 2026-08-14

- **页面种类用探测真值表，不要按菜单路径堆专用页**：`GetPage` 有效→entity，失败后再 `GetFields`+GET 对象→object；仅 Index/Db/File 因契约必然失败才短路。任意新 ObjectController 子类即可出配置页。
- **Cascader / axios blob 的取值形状必须纯函数归一**：`path-mode` 数组 vs 标量、拦截器 `unwrapResponse=false` 下 xml 不会自动变 Blob。用 `leafFromCascaderChange` / `blobOf` 单测锁死，不要在组件里 `as Blob`。
- **E2E 深度 2C 要把「无菜单 skip」写成用例而不是删路径**：宿主（CubeDemo vs CubeDemoNC）菜单与控制器集合不同；skip 须写原因，禁止为绿而删。须明确后端是带 `/Auth/Login` 的 CubeDemo。
- **单 OSC 不要承接多轮「再改一下布局」**：本号在级联+Object+Home 之后又叠 Db/File、配置中心、表格列、工具栏位置多次往返。控件形态宜 demo 给 2～3 个候选一次对齐，或拆号。
- **并行 OSC 时不要把 `wwwroot` 混进复盘提交**：`pnpm build` 会打进工作区其它号的 chunk；本号只提交本号源码与 OpenSpec 归档。

## OSC-260815fa86 — 2026-08-16

- **禁止再把 `AutomationRun` 加回 `Cube.xml`**：终态审计只写系统 `Log`（Action=`Automation`）；队列用内存 POCO（`Automation/AutomationRun.cs`）；`GET /Runs` 与 dateArrive `once` 读 Log。2026-08-16 T12.1 曾为对齐当时 design 落独立表，与 Log 双写；2026-08-19 已删。实现审计勿把归档 tasks T12.1 / 旧 §2.2「必须落库」当现行约束。
- **实体写路径自动化挂 Persistence 包装，不要只挂钩 Controller.OnInsert**：导入、批量启用、业务直接 `Insert` 都必须能入队；字符串级 `Update(where)` 无实体实例可另记残余。
- **前后端 Filter 同构要单测锁死边角**：缺字段 `isNull`、`contains` 大小写、非数字比较、after/before 灵活比较；勿假设 C# 与 `matchesViewFilter`「差不多」。
- **found 循环语义要「连续段」不是「单节点 foreach」**：design 锁定后对每条 found 执行紧邻的 update/notify/addComment 整段；空 found 跳过段，勿对 target=found 报「无目标」失败。
- **全量 xcode 整份 Cube.xml 会再生重复中文文件名**：只增量目标表或生成后立即清理重复 `*流程`/`*配置` 副本，避免污染 git。
- **Webhook 限流字典先校验 token 再 TryAcquire**：无效 token 不要撑大 ConcurrentDictionary；并做过期/超量淘汰。
- **httpRequest 默认要 SSRF 基线**：仅 http(s)、拒 loopback/私网/链路本地；单测覆盖 localhost/192.168/file。

## OSC-2608178bdb — 2026-08-17

- **Url 模板占位与行 JSON 键必须大小写容错**：后端常写 `{ID}`/`{Id}`，GetPage 行多为 camelCase `id`；`resolveUrl` 仅做 Pascal↔camel 首字母翻转不够（`ID`→`iD`）。应在 page-utils 提供 `lookupRowField`（忽略大小写 / 全大写缩写），并在冒烟首日用真实链接验参。
- **ListField 分流先锁纯函数真值表再接线 UI**：`classifyListLink`（url / dataAction / hasTypeName）单测钉死后，表格/卡片/看板只消费 `partitionListFields`，避免各视图各写一套「算不算合成列」。
- **操作列配额与自动化分离**：自定义链接直出 `OPS_LINK_INLINE_MAX`、自动化直出仍 3，顺序 detail→edit→delete→自定义→auto→更多；勿让一类挤掉另一类语义。
- **卡片底栏宽度不足时动态收缩直出**：VTable 固定直出上限；卡片用 `ResizeObserver` 按 ops 区宽计算可放个数，溢出进「更多」+ `nowrap`，否则窄卡竖排/折行。
- **验收并复盘时剔除同会话无关 WIP**：文档标题、悬停阴影等不进本号 commit；并行 `wwwroot` 全量构建产物勿混提（沿用 fa86 lessons）。

## OSC-260813397e — 2026-08-17

- **多租户中间件路径要冒烟匿名接口**：`GetTenantId` 读 Cookie 时 `Cookies[key]` 可为 null，禁止 `.ToString()`；EnableTenant 打开后 `/Auth/LoginConfig` 必须 200。
- **验证码 image 载荷要归一再展示**：DrawingCaptcha 常回 `data:image/png;base64,…`，按 SVG 直接 `v-html` 会成乱码；统一 `normalizeCaptchaImageHtml`。
- **租户切换放登录后用户菜单，勿登录页 Code + 顶栏双入口**：飞书路径是先登录再选组织；顶栏下拉与工具条抢位，终验改为用户菜单 `a-dgroup`。
- **401 必须清租户会话**：`clearTenantSession` 与 token 一并清，避免下一用户沿用旧 `X-Tenant` 被 EnsureTenantUser 串绑。
- **租户用户列表勿死等 NuGet JOIN**：Cube 侧 `UserTenantSearch` 绕开歧义；单测锁「外租户不可见」。

## OSC-26081903c0 — 2026-08-20

- **SFC 薄壳内所有 `ref` + `watch` 组合一律进 composable**：无论多小的 UI 状态（如 `openColorIdx` + `watch(visible)` 重置），都不允许留在 `.vue` 文件内。`sfcThin.spec.ts` 门禁只检 `watch(`/`onMounted(`/`cubeApi.`，但 `ref` 本身虽不禁止，搭配 `watch` 时仍构成"业务逻辑"，应进 composable。验收阶段检出返工成本高——实施期即应遵守。
- **竞品截图对照表是范围蠕变的最强防线**：design §2.0 把「抄什么/砍什么」逐行锁定（标题不改、历史不做、搭建不做、回形针不做），执行期零蠕变。以后涉及竞品参考的 OSC，design 必须逐截图行给「做/不做/留给别号」三列。
- **AI 类 OSC 的协议对齐优先于 UI 对齐**：本号 AI 浮窗 UI 抄竞品右侧停靠布局，但协议（SSE type/body/fill_form/run_js）严格对齐 Cube.Vue 既有实现。UI 可创新，协议必须兼容——否则后端 API 碎片化。
- **填色规则「一条一条件」是正确的复杂度截断**：曾考虑 AND/OR 多条件组合，最终以「一条规则 = 一个条件」截断。50 条上限 × 4 种范围 × 单条件，已覆盖 90%+ 场景；多条件应另号评估。

## OSC-260819e483 — 2026-08-21

- **局部更新必须只校验提交字段**：PATCH / BatchUpdateFields 走 `ValidateEntityFields(..., onlyFields)`；整表单校验会把未提交必填打成失败，表现为「确定后不成功」。写路径改动阶段内应用真实表单冒烟，勿只跑单测。
- **复用 AutomationFilter / NotificationRecord / 现有 Log.Remark，禁止平行造轮子**：viewFilter、评论提及、历史 diff 均接线现码；不新增 AST / MentionsJson / LogProvider 装饰器。
- **双栈：共享文件改一处，非 Link 文件改两处；PATCH 勿进被 Link 的 EntityController2**：PrepareFieldsForApi / EntityTree / 评论 POST 各改 WebAPI+CubeNC；新写 Action 用 WebAPI-only `partial`。
- **浏览器冒烟勿整包留到验收**：S.1–S.7 类真实环境项宜阶段间穿插；否则只能「仅记录不补齐」放行。tasks 与 verify 冒烟项勿双份勾选。
- **必填语义三处同源**：后端 `PrepareFieldsForApi`、`X-Cube-Field-Validation`、前端 `isFieldRequired`；改字段元数据语义须同步三处并跑三套测试。

## OSC-26082097c1 — 2026-08-21

- **纯文档 OSC 的审计基线在 Draft 期固化**：四套枚举映射表与目录 A/B 写入 design.md，执行期只复核不重猜，是文档号理想节奏；执行期自审一轮抓出 7 处潜在错误（Int32+TimeSpan、duration 先于数值、formula 模糊命中收窄等）。
- **TimeSpan ≠ 时钟**：`typeName=TimeSpan` 或 `itemType` 以 TimeSpan 开头（含 Int32 秒如 OnlineTime）→ `duration`，展示友好中文省略零档；`itemType=time`/`Time:*` 才是时刻。duration 解析必须先于数值，否则秒字段变数字框。详情图标 duration=`timer`，禁与 DateTime 共用 `time`。
- **只读公式/查找的边界**：formula 仅 `itemType∈{formula,compute,computed}`（无「只读且不在表单」模糊规则）、值必须已在实体 JSON；lookup 仅 Map+BatchLabel 显示关联名；可写外键（RoleId/DepartmentId）不是 lookup；二者不进提交体；禁 projections、双向写回、浏览器求值。
- **名称启发式白名单词干 + Id/Ids 推断单多选**：`RoleId` 单选 / `RoleIds` 多选；主键恰好 `Id` 不误判；`CreateUser` 字符串快照列不是人员选择器；`Vip` 不是 ip——终端 IP 仅 `CreateIP`/`UpdateIP` 与显示名「创建地址/更新地址」，不是地图。审计字段（创建/更新用户、IP、时间）仍不进新增/编辑表单。
- **交叉核对 grep 必须锚定定义上下文**：`ITEM_TYPE_TO_CONTROL` 核对曾误把函数参数 `model:`/`fields:` 当映射键、无引号映射键被当缺失——脚本模式应限定定义块内，或核对后读源码二次确认，勿「False 即缺口」。
- **规范文档引用的图标名须当场核对 `iconComponents.ts` 登记**：`font-size-two` 未登记名在执行期纠错为 `font-size`（T7 补录）；实现号消费 labelIcon 前先跑登记断言。

## OSC-260824fc7c — 2026-08-26

- **授权目录必须用未裁剪的菜单列表**：`/Cube/MenuTree` 已按当前用户裁剪，不能当角色授权数据源；用 `GET /Admin/Menu` 分页全量。无查看权则树空并提示，禁止把原 Permission 清成空。
- **Role.Permission 与 Menu.Permission 禁止共用解析器**：前者 `menuId#flags`，后者 `flag#name`；目录空才回退 1/2/4/8。
- **详情抽屉若不用 FormContent，只读控件必须单独接线**：design 写「readonly 时树 disabled」不够；`RecordDrawer` 详情走 `formatDetail` 会把权限显示成编码串。
- **Cube / CubeNC 非 Link 控制器要 tasks 点名两处**：本号 Menu ReadOnly / Role 去列表列只改了 WebAPI；CubeDemo 不受影响，MVC 宿主会漏。
- **复盘提交遇到并行未入库 OSC 时宁缺列表壳、勿混打**：03c0 源码仍 untracked，与本号 B.4/B.5/D.1 同文件；独立 AI/填色/wwwroot 排除（沿用 fa86/8bdb）。

## OSC-2608280e9e — 2026-08-28

- **Widget Query 租户 Where 必须与 CreateWhere 同等并进 XUnit**：只写「fail-closed 无上下文」却漏租户模式 AND `TenantId`，多租户会跨租户泄数；验收审计才发现。
- **分享短令牌不能只认 JWT**：`LoadToken` 末尾 `Split(".").Length != 3` 会丢掉 `UserToken.Token`；不透明串须可加载，且 Authorization/Query 优先于 Cookie。
- **平台 kind 砍洞察槽交付时同步改 proposal/DASH/§8.5.3**：代码禁 miniKanban 而文档仍写三种 kind，会造成验收假通过。
- **SFC 薄壳：分享弹层的 watch 必须在 composable**：验收门禁 `sfcThin` 会拦 `.vue` 内 `watch`；`ShareViewPopover` 曾因此红。
- **EmbedLayout 必须 `height:100%` 锁视口**：仅 `min-height:100vh` 时内容被 `#app height:100%` 裁切，滚动条与底部分页器消失。

## OSC-26082815a1 — 2026-08-29

- **角色模板空 `widgets:[]` 是有效域，会阻断系统种子**：尚未配置时点保存会钉死空墙；空保存应 `Clear`（PUT `""`）或拒绝，并提供「清除模板」。
- **三层优先级单测必须交叉压测**：只测「单层命中」不够；至少要有「用户合法压角色」与「空串 HomeJson + 角色 → role」。
- **Arco Table + Scrollbar 勿用 scrollTop 做自动滚**：表体常无真实纵向滚动端口；离散数据窗口轮播更稳。
- **insight 禁 kind 扩到 dataList/dataCard 时同步改 proposal 目标 4 / Catalog / PUT / Host**：只写 miniKanban 会漏。

## OSC-260830a1b2 — 2026-08-30

- **前端读响应头不可靠**：`api-core.createRequest` 只返回响应体、丢弃响应头；需要响应头值时应在响应体透传（如 `ApiListResponse.FilterNarrowed`）。本次时间窗提示因读 `res.headers` 恒空，验收才暴露。
- **VTable groupBy 组顺序取决于 records 首现顺序**：`GroupConfig.sort` 不生效；时间分桶需按时间字段**预排序 records**（组间+组内近到远）。
- **功能按钮勿被条件渲染容器连带隐藏**：`enableKey=false` 时查询按钮组被 `a-input` 连带隐藏（AC-15），应独立渲染。
- **monorepo `@newlifex/api-core` 改 src 后必须 build**：types 入口是 `dist`，否则 ArcoVue `vue-tsc` 看不到新导出（本号再次验证）。
- **XCode 无 `NotStartsWith/NotEndsWith`**：design 列出的操作符若 XCode 不支持，应裁剪并记录，而非强行实现。
- **验收必补会话内增量**：日期时间分组/查询持久化分层/数据更新重算/重置刷新/UI 微调均在会话窗口完成、不在 OSC 计划，验收需补录（T21–T25）。
- **时间窗时区用本地 `DateTime.Now.Date`**，与既有 `dtStart` 本地惯例一致，避免 UTC 零点把本地当日 0:00–7:59 挤出默认窗口。

## OSC-260902ef43 — 2026-09-02

- **菜单“永远第一组”= XCode `BigSort` 下 `Sort` 降序，顶置取“根级最大 Sort+1”**：`EntityTreeSetting.BigSort` 默认 true，`Root.Childs` 按 Sort 大者在前；顶置要取当前最大值 +1（封顶 Int32.MaxValue），不要写死小数字，也不要只读到“当前已最大”而不 +1。
- **XCode 实例 `Childs` 缓存删除后残留 stale**：删除/重建父子判断一律走 `Menu.Meta.Cache`（按 Url/ParentID 查），勿信任 `group.Childs` 实例缓存（单测先暴露）。
- **动态路由只注册一次（router `routesLoaded`）**：新增可导航菜单（发布命名工作台）后须 `resetMenuRoutesFlag() + userStore.fetchMenus()` 再 push，否则新路由不命中、侧栏不刷新（复用 useShellToolbar 租户切换同款）。
- **单测 SQLite 库跨 run 持久会污染“隐藏/可见”断言**：曾因失败残留隐藏父分组导致后续运行全红；集合测试应在 class fixture 每次运行重建库文件。
- **图标名/白名单只保留一份纯函数源**：slug 白名单曾三处复制且 menuRoutes 内联 `+` 正则漏掉 1 位合法 slug；统一 import `workbenchNamed.isValidNamedSlug`。图标/显示名映射同理只放 `iconRegistry`（`MENU_NAME_ICONS`），spec 用 `all.add(...)` 锁值有效性。
- **另存为（create 新对象）必须与“更新自身”区分**：统一 upsert 端点会让“另存为撞已存在 slug”静默覆盖既有共享看板；请求体加 `create=true`（已存在 → 409）+ 前端对 `namedList` 预检红字，重命名/发布更新不带 create。
- **列表“可见”过滤要与读授权同一语义（沿父链）**：只滤叶子 `Visible` 不够，父分组隐藏时列表仍点开即 403 死胡同；抽 `IsChainVisible` 叶子→根逐级判断（角色声明判定另属 IsAccessible）。
- **后端命名常量用中文显示名直接落代码，文档务必同步**：顶级组显示名“工作台→系统看板”涉及 ParentTitle、EnsureFirstGroup 幂等同步、图标键、proposal/design/verify/ui/迁移方案/功能清单多处；改一处忘同步会在 doc-sync 冒 P1。

## OSC-2608273d95 — 2026-09-14

- **验收判据必须钉到锁定包版本**：本地 XCode 仓库 HEAD 可能领先 NuGet 锁定包（本次 `ac108a773` 2026-09-11「本人数据始终可访问」晚于包 `12.2.2026.901` 2026-09-01），按仓库源码写 AC 会得出「实现不符」的假结论；冒烟先用 SQL 对照包内行为，再判定 AC 真伪。
- **`<Compile Include>` 链接文件是隐形依赖**：`Widgets/System/*.cs`、`AI/CubeTools.cs`、`Common/*` 由 `NewLife.CubeNC.csproj` 链接编译，在 WebAPI 项目新增被链接文件需要引用的共享类型时，必须同步补 `Link`，否则 MVC 栈编译失败（本轮首次编译即暴露）。
- **批量写库是行权旁路高发区**：`BatchInsert/BatchUpsert/BatchReplace/factory.Merge` 既不经过实体层拦截器（宿主内休眠）也不走逐行 `Valid`；导入必须前置显式校验归属，合并类还要按主键查已存在行，且 Zip/Db 包内可按类名反射任意实体数据集，需另校验目标实体页面权限。
- **页外出口要有一份共享助手**：部件、AI 记录上下文、导入等出口各自直查实体时行权必漏；统一到 `CubeDataScope`（`GetFilter`/`CanAccess`/`IsForgedOwner`）后，新增出口只需一行接入，且双栈行为一致。
- **`AdminOnly` ≠ 行权**：系统角色也可能持有非「全部」的 DataScope，内置部件统计（用户总数/在线/24h 日志）仍会放大，须与页面列表同口径。
- **测试注入范围的省事做法**：单测无 `ManageProvider.User` 时 `GetFilter(factory, null)` 会回落 `DataScopeContext.Current`，部件类集成测试直接 `DataScopeContext.Current = ctx`（用完复原）即可验证接线。
- **手写测试实体没有 `_` 字段访问器**：XCode 生成的 `_` 只在生成实体上，手写测试实体断言表达式应改用生成实体（如 `User._.ID`），否则 CS0117。
- **导入等批量入口的「拒绝」优于「跳过」**：违规静默跳过会让用户以为导入成功；改为拒绝整次并给出主体/归属原因，同时把校验方法留 `virtual` 供派生类放宽，兼顾安全与可扩展。
- **归档后必须回查旧路径（本次复现）**：`Move-Item` 之后编辑器的文件缓冲会把已编辑内容回写旧路径，本次产生 `changes/OSC-2608273d95 角色数据范围行权/verify.md`（与 `archive/` 同名文件哈希一致），且**创建时间晚于移动时间**（22:35:39 vs 22:31:31）；按规则哈希一致即直接删除。归档校验要放到**移动后至少再查一次**（本次第一次查询在回写前，得到「无残留」的假绿灯），必要时用 `LastWriteTime` 判断是否移动后回写。

## OSC-26090347f1 — 2026-09-22

- **`Menu.Meta.ConnName` 不能中途改**：XCode 会话绑在第一次访问时的库。并行用例若已碰过 `Menu`，测试再改连接名并 `File.Delete`，断言会打到新建的空文件（`Childs` 仍可能是旧对象，出现 5≠1 或 FindCount=0）。菜单播种单测用当前连接；写锁单测调用 `Register(false)`，避免 `EnsureMenus` 与播种抢同一棵树。
- **能力下线要拆「入口」和「引擎」**：设计器不再新建依次签，但历史 GraphJson 的 `sequence` 仍要能展示和执行，否则已发布流程打不开。
- **宿主冒烟不要和单测门禁绑死**：WebAPI 引用核心库后 Meta 恒 enabled；「未引用模块返回 404」只能在不 Link 的 MVC 宿主上验，环境坏了就记 G-07，不要假装 AC 已点过。

## OSC-260903e2a4 — 2026-09-22

- **候选字段集「多源合并」要区分「扩充」与「富化」**：条件编辑器候选必须收敛到后端白名单（search∪list）；`AutomationMeta`（kind=all 全字段）只能按名富化 displayName/类型，整集并入会给出后端 400 的字段并暴露敏感列名。验收代码审查抓出 P1。
- **保存端校验不能只写「合法分支」**：`extraFilter` 非对象、`conditions` 元素非对象等畸形形状必须显式 400，否则保存 200、查询期反序列化才炸（「保存即反馈」落空）。验收审计抓出 P1。
- **`$host` 解析只认宿主筛选的等值（eq/空）条件**：非等值（after/gt）不会解析 —— 该条件被跳过、数据不收敛、`hostFilterApplied=false`；测试里别把「时间字段引用」想当然成按时间过滤。
- **测试的 `$host` 字段要与断言语义一致**：引用字段（`value.$host`）、宿主筛选条件字段、期望过滤三者对齐；本号调试时因 `$host` 名与断言混用导致假失败（读到的是另一条用例的残留语义），靠逐项诊断输出才定性。
- **git worktree 快照测试要当心**：worktree 不含 gitignore 的 `node_modules/dist`；历史提交的包名可能落后（`@cube/*` → `@newlifex/*`），快照侧 import 解析失败。门禁以当前树为准，快照只用于「隔离 WIP」场景且需先补依赖。
- **目录联接（junction）清理顺序**：用 mklink /J 补快照依赖后，结束时要先删联接本身再 `git worktree remove`，否则可能沿联接误删主仓 `node_modules`。

## 流程 — 2026-09-15

- **共享规则收敛到 `openspec/README.md` 单一事实源**：五壳 Agent 只写阶段动作，编号/门禁/补录/测试/目标愿景/status 写法/SFC/框架文档一律引用 README；四处副本各自漂移（如 VTable「教程」链接曾错指 arco.design）不再复现。
- **`status.md` note 瘦身**：note 只追加 1~3 行状态摘要；过程细节、审查报告、测试输出进 verify/tasks/retro。历史长 note 不追溯改写。
- **lessons 顶部「条目索引」与正文双向锁定**：retro 追加条目时同步加索引行；`harness/verify-lessons.ps1` 校验（不一致 / 重复条目 / 归档缺条目即失败）；「待办」提升到文件头部。
- **五壳瘦身**：批准检查表 8→5 条；apply 收尾门禁改为「补录 → 双查（代码审查+实现审计）→ 补齐 → 复核 → 记录」；verify 动作 7→5 步、补录细则引用 README，门禁语义不变。
## OSC-260922201a — 2026-09-25

- **发布预校验必须按 kind（六选人）分支**：`validateGraph` 曾统一用 `wfRecipientTo(to).ids.length===0` 判缺人，对 `{kind:'manager'|'starterPick'|'field'}` 恒 `ids=[]` → 「部门负责人或签」等主打场景从设计器发布被前端拦截（验收期 🔴）。六选人协议下「无 Id」≠「缺人」：仅 users/roles/departments 校验 Id；manager 恒合法；starterPick 交提交侧；field 交后端；同类预校验先写 kind 维度用例。
- **UI 形态重构要同步 design/tasks/verify 三件套**：进度「节点流」被「意见时间线 + 只读画布」替代后，design §4.5 / tasks T5 / verify AC-15 仍描述旧形态，跨多轮反馈未被发现，直至验收审计才报 P1；重构 UI 时同步核对旧描述。
- **FlowGram 镜像过滤防「自我引用复活」**：镜像重建时保留旧节点的条件不能是「节点自身仍含 target/cases」（删除后仍会复活）；只能是「仍被现存节点 target 引用」。删分流节点后的回归即此坑。
- **排除统计勿依赖意见文案**：效率样本排除「自动跳过/自动通过」若以意见字面量匹配，存在误伤面且措辞变更后静默失效；应采用结构化标记（本轮已入后续 OSC 候选，勿重蹈）。
- **跨库唯一冲突 409 忌硬查 “UNIQUE”**：`ex.Message.Contains("UNIQUE")` 在 MySQL/MariaDB（“Duplicate entry”）不匹配，409 会退化为 500；应收敛为可单测的辅助方法并按库/错误码扩展。