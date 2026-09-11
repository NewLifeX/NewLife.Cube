# 访问规则

魔方提供基于规则的访问控制服务，支持 URL、IP、User-Agent、用户名等多维度匹配，兼具静态规则拦截和动态响应码检测两种工作模式，可实现 IP 黑名单、路径拦截、请求限流和爬虫/扫描攻击自动封禁。

在此基础上，魔方集成了内置威胁检测、持久化自动封禁与安全事件审计的[安全防御体系](PERM-安全防御.md)。

---

## 核心组件

| 组件 | 位置 | 说明 |
|------|------|------|
| `AccessRule` | `NewLife.Cube/Entity/` | 访问规则实体，持久化规则配置与自动封禁记录 |
| `AccessService` | `NewLife.CubeNC/Services/` | 访问控制服务，在请求管道中执行规则匹配和封禁 |
| `BlockService` | `NewLife.CubeNC/Services/` | 封禁服务，维护自动封禁快照（见安全防御） |
| `SecurityEventService` | `NewLife.CubeNC/Services/` | 安全事件审计与封禁告警（见安全防御） |
| `RunTimeMiddleware` | `WebMiddleware/` | HTTP 中间件，调用 AccessService 执行检测 |

`AccessService` 以文件链接方式同时被第三代（`NewLife.Cube`）和第二代（`NewLife.CubeNC`）引用，逻辑完全共享。

---

## 实体字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| `Name` | String | 规则名称 |
| `Priority` | Int32 | 优先级，数值越大越先匹配，默认 0 |
| `Enable` | Boolean | 是否启用 |
| `Url` | String | URL 路径匹配模式，支持通配符和反向匹配 |
| `ResponseCodes` | String | 触发响应码（如 `404,403`），设置后启用**响应码检测模式** |
| `UserAgent` | String | User-Agent 匹配模式 |
| `IP` | String | 来源 IP 匹配模式 |
| `LoginedUser` | String | 登录用户名匹配模式 |
| `ActionKind` | Enum | 命中后的动作：`Pass(1)` 放行 / `Block(2)` 拦截 / `Limit(3)` 限流 |
| `BlockCode` | Int32 | 拦截时返回的 HTTP 状态码（如 403/302） |
| `BlockContent` | String | 拦截时返回的响应内容，302 时填目标跳转 URL |
| `LimitDimension` | Enum | 限流维度：`IP(1)` / `User(2)` |
| `LimitCycle` | Int32 | 限流周期（秒），响应码检测时同时用作动态封禁时长 |
| `LimitTimes` | Int32 | 限流周期内允许的最大次数，超过后触发动作 |
| `ExpireTime` | DateTime | 过期时间。到期后规则自动失效，仅供系统自动封禁使用（见安全防御）；未设置表示永久有效 |

### 关于 `LimitCycle` 的双重语义

`LimitCycle` 在两种模式下含义不同：

- **限流规则**（`ActionKind=Limit`）：考察时间窗口，窗口内超过 `LimitTimes` 次则拦截
- **响应码检测**（配置了 `ResponseCodes`）：既是计数时间窗口，也是触发阈值后动态封禁 IP 的时长

---

## 匹配模式语法

Url / UserAgent / IP / LoginedUser 字段均支持以下语法：

| 语法 | 示例 | 含义 |
|------|------|------|
| 精确匹配 | `/api/login` | 完全相等 |
| 通配符 | `/api/*` | 星号匹配任意字符 |
| 反向匹配 | `!*.js` | 感叹号前缀：匹配"不以 .js 结尾"的路径 |
| 多值 OR | `192.168.*,10.*` | 逗号分隔：任一匹配即成立 |
| 空值 | _(留空)_ | 该维度不参与匹配（相当于全部通过） |

所有多值判断均为 **OR** 语义（任一匹配），各维度之间为 **AND** 语义（全部满足）。

---

## 两种工作模式

### 模式一：静态规则匹配

在**请求处理前**执行，适用于已知特征的黑名单拦截和限流。

```
请求到达 → Valid()
    │
    ├─ 检查IP是否命中自动封禁快照（持久化封禁，内存加速，见安全防御）
    │    └─ 命中 → 直接返回封禁规则，拦截
    │
    ├─ 加载所有启用规则（FindAllWithCache，有缓存；自动封禁规则不参与遍历）
    ├─ 按 Priority 降序、Id 降序排列
    │
    ├─ 逐条匹配 Url × UserAgent × IP × LoginedUser
    │    ├─ 命中 Pass  → 返回 null（放行，豁免后续规则与威胁检测）
    │    ├─ 命中 Block → 返回规则（中间件拦截并响应 BlockCode/BlockContent）
    │    └─ 命中 Limit → 检查会话限流次数
    │           ├─ 未超限 → 继续评估后续规则
    │           └─ 超限   → 返回规则（中间件拦截）
    │
    └─ 内置威胁检测（安全防御，可按观察/拦截模式配置）
         ├─ 未命中 → 返回 null（放行）
         └─ 命中   → 观察模式仅记录安全事件；拦截模式返回拦截规则（连续触发自动封禁）
```

**注意**：`Pass` 动作优先级最高，一旦命中立即放行，不再匹配后续规则。可用于内网白名单绕过全局限制。

### 模式二：响应码动态检测

在**响应完成后**执行（`await _next.Invoke()` 之后），用于检测 Web 扫描、爬虫等攻击行为。

```
响应完成 → TrackResponse(statusCode, url, ip, ...)
    │
    ├─ 加载配置了 ResponseCodes 的启用规则
    ├─ 逐条规则：
    │    ├─ 响应码是否在 ResponseCodes 列表中？否 → 跳过
    │    ├─ 请求URL是否匹配 rule.Url 模式？否 → 跳过
    │    ├─ 来源IP是否匹配 rule.IP 模式？否 → 跳过
    │    │
    │    ├─ 构造计数Key：access:resp:{ruleId}:{ip}:{timeWindow}
    │    ├─ 原子递增计数，首次写入时设置 LimitCycle 秒过期
    │    │
    │    └─ 计数 > LimitTimes ？
    │         └─ 是 → 写入封禁Key：access:block:{ip}（TTL = LimitCycle 秒）
    │
    └─ 下一次请求到达时，Valid() 在最前面检查封禁Key → 拦截
```

#### 关于拦截时序

响应码检测**无法拦截触发阈值的那条请求本身**，而是在下一次请求时生效。这是由 HTTP 请求-响应模型决定的，也是唯一可行的设计：

- 当前请求的响应码只有在 `_next.Invoke()` 执行完成后才可知
- 一旦响应完成，当前请求已无法被拦截
- 检测到阈值后写入封禁缓存，从下一条请求起立即生效

对于 Web 扫描攻击而言（通常发起数百次请求），在第 `LimitTimes+1` 次请求时封禁，后续所有请求均被阻断，防护效果充分。

---

## 默认规则（InitData）

系统内置 5 条示例规则，**默认均禁用**，供参考配置：

| 规则名 | 动作 | 核心条件 | 说明 |
|--------|------|----------|------|
| 封禁IP | Block | IP = `10.0.*` | 封禁指定IP段，跳转到指定URL |
| 封禁爬虫 | Block | UA = `*bot*,*spider*` | UA 含 bot/spider 则跳转 |
| IP访问太快 | Limit | 非静态资源；IP限流 60s/100次 | 频繁访问给出友好提示 |
| 内网优先 | Pass | IP = `192.*`，优先级 999 | 内网IP直接放行 |
| 404扫描检测 | Block | ResponseCodes=404；60s/20次 | 60秒内触发404超过20次，自动封禁该IP（时长按阶梯档位递增，见安全防御） |

启用 `404扫描检测` 可自动拦截绝大多数路径枚举式 Web 扫描攻击。

---

## 动态封禁缓存设计

| 缓存键 | 格式 | 用途 | TTL |
|--------|------|------|-----|
| 响应码计数 | `access:resp:{ruleId}:{ip}:{timeWindow}` | 统计某IP在时间窗口内触发次数 | LimitCycle 秒 |
| 动态IP封禁 | 访问规则表（名称前缀"自动封禁"）+ `BlockService` 内存快照 | 标记IP已被封禁，到期自动解封；已由持久化封禁取代原 `access:block` 缓存 | 到期时间 |

其中 `timeWindow = (今日已过秒数) / LimitCycle`，形成等长时间窗口。

> **边界说明**：时间窗口基于当天秒数整除，跨越零点时窗口会自然重置，连跨零点的攻击理论上不会累积到阈值。实际场景中攻击者不会只在零点附近行动，此设计不影响正常防护效果。

---

## 配置示例

### 拦截已知爬虫

```
Name:       封禁爬虫
ActionKind: Block
UserAgent:  *bot*,*spider*,*crawler*
BlockCode:  403
BlockContent: Forbidden
```

### IP 白名单（内网优先放行）

```
Name:       内网放行
ActionKind: Pass
IP:         192.168.*,10.*
Priority:   999
```

同时配置一条低优先级全局封禁（Priority=1）即可实现"仅允许内网访问"。

### API 接口限流

```
Name:          登录接口限流
ActionKind:    Limit
Url:           /admin/user/login
LimitDimension: IP
LimitCycle:    60
LimitTimes:    10
BlockCode:     429
BlockContent:  <h1>请求过于频繁，请稍后再试</h1>
```

### 404 扫描检测（自动封禁）

```
Name:          404扫描检测
ActionKind:    Block
Url:           !*.js,!*.css,!*.ico,!*.jpg,!*.png,!*.gif,!*.woff,!*.woff2
ResponseCodes: 404
LimitDimension: IP
LimitCycle:    300
LimitTimes:    20
BlockCode:     403
BlockContent:  <h1>您的IP已因频繁触发404被暂时封禁</h1>
```

将 `LimitCycle` 调大即可延长封禁时长（单位秒），例如 3600 = 1 小时。

---

## 模块优化分析

### 已知局限与优化方向

| 类别 | 问题描述 | 建议方案 |
|------|----------|----------|
| **性能** | `FindAllWithCache()` 在每条请求中被调用两次（`Valid` + `TrackResponse`），虽已缓存，但每次仍执行 LINQ 过滤排序 | 将已过滤排序的列表单独缓存，或在 Middleware 层一次性加载后传给两个方法 |
| **封禁管理** | ✅ 已实现：自动封禁持久化为访问规则，列表"解封时间"列一键解封 | 详见[安全防御](PERM-安全防御.md) |
| **审计日志** | ✅ 已实现：检测与封禁动作写入审计日志（类别"安全防御"） | 详见[安全防御](PERM-安全防御.md) |
| **子网聚合封禁** | 响应码检测目前只支持单 IP 封禁，精明的攻击者可切换 IP 绕过 | 参考登录风控的三级 IP 封禁（/32、/24、/16），扩展到响应码检测模块 |
| **静态资源提前退出** | `TrackResponse` 对所有请求遍历规则，包括已确定的静态资源 | 在进入循环前，若 URL 明确为静态资源扩展名则提前 return |
| **规则语义混合** | `ActionKind`、`LimitCycle` 在静态规则和响应码检测规则中含义不同，容易误配 | 可引入 `RuleMode` 字段（`Static` / `ResponseTrack`）明确区分，减少理解成本 |
| **封禁时长精度** | 零点跨越导致计数窗口重置，极端情况下攻击者可在零点附近规避阈值 | 改用基于 `DateTime.UtcNow.ToUnixTimeSeconds()` 的绝对时间窗口 |

---

## 管理后台

访问规则在管理后台 **系统管理 → 访问规则** 中管理：

- 增删改查规则
- 启用/禁用规则
- 调整优先级
- `ResponseCodes` 配置响应码检测（留空则仅为静态规则）
- 自动封禁行（名称前缀"自动封禁"）显示"解封时间"，点击即可解除封禁
- 安全事件查看：系统管理 → 日志，按类别"安全防御"筛选

---

## 相关文档

- [安全防御](PERM-安全防御.md)
- [安全与审计](SYS-安全与审计.md)
- [中间件与过滤器](BASE-中间件与过滤器.md)
- [访问规则架构图](安全访问架构.emmx)
