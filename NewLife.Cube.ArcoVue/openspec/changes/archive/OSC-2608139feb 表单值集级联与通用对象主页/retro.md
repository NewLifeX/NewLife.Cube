# OSC-2608139feb Retro

> 复盘在验收通过后由 openspec-retro 填写；归档后状态置 `Done`。

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | 26/26（AC-26 Star 本宿主 skip，代码路径 DefaultObject；E2E 20 passed / 17 skipped / 0 failed） |
| 三步编排 | 实现审计 ✅ → 代码审查 0🔴 + 2🟡 → 文档同步 ✅（SPA-7 验收期补记） |
| 自动化门禁 | Vitest **404/404** · api-core 69 · vue-tsc ✅ · vite build ✅ · `dotnet build` 0 Error · Playwright 20/17/0 |
| 代码质量 | 探测/取值/标签纯函数可测；新页均薄 SFC + `useXxx`；下载 `blobOf` 兼容拦截器形态 |
| 工期 | T1–T6 主路径 + T7 专用页/配置中心 + T8–T11 用户确认增量 |
| 手工/E2E 冒烟 | CubeDemo `:5000` + Vite `:5183`；User/Role/Menu/Department/Cube 实体与 Cube/Sys/Core/XCode/Db/File/Home |

## 实际完成范围

- Cascader `path-mode` 与叶子 ID 提交、懒加载 `load-more`、地区/LIST LOV 标签补齐、enum-like 提交转 number。
- `detectPageKind`：home / custom / entity / object / unknown；`DynamicPage` 按种类分发。
- 通用 `DefaultObject`（后演进为左列表配置中心）+ `ObjectController.GetFields` `PrepareForApi`。
- `DefaultHome` 对接 Index Main / ServerVar / Process / Assembly；MemoryFree/Restart 确认。
- `Admin/Db`、`Admin/File` 专用页；`FileController` Index/动作 JSON；Star 复用 DefaultObject。
- Playwright E2E（auth setup + entity-forms + object-home）。

## 做得好的

1. **契约探测而不是按路径白名单堆页面**：除 Index/Db/File 短路外，任意 ObjectController 子类自动落入 DefaultObject。
2. **取值形状用纯函数锁死**：`leafFromCascaderChange` / `blobOf` / `pageKind` 把拦截器与组件差异挡在单测里，避免再出现「选中变 undefined」「xml 当 Blob」。
3. **E2E 对缺失菜单 skip 而不是删用例**：CubeDemo 菜单不全时套件仍 0 failed，验收可重复。
4. **SFC 薄脚本从第一天遵守**：本号新增页均 `useXxx.ts`，sfcThin 53 全过。

## 待改进

1. **单号范围过大**：主需求（级联+Object+Home）之外叠加 Db/File、配置中心、表格列、多轮布局，tasks 从 T1–T6 长到 T11。大块 UI 增量宜拆号或在 design 阶段一次性对齐形态。
2. **E2E 深度 2C 依赖宿主菜单**：17 skip 全是 CubeDemo 未装菜单/无 Star 控制器，不能当成「这些实体已测过」。
3. **File「取消复制」只写在 composable**：模板未接线，design 矩阵有该项；清空剪切板可兜底，但验收才发现。
4. **`wwwroot` 与并行 OSC 冲突**：验收构建会打进工作区其它号的 chunk，复盘不得把混合产物提交进本号。

## 偏差

- DefaultObject UI：design §2.3 为 Category Tabs；执行期 T7 改为左菜单+分组不折叠+6/12 居中（用户确认），AC-08 仍满足「同一 DefaultObject + 保存」。
- Db/File 底部面板做过加/撤（T11），最终工具栏在表面顶部。

## 遗留与后续

- Star 在安装 StarController 的宿主上实网点验（🟢）。
- File 单条取消复制按钮（🟢）。
- `onMenuClick` 可下沉 `useDefaultObject`（🟢）。
- 与 OSC-260813397e 的 wwwroot/登录改动分开提交（本号已排除）。
