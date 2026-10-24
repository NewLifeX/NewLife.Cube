# 第36章 WebAPI接口规范

> 本章定义魔方 WebAPI 的**公共契约**：路由与端点、响应信封、错误码、分页排序、搜索过滤、写操作语义、字段校验、认证授权与异常边界。
> 单个接口的字段级契约以 OpenAPI（`/swagger`）为准；两者冲突时以 OpenAPI 与源码为准。

---

## 36.1 契约来源与兼容策略

| 来源 | 位置 | 用途 |
|------|------|------|
| OpenAPI（运行期） | `/swagger`（`NewLife.Cube.Swagger`） | 由控制器与实体元数据实时生成，**单一事实源** |
| OpenAPI（离线快照） | `CubeDemo/contracts/openapi/*.json` | 示例项目接口快照，可做 diff 审查 |
| 前端请求契约 | `NewLife.Cube.Vue/web/docs/standards/api-contract.md` | 前端请求层如何消费本契约 |

- **兼容策略**：客户端必须忽略未知字段；服务端只做增量新增。删除字段、修改字段类型或改变 `code` 语义属于不兼容变更。
- **不做 URL 版本化**：兼容性靠上述增量约定维持。

---

## 36.2 路由与端点

控制器统一继承 `ControllerBaseX`，类级路由为 `api/[area]/[controller]/[action]`：

- 实体控制器用字面量路由覆盖默认模板，形成 RESTful 子资源风格；其余动作沿用 `{action}` 模板。`{area}` 必填，如 `/api/School/Student`。
- 服务控制器（`Auth`、`Sso`、`OAuth` 等）用 `[Route("[controller]/[action]")]` 覆盖，**不带 `/api` 前缀**，如 `POST /Auth/Login`。

| 操作 | 方法与路径 | 权限 |
|------|-----------|------|
| 列表 | `GET /api/{area}/{controller}` | Detail |
| 详情 | `GET /api/{area}/{controller}/{id}` | Detail |
| 新增 | `POST /api/{area}/{controller}` | Insert |
| 修改 | `PUT /api/{area}/{controller}/{id}` | Update |
| 删除单条 | `DELETE /api/{area}/{controller}/{id}` | Delete |
| 批量 / 条件删除 | `DELETE /api/{area}/{controller}`：`?ids=1,2,3` 或 `?<搜索条件>` | Delete |
| 页面元数据 | `GET .../meta` | 登录即可 |
| 图表数据 | `GET .../charts` | Detail |
| 导出 | `GET .../export?format=excel`（excel/xlsx/csv/json/xml/zip） | Detail |
| 导入 | `POST .../import`（multipart） | Insert |
| 上传附件 | `POST .../attachments`（multipart，`file`/`id`/`title`） | Insert 或 Update |

> - **主键一律在路径**：详情、修改、删除均形如 `/{id}`。修改时 body 若也带主键，必须与路径一致，否则返回 `-2`。
> - **删除三态**：路径带主键删单条；`ids=` 删选中的多条（单次最多 100 个，超出由前端分片）；既无主键也无 `ids` 但带业务条件时，按条件删除全部匹配行（不受 `pageSize` 限制）。
> - **未匹配的 `/api` 请求统一返回 HTTP 404**（`UseCube` 已注册 `/api/{**path}` 回退，启用 SPA 皮肤时同样生效）。
> - **已移除的旧端点**：`GET .../Detail?id=`、`PUT /api/{area}/{controller}`（主键在 body）、`DELETE .../DeleteSelect`、`DELETE .../DeleteAll`、`GetPage`、`GetFields`、`GetChartData`、`ExportFile`、`ImportFile`、`UploadFile`。前端编译后嵌入 DLL、与后端同版本发布，无需兼容旧写法。
> - **导出不返回信封**：返回文件流，前端必须按 `content-type` 分流；上传失败按统一信封返回。

---

## 36.3 统一响应信封

`ApiResponse<T>`（实体控制器返回类型）的字段：

| 字段 | 类型 | 输出 | 说明 |
|------|------|------|------|
| `code` | Int32 | 恒定 | 0 成功；负数为魔方业务码；正数为协议码或项目自定义码 |
| `data` | T | 恒定 | 业务数据，可为 null |
| `message` | String | 按需 | 提示文本，为 null 时省略 |
| `traceId` | String | 按需 | 链路追踪编号，排障用 |
| `fieldErrors` | FieldError[] | 按需 | 字段级校验错误，结构见 36.8 |
| `page` | PageModel | 按需 | 分页信息，见 36.5；仅 `ApiListResponse<T>` 输出 |
| `stat` | T | 按需 | 统计行（与列表元素同类型），仅 `ApiListResponse<T>` 输出 |

序列化约定（`ControllerBaseX.OnJsonSerialize`）：

- 字段命名 camelCase；时间格式 `yyyy-MM-dd HH:mm:ss`。
- **Int64 输出为字符串**（`Int64AsString`），主键、`totalCount` 等均按此规则，避免 JS 精度丢失。
- 信封模型用 `[JsonIgnore(WhenWritingNull)]` 省略空的可选字段，响应体更精简。

成功判定：客户端只判 **`code === 0`**（历史兼容也接受 `200`），HTTP 200 不代表业务成功。

```json
{
  "code": 0,
  "message": "操作成功",
  "data": { "id": "100", "name": "测试" },
  "traceId": "0a0000031792650895535000026e3b3c"
}
```

HTTP 状态码与 body.code 双轨：

| 场景 | HTTP | body.code |
|------|------|-----------|
| 成功 | 200 | 0 |
| 业务失败 / 参数错误 / 校验失败 / 程序异常 | 200 | 非 0 |
| 未认证 | 401 | 401 |
| 无权限 / 租户不匹配 | 403 | 403 |
| 路由未匹配 | 404 | —（无响应体） |
| Content-Type 或请求体格式错误 | 415 | —（ProblemDetails，不是信封） |

---

## 36.4 错误码与异常边界

错误码分两层：全局 `ApiCode`（`NewLife.Remoting`，与 HTTP 语义对齐的协议码）与魔方 `CubeCode`（`NewLife.Cube.Models`，业务级负数 `-1`~`-6`）。

| 码 | 来源 | 场景 |
|---|------|------|
| 0 | `ApiCode.Ok` | 成功 |
| -1 | `CubeCode.Failed` | `ToFailApiResponse` 的默认失败码（如登录失败） |
| -2 | `CubeCode.ParamError` | 模型绑定失败、字段校验、缺少主键、主键与路径不一致、条件删除无条件 |
| -3 ~ -5 | `CubeCode` | 远程调用失败 / 需重新登录 / 内部错误 |
| -6 | `CubeCode.CaptchaRequired` | 登录需要图形验证码，`data` 含 `captchaId`/`image`/`captchaUrl` |
| 401 / 403 | `ApiCode.Unauthorized` / `Forbidden` | 认证 / 授权失败（HTTP 同步） |
| 404 | `ApiCode.NotFound` | 删除不存在的记录 |
| 500 | `ApiCode.InternalServerError` | `XException` 及其它非 `ApiException` 异常 |
| 自定义 | `ApiException.Code` | 抛出方指定，常见 `-2`、`-5`、`403` |
异常边界：

- **模型绑定 / 校验失败** → `InvalidModelStateResponseFactory`：HTTP 200 + `code=-2` + `fieldErrors`（见 36.8）。
- **动作内未捕获异常** → `ControllerBaseX.OnActionExecuted`：`ApiException` 透出自身 `code` 与消息；其余异常（含 `XException`）统一映射为 `500`。
- **生产环境脱敏**：`SysConfig.Develop = false` 时，`ApiException` / `XException` 消息保留，未预期异常消息替换为通用文案；开发模式始终返回原始消息。
- **授权过滤器** → HTTP 401 / 403 + 裸 `{code, message}`（不含 `traceId` / `fieldErrors`）。
- **路由未匹配 / 415** → 404 空响应体 / RFC7807 ProblemDetails。

约定：

- 全局 `ApiCode` 承担协议级语义，`CubeCode` 承担魔方业务级语义；项目自定义正数码按 36.4.1 分段分配。
- 排障优先看 `traceId` 与 `code`，不要依赖 `message` 文案做逻辑判断。

### 36.4.1 业务正数码分段约定

`code > 0` 全部归业务，但正数码与 `ApiCode` 的 `200/400/401/403/404/429/500` 同域，不加约定迟早撞车。现行分段：

| 码段 | 归属 | 说明 |
|------|------|------|
| `0` | `ApiCode.Ok` | 成功，恒定 |
| `-1` ~ `-99` | `CubeCode` | 框架级负数码（见上表），新增需在此登记 |
| `1` ~ `999` | **保留** | 含 `200/400/401/403/404/429/500` 等协议码，业务**一律不得占用** |
| `1000` ~ `9999` | 业务系统按模块分段 | 每个模块申请一个起点（建议四位、以 `000` 对齐，如 `1000`、`2000`），模块内自增；同一系统内不得重叠 |
| `≥ 10000` | 外部 / 第三方回调专用 | 与内部业务码隔离 |

已占用码段登记（新增时必须追加一行，否则视为未分配）：

| 码段 | 归属 | 用途 |
|------|------|------|
| `-1` ~ `-6` | `NewLife.Cube.Models.CubeCode` | 通用失败/参数/远程/登出/内部/验证码 |
| `1` ~ `999` | — | 保留，未分配 |
| `1000` ~ `9999` | — | 魔方自身未占用；留给业务系统 |

> 只约定、不强制：码段靠评审与本文登记维系。新模块提 PR 时在登记表追加一行即可，不需要改枚举或代码。

---

## 36.5 分页与排序

`PageModel` 只有四个字段：

| 字段 | 类型 | 说明 |
|------|------|------|
| `pageIndex` | Int32 | 从 1 开始，默认 1 |
| `pageSize` | Int32 | 默认 20；**非正数按默认 20 处理，上限 1000**（超出截断） |
| `totalCount` | Int64（JSON 字符串） | 总记录数 |
| `longTotalCount` | String | `totalCount` 的字符串形式，前端优先使用 |

- 没有 `pageCount`：总页数由前端按 `Math.ceil(totalCount / pageSize)` 计算。
- 排序两种方式：①单字段 `sort`（字段名，`-字段名` 表示倒序）+ `desc`（布尔）；②复合排序 `orderBy`（如 `Name,CreateTime desc`），**优先级高于 `sort`**，仅接受「字段名（可带表别名点号）+ 可选 asc/desc，逗号分隔」的表达式，含子查询/函数/注释/分号等一律丢弃。
- `PageSetting.EnableTotalCount = false` 时 `totalCount` 恒为 0，客户端需支持"无总数翻页"。
- `pageSize` 服务端有上限（`Pager.MaxPageSize`，默认 1000）：内部不分页场景（删除全部匹配行、分片导出）由服务端自己构造 Pager 后置 0，不受请求参数影响。

```text
"page": { "pageIndex": 2, "pageSize": 20, "totalCount": "0", "longTotalCount": "0" }
```

---

## 36.6 搜索与过滤

| 参数 | 说明 |
|------|------|
| `Q` / `q` | 关键字，对主字段做模糊匹配（`SearchWhereByKeys`），两者等价 |
| `dtStart` / `dtEnd` | 主时间字段区间 |
| 其它参数 | 按实体字段**精确等值**匹配，空值忽略 |

- 基类 `Search` 对实体字段逐个做 `Equal(ChangeType)` 精确匹配；模糊匹配、多值匹配**不是通用能力**，必须在控制器重写 `Search`。
- `SearchData` 在 `Search` 之外叠加数据权限（`CreateWhere()`）；需要保留数据权限时应重写 `Search` 而不是绕过 `SearchData`。

```csharp
protected override IEnumerable<Student> Search(Pager p)
{
    var exp = new WhereExpression();

    var classId = p["classId"].ToInt(-1);
    if (classId > 0) exp &= Student._.ClassId == classId;

    // 基类只做精确保值，模糊与多值需自行解析（如 ?status=1,2,3）
    var status = p["status"].SplitAsInt();
    if (status.Length > 0) exp &= Student._.Status.In(status);

    return Student.FindAll(exp, p);
}
```

---

## 36.7 写操作语义

- **新增**：body 不允许携带自增主键，带了会被拒绝。
- **修改**：按主键查出记录后，**只覆盖请求 JSON 顶层实际出现的字段**（提交多少改多少）；未出现的字段保持原值，显式传 `null` 则清空。前端无需做差异计算，按表单模型整体提交即可（20 个字段的表单只渲染 13 个，就提交这 13 个）；非 JSON 请求退回实体脏字段标记判断。
  - ⚠️ 没有版本号 / ETag，并发写为"后写覆盖"。
  - ⚠️ JSON 的 PUT 请求体会被缓冲，用于解析"实际提交了哪些字段"，大请求体会落盘。
- **删除**：`DELETE /{id}` 删单条；`?ids=` 删选中的多条（单次最多 100 个，超出由前端分片）；`?<搜索条件>` 按条件删全部匹配行。
- **记录不存在**：详情、修改抛出 `XException` → `code=500`（`message` 在生产环境保留）；仅删除返回 `code=404`。
- **条件删除**必须至少携带一个业务搜索条件，否则返回 `-2`，防止误删全表。
- **假删除 / 恢复**：实体含假删除字段（`Deleted`/`IsDelete`/`IsDeleted`）时，首次删除标记假删除，再次删除才物理删除；`DELETE /{id}?restore=true` 用于恢复。

---

## 36.8 字段校验失败

模型绑定失败与实体字段级校验统一返回：

```json
{ "code": -2, "message": "The Name field is required.", "data": null,
  "fieldErrors": [{ "field": "Name", "message": "The Name field is required.", "error": "..." }] }
```

- HTTP 状态码为 **200**，由 `ApiBehaviorOptions.InvalidModelStateResponseFactory` 生成。
- `field` 为绑定后的模型属性名（对应前端表单字段，注意 camelCase）；实体字段级校验（Model.xml 的必填、长度等）返回同样结构，`code` 也是 `-2`。
- 前端应优先按 `fieldErrors` 定位并提示具体控件，`message` 只作整体提示。

---

## 36.9 认证与权限

`LoadToken()` 依次尝试下列来源，且**只接受三段式 JWT**，其它形式一律视为无令牌：

| 顺序 | 来源 | 形式 |
|------|------|------|
| 1 | 请求头 | `Authorization: Bearer {token}` |
| 2 | 请求头 | `X-Token: {token}` |
| 3 | 查询串 | `?token={token}` |
| 4 | 查询串 | `?jwtToken={token}` |
| 5 | Cookie | `token-{系统名}`（需开启 `CubeSetting.TokenCookie`） |

- 登录与刷新：`POST /Auth/Login`、`POST /Auth/Refresh`（无 `/api` 前缀）。
- 401 / 403 报文由 `EntityAuthorizeAttribute` 直接返回，**只有 `code` + `message`**：

```text
HTTP 401  { "code": 401, "message": "没有登录或登录超时！" }
HTTP 403  { "code": 403, "message": "用户 admin 访问资源 [Student/Delete] 需要 删除 权限。" }
```

- 令牌有效但**租户校验失败**：带 `[EntityAuthorize]` 的动作由过滤器返回 **HTTP 403**；仅由 `ControllerBaseX` 兜底的路径返回 HTTP 200 + `code=403`。前端按 `code` 判定，不要只看 HTTP 状态。
- 自定义 Action 必须显式标注 `[EntityAuthorize(...)]`，否则不参与权限校验；多个权限位之间是"或"的关系。

---

## 36.10 页面元数据（meta）

`GET /api/{area}/{controller}/meta`（登录即可；公开页需在子类重写并标注 `[AllowAnonymous]`）。响应为**视图自描述**：`list / form / add / detail / search` 各自直接输出字段数组，消费端零合并；`@newlifex/api-core` 的 `getPage` 会展开为 `setting + list/addForm/editForm/detail/search + toolbar`，各皮肤调用方式不变。

```json
{
  "code": 0,
  "data": {
    "entity": { "name": "Student", "displayName": "学生", "primaryKey": "Id" },
    "setting": {
      "enableAdd": true, "enableSelect": true, "enableToolbar": true,
      "isReadOnly": false, "doubleDelete": true, "develop": false, "isSystem": false,
      "menuId": 12,
      "requiredPermissions": { "add": 2, "update": 4, "delete": 8 }
    },
    "list": [
      { "name": "Id", "displayName": "编号", "typeName": "Int32", "primaryKey": true, "readOnly": true },
      { "name": "Name", "displayName": "姓名", "typeName": "String", "length": 50 },
      { "name": "Remark", "displayName": "备注", "typeName": "String", "visible": false },
      { "name": "DetailLink", "displayName": "明细", "typeName": "String", "virtual": true, "url": "/School/StudentDetail?studentId={Id}", "text": "查看" }
    ],
    "form": [
      { "name": "Name", "displayName": "姓名", "typeName": "String", "required": true }
    ],
    "search": [
      { "name": "q", "kind": "keyword", "displayName": "关键字", "typeName": "String" },
      { "name": "ClassId", "displayName": "班级", "typeName": "Int32", "itemType": "singleSelect", "dataSource": { "1": "一班", "2": "二班" } },
      { "kind": "range", "name": "dtStart", "end": "dtEnd", "field": "CreateTime", "displayName": "创建时间", "typeName": "DateTime" }
    ],
    "toolbar": [
      { "name": "批量刷新", "displayName": "批量刷新", "kind": "request",
        "action": "Refresh", "method": "POST", "include": ["keys"], "requiresSelection": true,
        "confirm": "确认刷新选中记录？" }
    ]
  },
  "traceId": "0a0000031792650895535000026e3b3c"
}
```

- `entity`：实体名、显示名与主键名；单对象页只返回 `entity/setting/form`。
- `list` 是**全部可用列**；`visible: false` 表示默认隐藏但可由表格设置打开。列顺序和用户显隐由前端 `localStorage` 保存，不在 `/meta` 下发 `order` / `hidden`。
- `form` 是编辑表单字段数组；`add` / `detail` 与 form 完全相同时省略，消费端回退 `form`。字段 `category` 用于前端表单分组。
- `search` 是查询项数组。`kind: "keyword"` 的项由关键字框统一处理，不进入高级条件；缺省为单字段查询。主时间字段输出 `kind: "range"`，提交 `dtStart` / `dtEnd`。
- `setting.enableAdd` / `enableSelect` / `isReadOnly` 是页面能力；`requiredPermissions` 是动作所需权限位。有效权限由前端结合 `menuId` 与登录用户 `permission` 计算，不写入 `/meta`，避免缓存旧权限。
- `toolbar` 是**自定义按钮**，内置按钮（添加/导入/导出/批量删除/刷新）仍由 `setting` 控制。`kind` 取值：
  - `link`：跳转 `url`；`appendQuery: true` 携带当前查询参数；`target` 控制窗口。
  - `request`：请求 `{base}/{action}`（或用 `url` 指定地址）；`include` 声明提交字段，`keys` 表示选中行主键（逗号拼接），其它名字取当前查询参数；`method` 缺省 GET；`confirm` 为二次确认文案。
  - `upload`：选择文件后以 `multipart` 提交到 `{base}/{action}`，文件字段名 `file`，其余 `include` 字段随表单一并提交；`accept` 限定文件类型。
  - `custom`：由前端已注册的同名渲染器处理；未注册时前端忽略该按钮并告警一次。
- 权限：`toolbar` 由服务端按当前用户过滤；新增/编辑/删除/批量按钮由前端按 `menuId + requiredPermissions + UserInfo.permission` 过滤。`requiresSelection` 的按钮在未选中数据时禁用。
- 响应带 `ETag` / `Cache-Control: private, no-cache`，并按用户、租户、语言区分；`api-core` 的 `getPage` 按页面缓存展开结果。
- 字段对象结构见《第6章 字段元数据》；`view` / `props` 为渲染器名与扩展属性透传。

---

**下一章**：[Swagger文档](Swagger文档.md) - 了解 Swagger 文档的配置