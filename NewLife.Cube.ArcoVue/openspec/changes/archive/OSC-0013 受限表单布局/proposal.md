# OSC-0013 — 受限表单布局

## 1. 为何做

`RecordDrawer` 已使用 GetPage 的 add/edit/detail 字段和 Category 分组，但用户不能调整高频字段顺序、隐藏低频字段或折叠分组。飞书式编辑体验的价值在于“展示可调整、数据契约不变”。本号为 ViewProfile 增加受限 `FormJson`，让新增、编辑、详情三个模式可分别配置，而不改变 Cube 元数据和写入规则。

## 2. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | `FormJson` 存在 `ViewProfile`，按 `add`、`edit`、`detail` 三个模式独立配置。 |
| 2 | 每个模式仅允许字段顺序、字段显隐和现有 `Category` 分组折叠偏好。 |
| 3 | 字段存在性、类型、控件、默认值、必填、校验、读写权限及提交载荷继续由 GetPage、`resolveFieldsForKind`、`prepareSubmitPayload` 决定。 |
| 4 | 已删除字段静默忽略；新字段追加到原 Category 且默认可见；非法配置按元数据回退。 |
| 5 | 本号只提供个人表单布局；管理员模板继承由 OSC-0014 单独处理。 |

## 3. 做什么

- 在 `Cube.xml` 为 `ViewProfile` 增加 `FormJson`，运行 xcode 生成实体与 Model，再补 Biz/API 透传和 XUnit 测试。
- 扩展 api-core、ViewProfile 前端状态与 JSON 解析/序列化，保留旧记录兼容。
- 为 RecordDrawer/FormContent/详情分组提供统一的表单布局 resolver。
- 新增布局配置抽屉或 RecordDrawer 配置入口，分别编辑新增、编辑、详情布局，提供恢复当前模式默认布局。
- 同步核心接口、实体参考、迁移方案、功能清单与 web 文档。

## 4. 不做什么

- 不手写覆盖生成实体；数据模型必须通过 `Cube.xml → xcode` 生成。
- 不改变后端实体字段、GetPage 元数据、字段权限、校验或 CRUD API 语义。
- 不实现任意区块、跨 Category 拖拽分组、字段类型替换、自定义控件、公式或条件显示规则。
- 不实现模板发布、角色/租户覆盖和多人协同编辑。

## 5. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0002 | Done：ViewProfile 实体、API 和 XCode 生成基线 |
| OSC-0003 / OSC-0008 / OSC-0009 | Done：RecordDrawer、表单字段回退、提交归一化和字段分组基线 |
| OSC-0012 | 建议完成：共享 ViewProfile 配置扩展与配置入口模式；本号不消费其筛选行为 |
| DATA-4 / DATA-5 / DATA-6 / DATA-11 / SPA-7 | 本号消费的表单、详情、元数据 CRUD 能力 |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| XUnit | 是 | FormJson 生成字段、Model 映射、个人 upsert、旧记录兼容 |
| ArcoVue Vitest | 是 | 解析/合并/无效字段、新字段、三模式独立、保存失败回滚 |
| 组件测试 | 是 | 配置入口、拖动排序、显隐、分组折叠、恢复默认和无权限态 |
| 构建 | 是 | `NewLife.Cube`、api-core、ArcoVue web 无错误 |
| 手工冒烟 | 是 | Admin/User 等实体新增/编辑/详情布局与提交回归 |

## 7. 成功标准

- [ ] `ViewProfile.FormJson` 由 Cube.xml 生成，并通过 API 安全读写个人配置。
- [ ] 用户能分别配置新增、编辑、详情的字段顺序、显隐和 Category 折叠。
- [ ] 已删除字段不报错，新元数据字段默认出现；配置不能绕过权限、必填或校验。
- [ ] 恢复默认只删除当前模式的个人布局域，不影响视图和筛选域。
- [ ] 本 OSC 新增单测全部通过，相关构建无错误，文档按实际实现增量同步。
