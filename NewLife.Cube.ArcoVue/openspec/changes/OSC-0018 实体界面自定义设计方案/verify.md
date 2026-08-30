# OSC-0018 Verify

> 纯文档 OSC。代码门禁 N/A。进入 Validating 后勾选。

## 必须保留

- 零 `.ts/.vue/.cs` 业务改动。
- 不复制 Cube.Vue 技能正文进 Arco 仓。
- 文档不得把 FormJson/藏列写成授权；不得设计画布/用户公式。
- 不得建议新变更使用 `OSC-0010` 等顺序号。

## 命令

```powershell
# 交付文件存在
Test-Path "NewLife.Cube.ArcoVue/web/docs/实体界面自定义设计方案.md"

# SectionKey
Select-String -Path "NewLife.Cube.ArcoVue/web/src/core/composables/useSections.ts" -Pattern "ListSearchBar|ListToolbar|FormContent|DefaultListPage"

# Cube.Vue 技能目录
Get-ChildItem "NewLife.Cube.Vue/skills" -Directory | Select-Object -ExpandProperty Name

# DataField
Select-String -Path "packages/api-core/src/types.ts" -Pattern "typeName|itemType|dataSourceMap|mapField|category"

# 无业务代码（相对本 OSC 预期）
git status --short NewLife.Cube.ArcoVue
```

预期：设计方案文件存在；引用名与源码一致；status 仅 md/README。

## AC

- [ ] **AC-01** 文档存在且含：背景、L0～L4、矩阵、元数据映射、决策树、Section 速查、技能蓝图、切片、边界。
- [ ] **AC-02** 写明开发期 vs 运行期；运行期引用已归档 OSC + Dashboard/工作台。
- [ ] **AC-03** 矩阵 ≥10 行，每项有采用/重写/不采用。
- [ ] **AC-04** GetPage 映射可追溯；搜索消费方为工具栏查询簇（Q + 自定义查询），不是 SearchDrawer。预定义仍走 QueriesJson。
- [ ] **AC-05** L1 定制点含 ListFields/AddFormFields/EditFormFields/OnGetFields；计算列指向 §12。
- [ ] **AC-06** 决策树含：新增实体、改列、改筛选、改表单、整页、改壳；每条唯一层级。
- [ ] **AC-07** 11 SectionKey 与 `useSections.ts` 一致。
- [ ] **AC-08** ≥5 技能；触发词/输入/产出/差异/另号落地；重写不迁移。
- [ ] **AC-09** L0/L1 明确「详见迁移方案 §12」，无第二套打架步骤。
- [ ] **AC-10** 非目标含 §8.2.6；FormJson≠ACL；AppModule 不是 CRUD 主路径。
- [ ] **AC-11** 切片无 `OSC-00xx` 新号。
- [ ] **AC-12** 本号 git 无业务代码。

## Checklist

- checklist: **pending**
