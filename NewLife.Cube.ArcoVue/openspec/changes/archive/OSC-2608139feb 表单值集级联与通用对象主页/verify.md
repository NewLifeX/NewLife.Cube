# OSC-2608139feb Verify

> 状态：通过（openspec-verify）  
> 时间：2026-08-14T18:10+08:00  
> 触发：验收并复盘 OSC-2608139feb。  
> 编排：implementation-audit → code-review → doc-sync

## 执行阶段记录（openspec-apply）

- 范围扩展（用户确认）：`Admin/Db`、`Admin/File` 专用页 + `Admin/Star` 复用 DefaultObject 一并实现；后端 `IndexController` 仪表盘 JSON 化与 `FileController` SPA 最小修复纳入允许清单（proposal §2.7）。
- 后端改动：`FileController`（Cube 与 CubeNC）Index 返回列表 JSON、动作返回 JSON；`ObjectController.GetFields` 逐字段 `PrepareForApi()`；CubeNC `IndexController` Main 增加 AJAX 分支并搬运 ServerVarList/ProcessList/AssemblyList、MemoryFree 改 JsonRefresh；MVC `IndexController.MemoryFree` 改 `Json(0,...)`；顺带修复 `AuthController` 既有笔误 `o.Id`→`o.ID`。
- 前端新增：`cascaderValue.ts`、`areaLabels.ts`、`pageKind.ts`、`objectForm.ts`、`download.ts` 纯函数；`DefaultObject.vue`、`DefaultHome.vue`、`views/admin/db`、`views/admin/file`；静态路由兜底 `Admin/Index|Db|File|Star|Cube|Sys|Core|XCode`；菜单路由名 `menu-{name}`→`menu-{id}` 修复同名覆盖。
- T7–T11 为执行期用户确认增量（配置中心布局、首页表格列、Db 下载 Blob 解包、卡片操作、工具栏布局），已写入 tasks.md。
- E2E 环境：须 `CubeDemo`（5000）而非 CubeDemoNC（无 `/Auth/Login`）。CubeDemo 无 Star 控制器与部分实体菜单，对应用例显式 `test.skip`。

## 验收阶段记录（openspec-verify）

**自动化门禁复检**（2026-08-14，仓库根；前端 `PLAYWRIGHT_BASE_URL=http://localhost:5183`，后端 CubeDemo `:5000` 已起）：

- 本号相关 Vitest 13 files / **108 passed**（含 sfcThin 53）
- `pnpm --filter @cube/arco-vue test`：40 files / **404 passed**
- `pnpm --filter @cube/api-core test`：src 20 + node 49 = **69 passed**（含 getObject / getIndexMain URL 断言）
- `pnpm --filter @cube/arco-vue build`：`vue-tsc -b` + vite **exit 0**（chunk>500kB 既有警告）
- `dotnet build NewLife.Cube.csproj`：**0 Error**（警告可保留）
- Playwright：37 用例，**20 passed / 17 skipped / 0 failed（1.4m）**；skip 原因见下，无删用例充通过

**三步检查汇总**：

1. **实现审计**：proposal §3/§7 与 design §2 文件地图全部落地；T1–T11 勾选与代码一致。DefaultObject 由 design 初稿 Tabs 演进为左列表配置中心（T7，用户确认）。缺口：无阻断。
2. **代码审查**：0🔴。🟡 见风险：File 页 `cancelCopy` 已实现未接线模板；`DefaultObject.vue` 含 `onMenuClick` 薄分发（sfcThin 未扫函数定义）。未发明 design 外宿主页；无 `v-html` 注入本号路径；Db 卡片不展示连接串。
3. **文档同步**：`web/README.md` 动态页分发表已登记；迁移方案无「首页占位/配置页缺口」旧述故不改。验收期补 `Doc/功能清单.md` SPA-7（测试列 ✅ + 分发/级联/E2E 说明）。

**会话小任务补录**：验收 doc-sync 并入 T6.6，不新建独立任务项。

**验收结论**：全部 AC 通过（AC-26 Star 本宿主 skip，代码路径 DefaultObject、无手工页），checklist passed，保持 Validating，可复盘。

## 验收标准

### 级联与表单字段

- [x] **AC-01 Cascader 可选中**：`CascaderField` `path-mode=true` + `leafFromCascaderChange` 取数组末段；E2E User 添加抽屉级联控件存在。
- [x] **AC-02 Cascader 回显**：`useCascaderField.resolvePath` + `hydrateAreaLabels`；E2E User 详情软断言 annotation。
- [x] **AC-03 枚举/值集标签**：`PrepareForApi` + `enrichFieldsWithEnumDataSource` / Lookup / LovSelect；E2E User/Role 等抽屉控件可见。
- [x] **AC-04 详情/列表非裸 ID**：`detailText` / `formatFieldValue` 读 `areaLabelCache` + LOV dataSource；无缓存回退 ID（`detailFormat.spec`）。
- [x] **AC-05 布尔**：`resolveControl` Boolean → switch；详情「是/否」。
- [x] **AC-06 enum-like 提交**：`fieldControl.spec` `SexKinds` + `"1"` → `1`。

### 页面分发

- [x] **AC-07 实体仍走列表**：E2E `/Admin/User` 等可写实体打开添加抽屉（DefaultList），非对象表单。
- [x] **AC-08 Object 通用**：E2E `/Admin/Cube` `/Admin/Sys` `/Admin/Core` `/Admin/XCode` 均有保存、无「添加记录」。
- [x] **AC-09 探测**：`pageKind.spec.ts` 7 例覆盖 home / custom / entity / object / unknown。
- [x] **AC-10 未知页**：`DynamicPage` unknown → `a-empty`「无法识别页面类型」。

### 主页

- [x] **AC-11 `/home`**：E2E 系统信息 descriptions ≥3 + 刷新。
- [x] **AC-12 `/Admin/Index`**：与 `/home` 同一 DefaultHome（同用例）。
- [x] **AC-13 分块刷新**：DefaultHome 各块独立刷新按钮；失败该块 empty。
- [x] **AC-14 危险操作**：`canUpdate` 控制 MemoryFree/Restart；`Modal.confirm` 后才请求。

### 权限 / 空 / 旧数据

- [x] **AC-15 Object 无 Update**：`canUpdate` 假则表单 disabled、底部保存隐藏。
- [x] **AC-16 Object 无字段**：`a-empty`「无字段」，保存区要求 `fields.length`。
- [x] **AC-17 地区 ID 无详情**：`hydrateAreaLabels` 单 ID 失败忽略；展示回退原始 ID（spec）。
- [x] **AC-18 菜单缺失实体**：E2E `test.skip`，套件 0 failed。

### 暂缓保护

- [x] **AC-19 未删除**：`DefaultList`、`FieldInput`、`LovSelect`、`CascaderField`、GetPage 五分区仍在。
- [x] **AC-20 未做**：无非 Area 任意 URL 级联；无 `views/admin/star` 手工页。

### Db / File / Star

- [x] **AC-24 Db 页**：E2E 卡片 + 备份/备份并压缩/下载架构；`dbItemOf` 不含连接串。
- [x] **AC-25 File 页**：E2E 表格、排序、上传；后端 Index 返回 `{ current, list }` JSON。
- [x] **AC-26 Star 页**：无专用页；探测落入 DefaultObject。本机 CubeDemo 无 StarController → E2E skip（AC-18/23）；CubeNC 执行期已冒烟。

### 门禁

- [x] **AC-21 单测**：本号新增 `*.spec.ts` 全部通过（见命令摘要）。
- [x] **AC-22 构建**：`@cube/arco-vue` test+build 无错误；`dotnet build NewLife.Cube` 0 Error。
- [x] **AC-23 E2E**：37 用例执行完毕；**20 passed / 17 skipped / 0 failed**。skip：Star（无控制器）；Tenant/Parameter/OAuthConfig/MailConfig/SmsConfig/OAuthLog/UserStat/UserOnline/AppLog/TenantUser/AccessRule/UserConnect/UserToken/NotificationRecord/ModelTable/ModelColumn（CubeDemo 菜单未装）。

## 三步编排摘要

### 1. implementation-audit

- T1：`cascaderValue` / `path-mode` / `load-more` / enum-like 提交 / `areaLabels` / LOV 回写 dataSource — 齐。
- T2：`detectPageKind` 真值表 + custom 行；`DynamicPage` 按 kind 分发，无 Section 时不再无条件 DefaultList。
- T3：`GetFields`→`PrepareForApi`；`getObject`；`DefaultObject` + `objectForm`。
- T4：Index JSON API + DefaultHome；`/home` 薄壳。
- T5/T6：Playwright + README + 门禁。
- T7（专用页）+ T7（配置中心）+ T8–T11：用户确认增量均有对应代码与单测/E2E。
- 未越界：未做非 Area 通用级联、未搬 Cube.Vue 手工表单、未删微内核。

### 2. code-review

- 0🔴。
- 🟡 `useFilePage.cancelCopy` 已导出，File 模板仅「清空剪切板」，无单条「取消复制」（AC-25 未要求该项，不阻断）。
- 🟡 `DefaultObject.vue` `onMenuClick` 约 10 行分发逻辑在 SFC；sfcThin 只禁 watch/onMounted/cubeApi，可接受。
- 🟢 Db/File/Home/Object 新 `.vue` 均走同目录 `useXxx`；危险操作有确认框；下载经 `blobOf` 兼容 AxiosResponse。

### 3. doc-sync

- `web/README.md` 动态页分发 + E2E 命令 ✅
- 迁移方案无首页/配置页缺口旧述，不改 ✅
- `Doc/功能清单.md` SPA-7 验收期补记（T6.6）✅

## 自动化门禁

```text
pnpm --filter @cube/arco-vue exec vitest run --config vitest.config.ts
  pageKind / cascaderValue / fieldControl / detailFormat / fieldFormat /
  objectForm / areaLabels / objectPages / download / useDefaultHome /
  useDbPage / useFilePage / sfcThin
→ Test Files 13 passed; Tests 108 passed

pnpm --filter @cube/arco-vue test
→ Test Files 40 passed (40); Tests 404 passed (404)

pnpm --filter @cube/api-core test
→ vitest 20 + node:test 49; fail 0

pnpm --filter @cube/arco-vue build
→ vue-tsc -b && vite build; built in ~16.5s; exit 0

dotnet build NewLife.Cube\NewLife.Cube.csproj --no-restore
→ 0 个错误

$env:PLAYWRIGHT_BASE_URL='http://localhost:5183'
pnpm --filter @cube/arco-vue test:e2e
→ 20 passed / 17 skipped / 0 failed (1.4m)
```

## 风险

- CubeDemo 菜单不全导致 17 skip；Star 需 CubeNC 或安装 Star 控制器才能实网点验。
- 工作区并存 OSC-260813397e（登录 SSO/租户）与 OSC-0018 Draft；复盘提交必须排除无关 WIP。`wwwroot` 本次验收构建含 397e chunk，**不纳入本号提交**。
- File「取消复制」UI 未接线；清空剪切板可覆盖常见路径。
- 执行期范围从「级联+Object+Home」扩到 Db/File/配置中心/表格列/布局多轮，验收对照面大（记入 retro）。

## Checklist

- checklist: **passed**
- 可进入复盘：`复盘 OSC-2608139feb`（本消息已一并触发）
