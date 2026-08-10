---
applyTo: '**'
description: 愿景-实现进度差距分析与下一步行动计划
---

# 愿景-实现进度差距分析

> 分析日期：2026-08-01
> 基于 [docs/product/vision-and-roadmap.md](./docs/product/vision-and-roadmap.md) 四阶段路线图

## 总体健康度

| 维度     | 状态         | 说明                                                        |
| -------- | ------------ | ----------------------------------------------------------- |
| 单元测试 | ✅ 79 个通过  | fieldControl 映射矩阵 + FormContent 渲染 + ListFormDialog   |
| 主题合规 | ✅ 通过       | `check-theme-tokens.mjs` 零遗留违规                         |
| 构建     | ✅ 通过       | `pnpm build` 2m35s 完成                                     |
| 文档体系 | ✅ 良好       | 五层结构：architecture/reference/guides/standards/decisions |
| E2E 测试 | ⚠️ 薄弱       | 仅 1 个 CRUD 测试                                           |
| 类型检查 | ⚠️ 158 个错误 | 全部为预先存在的 apps/ 和 core/ 旧问题                      |

---

## 阶段 1：控制器即 CRUD 闭环 — 接近完成 ✅

### 已完成
- 默认页面引擎：`core/views/index.vue`（列表）+ `form.vue`（表单）+ `DefaultEntity.vue`（catch-all）
- 字段类型映射：`fieldControl.ts` 唯一真理源，19 种控件类型
- 命令式弹窗：`openListFormDialog()` + `ListFormDialog.vue`
- 数据导入/导出：`ExportFile` + `ImportFile`
- Section 覆盖机制：`useSections.ts` + `pageSections.ts` + Vite 插件
- 菜单动态路由：`menuRoutes.ts`
- 单元测试矩阵：`fieldControl.spec.ts` + `FormContent.fieldtypes.spec.ts`

### 关键差距
| 差距                     | 严重度 | 说明                                                                                                      |
| ------------------------ | ------ | --------------------------------------------------------------------------------------------------------- |
| DataSet 未集成到默认视图 | **高** | `core/dataset/DataSet.ts` 设计良好，但 `index.vue`/`form.vue` 绕过它直接用 `request` + `ref` 手动管理状态 |
| 表单校验缺失             | **中** | `FormContent.vue` 使用 `isRequired` 但未实现 `el-form` + `rules`，后端 nullable 未映射为前端校验          |
| 空态/加载态/错误态       | **中** | 列表无 `el-empty`，错误依赖全局拦截器 toast                                                               |
| 列表排序/列筛选          | **低** | 默认列表不支持 `sortable`/`filters`                                                                       |

---

## 阶段 2：最小覆盖 — 框架就绪，应用层未采用 ⚠️

### 已完成
- Section 系统：`SectionKeyMap` 定义 10 个 Section 边界
- Section 注册机制：`registerPageSections()` 通过 `provide/inject`
- Vite 插件：`cubeFront()` 自动扫描 `apps/*/src/views/**/[A-Z]*.vue`
- 文档：`customize-page.md` 详细说明覆盖步骤

### 关键差距
| 差距                                | 严重度   | 说明                                                                                               |
| ----------------------------------- | -------- | -------------------------------------------------------------------------------------------------- |
| 所有业务页面都是完整整页覆盖        | **严重** | `cube-admin` 20+ 页面、`cube-cube` 8+ 页面全部手写完整 `index.vue`，零 Section 覆盖                |
| 旧版搜索组件盛行                    | **中**   | 所有业务页面使用 `CubeListToolbarSearch`（旧版仅关键词搜索），而非 `ListSearchBar`（动态字段驱动） |
| 应用层未调用 `registerPageSections` | **中**   | Section 覆盖文件无法被自动发现                                                                     |
| 旧版 `CubeSearch.vue` 仍存在        | **低**   | 选项式 API，未被任何页面使用                                                                       |

---

## 阶段 3：AI 生成完整业务工作区 — 未开始 🔴

### 已完成
- `ai-iteration.md`：完整的 AI 工作流指南（定位→实现→验证→交付）

### 关键差距
| 差距               | 严重度 | 说明                                          |
| ------------------ | ------ | --------------------------------------------- |
| 无页面类型分类     | **高** | 无仪表盘/审批流/编辑器/报表等 `PageType` 定义 |
| 无非 CRUD 页面模板 | **中** | 默认引擎仅 list + form 两种模式               |
| 无跨实体协调模式   | **中** | 无主子表/关联数据选择/多步提交模板            |
| E2E 测试薄弱       | **中** | 仅 1 个 CRUD 测试，无 CI 集成                 |

---

## 阶段 4：持续学习与能力回流 — 基础就绪，实践未启动 🔴

### 已完成
- ADR：4 篇（core/apps 边界、菜单路由、设计系统、CRUD 递进覆盖）
- 文档体系：完整五层结构

### 关键差距
| 差距             | 严重度 | 说明                                                            |
| ---------------- | ------ | --------------------------------------------------------------- |
| 无重复模式追踪   | **高** | 20+ 整页覆盖高度重复，无识别/追踪机制                           |
| 无清理触发器     | **中** | 旧版组件（`CubeSearch.vue`、`CubeListToolbarSearch`）无清理计划 |
| 无回流优先级标准 | **中** | 愿景要求"两个及以上重复"回流，未定义评估标准                    |

---

## 按优先级排序的行动计划

| 优先级 | 行动                                                               | 阶段    | 影响                  |
| ------ | ------------------------------------------------------------------ | ------- | --------------------- |
| P0     | **集成 DataSet 到默认视图** — 替换手动 request+ref 模式            | Phase 1 | 消除核心数据管理冗余  |
| P0     | **迁移一个业务页面到 Section 覆盖**（如 role）作为示范             | Phase 2 | 验证全链路，验证机制  |
| P1     | **增加表单校验** — FormContent 外层包裹 el-form + rules            | Phase 1 | 提升 CRUD 完整性      |
| P1     | **迁移所有业务页从 CubeListToolbarSearch 到 ListSearchBar**        | Phase 2 | 消除 20+ 重复搜索实现 |
| P1     | **增加空态/错误态组件** — el-empty + 搜索无结果提示                | Phase 1 | 提升默认页面体验      |
| P2     | **定义页面类型分类** — PageType 枚举                               | Phase 3 | 为 AI 生成页面奠基    |
| P2     | **创建重复模式注册表** — pattern-registry.md                       | Phase 4 | 启动能力回流追踪      |
| P2     | **清理旧版组件** — 删除 CubeSearch.vue，标记 CubeListToolbarSearch | Phase 4 | 减少技术债务          |
| P3     | **创建主子表/仪表盘模板**                                          | Phase 3 | 扩展非 CRUD 页面能力  |
| P3     | **扩展 E2E 测试** — 关键业务流 Playwright 测试                     | Phase 3 | 保障业务质量          |

---

# 任务

下一次迭代，选择 P0 或 P1 行动中的一个开始执行。建议先从 **P0：迁移一个业务页面到 Section 覆盖** 入手，作为示范验证全链路可用性。具体：
1. 选择 `cube-admin` 中 `role` 或 `user` 页面
2. 创建 Section 覆盖文件（如 `ListSearchBar.vue`、`ListTableContent.vue`）
3. 验证路由、主题和回退行为
4. 更新文档记录