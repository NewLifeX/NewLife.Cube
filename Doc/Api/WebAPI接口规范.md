# 第36章 WebAPI接口规范

> 本章定义魔方 WebAPI 的**公共契约**：路由约定、响应信封、业务码、分页排序、搜索过滤、写操作语义、字段校验、认证授权与异常边界。
> 单个接口的字段级契约以 OpenAPI（Swagger）为准，本章不再逐条罗列实体接口的入参出参。

---

## 36.1 契约来源与兼容策略

| 来源 | 位置 | 用途 |
|------|------|------|
| OpenAPI（运行期） | `/swagger`（`NewLife.Cube.Swagger`） | 由控制器与实体元数据实时生成，**单一事实源** |
| OpenAPI（离线快照） | `CubeDemo/contracts/openapi/*.json` | 示例项目接口快照，可做 diff 审查 |
| 前端请求契约 | `NewLife.Cube.Vue/web/docs/standards/api-contract.md` | 前端请求层如何消费本契约 |

- 路由、参数、字段以 OpenAPI 为准；本章只描述跨接口的公共约定，两者冲突时以 OpenAPI 与源码为准。
- **兼容策略**：客户端必须忽略未知字段；服务端只做增量新增。删除字段、修改字段类型或改变 `code` 语义属于不兼容变更。

---

## 36.2 路由约定

WebAPI 控制器统一继承 `ControllerBaseX`，类级路由为：

```csharp
[ApiController]
[Produces("application/json")]
[Route("api/[area]/[controller]/[action]")]
public class ControllerBaseX : ControllerBase, IActionFilter
```

- **Area 必填**：实体接口形如 `/api/{area}/{controller}`，例如 `/api/School/Student`。
- 实体控制器用字面量路由覆盖默认模板，形成 RESTful 风格；其余动作沿用 `{action}` 模板。
- 服务控制器（`Auth`、`Sso`、`OAuth` 等）用 `[Route("[controller]/[action]")]` 覆盖，**不带 `/api` 前缀**，例如 `POST /Auth/Login`。

### 标准 CRUD 路由

| 操作 | 方法与路径（推荐） | 兼容写法 | 权限 |
|------|------------------|---------|------|
| 列表 | `GET /api/{area}/{controller}` | — | Detail |
| 详情 | `GET /api/{area}/{controller}/{id}` | — | Detail |
| 新增 | `POST /api/{area}/{controller}` | — | Insert |
| 修改 | `PUT /api/{area}/{controller}/{id}`（body 可省略主键） | `PUT /api/{area}/{controller}`（主键在 body） | Update |
| 删除 | `DELETE /api/{area}/{controller}/{id}` | — | Delete |
| 批量删除 | `DELETE /api/{area}/{controller}?ids=1,2,3`（单次最多 100 个，超出改用条件删除） | `?ids=1&ids=2`（重复参数） | Delete |
| 条件删除 | `DELETE /api/{area}/{controller}?<搜索条件>`（与列表搜索同形，必须至少一个业务条件） | — | Delete |

> **主键一致性**：路径式修改时，若 body 也带主键，两者必须一致，否则返回 `-2`，避免"路径指向 A、内容改 B"。
>
> **删除三态**：路径带主键删单条；`ids=` 删选中的多条；既无主键也无 `ids` 但带业务条件时，按条件删除全部匹配行（不受 `pageSize` 限制）。单条删除的响应 `data` 为被删行，批量与条件删除的 `data` 为删除行数。
>
> **已移除的历史端点**：`GET .../Detail?id=`、`DELETE .../DeleteSelect` 与 `DELETE .../DeleteAll` 不再提供。前端编译后嵌入 DLL、与后端同版本发布，无需兼容旧写法。
>
> **路径主键**：详情、修改、删除的主键一律在路径中，与 `GET /{id}`、`PUT /{id}`、`DELETE /{id}` 对称。
>
> **兼容说明**：`PUT` 主键在 body 是历史写法，仍可用但已不推荐；路径式路由与 `GET /{id}` 对称，符合 REST 习惯。
>
> 未匹配的 `/api` 请求统一返回 **HTTP 404**（`UseCube` 已注册 `/api/{**path}` 回退，启用 SPA 皮肤时同样生效，不会落到 `index.html`）。

---

## 36.3 统一响应信封

### 结构与字段

`ApiResponse<T>`（实体控制器返回类型）：

| 字段 | 类型 | 说明 |
|------|------|------|
| `code` | Int32 | 0 表示成功，非 0 表示失败，**恒定输出** |
| `message` | String | 提示文本；**为 null 时不输出** |
| `data` | T | 业务数据，**恒定输出**（可为 null） |
| `traceId` | String | 链路追踪编号，排障用；为 null 时不输出 |
| `fieldErrors` | FieldError[] | 字段级校验错误；无字段错误时不输出 |

**只有 `code` 与 `data` 恒定输出**，其余字段按需出现：普通接口通常有 `code`/`message`/`data`，列表响应追加 `page`/`stat`，只有字段校验失败才追加 `fieldErrors`。

`ApiListResponse<T>` 在 `ApiResponse<IList<T>>` 基础上增加：

| 字段 | 类型 | 说明 |
|------|------|------|
| `page` | PageModel | 分页信息，见 36.5；无分页时不输出 |
| `stat` | T | 统计行（与列表元素同类型）；无统计时不输出 |

### 序列化约定

来自 `ControllerBaseX.OnJsonSerialize`（`SystemJson`）：

- 字段命名 **camelCase**；
- **Int64 输出为字符串**（`Int64AsString = true`），主键、`totalCount` 等均按此规则，避免 JS 精度丢失；
- 时间格式 `yyyy-MM-dd HH:mm:ss`（`FullTime = false`）；
- 普通字段的 null 默认照常输出，但信封模型用 `[JsonIgnore(WhenWritingNull)]` 把为空的 `message`/`traceId`/`fieldErrors`/`page`/`stat` 省略，因此响应体更精简。

### 成功判定

客户端只判 **`code === 0`**（历史兼容也接受 `200`）。HTTP 200 不代表业务成功。

### 真实样例（成功）

```json
{
  "code": 0,
  "message": "操作成功",
  "data": { "id": 100, "name": "测试" },
  "traceId": "0a0000031792650895535000026e3b3c"
}
```

### HTTP 状态码与 body.code 双轨

| 场景 | HTTP | body.code | 说明 |
|------|------|-----------|------|
| 成功 | 200 | 0 | |
| 业务失败 / 参数错误 / 校验失败 / 程序异常 | 200 | 非 0（负数） | 业务语义放在 `code` |
| 未认证 | 401 | 401 | `EntityAuthorizeAttribute` 直接返回 |
| 无权限 / 租户不匹配 | 403 | 403 | |
| 路由未匹配 | 404 | — | **无响应体**（含启用 SPA 皮肤时） |
| Content-Type 或请求体格式错误 | 415 | — | RFC7807 ProblemDetails，**不是信封** |

---

## 36.4 错误码：全局 ApiCode + 魔方 CubeCode

错误码分两层，不要混为一谈：

| 层 | 定义位置 | 成员 | 定位 |
|---|---------|------|------|
| **全局 `ApiCode`** | `NewLife.Remoting`（NewLife.Core） | `Ok=0`、`Ok200=200`、`BadRequest=400`、`Unauthorized=401`、`Forbidden=403`、`NotFound=404`、`TooManyRequests=429`、`InternalServerError=500` | 与 HTTP 语义对齐的**通用**响应码，跨项目统一 |
| **魔方 `CubeCode`** | `NewLife.Cube.Models` | `Failed=-1`、`ParamError=-2`、`RemotingError=-3`、`LogOff=-4`、`Exception=-5`、`CaptchaRequired=-6` | 魔方**本地扩展**，各项目可自行增补正数码 |

实体接口实际返回的码：

| 码 | 来源 | 场景 |
|---|------|------|
| 0 | `ApiCode.Ok` | 成功 |
| -1 | `CubeCode.Failed` | `ToFailApiResponse` 的默认失败码（如登录失败提示） |
| -2 | `CubeCode.ParamError` | 模型绑定失败、字段校验、缺少主键 |
| -6 | `CubeCode.CaptchaRequired` | 登录需要图形验证码 |
| 401 / 403 | `ApiCode.Unauthorized` / `Forbidden` | 认证 / 授权失败（HTTP 同步） |
| 500 | `ApiCode.InternalServerError` | `XException` 及其它非 `ApiException` 异常 |
| `ApiException.Code` | 抛出方指定 | 业务自定义，常见 `-2`、`-5`、`403` |

约定：

- **前端只判 `code === 0`（兼容 `200`）**，其余一律按失败处理；
- 全局 `ApiCode` 承担"协议级"语义（400/401/403/404/429/500），`CubeCode` 承担"魔方业务级"语义（负数）；
- 业务自定义正数码由各项目自行分配，注意避免与全局 `ApiCode` 的 400/401/403/404/429/500 撞车；
- `throw new ApiException(code, message)` 的错误码与消息原样透出；其余异常（含 `XException`）统一映射为 `500`（`ApiCode.InternalServerError`）；
- `XException` 视为已知业务异常，消息在生产环境仍保留；未预期的异常消息仅在开发模式暴露。

---

## 36.5 分页与排序

`PageModel` 只有四个字段：

| 字段 | 类型 | 说明 |
|------|------|------|
| `pageIndex` | Int32 | 从 1 开始，默认 1 |
| `pageSize` | Int32 | 默认 20；**0 表示不分页**（返回全部，慎用） |
| `totalCount` | Int64（JSON 字符串） | 总记录数 |
| `longTotalCount` | String | `totalCount` 的字符串形式，前端优先使用 |

- **没有 `pageCount` / `sort` / `desc`**：总页数由前端按 `Math.ceil(totalCount / pageSize)` 计算。
- 仅支持 `sort`（字段名，可写 `-字段名` 表示倒序）+ `desc`（布尔）单字段排序；**不支持 `orderBy` 复合排序**。`sort` 由 `PageParameter` 做 SQL 安全性校验，`orderBy` 不做校验，因此未向前端开放。
- `PageSetting.EnableTotalCount = false` 时 `totalCount` 恒为 0（免查总数），客户端需支持"无总数翻页"。
- 后端默认 `pageSize=20`、前端默认 10；`pageSize` 目前**没有服务端上限**，客户端应自行约束。

```json
{
  "page": { "pageIndex": 2, "pageSize": 20, "totalCount": 0, "longTotalCount": "0" },
  "code": 0,
  "message": "操作成功",
  "data": [],
  "traceId": "0a000003179265090240100002ee3b3c"
}
```

---

## 36.6 搜索与过滤

| 参数 | 说明 |
|------|------|
| `Q` / `q` | 关键字，对主字段做模糊匹配（`SearchWhereByKeys`），两者等价 |
| `dtStart` / `dtEnd` | 主时间字段区间 |
| 其它参数 | 按实体字段**精确等值**匹配，空值忽略 |

- 基类 `Search` 对实体字段逐个做 `Equal(ChangeType)` **精确匹配**；模糊匹配、多值匹配**不是通用能力**，必须在控制器重写 `Search`。
- `SearchData` 会在 `Search` 之外叠加数据权限（`CreateWhere()`），需要保留数据权限时应重写 `Search` 而不是绕过 `SearchData`。

```csharp
protected override IEnumerable<Student> Search(Pager p)
{
    var exp = new WhereExpression();

    var classId = p["classId"].ToInt(-1);
    if (classId > 0) exp &= Student._.ClassId == classId;

    var key = p["Q"] + "";
    if (!key.IsNullOrEmpty()) exp &= Student._.Name.Contains(key);

    // 多值匹配需自行解析，基类不支持 ?status=1,2,3 的自动拆分
    var status = p["status"].SplitAsInt();
    if (status.Length > 0) exp &= Student._.Status.In(status);

    return Student.FindAll(exp, p);
}
```

---

## 36.7 写操作语义

- **新增**：body 不允许携带自增主键，带了会被拒绝。
- **读取**：详情为 `GET /{id}`，记录不存在返回 `404`。
- **修改**：先按主键查出库中记录，再**只覆盖本次请求实际提交的字段**（提交多少改多少）。
  - 提交范围由请求 JSON 的顶层字段名确定：未出现的字段保持数据库原值，显式传 `null` 则清空该字段；
  - 前端无需做差异计算，按表单模型整体提交即可（20 个字段的表单只渲染 13 个，就提交这 13 个）；
  - 非 JSON 请求（如程序内直接调用）退回实体脏字段标记判断提交范围；
  - 推荐 `PUT /{id}`；body 也可带主键，但必须与路径一致，否则返回 `-2`；
  - 没有版本号 / ETag，并发写为"后写覆盖"。
- **删除**：`DELETE /{id}` 删单条，`DELETE ?ids=1,2,3` 删选中的多条（单次最多 100 个，超出由前端分片），`DELETE ?<搜索条件>` 按条件删全部匹配行。删除不存在的记录返回 `404`。
- **条件删除** 必须至少携带一个业务搜索条件，否则返回 `-2`，防止误删全表；服务端按条件删除全部匹配行（不受 `pageSize` 限制）。
- **假删除/恢复**：实体含假删除字段时，入口删除标记为假删除，再次删除才物理删除；`DELETE /{id}?restore=true` 用于恢复。

### 其它标准端点

| 端点 | 方法 | 说明 | 权限 |
|------|------|------|------|
| `.../GetPage` | GET | 页面设置 + 列表/表单/搜索字段（含 `allList`），见 36.10 | 匿名 |
| `.../GetFields?kind=` | GET | 单一场景字段集合；kind：1 列表、2 详情、3 新增、4 编辑、5 搜索 | 匿名 |
| `.../GetChartData` | GET | ECharts 配置数组，最多 1000 行 | Detail |
| `.../ExportFile?format=` | GET | 导出 excel/csv/json/xml/zip，返回**文件流** | Detail |
| `.../ImportFile` | POST | 导入 Excel/Csv/Json/Zip（multipart） | Insert |
| `.../UploadFile` | POST | 上传附件（multipart，`file`/`id`/`title`） | 基类未标注，子类应覆写加权限 |

> 导出与上传**不返回信封**：前端必须按 `content-type` 分流，不能按 JSON 解析。

---

## 36.8 字段校验失败

模型绑定失败与实体字段级校验统一返回：

```json
{
  "code": -2,
  "message": "The Name field is required.",
  "data": null,
  "fieldErrors": [
    {
      "field": "Name",
      "message": "The Name field is required.",
      "error": "The Name field is required."
    }
  ]
}
```

- HTTP 状态码为 **200**，由 `ApiBehaviorOptions.InvalidModelStateResponseFactory` 生成。
- `field` 为绑定后的模型属性名（对应前端表单字段，注意 camelCase）。
- 实体字段级校验（Model.xml 的必填、长度等）返回同样的 `fieldErrors` 结构，`code` 也是 `-2`。
- 前端应优先按 `fieldErrors` 定位并提示具体控件，`message` 只作整体提示。

---

## 36.9 认证与权限

### 令牌传递

`LoadToken()` 依次尝试下列来源，且**只接受三段式 JWT**，其它形式一律视为无令牌：

1. `Authorization: Bearer {token}`
2. `X-Token: {token}`
3. query `?token={token}`
4. query `?jwtToken={token}`
5. Cookie `token-{系统名}`（需开启 `CubeSetting.TokenCookie`）

登录与刷新：`POST /Auth/Login`、`POST /Auth/Refresh`（无 `/api` 前缀）。

### 401 / 403 报文

未认证（HTTP 401，中文环境示例）：

```json
{ "code": 401, "message": "没有登录或登录超时！" }
```

无权限（HTTP 403）：

```json
{ "code": 403, "message": "用户 admin 访问资源 [Student/Delete] 需要 删除 权限。" }
```

两者由 `EntityAuthorizeAttribute` 直接返回，**不含 `traceId` / `fieldErrors`**。

> 注意：令牌已通过但**租户校验失败**时，由 `ControllerBaseX.OnActionExecuting` 兜底，返回的是 **HTTP 200 + `code=403`**（走 `Json()` 通道），与授权过滤器的 403 在 HTTP 状态上不同，前端应按 `code` 判定。

### 权限位与动作映射

| 动作 | PermissionFlags |
|------|-----------------|
| Index / Detail / GetChartData / ExportFile | Detail |
| Insert / ImportFile | Insert |
| Update | Update |
| Delete（单条 / `ids=` 批量 / 条件删除） | Delete |
| GetPage / GetFields | 匿名 |

自定义 Action 必须显式标注 `[EntityAuthorize(...)]`，否则不参与权限校验。

---

## 36.10 GetPage 页面元数据

`GET /api/{area}/{controller}/GetPage`（匿名）：

```json
{
  "code": 0,
  "data": {
    "setting": {
      "navView": null,
      "enableNavbar": true,
      "enableToolbar": true,
      "enableAdd": true,
      "enableKey": true,
      "enableSelect": true,
      "enableFooter": true,
      "isReadOnly": false,
      "enableTableDoubleClick": true,
      "orderByKey": true,
      "doubleDelete": true,
      "develop": false,
      "isSystem": false
    },
    "list": [],
    "allList": [],
    "addForm": [],
    "editForm": [],
    "detail": [],
    "search": []
  },
  "traceId": "0a0000031792650895535000026e3b3c"
}
```

- `list` 已应用当前用户的列配置（顺序、隐藏）；`allList` 是未裁剪的全量字段，供列设置面板使用。
- `develop` 与 `isSystem` 驱动开发类菜单（备份、还原、清空数据表）的显示。
- 字段对象结构见《第6章 字段元数据》，本章不重复。

---

## 36.11 异常边界

| 位置 | 拦截者 | 返回 |
|------|--------|------|
| 模型绑定 / 校验 | `InvalidModelStateResponseFactory` | 200 + `code=-2` + `fieldErrors` |
| 控制器动作内未捕获异常 | `ControllerBaseX.OnActionExecuted` | 200 + `ApiException.Code`；其它（含 `XException`）→ `500` |
| Insert / Update / Delete 内部捕获 | `BuildFailResponse` | 同上，并尽量带 `fieldErrors` |
| 授权过滤器 | `EntityAuthorizeAttribute` | HTTP 401 / 403 + 裸 `{code,message}` |
| 路由未匹配 / 415 | 框架 / `UseCube` 回退 | 404 空响应体 / 415 ProblemDetails |
| MVC 视图动作（`[ApiFilter]`） | `ApiFilterAttribute` | `{code, message, data}` |

- **生产环境脱敏**：`SysConfig.Develop = false` 时，未预期异常的消息被替换为通用文案；`ApiException` / `XException` 的消息保留。开发模式始终返回原始消息。
- 排障优先看 `traceId` 与 `code`，不要依赖 `message` 文案做逻辑判断。

---

## 36.12 已知限制

以下为当前实现的事实约束（不是推荐用法），评估新接口时需提前考虑：

| 限制 | 影响 |
|------|------|
| 无 `pageSize` 上限，`pageSize=0` 返回全部 | 单请求可能拉全表 |
| `sort` 由数据层做安全性校验 | 单字段安全；复合排序未开放，避免绕过校验 |
| 无并发控制（无 ETag / 版本号） | 并发修改为后写覆盖 |
| JSON 的 PUT 请求体会被缓冲 | 用于解析"实际提交了哪些字段"，大请求体会落盘 |
| 基类 `UploadFile` 未标注权限 | 子类必须覆写并加 `[EntityAuthorize]` |
| 错误码无命名空间约束 | `ApiCode` 与 `CubeCode` 仅靠文档约定分层，业务正数码无强制分段 |

---

## 附录：curl 示例

```bash
HOST=http://localhost:5000

# 1. 登录换取 JWT（服务未关闭密码登录时）
curl -X POST "$HOST/Auth/Login" -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"<password>"}'

# 2. 列表（分页 + 排序 + 关键字）
curl "$HOST/api/School/Student?pageIndex=1&pageSize=20&sort=Id&desc=true&Q=张" \
  -H "Authorization: Bearer $TOKEN"

# 3. 详情（主键在路径）
curl "$HOST/api/School/Student/1" -H "Authorization: Bearer $TOKEN"

# 4. 新增
curl -X POST "$HOST/api/School/Student" -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" -d '{"name":"张三"}'

# 5. 修改（RESTful，主键在路径）
curl -X PUT "$HOST/api/School/Student/1" -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" -d '{"name":"李四"}'

# 5b. 修改（兼容写法，主键在 body）
curl -X PUT "$HOST/api/School/Student" -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" -d '{"id":1,"name":"李四"}'

# 6. 删除单条（主键在路径）
curl -X DELETE "$HOST/api/School/Student/1" -H "Authorization: Bearer $TOKEN"

# 6b. 批量删除选中（ids 逗号分隔，亦可用重复参数 ids=1&ids=2）
curl -X DELETE "$HOST/api/School/Student?ids=1,2,3" -H "Authorization: Bearer $TOKEN"

# 6c. 条件删除（与列表搜索同形，必须至少一个业务条件）
curl -X DELETE "$HOST/api/School/Student?classId=12&Q=张" -H "Authorization: Bearer $TOKEN"

# 7. 页面元数据（匿名）
curl "$HOST/api/School/Student/GetPage"

# 8. 字段集合（匿名，1=列表）
curl "$HOST/api/School/Student/GetFields?kind=1"
```

> 请求体格式错误或缺少 `Content-Type: application/json` 时，框架返回 **415 ProblemDetails**，不是本章描述的 JSON 信封。

---

**下一章**：[Swagger文档](Swagger文档.md) - 了解 Swagger 文档的配置
