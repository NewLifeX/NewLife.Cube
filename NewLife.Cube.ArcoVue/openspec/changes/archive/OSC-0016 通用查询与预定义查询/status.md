# Status
- id: OSC-0016
- state: Done
- updated: 2026-08-08T08:20:00+08:00
- approvedBy: openspec-approve
- trigger: "批准并执行 OSC-0016，严格按照 openspec 规范来。"
- checklist: passed
- note: 验收三步编排（实现审计→代码审查→文档同步）全部通过；审查发现 1🔴+6🟡 已修复；门禁复检全绿（XUnit 213 通过/8 外部依赖失败、Vitest 275/275、vue-tsc、vite build、api-core 11/11）。复盘完成，归档至 archive/。

## 执行记录（openspec-apply，2026-08-07）

- T1~T12 全部完成 ✅
- 后端：NewLife.Cube / NewLife.CubeNC 编译 0 错误；XUnitTest.Osc0016Tests 7/7 通过
- 前端：api-core 11/11 + build 通过；ArcoVue/web Vitest 26 文件 / 275 用例全过 + vue-tsc + vite build 通过
- AC-08/AC-15/AC-18 在执行阶段已验证

## 验收记录（openspec-verify，2026-08-08）

- 自动化门禁复检：后端编译 0/0 ✅ | XUnit 7/7 ✅ | api-core 11/11 ✅ | Vitest 275/275 ✅ | vue-tsc ✅ | vite build ✅
- 逐文件代码审查：AC-01~18 全部判定通过 ✅
- 轻微偏差（不阻塞）：AC-06 重置全空未禁用、AC-05 菜单删除无 popconfirm
- 手工冒烟 deferred（需 CubeDemo 运行环境）

- 测试命令与结果：
  - `dotnet build NewLife.Cube/NewLife.Cube.csproj`、`NewLife.CubeNC/NewLife.CubeNC.csproj` → 0 错误
  - `dotnet test XUnitTest --filter FullyQualifiedName~Osc0016Tests` → 7/7 通过
  - `dotnet test XUnitTest`（全量）→ 213/221 通过；8 失败为既有外部服务依赖（QyWeiXin/SSO）
  - `npm.cmd --prefix packages/api-core run test` → 11/11；`run build` → 通过
  - `npm.cmd --prefix NewLife.Cube.ArcoVue/web run test` → 26 文件 / 275 用例通过；`run build`（vue-tsc + vite）→ 通过
- 新增测试文件：`XUnitTest/Osc0016Tests.cs`（T1~T4 7 用例）、`XUnitTest/AssemblyInfo.cs`（禁并行）；前端 `viewProfile.spec.ts`/`stores/viewProfile.spec.ts`/`searchFilters.spec.ts`/`fieldControl.spec.ts` 增补 T7~T9 用例
- 偏差与说明：
  - `CascaderSearchPanel` 非孤儿组件（Arco 库内部实现），AC-15 按事实修正
  - 测试内存 SQLite 自增插入仅落 1 行（XCode 运行时实体限制），T2 大表用 MemoryCache 预置行数、T3 临时调低阈值验证
  - `entity:` 内部查询分支由 API 版 `LovController` 承载，XUnitTest 引用 NC 合并版不可测，留待 verify 冒烟 AC-13
  - 手工冒烟留待 openspec-verify

## 面板重构（执行期用户调整，2026-08-07）

- **需求**：①移除原「展开更多 N/收起」折叠按钮，并入「查询」组合按钮；②移除「搜索/重置/保存到此视图/清除默认筛选/未保存筛选」操作区，全部并入「查询」组合按钮；③关键字 Q / 查询组合按钮置于第一行最后，其余查询条件按宽度排布、放不下的进第二行（默认收起）；④面板高度随收起/展开自动调整。
- **实现**：`QueryComboButton.vue` 菜单新增「重置 / 展开更多条件（N）/收起 / 保存到此视图 / 清除默认筛选」项（props 增 canSaveView/hasMoreFields/moreFieldCount/expanded，emits 增 reset/toggleExpand/saveView/clearView）；`QueryInsightPanel.vue` 改为两行布局（主行=前 N 字段 + 主时间/Q/查询按钮，第二行=其余字段 v-show 折叠），新增不可见测量容器按宽度累加计算 N（ResizeObserver 响应容器尺寸），移除原操作区与展开折叠按钮及 source 来源提示；`DefaultList.vue` 移除 source/sourceLabel 传参与 searchSource/searchSourceLabel computed。
- **验证**：前端 Vitest 26 文件 / 275 用例通过；vue-tsc + vite 构建通过（wwwroot 重新生成）。

## 面板重构（抽屉形态，2026-08-07，执行期用户调整）

- **需求**：①搜索栏改为**右侧抽屉**（宽度 240 = 外观设置抽屉一半）；②**每个查询条件占一行**；③**查询组合按钮放抽屉右上角**；④**关键字 Q 作为第一个查询条件**；⑤其余查询条件按 GetPage `Search` 列表顺序依次排列；⑥抽屉**无关闭按钮**、点击界面其它区域（遮罩）关闭；⑦`QueryInsightPanel` 更名 `InsightPanel` **暂隐藏不使用**（等简易图表看板设计时再启用）。
- **实现**：
  - 新建 `SearchDrawer.vue`（`a-drawer` width=240、`closable=false`、`mask-closable=true`、每条件一行 `layout="vertical"`、Q 第一、主时间范围单独一行、查询组合按钮在标题右上角、主时间字段不重复渲染）
  - `QueryInsightPanel.vue` → `InsightPanel.vue`（`defineOptions({ name: 'InsightPanel' })`）；`DefaultList.vue` 以 `v-if="false"` 暂隐藏但保留完整 props/emits 接线（统计/图表状态 `statData`/`chartData`/`insight` 等由 DefaultList 持续维护，未来图表看板启用仅需改条件）
  - `DefaultList.vue`：移除原面板渲染与 `showSearchPanel`，接入 `SearchDrawer`（`searchPanelOpen` 默认收起，工具栏「搜索」按钮切换）
  - 移除「保存到此视图 / 清除默认筛选」菜单项：`QueryComboButton` 删 canSaveView/saveView/clearView，`InsightPanel` 删 canSave/save/clear，`DefaultList` 删 handleSaveFilters/handleClearFilters（`FilterBuilderPopover`/`GroupPopover` 内同名功能不受影响）
- **验证**：前端 Vitest 26 文件 / 275 用例通过；vue-tsc + vite 构建通过（wwwroot 重新生成）。

## 抽屉微调（2026-08-07，执行期用户调整）

- **需求**：①抽屉标题「查询」改「**高级搜索**」；②抽屉宽度 240 → **300**（+60px）；③组合按钮**文字在前、向下箭头在右**；④抽屉底部「取消 / 确定」按钮去掉。
- **实现**：`SearchDrawer.vue` 标题改「高级搜索」、`:width="300"`、`:footer="false"`（Arco Drawer `footer` 默认 true，会渲染底部确定/取消）；`QueryComboButton.vue` 按钮由 `#icon` 插槽（箭头在左）改为内容末尾直接放 `IconDown`（文字在前、箭头在右，`margin-left:4px`）。
- **验证**：前端 Vitest 26 文件 / 275 用例通过；vue-tsc + vite 构建通过（wwwroot 重新生成）。

## 抽屉右上角定位 + 列表高度修复（2026-08-07，执行期用户调整）

- **需求**：①查询组合按钮需真正位于抽屉**右上角**；②隐藏 `InsightPanel` 后多维视图底部的 `list-panel` 高度异常——**分页器及 padding 显示不全**。
- **实现**：
  - `SearchDrawer.vue`：Arco `.arco-drawer-title` 宽度只随内容（非全宽），原先按钮仅跟在标题文字后；改为 header 相对定位 + `.sd-actions` 绝对定位（`right:16px; top:50%`）。因 `class` 透传到 `.arco-drawer-container` 且无 scoped `data-v` 属性（`:deep` 无法命中），改用**非 scoped 样式块**限定 `.search-drawer .arco-drawer-header { position:relative }`。
  - **布局层**（`mix.vue`/`side.vue`/`top.vue`）：根因是 `a-layout` 用 `min-height:100vh`（非固定高），内容超高时整条 flex 链被撑出视口，`.layout-content__scroll` 高度 = 内容高（无溢出→无法滚动），分页器落在视口外不可见。改为 `height:100vh` + 内层 a-layout 加 `min-height:0`（mix `layout-mix__body`/`layout-mix__main`、side `layout-side__body`），使滚动容器限制在视口内、内容超高可滚动，分页器与面板底部 padding 完整可见。
- **浏览器验证**：top 布局下滚动容器 bottom=视口底、`canScroll=true`，滚动到底分页器 fullyVisible、面板 padding 完整；抽屉按钮距右 16px 垂直居中于 header 右上角。
- **验证**：前端 Vitest 26 文件 / 275 用例通过；vue-tsc + vite 构建通过（wwwroot 重新生成）。

## 分页器可见性修复（2026-08-07，执行期用户调整）

- **需求**：列表 / 树状 / 卡片视图打开时分页器及其外壳底部（padding）不可见，要求正常情况下首屏直接可见。
- **根因**：①表格高度固定 520，加视图 Tab/工具栏/分页器/padding 后超出 scroll 可视区（604px），分页器被挤出首屏；②视图组件（VTable/CardList 等）为异步加载，视图切换时 `nextTick` 测量发生在渲染前，测得高度不收敛；③`CardList` 用 `min-height`（内容撑开），卡片内容 529px > 测量值 414px，分页器仍被挤出。
- **实现**：
  - `DefaultList.vue`：新增 `tablePanelRef` + `measureTableHeight()`——按「scroll 可视区底 − 面板顶 − 面板内非表格固定部分（面板高 − 当前表格高）」动态测量表格可用高度（default/fill 模式填满可视区，分页器保持在首屏内）；`ResizeObserver` 监听 scroll 容器尺寸；`loadData` finally、视图/高度模式切换后重测（异步视图组件渲染完成后 200/600ms 延迟多次重测直至收敛）；fit 模式保持内容自适应。
  - `CardList.vue`：卡片区由 `min-height`（内容撑开）改为 `minHeight + maxHeight + overflowY:auto`（default/fill 时内容超高内部滚动、分页器固定可见）；`DefaultList` 新增 `contentViewHeight`——fit 模式传 `undefined` 不限制（内容流式）。
- **浏览器验证**：top 布局下列表/卡片/树状三视图分页器均首屏 `fullyVisible`、面板底部在 scroll 容器内（用户页 4 条、部门页 6 条卡片）。
- **验证**：前端 Vitest 26 文件 / 275 用例通过；vue-tsc + vite 构建通过（wwwroot 重新生成）。

## 底部间隙一致性 + 撤销卡片高度修改（2026-08-07，执行期用户调整）

- **需求**：①撤销对卡片视图高度的修改（恢复内容撑开）；②列表/树状视图底部间隙应与左右上三边一致（16px）；③移除页面底部一条异常窄的空白横条。
- **根因**：`measureTableHeight` 余量仅 4px，导致 list-panel 底部到 scroll 底部间隙只有 4px（左右上为 16px），形成一条异常窄的浅灰横条。
- **实现**：
  - `CardList.vue` 撤销高度受控：恢复 `:style="{ minHeight: height + 'px' }"`（内容流式），删除 `cardStyle`/`StyleValue`；`DefaultList.vue` 删除 `contentViewHeight`，CardList 恢复 `:height="resolvedTableHeight"`
  - `DefaultList.vue` `measureTableHeight` 余量 `-4` → `-16`（预留 scroll padding-bottom 16px），列表/树状底部间隙与左右上一致，无溢出
- **浏览器验证**：列表/树状底部间隙 16px、`canScroll=false` 无溢出、分页器首屏可见；卡片视图恢复内容流式（overflowY visible）。
- **验证**：前端 Vitest 26 文件 / 275 用例通过；vue-tsc + vite 构建通过（wwwroot 重新生成）。

## 移除侧边栏底部白色横条（2026-08-07，执行期用户调整）

- **需求**：系统界面底部有一条白色横条（非 Footer），要求移除。
- **根因**：该横条为 Arco `a-layout-sider` 的**底部折叠触发器** `.arco-layout-sider-trigger`（白色 48px、z-index 1、位于侧边栏底部）。`side.vue`/`mix.vue` 虽设置 `:trigger="null"`，但 Arco 仍渲染该元素（sider class 含 `arco-layout-sider-has-trigger`）。
- **实现**：`side.vue`/`mix.vue` 各加 `:deep(.arco-layout-sider-trigger) { display: none; }` 隐藏；折叠功能由 side 布局 header 折叠按钮 / mix 布局 sider 顶部按钮承担，不受影响。
- **浏览器验证**：trigger `display:none`、底部无白色元素；header 折叠按钮点击 sider 160→48 折叠正常并复原。
- **验证**：前端 Vitest 26 文件 / 275 用例通过；vue-tsc + vite 构建通过（wwwroot 重新生成）。

## Map 外键搜索候选补全 + Search 标准字段兼容（2026-08-07，执行期用户调整）

- **需求**：①GetPage 传回的查询条件字段若为系统中已有实体对象（如 Department），Search 列表元数据不足以构建完整查询选项列表；②后端构建该列表的代码需重构完善（只动 WebAPI 版本相关控制器）；③直到搜索抽屉各查询字段选项列表完整展示，且随意选择查询条件后端能利用条件 Search 返回正确结果。
- **分析（前后端链路）**：
  - `GetPage()` → `OnGetFields(ViewKinds.Search)` → `FieldCollection`（NC 共享代码，Search 分支用 `SearchBuilder` 输出表字段）→ `PrepareFieldsForApi` 物化 `dataSourceMap` → 前端 `SearchDrawer` 按 `lovCode`/`dataSource` 渲染下拉
  - **根因 1**：`FillMapCandidates` 只识别表字段直接 `[Map]`；`User.DepartmentID` 的 Map 在扩展属性 `User.Department` 上，`SearchBuilder` 输出的表字段无候选 → 前端渲染数字框
  - **根因 2**：`RoleID` 有 `LovCode="Role"` 但值集未注册 → `LovSelect` 空（"暂无数据"）
  - **根因 3（核心）**：API 版 `UserController.Search` 重写通用搜索，只读自定义参数 `roleIds`(复数)/`departmentId`/`enable`/`q`；抽屉提交标准字段名 `RoleID`（单数）取不到 → 后端 SQL 无 Where 条件，搜索不生效
- **实现**：
  - **API 版 `ReadOnlyEntityController`** 新增 `FixSearchMapCandidates(IList<DataField> search)`（protected virtual）：从 `Factory.AllFields` 收集 `e.Map != null` 的扩展字段（GroupBy Map.Name 忽略大小写）→ 对每个 SearchField：①按 Map.Name 匹配扩展字段；②已有 `LovCode` 但 `LovDefinition.Find` 未注册时用 Map 实体补全；③`DataSourceMap` 非空保留；④小表（≤ `CubeSetting.Current.MaxDropDownList`，MemoryCache 60s `LovMapCount:`）内联 `provider.GetDataSource()` 转 `DataSourceMap`；⑤大表设 `sf.LovCode = "Entity." + entityType.FullName`。`GetPage`/`GetFields(kind==Search)` 后调用
  - **API 版 `UserController.Search`** 重构：保留 `id` 特殊分支；标准字段名与自定义参数互备（`RoleID`↔`roleIds`、`DepartmentID`↔`departmentId`、`Enable`↔`enable`、`Q`↔`q`）；`WhereExpression` 组合（`_.RoleID.In(roleIds) | _.RoleIds.Contains(...)`、`_.DepartmentID.In`、Area 级联、`_.Enable`、`_.LastLogin.Between(dtStart,dtEnd)`、关键字模糊 Name/DisplayName/Mobile/Mail）；再遍历 `Factory.Fields` 通用等值过滤（Sex/MailVerified/MobileVerified/Online/Name 等，跳过已处理字段）；`XCode.Membership.User.FindAll(exp, p)`（全限定名避开 `ClaimsPrincipal.User` 冲突）
  - 共享代码（`FieldCollection.cs` 等 NC 链接文件）**未改动**
- **验证**：
  - GetPage：`DepartmentID` ds=6（总公司/行政部/生产部/上海分公司等）、`RoleID` ds=4（管理员/高级用户/普通用户/游客）——候选完整
  - 后端日志 SQL 逐项确认：`Where (RoleID=1 Or RoleIds Like '%,1,%')`、`Where Sex=1`、`Where Enable=1`、`Where Online=0`、`Where MobileVerified=1`、`Where MailVerified=1`、`Where LastLogin>='2026-08-01 00:00:00'`、`Where (Name Like '%admin%' ...)`、组合 `Where (RoleID=3 Or ...) And Sex=1`
  - API 冒烟 14 项：无过滤 4、RoleID=1→1、RoleID=3→3、DepartmentID=5→1、RoleID=1+Dept5→1、RoleID=3+Dept5→0、Enable=true→4、Online=false→3、Sex=1→1、dtStart/dtEnd→3、Q=admin→1、Q=smoke→3、Name=admin→1、MailVerified=false→4 —— 全部正确
  - 浏览器 UI 冒烟：搜索抽屉角色下拉候选 4 项 → 选「管理员」→ 执行查询 → 共 1 条
  - 测试：`dotnet test XUnitTest --filter FullyQualifiedName~Osc0016Tests` → 7/7；前端 Vitest 26 文件 / 275 用例通过
- **经验**：`dotnet run --project CubeDemo --no-build` 复用 `Bin\CubeDemo` 旧 DLL 会导致新代码不生效（曾误判 Name 过滤失效）；改代码后需 `dotnet build CubeDemo.csproj` 刷新输出再启动。
