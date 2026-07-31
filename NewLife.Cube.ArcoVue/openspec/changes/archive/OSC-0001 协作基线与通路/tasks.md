# OSC-0001 Tasks

- [x] 更新 `NewLife.Cube.ArcoVue/web/vite.config.ts`：proxy 增加 `/Auth`、`/Mfa`
- [x] 更新 `Doc/Api/核心接口架构.md` §2.2：增加 MFA → 认证接口设计交叉引用
- [x] 检查并必要时更新 `NewLife.Cube.ArcoVue/web/README.md`（代理列表、UseArcoVue / 端口 5183）
- [x] 冒烟：后端可用则验证经代理的 Auth（或记录环境受限）
- [x] 同步文档勾选：核心接口架构 + README 与 design 影响表一致
- [x] 本地确认 openspec 五壳文件与 README 状态机文案一致（Implementing/Validating/Done）
- [x] **补测：** 引入 Vitest；`devProxy.spec.ts` 断言 proxy 含 `/Auth`、`/Mfa`
- [x] **跑测：** `pnpm test` — 2 passed
- [x] **构建：** `pnpm build` — 成功（无 error；仅有 chunk size warning）
- [x] 更新 openspec/harness 与 §9 硬门禁文案（流程已收紧测试要求）
