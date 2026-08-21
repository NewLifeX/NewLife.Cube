# OSC-26082097c1 UI 信息架构

框架：Arco Design Vue；列表单元格 VisActor VTable（本号不改列布局）。业务 TS 在实现号进 `useXxx.ts`。

## 模型

```
GetPage DataField → FieldMeta → resolveFieldKind → FieldKindSpec
  mode: edit | display | search | filter
  density: default | compact
```

后续表单/弹层只传 `field + mode + density`，禁止再复制 ControlType switch。

## 调度壳（实现号）

```
FieldWidget
  mode=edit|search|filter
  density=default|compact
  → 目录 A：Arco 原生 + TreeCascader（含现地区） / LovSelect / JsonEditor / RichEditor
  → 目录 B：PersonSelect / 角色下拉 / IpInput / formula·lookup 只读 / IconPicker / DurationInput（分段时长，非时钟） / 密码 / GMK / percent / code
```

`mode=display` 不进 Widget：`formatFieldValue` + `fieldBadge` + 图片缩略图（VTable / 卡片 / 详情）。**TimeSpan/duration 走 `formatDuration`（`1小时 2分钟`），禁止 `formatTime`。**

详情 RecordDrawer 标签行：

```
[labelIcon] 显示名     值
```

图标在文字之前；`aria-hidden`；`fieldIcon()` = kind 的 `labelIcon`（时长 `timer`）。编辑表单不加这套标签图标。

## 密度

| 表面 | mode | density |
| --- | --- | --- |
| 右抽屉表单 FormContent | edit | default |
| 对象页 DefaultObject | edit | default |
| 详情 RecordDrawer | display | — |
| 列表/卡片/看板 | display | — |
| SearchDrawer | search | default |
| FilterBuilderPopover 值区 | filter | compact |
| FormatPopover 值区 | filter | compact |
| 批量修改行 | edit | compact |
| 单元格编辑弹层 | edit | compact |
| 自动化动作条件值 | filter | compact |

compact：`size=small`；json/html/markdown 降为 textarea；image 不预览；填色值区宽约 132px。

## 筛选 / 填色行（值控件替换点）

现状行结构保持不变，只替换「请输入」那一格：

```
筛选
  [且/或]  [字段▾]  [操作符▾]  [FieldWidget filter compact]  ×
填色
  ⠿ [色] [范围▾] [字段▾] [操作符▾] [FieldWidget filter compact]  ×
```

- `isNull` / `notNull`：不渲染 Widget。
- 人员 / 部门 / 角色 / **终端 IP** / **时长**：走 PersonSelect / TreeCascader / 角色下拉 / IpInput / **DurationInput**（compact）。时长禁止 `a-time-picker` 与裸秒输入。
- 无页脚确定（填色改即生效，现行为保留）。

## 批量 / 单元格

```
批量修改
  [字段▾] [FieldWidget edit compact]  ×
  + 添加字段

单元格编辑
  字段（只读名）
  [FieldWidget edit compact]
```

禁止单元格弹层继续使用无元数据的 `a-input`。

## 树状级联 / 人员 / IP（实现号）

```
TreeCascader
  [省/一级 ▾] [市/二级 ▾] [区/叶子 ▾]     ← 懒加载；提交叶子或所选节点 ID
  多选 Ids：已选标签 + 级联勾选

PersonSelect
  [用户下拉/搜索]                            ← /Admin/User；单选存 ID
  多选 UserIds：标签 + 选择

IpInput
  [等宽 IPv4/IPv6]  [复制]                   ← CreateIP/UpdateIP；列表/详情只读

formula / lookup
  [只读值]  计算 | 关联名                      ← 禁止编辑；lookup 显示 BatchLabel

DurationInput
  [天] [小时] [分钟] [秒]                       ← 提交归一成存储单位（默认秒）
  compact 窄区：纵向堆叠或「自动单位 + 数字」
```

- 部门/菜单与地区同一套交互，仅 `treeKind` 与接口不同。
- compact 弹层可只展示到已选路径文案 + 点击展开级联。

## 明确不做的界面

- 飞书「添加字段」类型选择器（Cube 字段由后端实体决定）。
- 行内 Canvas 编辑。
- 飞书式实时协作头像堆叠（PersonSelect 为中后台选择器即可）。
- 把「创建地址」做成地图定位（IP 不是经纬度）。
- 客户端公式引擎、查找引用写回、未下发关联列 join。
- 条码 / 位置 / 评分 / 签名控件。
- 把时长当时钟（`HH:mm:ss` / `a-time-picker`）。
- 把详情标签图标扩到列表表头或强制加到编辑表单。
- 把 FieldInput 拆成 21 个页面级组件文件（注册表 + 少量专用件）。
