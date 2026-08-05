# OSC-0009 UI 交互约束

## 适用框架与官方资料

| 场景 | 框架 | 实施前必查资料 |
| --- | --- | --- |
| 表单、详情、搜索、抽屉、LOV 弹窗 | Arco Design Vue | [快速上手](https://arco.design/vue/docs/start) |
| table/tree 的 VTable 适配与单元格显示 | VisActor VTable | [教程](https://arco.design/vue/docs/start) · [ListTable 配置](https://visactor.com/vtable/option/ListTable) · [实例接口](https://visactor.com/vtable/api/Methods) |

实现中对 Arco 表单字段错误、选择器受控值、VTable formatter/列配置/API 生命周期存在不确定时，必须先查官方资料并严格按文档实现，不得凭印象补造 API。

## 表单、详情、搜索顺序

1. 表单字段顺序严格遵循 `GetPage` 对应分区；无配置时使用 design.md 指定的分区回退，不根据字段名重排。
2. 详情与编辑使用同一个字段标签解析：字典/状态显示标签，URL 显示安全链接，图片显示缩略图，文件显示文件名链接，JSON 显示截断摘要并可展开纯文本。
3. 搜索栏沿用对应搜索字段和控件；值集无有效 Meta 时禁用选择并显示明确加载失败/空态，不伪造选项。
4. LIST 单选点击行即可选中并关闭；LIST 多选在弹窗内勾选，底部固定“取消 / 确认”，取消不得修改外部 model。

## 空态与降级

| 输入 | 显示 | 可提交 |
| --- | --- | --- |
| 静态 dataSource 有匹配值 | 显示标签 | 是 |
| LIST `BatchLabel` 返回匹配标签 | 显示标签 | 是 |
| 值为空 | `-` | 按 nullable/required 规则 |
| 未知 LIST value | 显示原始值的安全文本并附“未知值”语义，不映射为其他记录 | 保持原值，除非用户重新选择 |
| Meta/ListData 失败 | 保留已知标签或原始安全文本，展示重试入口 | 不主动清空已有值 |

## 视图一致性

- table、tree、card、kanban、calendar、gantt 必须使用同一字段 label resolver。
- 视图不能为了显示标签改变原始 row 或提交模型；显示值与原始值分离。
- 本号不修改视图布局、排序、分组、拖拽写回或分页策略。
