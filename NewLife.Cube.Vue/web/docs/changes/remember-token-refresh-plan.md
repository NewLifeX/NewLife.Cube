# 记住我与可选令牌自动刷新方案

日期：2026-11-19。状态：代码与定向验证已复核，真实后端验收待核。

## 问题、目标与边界

当前请求层过期即清令牌；刷新接口已有但未接自动流程。Vue 密码表单当前没有记住我控件，loginApi 已支持 remember 参数。
目标是基础库提供可选能力，Vue 登录框保存选择并据配置刷新。默认关闭，不迁移旧登录态、不保存密码、不改后端有效期和旧接口。不重构现有请求层；若需结构重构先报用户审核。

## 行为与验收标准

1. 未配置 tokenRefresh：HTTP 401、业务 401/4001 保持原清令牌与未授权回调；403 不刷新。
2. Vue auth.tokenRefresh.enabled 默认 false。启用后登录框展示“记住我”，选择持久化，但密码不持久化；登录提交透传 remember。未启用时表单提交形状与旧版一致。
3. 刷新要求配置开启且当前会话持有刷新令牌和用户名。密码登录通过勾选“记住我”获得刷新资格，认证片段通过完整凭据获得刷新资格。其余会话沿用原过期处理。
4. 过期时调用 POST /Auth/Refresh，成功保存 accessToken，以及返回的新 refreshToken，再重试原请求一次。不设置空闲定时器，不无限续期。
5. 实体与服务客户端共享一次刷新；晚到的旧 token 401 使用已更新 token 重试，避免重复轮换。每个原请求最多重试一次，刷新请求不进入自动刷新链。
6. 刷新失败、无 accessToken 或重试仍过期，回落现有未授权处理；退出/切换用户期间的旧刷新结果不得恢复登录态。
7. 页面重载恢复选择；取消勾选立即停用新刷新策略，退出登录清理本次刷新资格。旧刷新令牌不作为默认开启依据。

## 增量设计

- api-core 新增可选 tokenRefresh 策略（isEnabled、refresh）及协调器；client 在既有未授权分支前增加尝试刷新与一次重放，未配置直接原分支。createCubeApi 两客户端使用同一策略对象。
- Vue 新增独立 tokenRefresh 工具：保存选择、资格、登录代际；独立 axios 调刷新接口，继承服务地址、凭证、附加头与超时；验证 HTTP 成功、JSON、code=0 与 accessToken。兼容 camel/snake/Pascal 令牌字段。
- AuthConfig 增加 tokenRefresh.enabled（可选），默认配置不启用。LoginForm 仅启用时展示复选框；LoginPage 成功登录后保存本次资格，失败不写登录资格；loginApi 既有 remember 参数透传。
- 不改 TokenStorage 原契约，不接管旧 tokenManager.setToken 空实现，不改变旧调用方存储语义。
- 清 access token 时撤销资格并推进代际，迟到刷新结果检查代际及原 token；同页退出/换用户不能复活。
- 实际后端 `AuthController.Refresh` 强制用户名；登录成功保存用户名，刷新同时发送 userName，退出清资格及用户名，不保存密码。
- 登录、登出、Challenge、Refresh 等认证端点不触发自动刷新，避免失败登录或登出被重放。

## 使用配置

在应用 `configs/config.ts` 的配置对象加入：

```ts
auth: {
  tokenRefresh: { enabled: true },
},
```

也可由 `window._CUBE_CONFIG_` 的同名配置覆盖。默认省略即关闭。开启后，现有会话需通过一次符合条件的登录获得刷新资格。有效期由后端决定。

认证片段支持 `#token=...`、`#/path#token=...` 和 `#/path?token=...`。追加 `&refresh_token=...&u=...` 可在配置开启时建立刷新会话，各值应按 URL 参数编码。读取访问令牌时会撤销旧刷新资格，随后保存新凭据或清理旧刷新令牌。`getSessionKey()` 在同一会话轮换令牌时保持稳定，退出或替换会话时更新。请求层按会话标识校验重放，Vue 写入刷新结果前还会核对发起刷新时的访问令牌和刷新凭据。

基础库其他皮肤通过 `createCubeApi({ tokenRefresh: { isEnabled, refresh } })` 显式接入；refresh 成功时须保存新访问令牌及后端返回的新刷新令牌，失败 reject。实体和服务请求使用相同策略对象共用刷新。策略对象不跨应用或标签页共享；跨标签页同时轮换可能被后端防重放拒绝，失败回原登录流程。

## 文件与执行顺序

1. 本方案先落盘；派收记录只追加。
2. packages/cube/src/api/client.ts、增量协调器及定向 tests：基础能力。
3. web/core/utils/tokenRefresh.ts、token.ts、request.ts：Vue 策略与取消机制。
4. web/core/configure/types.d.ts、pages/LoginForm.vue、LoginPage.vue：配置及表单联动。
5. 定向单测覆盖上述验收条件，构建基础包、前端 typecheck、定向 lint；真实浏览器点击与后端刷新闭环分别记录，缺环境不冒充通过。

## 验证与反面判据

开跑前判据：关闭配置旧用例必须全过；并发只发一次刷新；重放至多一次；退出后不得写 token。任一违反不交付为通过。
测试不得只镜像实现：使用 axios adapter 模拟实际 HTTP/业务过期、并发与失败；表单点击测试检查透传和重载选择。真实后端验收需安全可用本地后端及有效登录态，禁止为启动测试擅自写共享真实环境。
回滚：关闭 enabled 即停止新功能；代码回退仅撤销本次 diff，不覆盖其他会话内容。未授权提交、推送或部署。

## 派收记录

待收｜core-refresh｜基础请求库与定向测试｜implement_refresh｜执行人｜本轮交付｜复核人｜packages/cube/src/api 与 tests｜兼容及并发测试通过｜evidence-before-done
已接单｜core-refresh｜执行人已读源码，等待方案落盘后编辑。
待收｜vue-refresh｜Vue配置与记住我联动｜执行人｜用户｜本轮交付｜复核人｜web/core 与本方案｜配置关闭兼容、表单联动及验证证据｜evidence-before-done
已接单｜vue-refresh｜执行人开始实施；用户已授权按方案执行。
已收到成果｜core-refresh｜client.ts、api/index.ts、tests/tokenRefresh.test.cjs 已复核；当前定向测试 65/65 通过；真实后端待核，不关闭整体验收。
已收到成果｜vue-refresh｜配置、登录表单及刷新工具已复核，定向 5 文件 74/74 通过。参数管理页修复后完整类型检查通过。
待收｜browser-refresh｜真实登录页浏览器与接口桩补验｜remember_form｜执行人｜本轮交付｜复核人｜.temp/remember-browser｜实点选择、重载持久、配置关闭隐藏及截图｜evidence-before-done
已接单｜browser-refresh｜已叫原表单执行人接续浏览器补验；不使用真实账号，不写共享环境。

## 验证记录（当前复核版本，2026-11-19）

- 基础库构建：成功。
- 基础库定向测试：65/65；历史 51/51 已被当前结果替代。
- Vue 定向五文件（含关键字回归）：74/74；历史 32/32 已被当前结果替代。
- `pnpm install` 与主题检查：成功。
- 定向 ESLint：无错误。
- 参数管理页修复后，`pnpm run type-check` 全量通过。
- 隔离 SQLite 后端刷新回归通过 5/5。
- 浏览器连接真实后端的闭环待核实，需要独立测试服务和专用测试账号。验证包括登录、访问令牌过期、刷新、原请求重放和退出，刷新与重放须得到 HTTP 2xx、JSON 响应和业务 `code=0`。
- 同标签页并发已测；跨标签页同时轮换未串行化，不作已支持承诺。
- 浏览器接口桩补验通过 1/1，覆盖勾选、重载恢复、密码框为空和关闭配置后隐藏控件。当前运行状态见 `.temp/remember-browser/results/.last-run.json`，截图见 `.temp/remember-browser/`。
- 浏览器首轮失败记录保留：宽泛URL桩误拦源码；修正后断言原生隐藏input不可见，改点可见标签并断言checked；稳定截图禁用有限动画后可见勾。未改产品CSS。

在 Cube 根目录运行 `pnpm --dir NewLife.Cube.Vue/web run test:e2e:remember` 可复跑浏览器测试。测试在本地 5198 端口启动独立前端，认证接口由测试桩提供。

已收到成果｜browser-refresh｜接口桩补验1/1通过；复核实跑、打开截图核对；真实后端未验，整体验收仍保持未关闭。

## 交付文件清单

路径相对于 Cube 独立仓；全部在当前工作区原路径直接修改，无 worktree，无需同步。未提交或推送。

| 文件 | 变更 |
| --- | --- |
| packages/cube/src/api/client.ts | 可选刷新协调与一次重放，默认兼容守卫 |
| packages/cube/src/api/index.ts | 导出策略类型 |
| packages/cube/tests/tokenRefresh.test.cjs | 基础库并发、错误、兼容测试 |
| NewLife.Cube.Vue/web/core/configure/types.d.ts | 可选配置类型 |
| NewLife.Cube.Vue/web/core/pages/LoginForm.vue | 条件展示、保存与透传选择 |
| NewLife.Cube.Vue/web/core/pages/LoginPage.vue | 成功登录授予资格、绑定用户名 |
| NewLife.Cube.Vue/web/core/utils/request.ts | 接入可选策略 |
| NewLife.Cube.Vue/web/core/utils/token.ts | 认证片段解析、会话代际和刷新令牌清理 |
| NewLife.Cube.Vue/web/core/utils/tokenRefresh.ts | 独立刷新请求及响应校验 |
| NewLife.Cube.Vue/web/core/utils/tokenRefreshState.ts | 选择、资格和登录代际 |
| NewLife.Cube.Vue/web/core/__tests__/tokenRefresh.spec.ts | 刷新工具及迟到响应测试 |
| NewLife.Cube.Vue/web/core/__tests__/LoginForm.remember.spec.ts | 表单兼容与选择恢复测试 |
| NewLife.Cube.Vue/web/e2e/remember-token-refresh.spec.ts | 真实页面浏览器接口桩补验 |
| NewLife.Cube.Vue/web/playwright.remember.config.ts | 隔离真实账号 setup 的浏览器配置 |
| NewLife.Cube.Vue/web/docs/changes/remember-token-refresh-plan.md | 本方案、派收与验证记录 |

工作区记录：相关测试输出及截图位于前述 `.temp` 路径，不作为产品文件。

## 跨日交接（2026-11-18）

本段记录 2026-11-18 时的交接。当前验证见上方记录。

未完｜real-backend-refresh｜真实后端登录、过期、刷新与重放闭环｜执行人｜执行人｜安全本地后端及有效登录态就绪后本轮执行｜解锁后核对HTTP 2xx、JSON、code=0及截图网络证据；当前无真实后端验收结论。
历史阻断｜typecheck｜参数管理页曾有两处类型错误，修复后全量类型检查通过。
已核实｜local-service-cleanup｜当前浏览器测试退出后，5198 端口无监听。

本段为历史恢复记录。当前版本的验证结果以“验证记录（当前复核版本）”为准。未提交、未推送，未部署。

## 审查修复（2026-11-18）

按用户独立审查单执行，范围冻结：①client.ts重放URL二次解析，补实体/服务五种baseURL探针；②token.ts中hash登录撤销旧资格并清refresh_token，补tokenRefresh.spec；③全部locales补login.rememberMe，保留表单兜底。④退出未清refresh_token本体仅登记，不修。

待收｜review-url｜client.ts及tokenRefresh.test.cjs｜代理review_url｜复核人｜本轮｜复核人本轮核对｜.temp审查日志｜五种baseURL首次/重放一致及基础测试通过｜evidence-before-done
待收｜review-hash-i18n｜token.ts、tokenRefresh.spec.ts、全部locales｜代理review_hash｜复核人｜本轮｜复核人本轮核对｜.temp审查日志｜hash清旧态、Vue四文件测试、lint及类型检查无新增错误｜evidence-before-done

本段记录 2026-11-18 审查范围，最新验证见上方记录。

## 2026-11-26 提交前审查

- 登录页去掉误加的密码复杂度校验，只保留记住我。存量弱密码仍可提交。
- 11 个语言文件增加 `login.rememberMe`。原先被删掉的通知词条已恢复。ar、de、es、fr、ja、ko、pt、ru 补上与英文源文一致的 `notify.*` 和 `route.notifications`。词表键测试 11/11 通过。
- 参数页改回只认 `UserID` 的改动未纳入。现行控制器字段是 `UserId`，已提交页面已同时兼容两个名字。
- 默认列表页和对象页注入、关键字 `q` 回归与本功能分开提交。
- 本轮定向单测 4 个文件 55/55 通过，改动文件 ESLint 无错误，`vue-tsc --build` 通过。
- 真实后端的登录、过期、刷新、重放闭环本轮未复跑。默认 `test:e2e` 忽略记住我桩测，该桩测仍用 `playwright.remember.config.ts`。
