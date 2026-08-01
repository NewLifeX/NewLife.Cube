# OSC-0003 Verify

> 状态：通过（openspec-verify）  
> 时间：2026-08-01T08:51+08:00  
> 触发：验收并复盘 OSC-0003。

## AC 对照

| AC | 结果 | 说明 |
|----|------|------|
| 叶菜单 B3 动态注册 + props.type/authId | ✅ | `menuRoutes` + `router.beforeEach` |
| DynamicPage 薄宿主 + DefaultList 微内核 | ✅ | `views/dynamic` + `views/crud/*` + `core/*` |
| 列表字段可见（非 visible 误滤） | ✅ | `listColumns` + 联调截图（部门/角色列齐全） |
| 右侧记录抽屉 | ✅ | `RecordDrawer` `placement="right"`；规则 `.cursor/rules/arcovue-record-drawer.mdc` |
| 保存链路：必填对齐 !Nullable + 错误可读 | ✅ | `submitPayload` / `apiError`；联调曾暴露「保存失败」已修 |
| LOV / 图表 / 树 / Section·apps | ✅（代码） | 组件与 `_demo` 样例就绪；LOV/树/Log 实网未逐项勾满 |
| Vitest 本 OSC 新增全过 | ✅ | 见下（含 listColumns/submitPayload/apiError 等） |
| `pnpm build` 无错误 | ✅ | vue-tsc + vite build |
| 未改其他前端框架（本号意图） | ✅ | 交付在 ArcoVue；工作区另有 Vue/patches 等非本号 WIP |
| 迁移方案 §8 / §10.3 / §13 | ✅ | 加宽 A2 + 右侧抽屉已回写 |

## 冒烟（联调证据）

| 实体 | 列表 | 搜索 | 抽屉 | 备注 |
|------|------|------|------|------|
| Admin/User | ✅ | ✅ | ✅ 右侧详情 | 会话截图 |
| Admin/Role | ✅ | ✅ | — | 列表列修复后截图 |
| Admin/Department | ✅ | ✅ | ✅ 右侧新增 | 非白名单但同链路 |
| Admin/Menu | ⚠️ | — | — | 树检测代码就绪，本验收未截图 |
| Admin/Log | ⚠️ | — | — | 历史 Tab 调 Log；独立页未冒烟 |

| 能力 | 结果 |
|------|------|
| 右侧抽屉 | ✅ |
| 暗色/折叠后列表仍可用 | ✅（契约：微内核不读 appStore） |
| Section/apps `_demo` | ✅ 代码样例 |
| 评论 Tab | 预留（软依赖 0002） |

## 测试验证记录

```text
> pnpm test
 Test Files  8 passed (8)
 Tests  22 passed (22)
 # 本 OSC 相关：url / fieldControl / permissions / menuRoutes /
 # listColumns / submitPayload / apiError + 既有 devProxy
```

## 构建记录

```text
> pnpm build
 vue-tsc -b && vite build
 ✓ built in ~7.9s
 # chunk >500kB warning only，无 error
```

## 验收三连摘要

### 1. implementation-audit

- 对照 proposal A2 加宽范围：B3 路由、微内核、fieldControl、LOV 组件、树检测、Chart、Section/apps、右侧抽屉、Vitest、迁移文档均已落地。
- 执行期缺陷已闭环：列表 `visible` 误滤；抽屉改右侧；保存错误吞掉与必填不对齐。
- 未越界：未做 UserProfile 壳 / VTable / FlowGram；未改 Cube.Vue/NaiveUI 业务代码（本号意图）。

### 2. code-review

- 契约隔离：DefaultList/RecordDrawer 不读布局主题 store。
- 权限用 `checkAuth`+数字 Auth，避开 page-logic 字符串键陷阱。
- 提交体 `prepareSubmitPayload` 去自增 PK/空数值，对齐 Identity 约定。
- 风险：DynamicPage 包体偏大（echarts）；LOV List 模式 UX 偏简；历史 Tab 依赖 Log 查询约定。

### 3. doc-sync

- 迁移方案 §8.1 / M1 / §13 与实现一致；验收时补「左→右」抽屉措辞。
- OpenSpec design/ui/proposal 已改为右侧；项目规则已记入 `.cursor/rules`。

## 风险

- Menu 树表、Log 页、LOV 实网、导入导出未在本验收逐条截图勾满——代码路径具备，建议 OSC-0010 收口冒烟补齐。
- 工作区并存 lezer-php patches / Vue package.json / api-core 改动，**勿与 OSC-0003 混提**。

## Checklist

- checklist: **passed**
- 可进入复盘：`复盘 OSC-0003`（本消息已一并触发）
