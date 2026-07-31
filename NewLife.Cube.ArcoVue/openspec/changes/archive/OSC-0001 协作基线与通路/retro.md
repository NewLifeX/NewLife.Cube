# OSC-0001 Retro

> 复盘时间：2026-07-31  
> 终态：Done

## 做得好的

- 首跑打通 OpenSpec：`Draft → Accepted → Implementing → Validating → Done`。
- 代理配置抽出 `devProxy.ts`，单测可锁 Auth/Mfa 通路，避免仅改 vite 难测。
- 文档 MFA 单源交叉引用，未复制 AUTH-10 全文。
- 验收强制重跑 `pnpm test` + `pnpm build`，门禁可执行。

## 偏差与根因

- 初版 proposal 写「无 Vitest」；用户收紧硬门禁后才补测——根因：把「无业务逻辑」误等同于「可不跑测」。已写入 §9 与 harness。
- Auth 实网冒烟因后端未起跳过——预期内风险，verify 已记录。

## 测试与质量

- 新增：`devProxy.spec.ts`（2）全过；构建无 error。
- 教训：凡改前后端代码，执行期必须跑测；验收期新增单测全过 + 构建无错误。

## 后续 OSC 建议

- OSC-0002：后端三实体时严格执行 XUnit + build 门禁。
- 可选：CI 为 `@cube/arco-vue` 增加 `pnpm test` job；Registry 可用 npmmirror 或预缓存 vitest。
- 开发联调：起 CubeDemo 后再验 `/Auth` 经 5183 代理。
