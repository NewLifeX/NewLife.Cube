# OSC-260815fa86 Retro

> 复盘在验收通过后由 openspec-retro 填写；归档后状态置 `Done`。

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | 主路径 AC 通过；AC-02/03 完整 UI 创建→insert→Run 未点完（壳层遮罩），以单测 + AC-01 入口为准 |
| 三步编排 | 实现审计 ✅ → 代码审查（缺口补齐后 0🔴）→ 文档同步 ✅ |
| 自动化门禁 | api-core **50** · arco-vue **421** · vite build ✅ · Osc260815 **12** · Cube/CubeNC 0 error |
| 代码质量 | Graph 服务端编译真源；Persistence 包装覆盖实体写路径；Filter 前后端同构单测 |
| 工期 | T1–T11 主实现 + T12 验收缺口补齐（P0.1 Log 表 → P0–P2 全补） |
| 手工冒烟 | Admin/User、Cube/Area 顶栏「自动化」在搜索与高级之间（AC-01） |

## 实际完成范围

- 实体自动化：触发（insert/update/delete/fieldChange/schedule/dateArrive/button/webhook）+ 线性 Graph + 动作集（notify/update/create/find/http/delay/runAutomation/addComment/aiText）。
- `AutomationRun` 落 Log 库（ConnName=Log）；Worker 捞 queued；终态可写系统 Log Remark。
- ArcoVue：飞书风双栏编辑器、DefaultList 入口/行按钮、Runs Tab、站内 Inbox（remind）。
- 验收补齐：found 连续段、findRecords 下推/分页、Filter 对齐、自引用保存拒绝、SSRF、Hook 限流字典、废弃 created、WrapAll、批量 After、debounce 查库、租户裁剪、api-core URL 单测。

## 做得好的

1. **执行在 C#、配置用线性表单**：预留 Graph 与审批节点失败闭合，明确不做 FlowGram 运行时，产品边界清晰。
2. **Persistence 包装而非只挂 Controller**：导入/EnableDisable/直接 Insert 也能入队，对齐 AC-21。
3. **验收缺口清单分级后一次性补齐**：P0.1 先落库再全量 P0–P2，避免「文档写落库、代码仍内存」漂移。
4. **前后端 Filter 同构用单测锁死**：缺字段 isNull、contains 大小写、非数字比较等与 `matchesViewFilter` 对齐。

## 待改进

1. **design §2.2 与初版实现曾漂移**：先做内存队列再补 Log 表，多一轮 T12；涉及队列/持久化的字段应在 Implementing 首日按 xml+xcode 落地。
2. **完整 UI 冒烟易被壳层遮罩挡住**：验收期抽屉点击被拦截，AC-02/03 依赖单测；后续可加 Playwright 或约定关闭 AI/壳 overlay 再冒烟。
3. **测试实体 OscAutoItem Insert 在宿主返回 0**：found 段单测改用 NotificationRecord；临时实体要保证 Identity/建表可用。
4. **全量 xcode Cube.xml 易生成重复中文实体文件**：改表后应只生成目标表或事后清理重复 `实体自动化流程` 类文件。

## 偏差

- 配置 UI「添加动作」不提供 `runAutomation`（仍可执行/反编译旧图）——产品决策，与早期动作全集表述略有差别，design 已锁定。
- `target=created` 废弃为 current（保存归一），与早期「写入 context.created」并存：created 仍写上下文，但不再作 update 目标。

## 遗留与后续

- 字符串级 `Update/Delete(whereClause)` 仍不入队（无实体实例）（🟢 可另号）。
- 工厂注册后、Worker WrapAll 前极短窗口可能漏首次写入（🟢）。
- AC-02/03 完整浏览器创建→触发→Run 成功链路人工补点（🟢）。
- 二期：FlowGram 画布 / 分支审批节点（另立 OSC，非本号）。
