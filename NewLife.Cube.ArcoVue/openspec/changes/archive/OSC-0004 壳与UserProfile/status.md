# Status
- id: OSC-0004
- state: Done
- updated: 2026-08-01T09:10:02+08:00
- approvedBy: openspec-approve
- trigger: "验收并复盘 OSC-0004。"
- checklist: passed
- note: |
  verify 通过并复盘归档；Vitest 32；build ok；401 清 localStorage 已补。
- tests: |
  - pnpm test @ NewLife.Cube.ArcoVue/web → 32 passed
  - pnpm build @ packages/api-core + ArcoVue/web → ok
  - new: userProfile.spec.ts, tokens.spec.ts
