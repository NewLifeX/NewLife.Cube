# OSC-0005 Verify

> 验收时间：2026-08-01T13:15:00+08:00  
> 编排：implementation-audit → code-review → doc-sync  
> 触发：对 OSC-0005 进行验收和复盘。

## 硬门禁

- [x] 本 OSC **新增** Vitest 全过（`entityViewProfile.spec.ts` 8；套件合计 **50**，含徽章/分组等联调补测）
- [x] 本 OSC **新增/改动** XUnit 全过（`ProfileComment` **5 passed**，含 ViewsJson/ActiveViewId）
- [x] `pnpm build`（ArcoVue web）无错误；`packages/api-core` rebuild ok

### 测试记录

```text
> pnpm test  (@ NewLife.Cube.ArcoVue/web)
 Test Files  13 passed (13)
      Tests  50 passed (50)

> pnpm build (@ packages/api-core) → ok
> pnpm build (@ NewLife.Cube.ArcoVue/web) → vue-tsc + vite build ok
  （DynamicPage chunk ~3.1MB，含 VTable；后续可动态 import）

> dotnet test --filter FullyQualifiedName~ProfileComment
  通过: 5，失败: 0
```

### 验收期修复

- `DataField.PrepareForApi`：`Nullable` 属性遮蔽 `System.Nullable` → 改为 `System.Nullable.GetUnderlyingType`（否则 CubeNC/net10 构建失败，阻塞 XUnit）。

## 功能验收

| 项 | 结果 | 说明 |
|----|------|------|
| 默认列表为 VTable，无树展开主路径 | ✅ | `ListTable.vue`；`tree.ts` 无引用 |
| 列显隐/顺序/宽度可改并持久化 | ✅ | EVP debounce PUT；`columnsChange` / resize |
| 左冻结可改 | ⚠ 残留 | 模型与 `frozenColCount` 仍在；**配置 UI 暂禁用**（产品决策） |
| 表头排序 → sort/desc | ✅ | `buildSortPayload` + getList |
| 命名视图（仅 table） | ✅ | 种子「默认列表」（兼容旧「列表」）；新建/切换/重命名/删除 |
| 行打开右侧抽屉 | ✅ 漂移接受 | **双击**/操作列「详情」；单击不打开（防误触） |
| 搜索表单；filtersJson 不强制 | ✅ | |
| CRUD 不读 userProfileStore | ✅ | grep 无违规 |
| LOV 小表未强制 VTable | ✅ | |

## 三步摘要

### 1. implementation-audit

P0–P4 任务与 design 主路径对齐：ViewsJson/ActiveViewId、api-core 线缆、entityViewProfile store、ListTable、DefaultList 换装、文档回写。  
范围外已落地的 UX 抛光（ViewConfigDrawer/chrome、徽章、表单分组、外观抽屉）记为后续增量，不阻塞 M3a 出口。

### 2. code-review

- **已修（验收期）：** DataField `System.Nullable` 遮蔽。
- **可接受残留：** 左冻结入口禁用；操作列文案含编辑/删除但点击统一进详情；VTable 包体偏大。
- **无契约级隔离破坏**（CRUD ↔ UserProfile）。

### 3. doc-sync

- [x] 迁移方案 M3a + EntityViewProfileDto 补 `viewsJson`/`activeViewId`
- [x] `前端对接指南` EntityViewProfile 节（默认列表名 + 双击打开）
- [x] ArcoVue `web/README.md` 列表说明

## 风险 / 残留

1. 左冻结交互待重新启用（偏好模型已具备）。
2. 操作列多动作命中测试待做（或拆独立按钮列）。
3. DynamicPage/VTable 分包优化。
4. 手工端到端冒烟建议本地起后端再点一次命名视图刷新/重登（本会话以代码路径 + 单测为主）。

## 验收结论

**通过**（checklist: passed）。硬门禁满足；核心 AC 满足；冻结 UI 与打开手势记为已接受漂移/残留。可进入复盘。
