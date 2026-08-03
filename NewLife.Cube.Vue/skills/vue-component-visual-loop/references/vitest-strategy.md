# Vitest 逻辑回归策略

> 配套 `vue-component-visual-loop` SKILL.md §三。此处放细节，SKILL 只留索引。

## 一、运行

```bash
cd <web-root>
pnpm test:unit                                       # 全部 Vitest 测试
# 指定单个组件：
pnpm exec vitest run --config vitest.config.unit.ts core/components/<ComponentName>/<ComponentName>.spec.ts
```

> 无 `pnpm` / 被环境拦截时：`node node_modules/vitest/vitest.mjs run --config vitest.config.unit.ts <ComponentName>`（`.bin/vitest` 是 shell 封装，不能直接 `node` 跑，要走 `vitest.mjs` 入口）。

## 二、Mock 策略（关键）

不依赖后端，全部网络层桩化（本项目**不使用 MSW**，一律 `vi.mock` 模块桩 + 依赖注入）：

```typescript
vi.mock('@scope/core/configure', () => ({ getConfig: () => ({ request: { baseUrl: '' } }) }));
vi.mock('@element-plus/icons-vue', () => ({ Search: { name: 'Search', template: '<i class="el-icon-search" />' } }));
vi.mock('@scope/core/utils/request', () => ({ default: { get: vi.fn(), post: vi.fn() }, get: vi.fn(), post: vi.fn() }));
vi.mock('@scope/core/utils/<api-module>', () => ({
  fetchList: vi.fn(async () => ({ data: MOCK_ROWS, total: MOCK_ROWS.length })),
  fetchBatchLabel: vi.fn(async () => ({})),
  shouldDirectRequest: vi.fn((config) => !!(config?.requestUrl?.startsWith('/'))),
}));
```

要点：

- **别名优先于相对路径**：组件内部凡涉及被 mock 的模块，必须用项目别名（如 `@scope/core/utils/xxx`）导入，**不要用 `../../utils/xxx` 相对路径**——相对路径会绕过 Vite 别名桩，在 Gallery / CT 预览里直接打到真实后端，导致组件空白或返回原始值。这是最常见的"预览里组件不渲染"根因。
- Mock 返回的数据结构要与真实 API 对齐（分页 `{ data, total }`、字典 `{ id: label }` 等）。

## 三、新增测试步骤

1. `<ComponentName>.spec.ts` 中 `mount(Component, { props, global: { plugins: [ElementPlus] }, attachTo: document.body })`；
2. `setProps` 触发 watch、`await flushPromises()` + `await nextTick()` 等待异步；
3. 断言 DOM（`.el-button` / 自定义计数节点等）和 emits；
4. `wrapper.unmount()`；
5. 覆盖维度：纯函数输入输出、组件 emit、props 渲染、边界（空数据 / 加载中 / 错误态）。

## 四、纪律

- Vitest 守住**逻辑不退化**（纯函数、emit、props 渲染、边界）；视觉交给 CT。
- 逻辑改动后先跑 Vitest 秒级回归，再进视觉阶段。
