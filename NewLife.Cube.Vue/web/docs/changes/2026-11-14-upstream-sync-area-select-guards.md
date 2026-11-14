# 2026-11-14：上游 224 提交同步 × 本地在制 24 件合流（AreaSelect 守卫融合等）

## 目的

本地 master（`fd86e002`）落后上游 224 个提交。在制工作（AreaSelect 防风暴守卫、ct 挂载器 locale 钉定等 24 件）先行 `git stash push -u` 全量储藏后 `pull --ff-only` 至 `320b947b`，再 `stash pop` 合流。本文记录 6 个内容冲突的裁决依据与测试适配，供后续追溯「为什么代码长这样」。

## 冲突裁决（6 件）

| 文件 | 裁决 |
|------|------|
| `core/utils/fieldControl.ts` | 双豁免并存：上游「映射字段（mapField）不产必填」+ 在制「审计 auto-fill 列不产必填（`isServerAuditFillField`）」是两条独立规则，均保留于 `isRequiredField` |
| `ct/main.ts` | 取在制：挂载器 `i18n.global.locale` 钉 `zh-CN`（CubeTable 双轨取词需两轨同语言，CT 截图/断言才确定）。上游侧为空（无新增内容） |
| `core/components/ModalContainer.vue` | 取上游：在制侧仅一行尾随空白，无语义 |
| `core/components/AreaSelect/index.vue` | **特性融合**：以上游为主体（`fetchChildren(parentId, childLevel)` 级联深度推导、`childRequests` 并发合并、精细 `onChange`），融入在制增量（`createLazyGuard` 风暴短路、`loading` 态、「失败/空结果才 `mark`」的守卫契约、模板 `:disabled="disabled || loading"`）。成功路径不 `mark`（可重入），失败/空结果 `mark`（短路），成功结果由 `childRequests` 缓存 |
| `core/__tests__/fieldControl.spec.ts` | 双保留：上游 4 个新 describe（映射字段/时长/空串归一/多选序列化）+ 在制 `isRequiredField` describe（审计列豁免），各自补齐收尾 |
| `core/components/AreaSelect/__tests__/AreaSelect.spec.ts` | 双保留：上游「反复 lazyLoad 只请求一次」「按级联深度判叶子」+ 在制「守卫只计失败/空结果」三个用例 |

## 测试适配（合流语义差）

- AreaSelect spec 3 处期望 `level: 0` → `level: 1`：融合后 `toAreaOptions` 第三参 `fallbackLevel=childLevel`（手动 `lazyLoad({value})` 无 `node.level` → childLevel=1）。
- 守卫用例 ok2 断言 `calls=1` → `calls=0`：成功结果被 `childRequests` 缓存，重触发不发新请求（守卫只拦失败/空结果）——「面板 store 重建后可重新灌数据」的意图由缓存满足。
- `CubeTable.ct.spec.ts` 值集标签断言 `Enabled/Disabled/Draft` → `启用/禁用/草稿`：在制已把 `ct/mocks/lovStore.ts` 值集桩改为中文且挂载器钉 zh-CN，用例意图（显示标签而非码值）不变。

## 门禁（2026-11-14 12:1x）

- `pnpm test:unit`：836/844（8 败 = `localeKeys.spec.ts` 八语言对账，**上游存量**——上游 enUS 有 `parameterAdmin.*` 等键而 ar/de/es/fr/ja/ko/pt/ru 未跟上，纯净 pull 即红，与本次合流无关）。
- `pnpm run type-check`：2 错 = `apps/cube-admin/.../parameter/index.vue`（**上游存量**，工作区该文件与 origin/master 一致）。
- `pnpm test:ct`：**39/39 全绿**（CT 基线不入库，本地 update 重固化）。
- theme token 门禁：OK（563 token 校验）。

## 遗留（候上游/后续）

1. localeKeys 八语言 24 键缺失（上游翻译债）；
2. parameter/index.vue 两处 TS 错误（上游 `CubeEngineContext.type` 缺属性 + tag type 空串）。
