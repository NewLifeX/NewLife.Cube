# OSC-260815fa86 Design — 实体增删改自动化

> **现行约束（2026-08-19 修订，覆盖下文 2026-08-16「P0.1 必须落库」锁定）：**  
> `AutomationRun` **不是** XCode 实体、**不得**再写入 `Cube.xml`、**不得**再生成 `自动化运行.cs`。  
> 队列 = 进程内内存 POCO（`NewLife.Cube/Automation/AutomationRun.cs`）。  
> 终态审计 = 系统 `Log`（Action=`Automation`）。`GET /Runs` 只读该 Log。  
> 实现审计时勿把 §2.2 历史「ConnName=Log 表」或 tasks T12.1 改回来。

## 0. 适用框架与官方资料

| 场景 | 框架 | 资料 | 本号用法 |
| --- | --- | --- | --- |
| 壳/抽屉/表单/开关/空态 | Arco Design Vue | https://arco.design/vue/docs/start ；Drawer / Form / Select / Switch / Empty / Message / Modal / Tabs / Timeline | 配置 UI（飞书双栏，**不用** Steps） |
| 列表多维表（本号只加操作列按钮，不改视图引擎） | VisActor VTable | 教程：https://arco.design/vue/docs/start ；配置：https://visactor.com/vtable/option/ListTable ；接口：https://visactor.com/vtable/api/Methods | 扩展 `__ops` 自定义按钮 |
| 工作流画布 | FlowGram.AI | 指引：https://flowgram.ai/guide/getting-started/introduction.html ；例子：https://flowgram.ai/examples/index.html ；API：https://flowgram.ai/api/index.html | **只查阅、不引入**。本号执行器是 C#；画布留给后续「工作流」OSC |
| 图标 | IconPark | 先查站点再写入 `iconRegistry.ts` / `iconComponents.ts` | 自动化入口 `lightning`；站内通知 `remind`；动作菜单 `more`（横向 ⋯） |
| 对标 | 飞书多维表格「自动化」 | 线性触发+动作双栏；**不是**「工作流」 | 能力清单见 proposal §2 |

SFC：新 `.vue` 薄脚本，业务进同目录 `useXxx.ts` 或 `core/utils` 纯函数。

## 1. 总览

```mermaid
flowchart TB
  persist[XCode Insert/Update/Delete]
  btn[行按钮 POST Run]
  hook[POST Hook/token]
  cron[EntityAutomationTick]
  persist --> snap[Dirtys 快照]
  snap --> q[AutomationRun queued]
  btn --> q
  hook --> q
  cron --> q
  q --> worker[AutomationExecutor]
  worker --> graph[GraphJson 顺序执行]
  graph --> notify[NotificationRecord]
  graph --> write[create/update 记录]
  graph --> http[出站 HTTP]
```

**状态唯一来源**

| 状态 | 来源 | 禁止 |
| --- | --- | --- |
| 规则定义 | `EntityAutomation` 行（含 GraphJson） | 前端另存一份「运行时图」 |
| 一次运行队列 | 内存 `AutomationRun` POCO（queued/running/waiting）；**不落库** | 再建 `AutomationRun` 实体表 / 写回 `Cube.xml` |
| 流程审计日志 | 系统 `Log`（Category=TypePath，Action=`Automation`，Remark=JSON 摘要；**不改 Log 表结构**）。`GET /Runs` 只读此源 | 另造审计表、扩展 Log 列、或用独立运行表充当 Runs |
| 列表是否显示配置按钮 | `flags.canUpdate`（GetPage 权限） | 前端自造管理员角色判断 |
| 行按钮清单 | `GET /Cube/Automation?typePath=&triggerKind=button&enable=true` | 写死在 DefaultList |
| 站内未读数 | `GET /Cube/Automation/Inbox/UnreadCount` → `appStore.inboxUnreadCount` | 前端假计数 |
| 当前编辑草稿 | `useAutomationEditor` 内存；保存时提交服务端编译 | 用 GraphJson 当表单双向绑定主模型 |

## 2. 数据模型

先改 `NewLife.Cube/Entity/Cube.xml` **仅**增加 `EntityAutomation`，再 xcode 生成。禁止手写 `实体自动化.cs` 骨架。Biz 只补查询/编译校验/token。CubeNC csproj **必须 Link** 该实体 + Model（同 EntityComment）。**禁止**再为运行队列增加 xml 表或 Link `自动化运行.*`。

### 2.1 EntityAutomation（ConnName=Cube）

| 列 | 类型 | 约束 | 默认 | 说明 |
| --- | --- | --- | --- | --- |
| Id | Int64 | PK，DataScale=time | 雪花 | |
| TenantId | Int32 | 索引 | 0 | 0=平台/未开租户 |
| TypePath | String(100) | 非空 | | 与 GetPage 一致，如 `Admin/User` |
| Name | String(50) | 非空 | | 显示名，trim 后 1–50 |
| Enable | Boolean | | true | |
| Priority | Int32 | | 100 | 越小越先入队；同优先级按 Id |
| TriggerKind | String(32) | 非空 | | 见 §3.1 |
| TriggerConfig | String(-1) | JSON | `{}` | 见 §3.2 |
| GraphJson | String(-1) | JSON | | 见 §4 |
| HookToken | String(64) | 唯一索引，可空 | | 仅 webhook；32 位小写 hex |
| Version | Int32 | | 1 | 乐观并发；保存时 If-Match 语义：提交 version 须等于当前，否则 409 |
| CreateUser/CreateUserId/CreateTime/CreateIP | 扩展 | | | |
| UpdateUser/UpdateUserId/UpdateTime/UpdateIP | 扩展 | | | |

索引：`(TenantId, TypePath, Enable)`；`HookToken` Unique（允许多个 NULL，实现时用过滤唯一或保存前查重）。

### 2.2 AutomationRun（内存队列 POCO，非实体）

类路径：`NewLife.Cube/Automation/AutomationRun.cs`。**禁止** `Cube.xml` 表、**禁止** xcode 生成 `自动化运行.*`。

字段语义（进程内）：Id、AutomationId、TypePath、RecordKey、TriggerKind、Status（`queued` / `running` / `waiting`）、Depth、Error、StartedAt/FinishedAt、ResumeAt、WaitKind、WaitPayload、TenantId、TraceId、CreateUser、CreateTime。`Enqueue` / `FindQueued` / `FindDueWaiting` / `HasRecentActive` 为静态方法。NodeTrace 写入系统 Log Remark，不单独建表。

**终态：** `AutomationFlowLog.WriteTerminal` 写系统 `Log`（Action=`Automation`，Remark=JSON）。**不改 Log 表结构。**

**`GET /Runs`：** `AutomationFlowLog.SearchRuns` 解析系统 Log；**不**读运行表。

**进程语义：** 重启丢失 in-flight queued/waiting；delay 只在本进程续跑。可接受。Worker 扫内存 queued / 到期 waiting，不捞库。

**历史（已废止，勿回滚）：** 2026-08-16 T12.1 曾把同名表写入 `Cube.xml`（ConnName=Log）并生成 `自动化运行.cs`，当时锁定「禁止内存队列、`GET /Runs` 以本表为准」。2026-08-19 删除：与系统 Log 重复，且与 §1「流程审计日志 = 系统 Log」冲突。

### 2.3 JSON 未知字段

- `TriggerConfig` / `GraphJson` / 节点 `data`：反序列化用 `JsonNode` 或带 `[JsonExtensionData]` 的 DTO。
- **保留**未知顶层键与 `data` 内未知键（round-trip 写回）。
- **丢弃**：`null` 节点、空字符串 type、非对象 data（归一成 `{}`）。
- Graph `version` 缺省=1；`version>1` 保存拒绝（400「不支持的图版本」）；加载仍展示只读错误条。

## 3. 触发

### 3.1 TriggerKind 穷尽

合法值（大小写不敏感，入库小写）：

`insert` | `update` | `delete` | `insertOrUpdateIf` | `fieldChange` | `dateArrive` | `schedule` | `button` | `webhook`

非法/空 → 保存 400。UI 单选，无「其它」。

### 3.2 TriggerConfig schema

公共：未知键保留。

| Kind | 字段 | 类型 | 默认 | 非法归一 |
| --- | --- | --- | --- | --- |
| insert / delete | （无必填） | | `{}` | 忽略多余业务字段但仍保留未知键 |
| update | （无必填） | | `{}` | |
| insertOrUpdateIf | 条件在 filter 节点，不在 TriggerConfig | | `{}` | |
| fieldChange | `watchFields` | string[] | `[]` | 空数组则等价 update（任意字段变化都触发）；元素 trim，去空，最多 32 个，须属于该实体字段名（保存时用 GetPage 字段校验，未知名剔除并 Message） |
| dateArrive | `field` | string | 必填 | 必须是 DateTime 类字段 |
| dateArrive | `offsetMinutes` | int | 0 | 范围 -10080…10080（±7 天）；超界夹紧 |
| dateArrive | `once` | bool | true | 同一记录同一规则只成功跑一次（`AutomationFlowLog.HasSucceeded` 查系统 Log，**不**查运行表） |
| schedule | `cron` | string | 必填 | 5 或 6 段 Quartz/NewLife Cron；解析失败 400 |
| button | `label` | string | 「运行」 | trim，1–12 字 |
| button | `requirePermission` | `detail`\|`update` | `detail` | 非法→detail |
| webhook | （token 在 HookToken 列） | | | 保存 webhook 时若 HookToken 空则生成 32 hex |
| webhook | `requireSignature` | bool | false | true 时校验头 `X-Cube-Signature` = HMAC-SHA256(HookToken, rawBody) hex |

### 3.3 触发匹配矩阵

拦截器在 **SQL 成功之后** 调用 `AutomationTrigger.Collect(entity, method, dirtys)`。

| 方法 | 规则 TriggerKind | 额外条件 | 是否入队 |
| --- | --- | --- | --- |
| Insert | insert | Enable、TypePath 匹配、租户匹配 | 是 |
| Insert | insertOrUpdateIf | 同上 | 是（filter 在执行期） |
| Insert | 其它 | | 否 |
| Update | update | 同上 | 是 |
| Update | insertOrUpdateIf | 同上 | 是 |
| Update | fieldChange | dirtys ∩ watchFields 非空；watchFields 空则 dirtys 非空 | 是 |
| Update | 其它 | | 否 |
| Delete | delete | 同上 | 是 |
| Delete | 其它 | | 否 |
| 任意 | schedule / dateArrive / button / webhook | | 拦截器 **否**（由 Cron/API） |

**跳过工厂（硬编码类型名，大小写不敏感）：** `EntityAutomation`、`CronJob`、`NotificationRecord`、`EntityComment`（评论由 addComment 写入，避免评论再引爆评论流；若未来要对评论自动化，另开 OSC）。内存 `AutomationRun` 不是实体，无需列入。

**租户匹配：** `EnableTenant=false` 或规则 TenantId=0：匹配全部。否则规则 TenantId 必须等于 `TenantContext` 当前租户；平台管理员 TenantId=0 的规则仅在当前租户为 0 时跑（避免平台规则打进租户数据）。实施时用与 `DataScopeMiddleware` 相同的当前租户读取方式。

**导入批量：** 同一 TypePath 单次请求入队超过 **50** 条后，其余按 50 条一组 `ResumeAt=now+1s * 组号` 写成 waiting，由 Tick 续跑。不丢事件。

**防递归：** `AsyncLocal<bool> AutomationScope.IsExecuting`。执行器内 create/update 带该标记时，拦截器仍入队，但 `Depth` 继承；`Depth>=3` 丢弃并写一条 failed Run「超过最大深度」。同一 `(AutomationId, RecordKey)` 在 **3 秒** 内已有 queued/running：跳过（debounce）。

Update Dirtys：必须在 `next()` **之前** 复制 `entity.Dirtys` 字段名列表与旧值字典到 AsyncLocal，因为 Update 提交后 Dirtys 会清。

## 4. GraphJson

### 4.1 Schema

```json
{
  "version": 1,
  "nodes": [
    { "id": "n0", "type": "start", "data": { "triggerKind": "insert" } },
    { "id": "n1", "type": "filter", "data": { "filter": { "logic": "all", "conditions": [] } } },
    { "id": "n2", "type": "notify", "data": { } },
    { "id": "n3", "type": "end", "data": {} }
  ],
  "edges": [
    { "id": "e0", "source": "n0", "target": "n1" },
    { "id": "e1", "source": "n1", "target": "n2" },
    { "id": "e2", "source": "n2", "target": "n3" }
  ]
}
```

| 字段 | 规则 |
| --- | --- |
| nodes[].id | 非空，图内唯一；编译器用 `n0…nN` |
| nodes[].type | 见节点表 |
| edges | 本号线性：恰好一条从 start 出发的链，止于唯一 end；每个非 end 出度=1，end 出度=0 |
| 多出边 / 环 | 保存 400；执行若仍遇到则 failed |

### 4.2 节点类型

**本号执行**

| type | data 要点 |
| --- | --- |
| start | `triggerKind` 冗余拷贝，执行不读 |
| filter | `filter`: ViewFilter。`conditions` 空 = 恒 true |
| notify | `channel`: `InApp`\|`Mail`\|`Sms`\|`DingTalk`\|`WeCom`（非法→InApp）；`to`: 见 §5.3（**用户/角色/部门三选一**）；`title`/`body` 模板字符串，各最长 200/2000 |
| updateRecord | `target`: `current`\|`found`（默认 current；**废弃 `created`**，加载时归一为 current）；`fields`: `{ name: string, value: string }[]`，value 支持模板；最多 32 字段 |
| createRecord | `typePath` 必填（UI：有 Insert 权限的实体下拉）；`fields` 同上；成功后写入 context.created |
| findRecords | `typePath` 默认当前（UI：有 Update 权限的实体下拉）；`filter` ViewFilter；`limit` 1–100 默认 20；结果 `context.found[]`；空数组不失败 |
| httpRequest | `method` GET\|POST\|PUT 默认 POST；`url` 模板；`headers` 对象最多 16；`body` 字符串模板；超时 15s；仅 http/https；禁止 `file:`/`javascript:`；响应截断 64KB 进 trace |
| delay | `minutes` 1–10080 整数；节点把 Run 置 waiting + ResumeAt，Tick 从下一节点续跑 |
| runAutomation | `automationId` Int64；入队目标规则 Depth+1；禁止指向自身（保存校验）。**配置 UI「添加动作」不提供此类型**；旧图仍可执行与反编译 |
| addComment | `content` 模板；`target`: `current`\|`found`；调用 `EntityComment.AddComment`；category=当前 TypePath，linkId=目标主键 |
| aiText | `prompt` 模板；`outputField` 可选写入 current；无 IAIService/AISwitch=false → 该节点 failed |
| end | 无 |

**仅 schema 预留（UI 不出现；执行 failed「节点类型未实现」）**

`condition` | `switch` | `loop` | `approval`

其它未知 type：同样 failed，不得当 noop。

### 4.3 表单 → 图编译（前后端同一算法）

纯函数 `compileAutomationGraph(input) → GraphJson`：

```
input = { triggerKind, triggerConfig, filter: ViewFilter, actions: ActionDraft[] }
nodes = [{ id:n0, type:start, data:{ triggerKind } }]
若 filter.conditions.length>0：追加 filter 节点
对 actions 每一项：type 映射为节点 type，data=该项（去掉 UI-only 键）
追加 end
按顺序连边 e0… 
version=1
```

`ActionDraft.type` 合法（图/执行）：`notify|updateRecord|createRecord|findRecords|httpRequest|delay|runAutomation|addComment|aiText`。

前端「添加动作」菜单仅：`AUTOMATION_MENU_ACTION_TYPES` = 上列去掉 `runAutomation`（8 项）。非法项保存前剔除并提示。最多 **20** 个 action。

保存前额外校验：`validateFoundTargetChain`——任一 `updateRecord`/`notify`/`addComment` 的 `target===found` 时，其前方必须已有 `findRecords`，否则阻断保存并在动作卡片告警。

服务端保存：**忽略客户端 GraphJson**，用提交的 trigger/filter/actions 重编译后写入。客户端编译仅用于预览/单测。

反编译 `parseAutomationGraph(graph)`：从线性链还原 filter 与 actions，供编辑回填。若不是线性（多出边）→ 编辑器只读告警「请用表单重建」，不允许保存乱图。

### 4.4 模板与 NodeTrace

模板替换：`{{FieldName}}`、`{{trigger.FieldName}}`（同 trigger 记录）、`{{found.FieldName}}`（当前 found 游标）、`{{webhook.key}}`。只允许 **[A-Za-z_][A-Za-z0-9_]*`** 字段名；未知字段替换为空串。禁止 `{{#each}}`、JS、嵌套对象路径超过一层。

NodeTrace 项：`{ nodeId, type, at, ok, detail }`。detail 不含 HookToken、Authorization 头。

findRecords 之后、直到下一个 findRecords/end：若 target=found，对 `found` **逐条**执行后续 **仅 target=found 的 update/notify**；target=current 的节点仍只跑一次。实施锁死更简单语义：

**锁定语义：** `found` 写入 context 后，**紧邻的连续** `updateRecord/notify/addComment` 若 `target===found`，对每条 found 执行一遍该连续段，然后继续。其它节点不进入循环。单测必须覆盖：found 3 条 + 一个 updateRecord(target=found) → 3 次更新。

## 5. 执行器与拦截

### 5.1 文件（后端）

| 文件 | 动作 | 冻结不动 |
| --- | --- | --- |
| `NewLife.Cube/Entity/Cube.xml` | **仅**增加 `EntityAutomation`。**禁止**再增加 `AutomationRun` / `自动化运行` 表 | 不改既有表结构 |
| xcode 生成的 `实体自动化*.cs` + Models | 生成 | Biz 外不手改生成区；**禁止**再生成 `自动化运行.*` |
| `NewLife.Cube/Entity/实体自动化.Biz.cs` | FindByTypePath、EnsureHookToken、SaveCompiled | |
| `NewLife.Cube/Automation/AutomationRun.cs` | 内存队列 POCO：Enqueue、FindQueued、FindDueWaiting、HasRecentActive | **不是** Entity；勿放到 `Entity/` |
| `NewLife.Cube/Automation/AutomationModule.cs` | EntityModule：Valid 快照 Dirtys；Insert/Update/Delete 成功后 Enqueue | 不改 UserModule |
| `NewLife.Cube/Automation/AutomationTrigger.cs` | Collect 匹配矩阵 | |
| `NewLife.Cube/Automation/AutomationFilter.cs` | `Match(IEntity\|IDictionary, ViewFilterDto)` 与 searchFilters 对齐 | |
| `NewLife.Cube/Automation/AutomationGraph.cs` | Compile/Validate/Parse | |
| `NewLife.Cube/Automation/AutomationExecutor.cs` | 跑图 | |
| `NewLife.Cube/Automation/AutomationActions.cs` | 各动作实现 | |
| `NewLife.Cube/Automation/AutomationWorker.cs` | IHostedService 消费内存 queued / 到期 waiting | |
| `NewLife.Cube/Automation/EntityAutomationJob.cs` | `[CronJob("EntityAutomationTick", "0 * * * * ?", Enable=true)]` | 不改 JobService 调度算法 |
| `NewLife.Cube/Controllers/AutomationController.cs` | 见 §6 | 不把 8+ Action 塞进已膨胀的 CubeController |
| `NewLife.Cube/CubeService.cs` | UseCube 后 `AutomationModule.RegisterAllFactories()`；DI Worker | 不改登录/租户中间件 |
| `NewLife.CubeNC/*.csproj` | Link `EntityAutomation`、Model、Automation 目录、Controller（MVC 可不暴露 SPA 路由，但拦截器要挂）。**不** Link `自动化运行.*` | |
| `NewLife.Cube.Tests/Osc260815AutomationTests.cs` | 见测试设计 | |

拦截注册：`UseCube` 内 `EntityFactory.InitAll` 之后遍历 `EntityFactory.Entities`，对每个 factory `Modules.Add(new AutomationModule())`（若已添加则跳过）。再找 XCode 是否有全局 Persistence 包装；**以「任意 IEntity.Insert 成功都能入队」为准**，实施时对照 `XCode.EntityModule` / `IEntityPersistence` 实际 API，不得退化为只改 `EntityController2`。

`EntityController2` / `OnMerge` / `EnableOrDisableSelect` **本号不改行为**，只靠拦截器覆盖。

### 5.2 Filter 操作符（必须与 OSC-0015 一致）

`logic`: `all`=AND，`any`=OR；`conditions` 空 → true。

| op | 语义 |
| --- | --- |
| eq / neq | 标量或数组（数组=任一相等） |
| contains / notContains | 字符串包含，大小写不敏感 |
| isNull / notNull | null 或空串或空数组 |
| gt / gte / lt / lte | 数字 |
| after / before | 日期（含当日边界：after 为 >，before 为 <） |

字段名匹配大小写不敏感。未知 op → 该条件 false。未知字段 → 该条件 false。

前端 `matchesViewFilter` **不改语义**。C# 单测用例与 `searchFilters.spec.ts` 已有例子一一对应（status/name/age/createTime/isNull）。

### 5.3 notify 发送

1. Insert `NotificationRecord`（Channel/UserId/Title/Content/Success）。
2. Mail：`MailService.GetConfig(tenantId, "Notify")` + 现有 Smtp 发送；无配置则 Success=false，Run 不整单失败（节点 ok=false 但继续）。**锁定：notify 渠道失败不中止后续节点**；create/update/http/ai 失败则中止。
3. Sms：SmsConfig 同类。
4. DingTalk/WeCom：若实体配置含 Webhook URL 则 POST text；没有则只记 InApp 式记录且 ok=false。
5. InApp：只写记录；用户可在壳顶栏「站内通知」抽屉查看。

**接收人 `to`（现行）：**

```json
{
  "kind": "users" | "roles" | "departments",
  "users": [1, 2],
  "roles": [],
  "departments": []
}
```

- UI：**三选一**（与条件区「并且/或者」同款 `a-radio-group type="button"`）；切换 kind 清空非当前数组。
- 执行：`kind=users` 直接取 ID；`roles` 展开启用用户（RoleID / RoleIds）；`departments` 按 DepartmentID 展开。
- 兼容旧数据：无 `kind` 时若仅一个数组有值则推断；仍支持旧 `mode=userId|field|fixed`。
- 选项数据：`GET /Cube/Automation/Recipients?kind=&key=`；前端并行回退 `page.getList(/Admin/User|Role|Department)`（pageIndex 从 0）。

### 5.4 站内通知（壳）

- 顶栏租户与外观之间：`remind` 图标 + 未读徽标。
- 抽屉：`InboxDrawer`，`placement="right"`，`:footer="false"`（无取消/确定）；点遮罩关闭。
- API：Inbox / UnreadCount / MarkRead（见 §6）。

## 6. API

控制器：`[Route("Cube/Automation")]`，登录校验同 CubeController（token）。**仅** `POST Cube/Automation/Hook/{token}` 标 `[AllowAnonymous]`。

权限辅助：`AutomationAuth.CanConfigure(user, typePath)` = 该菜单实体 **Update** 或系统管理员；`CanViewRuns` = **Detail** 或可配置；`CanPressButton` = 规则 `requirePermission`。

| 方法 | 路径 | 权限 | 入参 | 成功 |
| --- | --- | --- | --- | --- |
| GET | `/Cube/Automation` | 可配置或可看 | `typePath` 必填；可选 `enable`、`triggerKind` | 数组，**不**返回 GraphJson 全量可改 HookToken：列表项含 `hasWebhook` 布尔，token 仅详情且可配置者可见 |
| GET | `/Cube/Automation/{id}` | 可配置 | | 含 TriggerConfig、GraphJson、HookToken（可配置） |
| POST | `/Cube/Automation` | 可配置 | body：Name, Enable, Priority, TypePath, TriggerKind, TriggerConfig, filter, actions | 服务端编译；201 |
| PUT | `/Cube/Automation` | 可配置 | 同 POST + Id + Version | 409 若 Version 不匹配 |
| DELETE | `/Cube/Automation?id=` | 可配置 | | 204 |
| GET | `/Cube/Automation/Runs` | 可看 | `typePath`；可选 `automationId`、`recordKey`、分页 | 列表来自系统 Log（Action=`Automation`），无敏感头 |
| POST | `/Cube/Automation/Run` | 按钮权限 | `{ automationId, recordKey }` | 入队 Run Id |
| POST | `/Cube/Automation/Hook/{token}` | 匿名 | raw JSON body | 200 `{ runId }`；token 无效 404（不暴露是否曾存在）；签名失败 401 |
| GET | `/Cube/Automation/Meta?typePath=` | 可配置 | 可选 kind | 字段名/显示名/typeName，供选择器 |
| GET | `/Cube/Automation/Entities?permission=update\|insert` | 可配置 | | 有对应权限的实体 `{ typePath, displayName, name }`，供 create/find 下拉 |
| GET | `/Cube/Automation/Recipients?kind=user\|role\|department&key=` | 登录 | | 最多 50 条 `{ id, name, displayName }`；无关键词时 Enable 为空可回退全量前 50 |
| GET | `/Cube/Automation/Inbox` | 登录 | pageIndex/pageSize/unread | 当前用户 InApp 列表 |
| GET | `/Cube/Automation/Inbox/UnreadCount` | 登录 | | `{ count }` |
| POST | `/Cube/Automation/Inbox/Read` | 登录 | `{ id?, all? }` | 标记已读 |

错误：统一 `ApiResponse` code≠0。未登录 401。无权限 403。typePath 空 400。

Webhook 限流：同 token 每分钟 60，超限 429。缓存键 `auto:hook:{token}`。

## 7. 前端文件地图

| 文件 | 动作 | 冻结 |
| --- | --- | --- |
| `packages/api-core/src/api.ts` | `createAutomationApi`（含 entities/recipients/inbox*） | 不改 comment/page 契约 |
| `packages/api-core/src/cube.ts` + `index.ts` | 挂 `automation` | |
| `packages/api-core/src/api.spec.ts` | URL 用例 | |
| `web/src/api/index.ts` | 聚合导出 | |
| `web/src/core/utils/automationGraph.ts` | compile/parse/normalizeTrigger；`AUTOMATION_MENU_ACTION_TYPES`；`validateFoundTargetChain` | |
| `web/src/core/utils/automationGraph.spec.ts` | ≥8 例 + found 链路 | |
| `web/src/core/utils/opsAction.ts` | `buildOpsParts` 可选 `automationButtons`，最多 **3** 个 | detail/edit/delete 文案不改 |
| `web/src/core/utils/opsAction.spec.ts` | 额外按钮与上限 | |
| `web/src/core/utils/iconRegistry.ts` + `iconComponents.ts` | `lightning` / `remind` / `more` / 动作菜单图标 | 不改 DEFAULT_MENU_ICON |
| `web/src/views/crud/DefaultList.vue` | 顶栏「自动化」；合并 automationFields | **不删** FilterBuilder/Group/搜索/高级 |
| `web/src/views/crud/useListAutomation.ts` | 拉 button 规则、打开抽屉 | |
| `web/src/features/vtable/useListTable.ts` | `__ops` 点击 `auto:{id}` | 分组/冻结列语义不改 |
| `web/src/views/crud/automation/AutomationDrawer.vue` | 薄：流程卡片中心 | |
| `web/src/views/crud/automation/useAutomationDrawer.ts` | 列表/启用/删 | |
| `web/src/views/crud/automation/AutomationEditor.vue` | 薄：飞书双栏 + 运行日志 Tab | |
| `web/src/views/crud/automation/useAutomationEditor.ts` | 草稿、保存、字段条件勾选 | |
| `web/src/views/crud/automation/AutomationActionCard.vue` | 薄：各动作表单 | |
| `web/src/views/crud/automation/useAutomationActionCard.ts` | 接收人/字段赋值/实体下拉 | |
| `web/src/layouts/ShellToolbar.vue` + `useShellToolbar.ts` | 站内通知入口 | |
| `web/src/layouts/RootLayout.vue` | 挂 InboxDrawer | |
| `web/src/stores/app.ts` | `inboxDrawerVisible` / `inboxUnreadCount` | |
| `web/src/views/inbox/InboxDrawer.vue` + `useInboxDrawer.ts` | 时间轴 + 已读 | |
| `web/src/views/crud/RecordDrawer.vue` | **不**再放自动化 Runs Tab（迁入编辑器运行日志） | 不删历史/评论 Tab |
| `FilterBuilderPopover.vue` / `filterBuilder.ts` / `searchFilters.ts` | **不改语义**；条件 ops 矩阵复用 | 禁止删除 |

### 7.1 顶栏与编辑器 UI（飞书对齐）

**列表顶栏：** 添加记录（左）｜筛选 → 分组 → 搜索 → **〔插入〕自动化** → 高级。

「自动化」：`a-button type="text"`，图标 `lightning`，`v-if="flags.canUpdate"`。任意实体视图（含 tree/card）显示。

**抽屉：** `placement="right"`，中心约 880px / 编辑约 1100px（`<768px` 全宽），`unmount-on-close`。

**编辑器布局（替代原 Steps 三步）：**

```
Tabs：编辑 | 运行日志
编辑：名称
      ┌ 第1步：当…发生时 ┐   →   ┌ 第2步：就执行… ┐
      │ 触发下拉         │       │ Timeline 动作卡片 │
      │ 字段条件卡片列表 │       │ + 添加动作        │
      └──────────────────┘       └──────────────────┘
底栏：自然语言摘要 | 仅保存 | 保存并启用
```

**第 1 步字段条件（飞书式）：**

- 标题「同时满足以下条件」或 fieldChange「关注以下任一字段的变更」；非 fieldChange 时标题旁「并且/或者」按钮组 + 全选/取消。
- **每个字段一张浅灰圆角小卡片**：勾选框 + 字段图标 + 名称。
- 勾选后：字段名与下方条件区用 **分隔线**；行内「满足/变更为」+ 运算符（约 **4/12**）+ 条件值（约 **8/12**）。
- 条件字段源：列表/搜索/编辑字段并集，缺省时 Meta 回退；支持「+ 添加同时满足的条件」孤儿行。

**第 2 步动作卡片：**

- 头栏左侧：动作类型名；右侧：`⋯`（`more`）下拉含上移/下移/删除（各带图标）+ **放大**收起/展开箭头。
- 「添加动作」下拉 **8** 项（无 runAutomation）。
- notify：渠道；接收对象按钮组（用户/角色/部门）+ 多选下拉；标题/正文。
- updateRecord / addComment：目标 current|found；found 无前置 find 时卡片告警。
- createRecord / findRecords：Entities 下拉；find 可配 ViewFilter 行。

Webhook：只读 token + 复制；「重新生成」确认后 PUT。

### 7.2 ops 列

`buildOpsParts` 返回 `(OpsAction | string)[]`，自动化项为 `auto:{id}`，标签用规则 `label`。宽度：每多一个按钮 +56px，最多 3 个自动化。点击发 `POST Run`，Message「已开始运行」；403 Message.error。

### 7.3 运行日志位置

- **流程编辑器** Tab「运行日志」：该规则系统 Log（Action=`Automation`），**不**改 Log 表结构。
- RecordDrawer **不**再增加「自动化」Runs Tab（与早期草案不同；见 `ui/information-architecture.md`）。

### 7.4 壳站内通知

顶栏顺序：…租户 → **站内通知(remind)** → 外观 → 账号。抽屉无 footer；点击主界面遮罩关闭。
## 8. 条件矩阵（UI）

| 输入 | 配置入口 | 行按钮 | 保存 | 运行日志（编辑器） | 站内通知 |
| --- | --- | --- | --- | --- | --- |
| entity + canUpdate | 显示 | 显示已启用 button 规则 | 允许 | 已保存流程可见 | 登录后可见 |
| entity + !canUpdate + canViewDetail | 隐藏 | 若 requirePermission=detail 仍显示 | 无入口 | 无配置入口 | 可见 |
| entity + 只读页 isReadOnly | 隐藏 | 隐藏 | 无 | 无 | 可见 |
| object / home / custom | 无 DefaultList | 无 | 无 | 无 | 可见 |
| 未登录 | 整站去登录 | | | | |

## 9. 核心文档影响

| 文档 | 动作 |
| --- | --- |
| `NewLife.Cube.ArcoVue/web/README.md` | 登记自动化入口、API、SFC 目录 `views/crud/automation/` |
| `Doc/功能清单.md` | 追加 **DATA-13** 实体自动化（拦截+执行器+API）；追加 **SPA-20** ArcoVue 自动化配置（SPA-19 已是 EntityComment，不得改写既有行） |
| `Doc/Api/核心接口架构.md` | `/Cube/Automation*` |
| `NewLife.Cube.ArcoVue/ArcoVue企业中后台迁移方案.md` | 写明：**实体自动化由本号交付（GraphJson+C#）**；FlowGram 仍是后期工作流画布样例，**不是**本自动化运行时；修正「为 FlowGram 留空洞号」旁若仍暗示自动化=FlowGram 的句子 |

不改 OSC-0018 设计稿。不改 397e 进行中文档。

## 10. 测试设计

### Vitest

- `compileAutomationGraph`：无条件；有 filter；20 个动作；非法 type 剔除。
- `parseAutomationGraph`：线性往返；分叉图返回 error 标志。
- `normalizeTriggerConfig`：fieldChange 空字段；offset 夹紧；button label 过长截断 12。
- `buildOpsParts`：0/3/4 个按钮。
- `validateFoundTargetChain`：无 find / 有 find 后再 found。
- api-core：list/save/run/recipients/entities/inbox URL。

### XUnit（内存 SQLite + DAL.AddConnStr，仿 OSC-0002）

- Filter 与 searchFilters.spec 同构用例。
- Compile 校验环/未知 type。
- Debounce 3s；Depth 3 丢弃。
- Dirtys：改 Name 触发 fieldChange(watch Name)，只改其它字段不触发。
- HookToken 错误 404；签名错误。
- TenantId 不匹配不入队。
- 预留 `approval` 节点执行 failed。

### 构建命令（验收必跑）

```
pnpm --filter @cube/api-core test
pnpm --filter @cube/arco-vue test
pnpm --filter @cube/arco-vue build
dotnet test NewLife.Cube.Tests --filter Osc260815
dotnet build NewLife.Cube
dotnet build NewLife.CubeNC
```

预期：0 failed；0 error。

### 手工

Admin/User：创建 insert→notify(InApp，选用户接收人)；新增用户后 Runs=succeeded，顶栏站内通知可见。关掉 Enable 再新增无新 Run。无 Update 角色看不到顶栏「自动化」。动作卡片 `⋯` 菜单与字段条件卡片布局符合 §7.1。
