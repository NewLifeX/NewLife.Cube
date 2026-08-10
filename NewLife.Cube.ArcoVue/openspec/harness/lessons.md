# OpenSpec Harness Lessons

跨变更教训库。由 `openspec-retro` 追加；勿删历史条目。

## 格式

```markdown
## OSC-00xx — <日期>
- …
```

---

## 流程 — 2026-07-31

- 触及前后端代码时：执行阶段必须跑单元测试；验收阶段须**本 OSC 新增单测全过**且**构建无错误**。不得以「无业务逻辑 / 仅配置」跳过跑测（纯文档/纯 openspec 文案除外）。
- 变更目录命名：`OSC-00xx <简洁中文描述>`（进行中与 archive 相同）；禁止仅编号或英文 slug。
- 实施型 OpenSpec 要面向**小参数模型可准确执行**：design 必须逐文件写清组件/函数/状态及冻结不动的符号；条件分支给完整矩阵；JSON/DTO 给 schema、默认/非法值归一化与旧数据策略；UI 给 props/emits、阈值、断点、空态和范围外行为。verify 的 AC 必须覆盖正常、无权限、边界/非法输入、旧数据兼容，并给出命令和可判定结果；不得用「按需」「适配」「优化」等隐含决策替代细节。

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
- 改 `@cube/api-core` **src 后必须 `pnpm build` 该包**（types 入口是 `dist`），否则 ArcoVue `vue-tsc` 看不到新导出。
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

- `EntityViewProfile` 已统一收敛为 `ViewProfile`：后端实体 / 控制器路由、`@cube/api-core` 类型与方法名、ArcoVue store/工具模块、README/对接文档必须同步改名，避免旧路径残留导致接口与类型双轨并存。
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

## 待办 — 字体规范（Harness）

- 后续按现代中后台常见 **组件/场景**（列表表头、单元格、表单标签、抽屉标题、徽章等）在 Harness 建立统一的 **字体 / 字号 / 字重** 规范，并替换各处临时字重（如 VTable `headerStyle.fontWeight: 400`）。
- 列表布尔徽章勿用 `borderRadius: 999`：短文案「是/否」会视觉成圆；用小矩形圆角（≈ Arco `--border-radius-small` / 4px）。

