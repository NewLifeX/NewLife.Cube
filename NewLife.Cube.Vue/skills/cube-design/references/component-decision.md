# 组件选型决策树

> 不需要"设计"组件，只需要"选择"组件。选择顺序固定，消灭"这个场景该用什么组件"的分叉点。

## 选型优先级（自上而下，命中即停）

```
1. 框架默认页面与已接入业务组件（core/views/、core/pages/、core/components/）
  ├─ 标准实体 CRUD        → DefaultEntity.vue + core/views/index.vue / form.vue
  ├─ 列表 / 搜索 / 分页   → core/views/components/ 默认 Section，按需覆盖
   ├─ 关联对象选择（LOV）  → LovSelect.vue / LovSelectTable.vue
   ├─ 富文本               → RichEditor.vue
   ├─ JSON 编辑            → JsonEditor.vue
   ├─ 文件上传             → Uploader.vue
   ├─ 图标选择             → IconSelector.vue
   ├─ 整页 CRUD            → DefaultEntity.vue（见 page-types.md）
   └─ 错误/无权限/加载态   → PageNotFound.vue / PageUnauthorized.vue / Loading.vue
2. Element Plus 原生组件（外观已解决 hover/focus/disabled/loading/暗色）
3. Tailwind 摆放上述组件（见下方"布局边界"）
4. 以上都不满足 → 新建业务组件，且必须过一遍 spec-checklist.md
```

## 按场景选型

| 场景                             | 选择                           | 依据                                            |
| -------------------------------- | ------------------------------ | ----------------------------------------------- |
| 展示数据，字段多                 | 默认列表 Section / `el-table`  | 表格适合行列对齐、批量操作                      |
| 展示数据，字段少（≤6 个）        | `el-descriptions` / `el-card`  | 表格对少字段是浪费，卡片更聚焦                  |
| 用户输入                         | `el-form` + `el-form-item`     | 表单是唯一输入容器，不用自定义 label+input 拼接 |
| 确认操作（轻量、单一后果）       | `el-popconfirm`                | 不打断上下文                                    |
| 确认操作（重量、多字段或不可逆） | `el-dialog`                    | 需要更多空间承载确认内容                        |
| 操作级反馈（成功/失败提示）      | `ElMessage`                    | 短暂、不阻塞                                    |
| 系统级通知（需要用户主动关闭）   | `ElNotification`               | 更持久、可带操作按钮                            |
| 关联对象选择                     | `LovSelect` / `LovSelectTable` | 已封装远程搜索 + 分页，不用重写                 |

## 布局边界：Tailwind 与 Element Plus 栅格不并存

**规则**：页面级布局（页面骨架、卡片排列、多列区块）一律使用 Tailwind 的 `flex`/`grid`/`gap`/响应式前缀；`el-row`/`el-col` **只允许**用在 `el-form` 内部的字段网格（表单字段本身是 Element Plus 的职责范围）。

```html
<!-- ✅ 页面级布局：Tailwind -->
<div class="flex gap-4 p-6">
  <el-card class="flex-1" shadow="never">...</el-card>
  <el-card class="flex-1" shadow="never">...</el-card>
</div>

<!-- ✅ 表单内部字段网格：el-row/el-col 允许 -->
<el-form :model="form">
  <el-row :gutter="16">
    <el-col :span="12"><el-form-item label="姓名"><el-input /></el-form-item></el-col>
    <el-col :span="12"><el-form-item label="电话"><el-input /></el-form-item></el-col>
  </el-row>
</el-form>

<!-- ❌ 页面级布局却用 el-row/el-col：两套栅格系统并存 -->
<el-row :gutter="24">
  <el-col :span="8"><el-card>统计卡片</el-card></el-col>
</el-row>
```

**为什么**：两套栅格系统同时存在于页面布局层，会出现"这个区域的间距/断点该改 gutter 还是改 gap"的新分叉点。表单字段网格是 Section 覆盖机制里天然依赖 `el-form` 内部结构的场景，是唯一例外。

## 布局摆放规则

- 容器：`flex` / `grid` / `space-y-6`
- 间距：`gap-4` / `p-5` / `mt-4`（Tailwind 4px 基数，`p-1`=4px，天然对齐 4/8/12/16/20/24/32 间距阶梯）
- 对齐：`items-center` / `justify-between`
- 响应式：`lg:grid-cols-3` / `md:flex-col`
- 颜色/圆角：只用 `bg-*`/`text-*`/`border-*`/`rounded-*` 中映射到 `--el-*` 的语义类（见 [UI 规范](../../../web/docs/standards/ui-spec.md)），不用 Tailwind 默认调色板（`bg-red-500` 等）

如果发现自己要写 `<style>` 硬编码尺寸/颜色，说明布局方案没选对，或 spec 有遗漏——先查 spec-checklist.md，再考虑是否触发"spec 缺口"流程。
