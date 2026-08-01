# OSC-0003 Tasks

> 状态：Implementing 完成待验收。代码范围仅 `NewLife.Cube.ArcoVue/**` + 文档回写。

## P0 — 路由与宿主

- [x] `core/utils/url.ts`：normalize / routeToApiPrefix
- [x] `core/utils/menuRoutes.ts`：叶节点 addRoute（B3）；apps `index.vue` 优先
- [x] `router/index.ts`：`beforeEach` + MenuTree 加载；移除主路径 catch-all
- [x] `layouts/default.vue`：菜单绑定 menus；最小改动
- [x] `DynamicPage.vue`：薄宿主（type/authId → DefaultList / 覆写）
- [x] DefaultList：GetPage + list + search + add/edit/delete + pk + getDetail
- [x] 权限：`checkAuth` + pageSetting

## P1 — 字段矩阵与批处理

- [x] 移植 `fieldControl.ts`（control/search/list/serialize）
- [x] `FormContent` + `FieldInput`：Arco 映射；缺件落入 `components/`
- [x] 列表列渲染（switch / dataSource / url / 日期等）
- [x] 导入（鉴权上传）、导出、批量删除
- [x] 表单校验（required 等）

## P2 — LOV + 图表

- [x] `lov-api.ts` + `LovSelect` / `LovSelectTable`
- [x] 搜索/表单/列表 BatchLabel
- [x] Toolbar 图表 + GetChartData + ECharts 弹层

## P3 — 树表

- [x] 层级数据 / TreeEntity 检测
- [x] `a-table` 树展示；Admin/Menu 冒烟（待手工）

## P4 — Section / apps / 抽屉

- [x] `useSections` + SectionKeyMap（与 Cube.Vue 同名）
- [x] 约定扫描注册；`apps/_demo` 整页 + ListPageHeader 样例
- [x] `RecordDrawer`：表单 Tab + 历史 Tab（Log）；评论 Tab 预留
- [x] 行点击 / 编辑走**右侧**抽屉；微内核不读 appStore 主题

## P5 — 测试与文档

- [x] Vitest：menuRoutes、fieldControl、url、权限门闩（**新增**）
- [x] 执行期：`pnpm test` — **16 passed**
- [x] 执行期：`pnpm build` — **成功**（vue-tsc + vite）
- [ ] 四实体手工冒烟记录到 verify.md（验收阶段）
- [x] 回写迁移方案 §8 / §10.3 / §13

## 明确不做（本号）

- [x] ~~UserProfile 主题/布局~~ → OSC-0004
- [x] ~~VTable / EntityViewProfile~~ → OSC-0005+
- [x] ~~改 Cube.Vue / NaiveUI~~

## 执行记录

| 命令 | 结果 | 时间 |
|------|------|------|
| `pnpm test`（ArcoVue/web） | 5 files / 16 tests passed | 2026-08-01 |
| `pnpm build`（ArcoVue/web） | success | 2026-08-01 |

### 本 OSC 新增测试文件

- `web/src/core/utils/url.spec.ts`
- `web/src/core/utils/fieldControl.spec.ts`
- `web/src/core/utils/permissions.spec.ts`
- `web/src/core/utils/menuRoutes.spec.ts`
