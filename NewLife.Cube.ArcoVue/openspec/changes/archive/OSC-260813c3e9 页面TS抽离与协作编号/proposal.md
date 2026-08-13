# OSC-260813c3e9 — 页面 TS 抽离与协作编号

## 1. 为何做

1. **顺序号抢号**：`OSC-00xx` 用 `changes/` 最大号 +1。多人并行创建时必然冲突，且「为 FlowGram 预留 0010」制造空洞号。OpenSpec 已作为团队工作流，必须换成创建时即可唯一、无需协调的 ID。
2. **SFC 内嵌业务 TS**：`web/src` 现有 **47** 个 `.vue`，几乎全部 `<script setup lang="ts">` 内联业务（`DefaultList.vue` script ≈1431 行）。Vitest 为 node、无 Vue plugin、无 SFC 挂载测试，内联逻辑无法单测，也不利于后续迁移。

## 2. 已锁定范围

| # | 决策 |
| --- | --- |
| 1 | **新 ID**：`OSC-YYMMDDxxxx`。`YYMMDD` = 创建日 `Asia/Shanghai`；`xxxx` = 4 位密码学随机小写 hex，**紧接日期、中间无 `-`**。目录 `{ID} <中文简述>`。创建时在 `changes/` **与** `archive/` 查前缀唯一，冲突重抽。**禁止** `max+1` / 落地顺序递增 / 预留空洞号。 |
| 2 | **历史豁免**：`OSC-0001` … `OSC-0019`（含进行中的 OSC-0018）永不改名；`批准 OSC-0018` 仍有效。 |
| 3 | **触发定位**：`批准/执行/验收/复盘 OSC-260813c3e9` 按目录名前缀唯一匹配。 |
| 4 | **SFC 模式 = composable**：`.vue` 保留构薄 `<script setup>`（`defineProps`/`defineEmits`/`defineExpose` + `useXxx()`）。**不**用 `<script setup src>`。 |
| 5 | **范围 = 规范 + 全部 47 个 `.vue`**。已足够薄且无禁止 token 的文件只审计、不造空 composable。 |
| 6 | 构薄规则：除 import/宏外建议 ≤20 行；禁止在 `.vue` 写业务 `watch`/`onMounted`/`onBeforeUnmount`、`cubeApi.*`。纯函数进已有 `core/utils` 或 sibling `*Helpers.ts`。 |
| 7 | VTable/Gantt：`.vue` 只留宿主 `div`；生命周期在 composable。 |
| 8 | **不改** UI/API/Pinia 契约、抽屉 `placement="right"`、OSC-0003 内核/壳隔离。不实现 OSC-0018。 |
| 9 | 机械门禁：`sfcThin.spec.ts` 扫描 `.vue` 的 `<script>`；抽离完成后 **allowlist 必须为空**。 |
| 10 | **不绑定特定模型**：OpenSpec 规范与五壳 Agent **不得**写入 DeepSeek / Flash / Pro 等厂商执行粒度。 |

## 3. 做什么

1. 把编号规则写入：`openspec/README.md`、五壳 Agent（尤其 `openspec-create`）、`changes/` 与 `archive/` README、`harness/lessons.md` 格式行、迁移方案 §9.2 / §10.1 / §13 / §14。并**删除**其中 DeepSeek / Flash / Pro 执行粒度条文。（创建期可已落地，执行期核对勾选。）
2. 新增 `web/src/core/utils/sfcThin.spec.ts`，初始 allowlist = 须抽离的 `.vue`。
3. 按 design 文件地图抽离：`DefaultList` 拆为 `listContext` + 4 个领域 composable + 组装器；其余须抽离文件各一个 `useXxx.ts`。
4. 审计已薄文件，不新建空 composable。
5. 清空 allowlist；同步 `web/README.md` 与 openspec README 的 SFC 活规则。

## 4. 不做什么

- 不在 OpenSpec 规范中绑定 DeepSeek 或其它厂商模型的执行粒度。
- 不与 OSC-0018 合并；不实现实体界面自定义、技能体系。
- 不改 GetPage / Lov / ViewProfile JSON schema、不改 `/api` 路径、不改可见文案与工具栏顺序。
- 不引入 Vue Test Utils / 不强制 SFC 挂载测试。
- 不采用 `<script setup src>`。
- 不改 `NewLife.Skills` 正文。
- 不修改本地 `master` 分支、不推送远程（除非用户另指令）。

## 5. 依赖

| 依赖 | 关系 |
| --- | --- |
| OSC-0003 | Done：微内核 / DynamicPage / DefaultList / Section |
| 既有 composable 样板 | `layouts/useShellAuth.ts`、`features/views/cardHelpers.ts` |
| Vitest | `web/vitest.config.ts` node 环境；`include: ['*.spec.ts', 'src/**/*.{spec,test}.ts']` |
| OSC-0018 | 并行 Draft，本号不改其文档、不实现其方案 |

## 6. 测试范围

| 类型 | 是否做 | 说明 |
| --- | --- | --- |
| 新增单测 | 是 | `sfcThin.spec.ts`：禁止 token + allowlist；抽离结束 allowlist `[]` |
| 回归 | 是 | 现有 28 个 spec、约 307 条 Vitest 必须保持全绿 |
| 构建 | 是 | `pnpm --filter @cube/arco-vue build`（`vue-tsc -b && vite build`）无错误 |
| 后端 XUnit | 否 | 本号不改 C# |
| 纯文档核对 | 是 | 新号正则 `OSC-YYMMDDxxxx`、create 禁 `max+1`、规范中无 DeepSeek 执行粒度 |

## 7. 成功标准

- [ ] 新 OSC 只能以 `OSC-YYMMDDxxxx` 创建；create agent 已删除「最大号 +1」。
- [ ] 历史 `OSC-0018` 等旧触发语仍能定位目录。
- [ ] 47 个 `.vue` 均已抽离或审计；`sfcThin` allowlist 为空。
- [ ] `pnpm --filter @cube/arco-vue test` 全绿；`build` 无错误。
- [ ] 列表/抽屉/六视图/登录/壳的外部行为与抽离前一致（verify 冒烟清单）。
