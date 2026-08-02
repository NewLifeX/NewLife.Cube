# Status
- id: OSC-0007
- state: Done
- updated: 2026-08-02T16:35:00+08:00
- approvedBy: openspec-approve
- trigger: "对 OSC-0007 进行验收和复盘。"
- checklist: passed
- note: |
  验收通过并复盘归档。Vitest 113 / pnpm build ok。
  增强：CardMapping.bodyColumns/fieldOrientation。
  残留：浏览器手工冒烟；分组/排序入口仍为占位。
  明确不交付：列表/树拖拽排序（试做后已撤销）。
- tests: |
  - pnpm test → 113 passed
  - pnpm build ArcoVue web → ok
