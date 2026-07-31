# OSC-0002 — 后端三实体 Profile / Comment

## 1. 为何做

为 ArcoVue（及后续皮肤）提供**服务端权威**的个人呈现配置与实体评论能力：布局/主题（UserProfile）、按实体的视图与列布局（EntityViewProfile）、记录评论（EntityComment）。前端 OSC-0004/0005/0008 依赖本变更接口就绪。

## 2. 范围（做）

- 在 [`NewLife.Cube/Entity/Cube.xml`](../../../../../NewLife.Cube/Entity/Cube.xml) **一次加入三张 Table**（结构对齐迁移方案 §5.2.1）。
- 按 **xcode** 协作指令/Agent **生成**实体、Model（`NewLife.Cube.Entity` / `ConnName=Cube`）；**禁止**大段手写实体骨架。
- 按 **cube** 协作指令补齐面向前端的三套 API（建议落在 `Controllers/CubeController` 或同级专用控制器，路径见 design）：
  - UserProfile：当前用户 GET/PUT
  - EntityViewProfile：按 `typePath` GET/PUT/DELETE（恢复默认）
  - EntityComment：按 `category`+`linkId` GET（可选 parentId）；POST（可 `parentId` 同表回复）；DELETE（本人或管理员）
- **XUnitTest**：鉴权、读写、唯一约束、Comment 按 category+linkId 筛选。
- 文档：核心接口架构高级表登记三路径；功能清单回写新增编码（若方案要求）；必要时内置前端皮肤/迁移方案交叉核对。

## 3. 不做什么

- **任何** ArcoVue / 其他皮肤 UI、localStorage 先行逻辑。
- VTable / 抽屉 / FlowGram / MFA UI。
- 不改 OpenSpec 五壳正文（除非发现与本变更冲突的笔误）。
- 不拆成三个 OSC（方案已定合并为 OSC-0002）。

## 4. 依赖

- **OSC-0001**：Done（协作基线；本变更无硬依赖其代码，但编号顺序与分支约定已就绪）。
- 实施在 **`ArcoVue` 分支**（后端改动同属该产品线交付）。

## 5. 验收 / 测试范围

| 类型 | 是否做 | 说明 |
|------|--------|------|
| **XUnitTest（新增）** | **是** | 三实体/API：鉴权失败、当前用户读写 Profile、ViewProfile 唯一键与 DELETE 恢复、Comment 列表/发表/删 |
| **构建** | **是** | `dotnet build` 相关工程（NewLife.Cube + XUnitTest）无错误 |
| 生成物核对 | 是 | 实体/Model 与 Cube.xml 一致，无大段手写实体 |
| 文档 | 是 | 核心接口架构 + 功能清单（编码） |
| Vitest / Arco UI | 否 | 本变更无前端代码 |
| E2E | 否 | |

硬门禁：触及后端代码 → 执行期必须跑新增/相关单元测试；验收期**本 OSC 新增单测全过** + **构建无错误**。

## 6. 成功标准

- [ ] Cube.xml 含 UserProfile、EntityViewProfile、EntityComment 及约定索引
- [ ] XCode 生成物齐全且与 xml 一致
- [ ] 三套 API 可按 design 路径调用（需登录；数据隔离到当前用户）
- [ ] 本 OSC 新增 XUnit 全过；`dotnet build` 无错误
- [ ] 核心接口架构已登记；功能清单已回写（或 verify 说明无新编码号段时的处理）
