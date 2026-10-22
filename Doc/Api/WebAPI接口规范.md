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

- 全局 `ApiCode` 承担协议级语义，`CubeCode` 承担魔方业务级语义；项目自定义正数码自行分配，避开 400/401/403/404/429/500。
- 排障优先看 `traceId` 与 `code`，不要依赖 `message` 文案做逻辑判断。

---

## 36.5 分页与排序

`PageModel` 只有四个字段：

| 字段 | 类型 | 说明 |
|------|------|------|
| `pageIndex` | Int32 | 从 1 开始，默认 1 |
| `pageSize` | Int32 | 默认 20；**0 表示不分页**（返回全部，慎用） |
| `totalCount` | Int64（JSON 字符串） | 总记录数 |
| `longTotalCount` | String | `totalCount` 的字符串形式，前端优先使用 |

- 没有 `pageCount`：总页数由前端按 `Math.ceil(totalCount / pageSize)` 计算。
- 排序仅支持 `sort`（字段名，`-字段名` 表示倒序）+ `desc`（布尔），单字段；**不支持 `orderBy` 复合排序**（`sort` 由 `PageParameter` 做安全校验，`orderBy` 不校验，故未开放）。
- `PageSetting.EnableTotalCount = false` 时 `totalCount` 恒为 0，客户端需支持"无总数翻页"。
- ⚠️ `pageSize` 没有服务端上限（后端默认 20、前端默认 10），客户端应自行约束。

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

`GET /api/{area}/{controller}/meta`（登录即可；公开页需在子类重写并标注 `[AllowAnonymous]`）：

```json
{
  "code": 0,
  "data": {
    "setting": { "enableAdd": true, "isReadOnly": false, "doubleDelete": true, "develop": false, "isSystem": false },
    "list": [], "allList": [], "addForm": [], "editForm": [], "detail": [], "search": []
  },
  "traceId": "0a0000031792650895535000026e3b3c"
}
```

- `setting` 与 `PageSetting` 同名，完整清单见 OpenAPI；`develop` / `isSystem` 驱动开发类菜单（备份、还原、清空数据表）的显示。
- `list` 已应用当前用户列配置（顺序、隐藏）；`allList` 是未裁剪的全量字段，供列设置面板使用。
- 字段对象结构见《第6章 字段元数据》；单对象控制器（`ObjectController`）只返回 `setting` 与 `editForm`。
- 原 `GetPage` / `GetFields` 已并入本接口并移除。

---

**下一章**：[Swagger文档](Swagger文档.md) - 了解 Swagger 文档的配置