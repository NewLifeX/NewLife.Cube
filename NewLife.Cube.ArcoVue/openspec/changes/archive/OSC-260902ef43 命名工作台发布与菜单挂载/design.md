# OSC-260902ef43 Design — 命名工作台发布与菜单挂载

适用前端：Arco Design Vue（https://arco.design/vue/docs/start）。`▾` 弹出菜单对齐 `web/src/features/search/QueryComboButton.vue`（OSC-260830a1b2）既有样式（`a-dropdown trigger="click"` + `a-doption` + `a-divider` + 当前项打勾）。不涉及 VisActor 新配置、不涉及 FlowGram。`.vue` 薄 script；命名工作台操作逻辑进 `useWorkbench.ts`，纯函数进 `core/utils`，禁止在 `.vue` 写 `cubeApi.*`。

图标：IconPark。新增如需图标先在 https://iconpark.oceanengine.com/official 确认再登记 `iconRegistry.ts` + `iconComponents.ts`。菜单行 Icon 沿用 CubeNC `fa-th-large` → IconPark `application-menu`（映射已存在），不改皮肤映射。

## 1. 冻结与加法

| 符号 | 冻结 |
|------|------|
| `UserProfile.HomeJson` 个人墙语义、`WorkbenchResolver` user>role>system 解析链 | 不改 |
| `WorkbenchRoleStore`（`Workbench.Role` 角色模板）与 `/settings/workbench-role` | 不改 |
| `DashboardJson.TryNormalize(SurfaceWorkbench)` 校验规则 | 复用，不改 |
| `WidgetSurfaceContext` / `WidgetHost` / `WidgetGrid` / `WidgetConfigDrawer` / 各 kind 渲染器 | 复用，不改签名 |
| `/Cube/Workbench`（个人 GET/PUT）、`/Cube/Workbench/Role/{id}` | 不改既有行为 |
| Cube.Vue / NaiveUI / CubeNC Razor | 不改 UI |

加法：Parameter 新 Category `Workbench.Named`（`WorkbenchNamedStore`）；`WorkbenchController` 4 个命名端点；`@cube/api-core` `named*` API；`menuRoutes` `/Workbench/{slug}` 前缀分支；薄壳 `WorkbenchPage.vue`；`useWorkbench(slug)` + `Workbench.vue` 组合按钮。

## 2. 状态唯一来源

| 状态 | 来源 | 禁止 |
| --- | --- | --- |
| 命名槽配置 | Parameter(`Workbench.Named`, Name=slug).LongValue（归一化后整份） | 前端 localStorage；ViewProfile 字段 |
| 命名槽标题 | 同 Parameter 行 .Value（≤40） | 从配置 JSON 的 widgets 反推 |
| 菜单行 | XCode `ManageProvider.Menu`（Url=`/Workbench/{slug}`） | slug 与菜单行分离脱钩 |
| 当前是默认/命名 | 路由有无 `slug` prop（`/home` 无；`/Workbench/{slug}` 有） | 本地 ref 当事实源 |
| 可编辑 | 默认工作台=本人；命名工作台=`IsSystem` | 前端按角色名猜 |
| 命名槽可见性 | 菜单行对当前用户 IsAccessible（与 `/Cube/MenuTree` 同规则） | slug 可直读拉配置 |

## 3. 持久化 schema（Parameter）

行定位：`Parameter.FindByUserIDAndCategoryAndName(0, "Workbench.Named", slug)`（同 `WorkbenchRoleStore`）。

| 字段 | 内容 | 约束 |
| --- | --- | --- |
| UserID | 0 | — |
| Category | `Workbench.Named`（新常量） | — |
| Name | slug | `^[a-z][a-z0-9-]{0,31}$`；唯一 |
| Value | 标题 | ≤40 字符；重命名只改这里（连同菜单 DisplayName） |
| LongValue | 归一化后的 `DashboardConfig` JSON（`version:1 + widgets[]`，不含标题） | `DashboardJson.TryNormalize(workbench)` 输出；16 张 / 64KiB / 禁 legacyChart |

菜单行（`ManageProvider.Menu` 子树）：

> **置顶依据（已核实）**：XCode `EntityTreeSetting<TEntity>.BigSort` 默认 `true`，`EntityTree.FindAllByParent` 对子节点按 `Sort` **降序**排列（`Sort` 大者在前）；菜单树 `Root.Childs` 即该路径。因此让 `系统驾驶舱` 顶级组（`Name=Workbench`）“永远在第一组”＝把其 `Sort` 顶置为**当前根级顶级菜单最大值 + 1**，并在每次发布/更新时重新顶置（防止被后续更大 Sort 的顶级菜单超越）。

| 属性 | 值 |
| --- | --- |
| 父节点 | 根下查找 `Name=="Workbench"`；不存在则 `Root.Add("Workbench","系统驾驶舱",…)` 后使用（发布首个命名工作台时自动建） |
| 父节点 Sort | **顶置**：`Sort = max(根级顶级菜单 Sort) + 1`（封顶 `Int32.MaxValue`）；每次 `MountMenu` 时重算 |
| Name | slug |
| DisplayName | 标题（重命名时同步） |
| Url | `/Workbench/{slug}` |
| Icon | `fa-th-large`；Visible=true |
| 权限 | 不写 `Permissions`（保持“未声明→默认全员可见”，限角色由授权树勾选声明） |

未知字段/非法值归一顺序（沿用 `DashboardJson.TryNormalize` 既有行为）：非法 JSON → 当未配置（GET 返回 `config:null` 不 500）；`version!=1` / `widgets` 非数组 → 当未配置；标题空、slug 非法 → PUT/POST 400；slug 与既有行冲突（PUT 本行则视为更新，不冲突）。菜单行不存在但 Parameter 存在（脏数据）→ `GET Named/{slug}` 404，`PUT` 重建菜单行自愈。

## 4. 后端改动地图（`NewLife.Cube/`）

### 4.1 新建 `Widgets/WorkbenchNamedStore.cs`

仿 `WorkbenchRoleStore`，region：字段/常量 → 列表 → 读写 → 菜单 → 鉴权。

```csharp
public static class WorkbenchNamedStore
{
    public const String Category = "Workbench.Named";
    public const String ParentName = "Workbench";      // 菜单父分组 Name
    public const String ParentTitle = "系统驾驶舱";

    // 列表（系统角色用）：Parameter.FindAllByCategory(Category) → { Name=slug, Value=title }，按 Name 排序
    public static IList<NamedItem> GetList();

    // 读：FindByUserIDAndCategoryAndName(0, Category, slug)?.LongValue（未配置返回 null）
    public static String Get(String slug);

    // upsert：GetOrAdd → Kind=String、Enable=true、Value=title、LongValue=json、Save
    public static void Save(String slug, String title, String json);

    // 删除 Parameter 行
    public static void Delete(String slug);

    // 菜单挂载：找/建父分组 → 父分组 Sort 顶置（EnsureFirstGroup）→ Parent.Add(slug, title, null, "/Workbench/"+slug)（存在则改 DisplayName 后 Update）
    public static IMenu MountMenu(String slug, String title);

    // 置顶：把 Workbench 父分组 Sort 设为 根级顶级菜单最大 Sort+1（BigSort=true Sort 大在前），保证整组永远第一组
    public static IMenu EnsureFirstGroup();

    // 菜单卸载：按 Url 匹配子菜单并删除（不级联改角色 Resources；角色资源残留 id 无碍）
    public static void UnmountMenu(String slug);

    // 鉴权：菜单 Url 匹配行 IsAccessible（复制 CubeController 菜单过滤规则，抽取到本类内私有函数 + 单测钉死）
    public static Boolean IsAccessible(IUser user, String slug);
}
```

> `IMenu` 工厂/创建参考 `MenuHelper.ScanActionMenu` 既有用法（`menu.Parent.Add(name, displayName, fullName, url)`、`IEntity.Update()`）与 `EntityAuthorizeAttribute.CreateMenu`。执行期若 `IMenu.Add` 签名有出入，以 XCode Membership `IMenu` XML 注释为准（本号不新增 Menu 实体列）。

### 4.2 修改 `Controllers/WorkbenchController.cs`

沿用 `[Route("Cube/Workbench")]` 与既有 `ValidateToken` / `IsSystem` 鉴权模式，新增 4 个 action（均 `[HttpPut]/[HttpPost]` 双兼容可选；GET 用 `[HttpGet]`）：

| Action | 路由 | 鉴权 | 行为 |
| --- | --- | --- | --- |
| `NamedList` | `GET Named` | 仅系统角色（非系统 403） | `GetList()` → `[{slug, title}]` |
| `NamedGet` | `GET Named/{slug:regex(...)}` | 登录用户 + `IsAccessible` | 菜单/槽不存在 404；返回 `{slug, title, config=Decode(json)}` |
| `NamedPut` | `PUT Named/{slug}` | 仅系统角色 | body `{title, homeJson, create?}`；**create=true 且 slug 已存在 → 409**（另存为防覆盖既有看板，G1）；空 title 400；空 homeJson=清空→等效删除槽与菜单（400 提示改用 DELETE）；否则 `TryNormalize(workbench)` 失败 400；`Save` + `MountMenu`；返回 `{slug, title}` |
| `NamedDelete` | `DELETE Named/{slug}` | 仅系统角色 | `UnmountMenu` + `Delete`；返回 `{slug, deleted:true}` |

`Decode(...)` 复用控制器既有私有方法。slug 非法字符 → 400（不 404，避免路由正则不一致）。

### 4.3 冻结不动

`WorkbenchResolver`、`WorkbenchRoleStore`、`WorkbenchSeeds`、`WidgetController`、`DashboardJson`。

## 5. 前端改动地图（`NewLife.Cube.ArcoVue/web/src/`）

### 5.1 `@cube/api-core`（`packages/api-core/src/`）

- `api.ts` `createWorkbenchApi` 追加：

```ts
namedList: () => request<NamedWorkbenchItem[]>({ url: '/Cube/Workbench/Named', method: 'get' }),
namedGet: (slug) => request<NamedWorkbenchResult>({ url: `/Cube/Workbench/Named/${slug}`, method: 'get' }),
namedPut: (slug, body: { title: string; homeJson: string; create?: boolean }) =>
  requestWithPostFallback(request, { url: `/Cube/Workbench/Named/${slug}`, method: 'put', data: body }),
namedDelete: (slug) => requestWithPostFallback(request, { url: `/Cube/Workbench/Named/${slug}`, method: 'delete' }),
```

- `widget.ts` 或 `types.ts` 加类型：`NamedWorkbenchItem { slug: string; title: string }`、`NamedWorkbenchResult { slug: string; title: string; config: DashboardConfig | Record<string, unknown> | null }`；`index.ts` 导出。
- `api.spec.ts` 补 4 条 URL/method 断言（照现有 `createWorkbenchApi` describe）。

### 5.2 `core/utils/menuRoutes.ts`（buildLeafRoutes 前缀分流）

- `buildLeafRoutes` 内，在 `path = normalizeMenuUrl(item.url, 'pascal')` 后新增分支：

```ts
const seg = path.replace(/^\/+/, '').split('/').filter(Boolean); // ['Workbench', slug]
if (seg.length === 2 && seg[0].toLowerCase() === 'workbench' && /^[a-z][a-z0-9-]+$/i.test(seg[1])) {
  // 命名工作台菜单：slug 页，组件 = WorkbenchPage（不吃 DynamicPage/GetPage 探测）
  routes.push({ path: seg.join('/'), name: 'menu-' + (item.id ?? item.name),
    component: withRouteComponentName(() => import('@/views/home/WorkbenchPage.vue'), routeName),
    props: { slug: seg[1].toLowerCase(), authId: item.id },
    meta: { title: item.displayName || item.name, icon: item.icon, menuId: item.id, typePath: '' } });
  continue;
}
```

- 单段 `/Workbench`（无 slug）、`/Workbench/{slug}` 非法字符 → 维持原 DynamicPage 流程（不特殊处理）。
- `registerLeafRoutes` / `ensureDynamicLeafRoute` 不加分支（菜单驱动已覆盖）。

### 5.3 新建 `views/home/WorkbenchPage.vue`（薄壳）

```vue
<template><Workbench :slug="slug" /></template>
<script setup lang="ts">
import Workbench from './Workbench.vue';
defineOptions({ name: 'WorkbenchPage' });
const props = defineProps<{ slug?: string }>();
const slug = props.slug ?? '';
</script>
```

> 无模板业务：不建额外 use 文件（README 允许“已足够薄的展示组件不造空 composable”）。`index.vue`（/home）不改，`Workbench.vue` 声明可选 `slug` prop（缺省空 = 默认工作台）。

### 5.4 `views/home/Workbench.vue` 标题栏组合按钮

`useWorkbench.ts` 暴露（见 5.5）后模板改为：

```vue
<a-space :size="4">
  <!-- 左：进入/退出编辑态（现有按钮，逻辑不变） -->
  <a-tooltip :content="editing ? '完成' : '自定义工作台'">
    <a-button type="text" class="wb-icon-btn" @click="toggleEdit">
      <icon-park :type="editing ? 'check' : 'setting-config'" :size="16" />
    </a-button>
  </a-tooltip>
  <!-- 右：▾ 弹出菜单（仅 系统角色 或 hasNamedItems 时渲染；样式对齐 QueryComboButton） -->
  <a-dropdown v-if="showNamedMenu" trigger="click" @select="onNamedSelect">
    <a-button type="text" class="wb-icon-btn wb-icon-btn--caret"><icon-park type="down" :size="12" /></a-button>
    <template #content>
      <a-doption value="__publish"  :disabled="!canPublish">发布…</a-doption>
      <a-doption value="__rename"   :disabled="!canRename">重命名…</a-doption>
      <a-doption value="__delete"   :disabled="!canDelete" class="danger">删除…</a-doption>
      <a-divider class="wb-divider" />
      <div class="wb-group-title">切换工作台</div>
      <a-doption value="__default" :class="{ 'wb-checked': !isNamed }">
        <icon-park v-if="!isNamed" type="check" class="wb-check" /> 默认工作台
      </a-doption>
      <a-doption v-for="n in namedList" :key="n.slug" :value="`__named:${n.slug}`"
        :class="{ 'wb-checked': isNamed && currentSlug === n.slug }">
        <icon-park v-if="isNamed && currentSlug === n.slug" type="check" class="wb-check" /> {{ n.title }}
      </a-doption>
    </template>
  </a-dropdown>
  <!-- 全屏 / 恢复默认（恢复默认仅默认工作台可见）…保持现状 -->
</a-space>
<!-- 发布/重命名 对话框（a-modal + a-input）：字段见 5.6 -->
<a-modal v-model:visible="publishVisible" … />
```

按钮在横幅右侧顺序：**编辑（左）→ ▾（右）→ 全屏 → 恢复默认**；恢复默认按钮仅在 `!isNamed && canRestore` 时渲染。

### 5.5 `views/home/useWorkbench.ts` 扩展

签名 `useWorkbench(slug?: string)`（`Workbench.vue` 从 `defineProps` 传入）。

| 成员 | 语义 |
| --- | --- |
| `isNamed` | `!!slug` |
| `currentSlug` | `slug`（空 = 默认工作台） |
| `isSystem` | `userStore.userInfo?.isSystem === true`（同 AppearanceDrawer 判定） |
| `namedList` | `ref<NamedWorkbenchItem[]>`；`isSystem` 时 `load()` 拉 `cubeApi.workbench.namedList()`，否则 `[]` |
| `source` | slug 页恒 `'named'`；默认工作台走现有 `user/role/system` |
| `showNamedMenu` | `isSystem || namedList.length > 0` |
| `canEdit` | 默认工作台：编辑态开启即可（现状）；命名工作台：`editing && isSystem` |
| `canPublish` | `isSystem`（默认/命名均可发布/更新） |
| `canRename/canDelete` | `isSystem && isNamed`；**默认工作台（无 slug）恒 false**——禁止对默认工作台重命名/删除（proposal 决策 11），`▾` 两项置灰 |
| `canRestore` | 仅默认工作台且 `source==='user'`（现状不变） |

加载分支：

```
slug 空  → GET /Cube/Workbench（现状 load()）
slug 有  → GET /Cube/Workbench/Named/{slug}
           ├ 200 → dashboard=parseWorkbenchConfig(config)（null→emptyDashboard 只读空壳）、source='named'、namedTitle=title
           └ 404/403 → loadError 提示并保持空墙（不自动跳转，避免循环）
```

保存（`saveDashboard`）：默认工作台 → `cubeApi.workbench.put(json)`（现状）；命名工作台（仅系统角色会进入编辑）→ `cubeApi.workbench.namedPut(slug, { title: namedTitle, homeJson: json })`，失败回滚（仿现状 try/catch 回滚）。

动作：

| 动作 | 触发 | 行为 |
| --- | --- | --- |
| `publish`（无 slug，另存为） | `__publish` | 开 `a-modal`：字段 标题(必填≤40) + slug(必填，预填 `wb-<4hex>`，校验 `^[a-z][a-z0-9-]{0,31}$`、slug 已占用预检红字)；确认 → `namedPut(slug,{title, homeJson: serialize(dashboard,'workbench'), create:true})` → 清空命名输入缓存 → `router.push('/Workbench/'+slug)`（成功后当前页已是该命名页）。slug 占用/冲突由前端预检 + 后端 409 兜底。 |
| `publish`（有 slug，更新发布） | `__publish` | 直接 `namedPut(currentSlug,{title:namedTitle, homeJson:serialize(dashboard)})`；Message 成功。无对话框。 |
| `rename` | `__rename` | 开 `a-modal`：标题(必填≤40) 预填当前标题；确认 → `namedPut(currentSlug,{title:newTitle, homeJson:serialize(dashboard)})` → 本地更新标题。slug 不变。 |
| `delete` | `__delete` | `a-popconfirm`/`a-modal` 确认 → `namedDelete(currentSlug)` → `router.push('/home')`（回默认工作台）。 |
| 切 `__default` | 菜单 | `router.push('/home')` |
| 切 `__named:{slug}` | 菜单 | `router.push('/Workbench/'+slug)` |

对话框字段状态（publishVisible / publishTitle / publishSlug / renameVisible / renameTitle / slugError）留在 `useWorkbench.ts`（有响应式状态，允许——薄 script 约束针对 `.vue` 文件，composable 内正常）。

### 5.6 归一化与回退矩阵（前端）

| 输入 | 输出 |
| --- | --- |
| `GET Named/{slug}` 200 且 config 为合法 JSON | 渲染墙；`source='named'` |
| 200 且 config=null（未配置但菜单在） | 只读空壳 + 空态文案「该工作台尚未配置」；无「+添加部件」（`canEdit=false`） |
| 404（槽/菜单都不在） | `loadError` 黄条（现状样式），空墙 |
| 403（无权） | 同上；不泄露配置 |
| 普通用户直接输 `/Workbench/{slug}` | 后端 403/404 → 黄条；菜单不可见则无入口，路由守卫不额外拦截（后端 fail-closed 已兜底） |
| 非系统角色在默认工作台 | `▾` 不渲染（无发布权且 namedList 空） |
| 系统角色在默认工作台 | `▾` 渲染；`发布…` 可用；`重命名…/删除…` **恒禁用**（默认工作台不可重命名/删除） |
| slug 页保存非系统角色 | 前端 `canEdit=false` 无保存路径；后端 403 兜底 |
| 重命名空标题 / 发布空标题 / 非法 slug | 对话框内禁用确认并显示错误文案，不发请求 |
| 另存为（create=true）撞已存在 slug | 前端对照 namedList 预检红字 + 后端 409（不覆盖）；更新自身（create 缺省 false / 脏数据 PUT）按 upsert 更新本行 |

## 6. 核心文档影响

| 文档 | 变更 |
| --- | --- |
| `ArcoVue企业中后台迁移方案.md` §8.5 | 增补 8.5.2 之后小节「命名工作台与菜单挂载（OSC-260902ef43）」：Parameter `Workbench.Named` + 菜单行模型 + `▾` 组合按钮 + slug 只读语义 + 发布/重命名/删除/切换矩阵 |
| `openspec/changes/archive/OSC-26082815a1` 等 | 不改（历史冻结） |
| `@cube/api-core` README（如有） | 仅当存在 API 列表处追加 named*（执行期核对） |
| 功能清单（NewLife.Cube 根 `Doc/`，如适用） | 若工作台条目未覆盖「多命名工作台」则补一行并回写 OSC 号 |

## 7. 测试设计

后端 xUnit（`NewLife.Cube.Tests/Osc260902ef43NamedWorkbenchTests.cs`，仿 `Osc26082815a1WorkbenchTests` 内存库 + 假用户/角色）：

1. `Store`：Save/Get/Delete/GetList 往返；Value=标题 LongValue=配置；非法 slug 拒绝。
2. `MountMenu`：首次发布自动建 `Workbench` 父分组；重复发布同名 slug 不产生重复菜单（更新 DisplayName）；`UnmountMenu` 删除菜单行。
3. `IsAccessible`：未声明权限菜单全员可见；声明后仅授权角色可见（沿用 CubeController 语义钉死）。
4. Controller：非系统角色 PUT/DELETE/列表 403；普通用户 GET 授权 403/404；TryNormalize 超限 400（16 张 / 非法 kind）。
5. 回归：个人 `GET /Cube/Workbench`、`Workbench.Role` 角色模板不受影响。

前端 Vitest：
- `menuRoutes.spec`：`/Workbench/ops` 菜单 → WorkbenchPage loader + slug prop；`/Workbench` 单段与 `/Admin/User` 不误分流。
- `useWorkbench.spec`（slug 分支）：加载 namedGet 成功/404/403、canEdit/canPublish/canRename/canDelete 真值、publish 默认→namedPut+跳转、delete→namedDelete+跳 /home、重命名仅改标题 slug 不变。api-core `api.spec` 4 条 URL/method。

## 8. 手动冒烟

1. 系统角色登录 → `/home` 编辑加 1-2 部件 → `▾ 发布…` 标题+slug → 跳到 `/Workbench/{slug}`；左侧菜单出现「系统驾驶舱 > 标题」。
2. 普通用户登录 → 菜单点击该工作台 → 只读墙；无编辑/`▾`；直接 URL 也一致。
3. 系统角色在 `/Workbench/{slug}` → 编辑改部件 → `▾ 发布`（更新）→ 普通用户刷新看到新内容。
4. 重命名 → 菜单 DisplayName 变、Url 不变；删除 → 菜单项消失、回到默认工作台。
5. 默认工作台回归：恢复默认、角色模板、空墙清空行为与 OSC-26082815a1 一致。
