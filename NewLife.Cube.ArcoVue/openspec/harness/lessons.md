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

- EntityViewProfile 命名视图：`ViewsJson` 权威 + `ActiveViewId`；`columnsJson` 与活跃视图同步；默认种子名「默认列表」（兼容旧「列表」）。
- VTable 适配层单点收敛；列宽/显隐/顺序走 debounce PUT；表头排序接 `sort`/`desc`。
- `DataField` 上若有 `Boolean Nullable` 属性，禁止写 `Nullable.GetUnderlyingType`——须 `System.Nullable.GetUnderlyingType`。
- 验收与范围外抛光分列：chrome/徽章/分组等不充当 M3a 硬 AC；冻结 UI 暂禁用须在 verify 记残留。
- 打开详情手势变更（单击→双击）必须同步 README/对接指南，避免「行点」文档漂移。

## 待办 — 字体规范（Harness）

- 后续按现代中后台常见 **组件/场景**（列表表头、单元格、表单标签、抽屉标题、徽章等）在 Harness 建立统一的 **字体 / 字号 / 字重** 规范，并替换各处临时字重（如 VTable `headerStyle.fontWeight: 400`）。
- 列表布尔徽章勿用 `borderRadius: 999`：短文案「是/否」会视觉成圆；用小矩形圆角（≈ Arco `--border-radius-small` / 4px）。

