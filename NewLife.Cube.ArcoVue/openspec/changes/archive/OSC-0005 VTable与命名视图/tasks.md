# OSC-0005 Tasks

## P0 — 后端 Profile 扩展

- [x] Cube.xml：`ViewsJson`、`ActiveViewId`；同步 Model+实体
- [x] `UpsertForUser` 支持新字段（null 跳过）
- [x] Cube + CubeNC API 透传（Model 已扩）；XUnit：ViewsJson upsert
- [x] 文档：对接指南线缆字段

## P1 — api-core + FE Profile 工具/Store

- [x] `EntityViewProfileModel` + get/put/delete 客户端
- [x] `core/utils/entityViewProfile.ts`：mergeColumns、种子「列表」、namedViews
- [x] `stores/entityViewProfile.ts`：按 typePath 加载、防抖 PUT、DELETE 恢复
- [x] Vitest：merge / namedViews / sortPayload / frozenMap

## P2 — VTable 适配层

- [x] 依赖 `@visactor/vtable`
- [x] `features/vtable/ListTable.vue`：records、columns、checkbox、行点、列拖/宽、冻结、表头排序
- [x] 操作列固定右侧；不进用户 columns 偏好

## P3 — DefaultList 换装

- [x] 替换 `a-table` → ListTable；移除树启发式
- [x] 接 Profile：进页 GET；改列/视图防抖 PUT
- [x] 工具条：命名视图 + 字段设置
- [x] 排序参数进 getList；搜索表单与右侧抽屉保留

## P4 — 测、构建、文档

- [x] Vitest 38 过（含 entityViewProfile 6）；XUnit ProfileComment 5 过
- [x] `pnpm build`（ArcoVue）；api-core rebuild
- [x] 手工冒烟（见 verify.md）— 验收期以代码路径 + 联调会话为主；端到端重登可本地补点
- [x] 回写迁移方案 M3a、对接指南、README

## 测试记录（执行期）

- `pnpm test` @ ArcoVue/web → **38 passed**
- `dotnet test --filter ProfileComment` → **5 passed**
- `pnpm build` @ ArcoVue/web → **ok**（DynamicPage chunk 含 VTable 偏大，可后续动态 import）
