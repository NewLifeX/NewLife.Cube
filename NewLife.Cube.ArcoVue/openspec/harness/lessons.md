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

## 待办 — 字体规范（Harness）

- 后续按现代中后台常见 **组件/场景**（列表表头、单元格、表单标签、抽屉标题、徽章等）在 Harness 建立统一的 **字体 / 字号 / 字重** 规范，并替换各处临时字重（如 VTable `headerStyle.fontWeight: 400`）。
- 列表布尔徽章勿用 `borderRadius: 999`：短文案「是/否」会视觉成圆；用小矩形圆角（≈ Arco `--border-radius-small` / 4px）。

