# OSC-0008 Design — 表单提交归一化与抽屉历史评论

## 1. 背景与根因（证据链）

**宿主**：NewLife.Cube（MVC 版 WebAPI，前后端分离）。ArcoVue SPA 通过 `POST/PUT {typePath}` + `application/json` 调 `EntityController.Insert/Update`。

**根因**：MVC 版 `CubeService.AddCube()` **未注册** CubeNC 的 `EntityModelBinderProvider`（仅 `NewLife.CubeNC/CubeService.cs` 注册），`[ApiController]` 下 `Insert(TModel model)` 走 **System.Text.Json 直接反序列化 JSON body → TEntity**。System.Text.Json 默认**拒绝** JSON 字符串绑定到 `Int32`/数值枚举属性（抛 `JsonException`）→ `InvalidModelStateResponseFactory` 返回「请求数据格式不正确」。

**对比 Cube.Vue（正常）**：Cube.Vue select 直接用后端 `/Cube/Lookup` 的原始 `value`（`CubeController.Lookup` 返回 `["Value"] = vs.GetValue(i)` 为 number，且 `ControllerBaseX.OnJsonSerialize` 设 `CamelCase=true` 输出 `value`），提交 `number` 绑定成功。ArcoVue `lov-api.ts` 用 `String(o.value)` 字符串化存入 `dataSource: Record<string,string>`，select 选中值为字符串 → 提交失败。

**字段名结论**：GetPage/GetFields/GetDetail 响应均经 `ControllerBaseX.OnActionExecuted`（`IApiResponse` → FastJson `CamelCase=true`）输出，`DataField.name` 为 camelCase；ArcoVue `toFieldMeta` 直接取 `field.name`，故 `FieldMeta.name` 即 camelCase，与 Cube.Vue 一致。**本号不做字段名转换**。

## 2. 物理改动地图

| 文件 | 改动 | 不应改动 |
|---|---|---|
| `web/src/core/utils/fieldControl.ts` | `serializeSubmitModel` 增加类型归一化；新增 `normalizeSubmitValue` | LOV/控件解析、`resolveControl` 等 |
| `web/src/core/utils/submitPayload.ts` | `prepareSubmitPayload` 空值矩阵调整（String 提交 `""`） | 主键过滤、字段集来源 |
| `web/src/core/utils/submitPayload.spec.ts` | 新增类型/空值矩阵单测 | 既有用例 |
| `packages/api-core/src/types.ts` | 新增 `EntityCommentModel` | 既有类型 |
| `packages/api-core/src/api.ts` | 新增 `createCommentApi` | 既有 API |
| `packages/api-core/src/index.ts` | 导出 `createCommentApi` + 类型 | 既有导出 |
| `packages/api-core/src/api.spec.ts` | comment API 用例 | 既有用例 |
| `web/src/api/index.ts` | 暴露 `comment` API | — |
| `web/src/views/crud/RecordDrawer.vue` | 历史 Tab 分页/筛选/展示；评论 Tab 实现 | 表单/详情/导航逻辑 |
| `web/src/core/utils/datetime.ts`（新增） | `formatDateTime` 纯函数 | — |
| 文档 | 迁移方案 M4a/M4b、对接指南、web README | 只做事实性增量 |

**构建顺序**：先改 `packages/api-core` 并 `pnpm build`（types 入口在 dist），再改 ArcoVue web。

## 3. 表单类型归一化（方案 A）

### 3.1 归一化函数（fieldControl.ts）

在 `serializeSubmitModel` 同文件新增并导出：

```ts
/** 提交值类型归一化：字符串数字→number、字符串布尔→boolean；空值原样交给上层 */
export function normalizeSubmitValue(field: FieldMeta | undefined, value: unknown): unknown {
  if (value == null || value === '') return value;
  const typeName = (field?.typeName ?? '').trim();
  if (NUMERIC_TYPES.has(typeName)) {
    if (typeof value === 'number' && Number.isFinite(value)) return value;
    if (typeof value === 'string' && /^-?\d+(\.\d+)?$/.test(value.trim())) return Number(value);
    return value;
  }
  if (typeName === 'Boolean') {
    if (typeof value === 'boolean') return value;
    if (value === 'true' || value === '1') return true;
    if (value === 'false' || value === '0') return false;
    return value;
  }
  if (typeName === 'Enum') {
    if (typeof value === 'string' && /^-?\d+$/.test(value.trim())) return Number(value);
  }
  return value;
}
```

`NUMERIC_TYPES` 复用文件内既有集合（含 `Int32/Int64/Int16/UInt32/UInt64/Byte/SByte/Decimal/Double/Single/Short/UShort`）。

### 3.2 serializeSubmitModel 改造

```ts
export function serializeSubmitModel(model, fields) {
  const fieldMap = new Map(fields.map(f => [f.name, f]));
  const multiNames = new Set(fields.filter(f => f.multiple || f.itemType === 'multipleSelect').map(f => f.name));
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(model)) {
    if (multiNames.has(k) && Array.isArray(v)) {
      // XCode 多选约定：逗号分隔字符串
      out[k] = (v as unknown[]).map(String).join(',');
      continue;
    }
    out[k] = normalizeSubmitValue(fieldMap.get(k), v);
  }
  return out;
}
```

### 3.3 prepareSubmitPayload 空值矩阵（submitPayload.ts）

保持「主键过滤（add）」与「字段集来自 addForm/editForm」不变，仅调整空值分支：

| 字段 typeName | 空值（null/undefined/''）行为 |
|---|---|
| NUMERIC_TYPES | 过滤（不提交，后端保持默认） |
| Boolean | 过滤（null/undefined）；`false` 是合法值必须提交 |
| DateTime / TimeSpan / Guid | 过滤 |
| String | **提交 `""`**（对齐 Cube.Vue，避免 DB NOT NULL 报错） |
| 其它 | 过滤 |

```ts
if (isEmptyValue(value)) {
  if (typeName === 'String') { out[key] = ''; continue; }
  continue;
}
```

> 非空 String 字段由前端 `rulesFor`（`!primaryKey && !nullable`）强制必填，正常不会为空；此处兜底避免 DB 层报错。

### 3.4 影响面

- 枚举/Lov/select 字段：值从 `"1"` → `1`，后端绑定成功（本号核心修复）。
- `inputNumber` 已输出 number：归一化幂等。
- 详情回填的 `formModel`（camelCase key）不受影响。
- 列表/看板/卡片展示仍用 `dataSource` 字符串映射查 label，不改变。

## 4. api-core 评论 API（M4b）

### 4.1 类型（types.ts）

```ts
/** 实体评论（对应后端 EntityComment；OSC-0002 已就绪） */
export interface EntityCommentModel {
  id?: number | string;
  /** 实体类别：typePath 去前导 /，如 "Admin/User" */
  category?: string;
  /** 记录主键 */
  linkId?: number | string;
  /** 父评论 ID（0=顶层） */
  parentId?: number | string;
  /** 线程根 ID */
  rootId?: number | string;
  /** 被回复用户 ID */
  replyUserId?: number | string;
  /** 被回复用户名 */
  replyUser?: string;
  /** 评论内容 */
  content?: string;
  /** 创建人 */
  createUser?: string;
  createUserId?: number | string;
  createTime?: string;
  updateTime?: string;
}
```

### 4.2 API（api.ts）

```ts
/** 实体评论 API（M4b，消费 OSC-0002 后端） */
export function createCommentApi(request: RequestFn) {
  return {
    /** 评论列表；parentId 缺省/负数=全部，0=仅顶层，>0=直接回复 */
    getList: (params: {
      category: string;
      linkId: number | string;
      parentId?: number | string;
      pageIndex?: number;
      pageSize?: number;
    }) => request<EntityCommentModel[]>({ url: '/Cube/EntityComment', method: 'get', params }),
    /** 发表评论；body 含 parentId 表示回复 */
    post: (data: { category: string; linkId: number | string; content: string; parentId?: number | string }) =>
      request<EntityCommentModel>({ url: '/Cube/EntityComment', method: 'post', data }),
    /** 删除评论（本人或管理员） */
    remove: (id: number | string) =>
      request<void>({ url: '/Cube/EntityComment', method: 'delete', params: { id } }),
  };
}
```

`index.ts` 导出 `createCommentApi` 与 `EntityCommentModel`；`cube.ts` 的 `CubeApi` 接口增 `comment: ReturnType<typeof createCommentApi>`；`createCubeApi` 返回对象增 `comment: createCommentApi(request)`。

`web/src/api/index.ts`：`createCubeApi({...})` 后 `const cubeApi` 已含 `comment`，无需额外包装（页面直接 `cubeApi.comment.getList/post/remove`）。

## 5. RecordDrawer 历史 Tab 增强（M4a）

### 5.1 数据源

`GET /Admin/Log`，参数：`category = typePath 去前导 /`、`linkId = 记录主键`、`pageIndex`、`pageSize = 20`、`action`（可选筛选）。

### 5.2 UI 结构

```
历史 Tab
├─ 筛选行：Action 下拉（全部 / 新增 / 更新 / 删除），变更即重置到第 1 页并加载
├─ a-timeline：每条
│   ├─ 时间：formatDateTime(row.createTime)（YYYY-MM-DD HH:mm:ss）
│   ├─ 操作人：row.createUser
│   ├─ 徽章：成功（Success=true）→ 绿色；失败 → 红色（row.success）
│   └─ 内容：row.action + row.remark（white-space: pre-wrap 换行）
└─ a-pagination：current/pageSize/total，@change 加载
```

- Action 候选值：`Insert / Update / Delete`（对应后端 `Log.Action` 写入值）。筛选传 `action=`；空表示全部。
- 时间格式化纯函数放新增 `web/src/core/utils/datetime.ts`：

```ts
/** 格式化日期时间：YYYY-MM-DD HH:mm:ss；无效输入返回 '-' */
export function formatDateTime(v: unknown): string {
  if (v == null || v === '') return '-';
  const d = typeof v === 'string' || typeof v === 'number' ? new Date(v) : (v as Date);
  if (Number.isNaN(d.getTime())) return '-';
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`;
}
```

- 状态：`historyPage`、`historyTotal`、`historyAction`、`historyLoading`、`historyRows`。
- 现有 `loadHistory()` 改为带分页/筛选参数；`watch(visible && activeTab==='history')` 触发；切换 Action 时重置页码。

## 6. RecordDrawer 评论 Tab（M4b）

### 6.1 数据

一次拉取 `commentApi.getList({ category, linkId })`（parentId 缺省=全部），前端按 `parentId` 组装树：
- 顶层：`parentId === 0 || parentId == null`
- 回复：`parentId === 某顶层 id`，按其 `createTime` 排序；`rootId` 存在时按 root 分组（本号简化：仅一层回复 + 顶层，更深嵌套统一挂到父级）。

### 6.2 UI 结构

```
评论 Tab
├─ 发表框（顶部）：a-textarea（content）+「发送」按钮；发送成功后清空并刷新
├─ 评论列表
│   ├─ 每条：createUser + createTime + content
│   ├─ 操作行：回复 / 删除（本人 createUserId === 当前用户 id，或管理权限）
│   └─ 回复区：缩进展示子评论（@replyUser 前缀），每条同样可回复/删除
├─ 回复输入：点「回复」→ 该条下方展开 textarea + 取消/发送
└─ 空态：a-empty「暂无评论」
```

- 当前用户：`useUserStore()` 取 `userStore.userInfo?.id`（若 store 无 id，删除按钮以后端 403 为准并提示）。
- 删除权限兜底：前端仅按本人显示删除；后端仍校验（本人或管理员），403 时 `Message.error`。
- 发表/回复载荷：`{ category, linkId, content, parentId? }`；回复时 `parentId = 目标评论 id`。
- 交互反馈：发表/删除成功 `Message.success` 并 `loadComments()`；失败 `formatApiError`。

### 6.3 状态

`comments`、`commentLoading`、`replyTarget`（正在回复的评论 id）、`replyText`、`commentText`。

## 7. 测试设计

| 目标 | 输入 | 断言 |
|---|---|---|
| `normalizeSubmitValue` 数值 | `"123"`→Int32；`"12.5"`→Decimal；`123`→Int32；`"abc"`→String | number/原样 |
| `normalizeSubmitValue` 布尔 | `"true"/"1"→true`；`"false"/"0"→false`；`true` 原样 | boolean |
| `normalizeSubmitValue` 枚举 | `"1"`→Enum → 1 | number |
| `serializeSubmitModel` 多选 | `["a","b"]` → `"a,b"` | 逗号字符串 |
| `prepareSubmitPayload` 空值矩阵 | 数值空→过滤；String 空→`""`；Boolean false→保留 | 按矩阵 |
| `prepareSubmitPayload` 主键 | add 模式 pk → 过滤 | 无主键 |
| `createCommentApi` URL | getList/post/remove | `/Cube/EntityComment` + 方法/参数 |
| `formatDateTime` | 有效/无效/空 | 格式化 / '-' |

## 8. 风险与回滚

| 风险 | 缓解 |
|---|---|
| 类型归一化影响既有正常提交 | 只对「字符串数字→number」生效，number 输入幂等；单测覆盖 |
| String 空值提交 `""` 改变既有行为 | 仅 Nullable String 受益；非空 String 前端必填兜底 |
| comment API 契约与后端偏差 | 严格按 OSC-0002 文档契约；手工冒烟验证 |
| api-core dist 未重建导致类型缺失 | 构建顺序强制：先 `pnpm build` api-core |
| 历史筛选值 `Insert/Update/Delete` 与实际 Action 大小写不符 | 冒烟验证；后端 `Log.Action` 为英文动词 |
