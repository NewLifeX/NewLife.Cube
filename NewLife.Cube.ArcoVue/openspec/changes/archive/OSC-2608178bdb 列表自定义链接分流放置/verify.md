# OSC-2608178bdb Verify

> 状态：**通过**（openspec-verify 2026-08-17）  
> 触发：验收并复盘 OSC-2608178bdb。  
> checklist: **passed**

## 执行阶段记录（openspec-apply）

- **测试**：`pnpm --filter @cube/arco-vue run test` → 48 文件 464 通过（执行收尾）
- **构建**：`pnpm --filter @cube/arco-vue run build`（vue-tsc + vite）通过
- **新增/关键**：`listLinkFields`、`useListOpsLinks`、`opsAction` 扩展、`useListTable`/`DefaultList`/`RecordCard`/`RecordDrawer`、`page-utils` `lookupRowField`
- **已知限制**：`{page:}` 占位；Map 空 Url 不前端补造

## 验收阶段记录（openspec-verify）

### 会话小任务补录

- 已并入 **T4**：`lookupRowField`（`{ID}`↔`id`）
- 已并入 **T7**：卡片 ops 防竖排 + `ResizeObserver` 自适应直出
- 同会话无关 WIP（浏览器标题、卡片悬停阴影）**不纳入**本号

### 三步编排摘要

| 步骤 | 结论 |
| --- | --- |
| 实现审计 | 愿景 4 目标均有代码落点；分流真值表 / `__ops` 顺序 / 多视图接线与 design 对齐；无 P0/P1 缺口 |
| 代码审查 | 无 🔴；🟢 卡片直出数可随宽度动态（优于固定 2），与 VTable 固定 `OPS_LINK_INLINE_MAX` 并存，可接受 |
| 文档同步 | `web/README.md`、`Doc/功能清单.md` DATA-4、`ArcoVue企业中后台迁移方案.md` 矩阵行均已回写 |

### 测试与构建门禁（验收重跑）

| 命令 | 结果 |
| --- | --- |
| `pnpm --filter @cube/arco-vue run test` | **50 文件 472 通过** |
| `pnpm --filter @cube/arco-vue run build` | vue-tsc + vite **通过**（chunk size 警告既有，非错误） |

### 目标愿景对照

| 目标 | 结论 |
| --- | --- |
| 1 字段挂 Url → 单元格可点 | ✅ `classify=cell` + `tableColumns.cellLink` + VTable 点击 |
| 2 合成/`dataAction` → `__ops` 不占空洞列 | ✅ `partitionListFields` + `selectListColumns` 过滤 |
| 3 dataAction 归一 + AJAX/导航 | ✅ `FieldMeta.dataAction` + `useListOpsLinks` |
| 4 多视图一致；日历/甘特次级入口 | ✅ card/kanban 底栏；`RecordDrawer` 标题区链接 |

### 缺口清单（用户决策：仅记录不补齐 → 验收通过）

| 级 | 缺口 | 处置 |
| --- | --- | --- |
| 🟢 | 完整浏览器点验 User「链接」/ CronJob「日志·马上执行」 | 代码路径已具备；记残余，不阻断 |
| 🟢 | `{page:}` 占位 | design 已知限制，另号 |
| 🟢 | MapProvider 空 Url 前端补链 | design 已知限制，另号 |

## 验收标准

### 分流与元数据

- [x] **AC-01 hasTypeName / dataAction**：单测覆盖
- [x] **AC-02 真值表**：`listLinkFields.spec` 覆盖 design §3
- [x] **AC-03 合成列不进表**：`selectListColumns` 过滤 + listColumns.spec

### 单元格与操作列

- [x] **AC-04 字段挂链接**：`tableColumns.cellLink` + click 导航（代码）
- [x] **AC-05 操作列顺序**：`buildOpsPartsWithLinks` 单测
- [x] **AC-06 更多溢出**：直出 2 + more 外挂菜单（代码）；卡片按宽自适应
- [x] **AC-07 dataAction**：`requestDataAction` + 刷新列表（代码）
- [x] **AC-08 配色**：`OPS_LINK_COLOR` 用于 link/more

### 多视图与边界

- [x] **AC-09 card/kanban**：RecordCard 同源直出+更多
- [x] **AC-10 calendar/gantt**：详情标题区 ops 链接
- [x] **AC-11 工具栏**：未往 topbar/高级塞行链接
- [x] **AC-12 无 Url 页面**：分流 none 时行为不变（既有测试回归）

### 门禁

- [x] **AC-13 单测**：全绿（472）
- [x] **AC-14 构建**：通过
- [x] **AC-15 文档**：README / 功能清单 / 迁移矩阵已回写
- [x] **AC-16 占位符大小写**：`lookupRowField` 覆盖 `{ID}`/`id`

## 手工冒烟清单

| # | 步骤 | 期望 | 验收 |
| --- | --- | --- | --- |
| 1 | Admin → 用户 → 「链接」 | 无空洞列；操作列可进 UserConnect | 代码路径 ✅；浏览器完整点验 🟢 残余 |
| 2 | Cube → 定时作业 →「日志」「马上执行」 | 日志导航；马上执行 AJAX 后刷新 | 同上 |
| 3 | 字段挂 Url 实体 | 单元格可点 | 代码路径 ✅ |
| 4 | 卡片视图 | 底栏含自定义链接；窄宽进更多 | 代码 + 单测 ✅ |
| 5 | 高级菜单 | 无行级 Url | 审计 ✅ |

## 必须保留（防误删）

- `buildOpsParts` CRUD 三动作与自动化 `auto:{id}` 能力
- OSC-0007 工具栏精简（不恢复自定义工具栏按钮）
- `@cube/page-utils` `resolveUrl` / `lookupRowField` 导出

## 已知限制（可接受）

- GetPage `url` 为空时的 MapProvider 自动链接不在本号前端补造
- `{page:xxx}` 占位若 `resolveUrl` 不支持，保留原文
- 完整浏览器冒烟依赖用户环境（VTable canvas 合成事件限制见 lessons）
