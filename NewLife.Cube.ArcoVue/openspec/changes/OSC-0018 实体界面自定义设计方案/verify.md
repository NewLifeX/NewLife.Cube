# OSC-0018 Verify

> 进入 `Validating` 后逐项勾选。本号为纯文档 OSC（零代码改动），验收以「设计方案文档完整性 + 交叉核对」为核心；代码门禁声明 N/A。

## 执行阶段记录（openspec-apply）

- （待执行后填写：研究产出摘要、文档章节完成度、交叉核对结果）

## 验收阶段记录（openspec-verify）

- （待验收后填写：逐条 AC 判定、文档修订记录）

## 验收标准

### 交付物
- [ ] **AC-01 文档交付**：`web/docs/实体界面自定义设计方案.md` 存在且结构完整（背景目标 / L0~L4 分层模型 / 能力矩阵 / 元数据利用模型 / 决策树 / Section 速查 / 技能蓝图 / 实施切片 / 边界）
- [ ] **AC-02 定位准确**：文档明确本方案聚焦**开发期自定义**，运行期用户自定义（OSC-0012~0016）仅作边界引用

### 能力矩阵与元数据
- [ ] **AC-03 能力矩阵**：Cube.Vue ↔ ArcoVue 矩阵覆盖 ≥10 能力项，每项含 Cube.Vue 现状 / ArcoVue 现状 / 差距 / 「采用/重写/不采用」结论
- [ ] **AC-04 元数据映射**：GetPage setting + 五分区（list/addForm/editForm/detail/search）+ DataField 属性 → 前端消费映射表逐项可追溯，覆盖 fieldControl/SearchDrawer/RecordDrawer/ListTable 等消费方
- [ ] **AC-05 定制点速查**：L1 后端定制点（ListFields/AddFormFields/EditFormFields/OnGetFields/Search/FixSearchMapCandidates）完整列出

### 决策树与 Section
- [ ] **AC-06 决策树**：覆盖「新增实体页 / 改列表列 / 改搜索条件 / 改表单字段 / 整页特殊交互 / 改壳」场景，每场景明确落到 L0~L4 层级与具体动作
- [ ] **AC-07 Section 速查**：11 个 SectionKey 的名称/作用/覆盖方式/与 Cube.Vue 对应关系齐全，差异表与 design.md §1.2 一致

### 技能蓝图
- [ ] **AC-08 技能蓝图**：为 ArcoVue 规划 ≥5 个技能（对照 Cube.Vue 8 技能核心子集），每技能含触发词 / 输入参数 / 产出文件 / 与 Cube.Vue 同名技能差异 / 落地所属 OSC
- [ ] **AC-09 不迁移声明**：文档明确 ArcoVue 技能为 Arco 栈**重写不迁移**（不复制 Element Plus 实现）
- [ ] **AC-10 实施切片**：按依赖排序给出后续 OSC 建议（技能落地 / Section 覆盖点补齐 / 能力差距修复 / 样例工程），与既有 OSC 无冲突

### 交叉核对（防虚构）
- [ ] **AC-11 引用一致性**：文档引用的 SectionKey 与 `useSections.ts` 实际一致（grep 验证）；技能名与 `NewLife.Cube.Vue/skills/**` 实际一致；DataField 属性与 `api-core/types.ts` 一致；Controller 方法（GetPage/GetFields/OnGetFields）与 `ReadOnlyEntityController.cs` 一致
- [ ] **AC-12 边界约束**：文档非目标章节引用迁移方案 §8.2.6（无画布/拖拽/跨实体/公式），未出现低代码画布类越界设计

### 门禁
- [ ] **AC-13 零代码改动**：本 OSC 未修改任何 `.ts/.vue/.cs` 业务代码（仅新增 `web/docs/` 文档 + 可选文档登记），代码门禁 N/A

## 自动化门禁

```powershell
# 交叉核对（引用一致性，T5 执行）
# SectionKey 与 useSections.ts
grep -c "SectionKeyMap\|ListSearchBar\|ListToolbar\|FormContent\|DefaultListPage" "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue\web\src\core\composables\useSections.ts"

# Cube.Vue 技能名与 skills 目录
Get-ChildItem "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.Vue\skills" -Directory | Select-Object -ExpandProperty Name

# DataField 属性与 api-core types
grep -c "typeName\|itemType\|dataSourceMap\|mapField\|category" "f:\Git Repos\1.Newlife\NewLife.Cube\packages\api-core\src\types.ts"

# 零代码改动核查（应只新增 web/docs/ 与 openspec 文档）
git status --short "f:\Git Repos\1.Newlife\NewLife.Cube\NewLife.Cube.ArcoVue"
```
