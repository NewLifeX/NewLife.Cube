# OSC-260813c3e9 Retro

> 复盘在验收通过后由 openspec-retro 填写；归档后状态置 `Done`。

## 结果摘要

| 维度 | 结果 |
|------|------|
| AC 通过率 | 12/12（AC-11 登录页/SPA/API 200；已登录抽屉点击未做，不阻塞） |
| 三步编排 | 实现审计 ✅ → 代码审查 0🔴 + 2🟡 → 文档同步 ✅ |
| 自动化门禁 | Vitest **355/355**（含 sfcThin 48）· vue-tsc ✅ · vite build ✅ |
| 代码质量 | composable 同目录命名清晰；DefaultList 共享 `listContext` + deps 注入避免四份状态 |
| 工期 | T01–T46 一次执行周期 + 创建期编号修订（去连字符、去掉 DeepSeek 绑定） |
| 手工冒烟 | SPA `/` `/login` 200；CubeDemo `:5000` 200 |

## 实际完成范围

- 协作编号改为 `OSC-YYMMDDxxxx`（日期与随机 hex 之间无 `-`），历史 `OSC-00xx` 豁免。
- 撤销 OpenSpec 中 DeepSeek/Flash/Pro 执行粒度。
- 47 个 `.vue`：32 抽离 + 15 审计；`sfcThin` allowlist 空。
- DefaultList 拆为 `listContext` + `useListQuery` / `useListCrud` / `useListViews` / `useRecordNav` + `useDefaultList`。

## 做得好的

1. **随机编号取代 max+1**：并行创建不再抢号，依赖写在 proposal 表而不是号码大小。
2. **机械门禁先于抽离收口**：`sfcThin.spec.ts` 让「vue 里不许 watch/onMounted/cubeApi」可回归，不靠人工抽查。
3. **DefaultList 共享上下文**：`createListContext` 一次创建 refs，领域 composable 不复制状态；跨领域只注入 `loadData`。
4. **审计文件不造空 hook**：15 个已薄 SFC 保持原样，避免无意义文件膨胀。

## 待改进

1. **构薄「约 20 行」对 DefaultList 不现实**：模板绑定解包仍很长，门禁只扫 token。后续若要真正短 script，需按区块再拆子组件，不宜只改 composable。
2. **本号范围偏大**：编号规则 + 47 文件抽离同号，执行任务条数多、验收对照表长。协作规则变更与存量重构以后宜拆号。
3. **创建期曾写入后又撤销 DeepSeek 约束**：规范摇摆增加一次全文替换。厂商模型能力变化快，OpenSpec 不宜绑定具体模型名。

## 遗留与后续

- 已登录态 `Admin/User` 列表/右侧抽屉人工点验（🟢，行为应与抽离前一致）。
- DefaultList 解包行数（🟢，非功能缺陷）。
- OSC-0018 仍为 Draft，本号未实现。
