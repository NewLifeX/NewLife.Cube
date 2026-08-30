# OSC-260830a1b2 Verify

> 状态：待执行（openspec-verify）  
> 时间：—  
> 触发：验收并复盘 OSC-260830a1b2。

## AC 对照

| AC | 结果 | 说明 |
|----|------|------|
| viewFilter 字段仅限 search ∪ list，白名单外被拒绝 | 待验证 | T1 单测 |
| 时序实体无时间条件自动注入 30 天窗口 + 前端提示 | 待验证 | T2 / T8 |
| startsWith 操作符可用；条件数超限被拒 | 待验证 | T3 / T4 / T8 |
| SearchDrawer / QueriesJson 已移除，无残留；Q 保留 | 待验证 | T6 / T7 / T12 |
| 18 个重写 Search 控制器透明下推经单测钉死 | 待验证 | T5 |
| 本 OSC 新增单测全过；Cube 与前端构建无错误 | 待验证 | T10 / T11 |
| 迁移方案 / 竞品报告 / web README 已回写 | 待验证 | T13 / T14 / T15 |

## 测试验证记录

```text
（待执行）
```

## 构建记录

```text
（待执行）
```

## 风险

- 时间窗默认开启可能引入结果集与用户直觉偏差——前端必须展示「已自动限定近 30 天」提示。
- `contains` 在 `logic=any` 且无时间窗的大表上仍可能全表扫描；以 T2 时间窗兜底 + 巡检告警。
- 移除 SearchDrawer 属 UI 行为变更，需确认业务包 `registerSection` / apps 覆写未依赖该入口。

## Checklist

- checklist: **pending**
- 可进入复盘：`复盘 OSC-260830a1b2`（本消息已一并触发）
