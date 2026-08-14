# OSC-2608139feb Tasks

> 进入 `Implementing` 后逐项勾选。顺序：T1 级联/标签 → T2 探测分发 → T3 Object → T4 Home → T7 Db/File 专用页与 Star 验证 → T5 E2E → T6 构建与文档。每项完成后先跑对应单测再推进。

## T1 级联、枚举提交、详情/列表标签

- [x] 1.1 新增 `web/src/core/utils/cascaderValue.ts`：`leafFromCascaderChange`（数组末段 / 标量 / 空→undefined）。`cascaderValue.spec.ts` 覆盖这三类。
- [x] 1.2 `CascaderField.vue`：`path-mode=true`，绑定 `:load-more`。`useCascaderField.ts`：`onChange` 改用 `leafFromCascaderChange`；实现 `loadMore` 调 `ensureChildren` 后 `done`。
- [x] 1.3 `normalizeSubmitValue`：`isEnumLikeTypeName` 与 `Enum` 同样把纯数字字符串转为 Number。`fieldControl.spec.ts` 增 1 例（`SexKinds` + `"1"` → `1`）。
- [x] 1.4 新增 `areaLabels.ts`：`collectCascaderIds`、`mergeAreaLabel`。`useListQuery.hydrateAreaLabels` 批量 `getDetail('/Cube/Area')` 写入 `areaLabelCache`。`listContext.renderCell` 传入 `areaLabelCache`。
- [x] 1.5 `hydrateLovLabels` 回写 `detailFields`/`editFields`/`addFields` 同名 lov 字段 dataSource。`detailFormat.detailText` 支持可选 cache（cascader + lov）。`useRecordDrawer` 打开前补齐当前行标签。
- [x] 1.6 `detailFormat.spec.ts` / `fieldFormat.spec.ts` / `areaLabels.spec.ts` 补测；`pnpm exec vitest run` 相关文件全过。

## T2 DynamicPage 种类探测

- [x] 2.1 新增 `pageKind.ts`：`isValidEntityPageMeta`、`detectPageKind`（home 短路 / custom / entity / object / unknown），实现与 design §2.2 真值表一致。
- [x] 2.2 `pageKind.spec.ts`：至少 4 例对应真值表四行。
- [x] 2.3 `useDynamicPage` 探测并缓存 `pageKind`；`DynamicPage.vue` 按 kind 异步挂 DefaultList / DefaultObject / DefaultHome；unknown 用 `a-empty`。
- [x] 2.4 无 Section 覆写时不得再无条件渲染 DefaultList。

## T3 通用 ObjectController 页

- [x] 3.1 `ObjectController.GetFields`：`GetMembers` 后逐字段 `PrepareForApi()`。`dotnet build NewLife.Cube` 无错误。
- [x] 3.2 api-core：`getObject(type)` = `GET {type}`。已有 `update` 复用。`api.spec.ts` 增 URL 断言。
- [x] 3.3 `objectForm.ts`：`groupFieldsByCategory`、`mergeObjectModel` + spec。
- [x] 3.4 `DefaultObject.vue` + `useDefaultObject.ts`：GET + GetFields + enrich + FieldInput + PUT；权限不足只读；空字段 empty。遵守 SFC 薄脚本。
- [x] 3.5 手工冒烟（执行期）：打开 `/Admin/Cube` 与 `/Admin/Sys`，确认不是实体表格，开关/输入可改并保存成功（或权限不足只读）。

## T4 主页仪表盘

- [x] 4.1 api-core：`getIndexMain`、`getServerVarList`、`getProcessList`、`getAssemblyList`、`memoryFree`、`restart` 指向 `/Admin/Index/*`。单测 URL。
- [x] 4.2 `DefaultHome.vue` + `useDefaultHome.ts`：Main 全键 descriptions；三块列表；分块刷新；MemoryFree/Restart 确认框 + Update 权限显隐。
- [x] 4.3 `home/index.vue` 只渲染 DefaultHome，删除四个 value=0 统计。
- [x] 4.4 `/Admin/Index` 经 pageKind=`home` 使用同一 DefaultHome。

## T5 Playwright E2E（2C）

- [x] 5.1 增加 `@playwright/test`、`web/playwright.config.ts`（baseURL `http://localhost:5183`）、`package.json` 脚本 `test:e2e`。`e2e/auth.setup.ts` 登录写入 `playwright/.auth/user.json`（`E2E_USER`/`E2E_PASSWORD`，默认 admin/admin）。
- [x] 5.2 `e2e/entity-forms.spec.ts`：design §5 可写实体表——打开页、点添加（只读实体 skip）、断言抽屉内对应控件（select/switch/cascader）存在且选项或开关可见；打开首行详情，对已知字典/级联字段断言文本不是纯数字 ID（无数据则 skip）。
- [x] 5.3 只读实体：列表容器可见且无「GetPage 响应无效」。
- [x] 5.4 可选实体：无菜单 `test.skip`。
- [x] 5.5 `e2e/object-home.spec.ts`：`/home` 与 `/Admin/Index` 系统信息 ≥3 项；`/Admin/Cube`、`/Admin/Sys` 有保存、无「添加记录」；含 Db/File/Star 用例（7.8）。
- [x] 5.6 文档化运行：`verify.md` 命令在本机（前端 5184 + 后端 CubeDemo 5000）跑通；失败实体写明 skip 原因（菜单/权限/无数据），无静默删用例。

## T7 Admin/Db、Admin/File 专用页与 Admin/Star 验证

- [x] 7.1 后端 `FileController` 最小修复（Cube 与 CubeNC 同步）：`Index` 返回 `Json(0, null, new { current, list })`；Delete/Compress/Decompress/Upload/Copy/CancelCopy/Paste/Move/ClearClipboard 返回 JSON 而非 Redirect。
- [x] 7.2 api-core：`getDbList`、`backupDb`、`backupAndCompressDb`、`downloadDbSchema`、`getFileList`、`uploadFile`、`downloadFile`、`compressFile`、`decompressFile`、`copyFile`、`pasteFile`、`moveFile`、`cancelCopyFile`、`clearClipboard`、`deleteFile`。`api.spec.ts` 增 URL 断言。
- [x] 7.3 `pageKind` 真值表新增 `Admin/Db`、`Admin/File` → `custom`；`DynamicPage.vue` 按 kind 挂 Db/File 专用页；`pageKind.spec.ts` 增 2 例。
- [x] 7.4 `views/admin/db`：`useDbPage.ts` + 薄 SFC。列表卡片（名称/类型/版本/备份数，不含连接串）+ 备份/备份并压缩/下载架构。
- [x] 7.5 `views/admin/file`：`useFilePage.ts` + 薄 SFC。目录导航、排序、上传、下载 blob、压缩/解压、复制/粘贴/移动/删除、剪切板提示。
- [x] 7.6 `/Admin/Star` 走 DefaultObject（探测自动落入 object），手工冒烟验证渲染与 PUT 保存。
- [x] 7.7 `useDbPage.spec.ts` / `useFilePage.spec.ts`：纯函数（排序/路径解析/剪切板过滤）补测。
- [x] 7.8 E2E：db/file 页列表可见、操作按钮存在；star 页有保存按钮（无菜单则直接 URL 访问）。

## T6 验证与文档

- [x] 6.1 `pnpm --filter @cube/arco-vue test` 全过（含本号新增，395 例）。
- [x] 6.2 `pnpm --filter @cube/arco-vue build` 无错误。
- [x] 6.3 `dotnet build` `NewLife.Cube` 无错误（CubeNC 同步 0 error）。
- [x] 6.4 `pnpm --filter @cube/arco-vue test:e2e`（后端已启动）按 §5 清单执行：20 passed / 17 skipped / 0 failed，skip 原因见 verify.md。
- [x] 6.5 `web/README.md` 登记三种宿主与 E2E 命令。迁移方案仅在确有「首页/配置页缺口」旧述时最小回写。
- [x] 6.6 验收 doc-sync：`Doc/功能清单.md` SPA-7 补记 DynamicPage 分发 / DefaultObject / DefaultHome / Db·File / Cascader 叶子 ID 与 E2E（OSC-2608139feb）。

## T7 魔方设置优化（DefaultObject 配置中心）

> 需求：①外围底部面板、展示区与主题一致；②description 用 Arco 官方方式展示；③参数按 Category 分组；④左列表右配置，自动注入 ObjectController 配置页。

- [x] 7.1 新增 `web/src/core/utils/objectPages.ts`：`collectObjectCandidates`（菜单树 → 两层 URL 可见候选，去重、排除当前页）+ `objectPages.spec.ts`（2 例）。
- [x] 7.2 `useDefaultObject.ts`：`currentType` 切换、`objectPages` 自动发现（detectPageKind 探测 + 会话级缓存）、`activeCategory`/`visibleGroups` 分类过滤、`openKeys` 子菜单自动全部展开；load/save 全部按 `currentType` 走。
- [x] 7.3 `DefaultObject.vue`：左 `a-menu` 配置页列表，当前对象 Category 作为子菜单（受控 open-keys 自动展开）；右 `obj-surface` 主题表面（`--color-bg-*`/`--color-border-*`）；Category 分组 section 不折叠、占满面板；每个配置项占一行居中排版、宽 6/12（`isFullWidthControl` 全宽 12/12）；每个 `a-form-item` 增加 `:tooltip="f.description || undefined"`（Arco 官方 label 问号气泡）；底部 `obj-footer` 常驻保存面板、按钮右对齐（参照默认列表视图 `list-pager`）。
- [x] 7.4 验证：`objectPages.spec.ts` 2 例 + 全量 vitest 397 全过；`pnpm build` 通过；E2E `object-home` 8 passed / 1 skipped（含左菜单/子菜单断言）；浏览器人工冒烟：/Admin/Cube 五分类子菜单自动展开、切换分类/对象正常、tooltip 悬浮显示 description、每项一行居中 0.48≈6/12、底部保存按钮右对齐。

## T8 首页进程模块表格

- [x] 8.1 `useDefaultHome.ts` 新增 `formatSizeMb`（字节 → MB 两位小数，非正数/非法返回空）与 `processTableRows`（后端 ProcessList 七字段映射），补 2 用例。
- [x] 8.2 `DefaultHome.vue` 进程模块表格改七列：名称/公司/产品/说明/版本/大小(MB)/文件名（宽列名省略+悬浮提示）。
- [x] 8.3 程序集表格改六列：名称/显示名/文件版本/版本/编译时间/文件位置；`useDefaultHome.ts` 新增 `assemblyTableRows`（后端 AssemblyList 六字段透传）+ 1 用例；后端 AssemblyList 保持原始六字段（name/title/fileVersion/version/compileTime/location）。
- [x] 8.4 验证：vitest 400 全过、build 通过；5183 热更新实测进程模块七列（CubeDemo.exe 0.18 MB）与程序集六列（CubeDemo / 魔方WebAPI示例 / 6.8.2026.0814 / 6.8.9722.22109 / 编译时间 / dll 路径）。

## T9 数据库页下载修复

- [x] 9.1 根因：`cubeApi.client.request` 在 `unwrapResponse=false` 下返回完整 AxiosResponse，且响应拦截器仅对 octet-stream/arraybuffer 透传 Blob；`Admin/Db/Download` 返回 application/xml，`useDbPage.downloadSchema` 把 AxiosResponse 强转 Blob → `URL.createObjectURL` 报 Overload resolution failed。
- [x] 9.2 修复：`download.ts` 新增 `blobOf(res)`（兼容直接 Blob 与 AxiosResponse.data）；`useDbPage.downloadSchema` 改用 `blobOf` 解包（空则报错）；`useFilePage.download` 同步复用；新增 `download.spec.ts`（4 例）。
- [x] 9.3 E2E：`object-home.spec.ts` Db 用例增加「下载架构」download 事件断言（文件名 .xml）与「数据库备份」确认框断言（不点确认）；`test:e2e object-home` 8 passed / 1 skipped。
- [x] 9.4 验证：vitest 404 全过、build 通过、E2E 通过。

## T10 数据库卡片操作

- [x] 10.1 核实：后端 `Backup/BackupAndCompress/Download` 的 name 均为数据库连接名（`DAL.Create(name)`），操作本就针对指定数据库。
- [x] 10.2 `useDbPage.ts`：`confirmBackup(name, compress)`/`runBackup(name, compress)` 接收数据库名；移除全局 `backupName` 输入框；成功提示带库名。
- [x] 10.3 `index.vue`：参照实体对象卡片视图，三个操作按钮放入每个数据库卡片的 `#actions` 底部操作区（左对齐，`:deep(.arco-card-actions){justify-content:flex-start}`）；工具栏仅保留刷新。
- [x] 10.4 E2E：`数据库备份` 按钮改 `.first()` 点击（每卡片一个，避免 strict mode）；`test:e2e object-home` 8 passed / 1 skipped；5183 实测 Log 卡片确认框标题「确认备份 Log？」。

## T11 Db/File 页底部面板

- [x] 11.1 Db/File 页参照实体对象列表界面加主题底部面板：外层 flex column gap 12，`*-surface` 主题表面（`--color-bg-2`/`--color-border-2`/圆角 8，padding 16）承载工具栏/表格/卡片，`*-footer` 底部面板（统计居左、刷新按钮右对齐，与 DefaultObject.obj-footer 同源）。
- [x] 11.2 验证：vitest 404 全过、build 通过、E2E 8 passed / 1 skipped；5183 实测 Db（共 5 个数据库）、File（当前目录：根目录 · 48 项）。
- [x] 11.3 Db 页调整（用户确认）：数据库数量与刷新按钮移入 `db-surface` 顶部工具栏（统计居左、刷新居右），移除底部面板。
- [x] 11.4 样式（用户确认）：Db 卡片「数据库备份」用 `type="primary"` 主题色（备份并压缩 success）；File 操作区改为实体对象列表视图 RecordCard 同款操作按钮（原生 button + fill-2 底/text-1 字/hover fill-3，删除 danger 变体）。E2E 8/1/0。
- [x] 11.5 File 页调整（用户确认）：文件夹位置（面包屑）/文件数量/刷新/上传全部移入顶部工具栏（位置+数量+排序居左，刷新+上传居右），移除底部面板。
