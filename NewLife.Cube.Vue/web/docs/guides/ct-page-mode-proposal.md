# 组件测试新增「页面集成模式」方案（CT Page/Flow Mode）【草案】

> **状态**：草案 / 待评审。**尚未实现**。本文只给方案、架构与改动清单，落地步骤见 §11。
>
> 操作细节（命令、挂载契约、Gallery）最终收口于技能 `vue-component-visual-loop`（`skills/vue-component-visual-loop/`，references：`ct-gallery.md`、`ct-environment-setup.md`）。本文与 `guides/component-visual-dev.md` 是同一套 CT 能力的"规范/方案"层，本篇新增**第二种 CT 模式**：页面集成模式。

---

## 一、背景与动机

现有 CT 能力 = Gallery 宿主 + `*.story.ts`（状态变体）+ `*.ct.spec.ts`（像素级截图 + 关键状态断言）。它解决了两件事：**"长什么样"**（截图回归）和 **"关键静态状态对不对"**（回显文字、翻页统计数等 `expect` 断言）。

但它有一个空白：**组件在"页面里"的真实交互闭环没有被覆盖**——

- 页面挂载即发起请求；
- 用户选择 / 填写，触发提交；
- 提交真的以"正确 method + url + body"调了某个接口；
- 响应回来后 UI 正确变化（成功提示 / 列表刷新 / 错误态）。

这些是**"页面 + 逻辑"综合**验证。截图模式只证明"长这样"，不证明"点得动、提交对了"；现有的类人点击脚本虽然能点，但 mock 是写死的 canned 数据，**无法断言"提交那一刻组件到底以什么 payload 调了哪个接口"**。

**用户诉求**（原话归纳）：类似 CT 再新增一种模式——测试"一个页面渲染、请求、提交、调接口"，完整验证组件在「页面 + 逻辑」综合下的表现；**只渲染一个页面**（不是整站 SPA），页面内**只有被测组件真实**，其余（接口请求等）**全模拟**，跑"组件 + 逻辑"，其他一切模拟。

---

## 二、目标与范围（Definition of Done）

| # | 目标 | 说明 |
| - | --- | --- |
| G1 | 渲染单个页面 | 不是整站路由，是一个最小页面外壳（表单 / 提交按钮 / 结果提示）内嵌被测组件 |
| G2 | 接口全模拟 + 可断言 | 所有 API 被模拟；spec 能断言"组件/页面以什么 method/url/body 调了哪个接口" |
| G3 | 跑"组件 + 逻辑"综合 | 挂载即请求 → 用户操作触发提交 → 响应驱动 UI 变化，全程可被 Playwright 驱动并断言 |
| G4 | 与现有 CT 并存 | 复用同一份 `ct/vite.config.ts`、同一端口 5190、同一 Playwright 配置；手动 `ct:server` 开 `?page=` 也能预览 |
| G5 | 不偏离团队约定 | 不引入 MSW、不引入网络拦截、不新增 vite 配置 / 端口；继续"别名优先" |

**非目标**：整站端到端（多页跳转 / 真实路由）；真实后端联调；性能 / 压测。

---

## 三、方案选型（关键决策）

讨论过三种 Mock 机制：

| 机制 | 复用现有 vite/端口 | 手动 `ct:server` 预览 | 与"别名优先/不用 MSW"约定 | 能断言真实请求契约 | 复杂度 |
| --- | --- | --- | --- | --- | --- |
| **A. 可编程别名桩（进程内 Mock 注册表）** | ✅ 同一份 | ✅ `?page=` 用默认 mock 渲染 | ✅ 一致 | ⚠️ 断言"组件调用 request 的入参"（method/url/body），非 HTTP 传输层 | 低 |
| B. Playwright `page.route` 网络拦截 | ❌ 需第二份 vite（不别名 request/lov-api） | ❌ 无拦截器会真实 404 | ❌ 偏离别名桩惯例 | ✅ 断言真实 HTTP 请求 | 中 |
| C. MSW | ❌ 需注入 service worker | ❌ 需 worker 激活 | ❌ 团队明确"不使用 MSW" | ✅ | 高 |

**结论：采用 A（可编程别名桩 / 进程内 Mock 注册表）。**

理由：

1. 与项目测试策略 **"别名优先、不使用 MSW、一律 `vi.mock` 模块桩 + 依赖注入"** 完全一致（见技能 `references/vitest-strategy.md` §二）。
2. **复用同一份 `ct/vite.config.ts` 与端口 5190**，无需第二套 server；手动 `ct:server` 打开 `?page=<id>` 也能用默认 mock 渲染并交互。
3. 确定性高、无网络、CI 稳定；组件对 `request` 的**调用过程**与**响应处理逻辑**都真实运行——这正是"组件 + 逻辑"要测的部分。
4. MSW / 网络拦截（B、C）会偏离约定，且会破坏手动预览（裸浏览器里没有拦截器，组件会因真实请求失败而报错），还需额外的 fetch patch 兜底。

**取舍说明**：A 不走真实 HTTP 传输层（axios 序列化细节不测），但那不是本项目的业务**逻辑**；本项目要测的逻辑在"调 `request` + 处理返回"这一层，A 完整覆盖。

---

## 四、架构总览

页面集成模式与现有"Gallery / Story 视觉模式"**并列**，共用 Gallery 宿主、vite、Playwright 配置。核心新增：一个**进程内 Mock 注册表** `window.__mockApi`，让 spec 能为场景注入响应、并读取组件真实发出的接口调用用于断言。

```
[*.page.ts] ──显式静态导入──> [ct/pages.ts]
                                      │
                                      ▼
[Playwright *.page.spec.ts] ──?page=<id>──> [ct/index.html + main.ts] ──mountPage──> 单页(.vue)
        │                                 │                                      │
        │ page.evaluate(__mockApi.set)    │                                      │ 组件/页面真实运行
        │ 注入场景响应 / 读 getCalls()     │                                      │ 调用 request/lov-api(别名桩)
        ▼                                 ▼                                      ▼
[ct/mocks/mock-registry.ts] <──────── 记录 calls + 命中场景响应 ───────────────┘
        │
        └── getCalls() 供 spec 断言 API 契约（method/url/body）
```

与现有 Story 模式的差异：

| 维度 | Story 视觉模式（现有） | 页面集成模式（新增） |
| --- | --- | --- |
| 入口 | `?story=<id>` | `?page=<id>` |
| 渲染对象 | 孤立组件（props 变体） | 单个页面（组件 + 最小外壳） |
| 验证重点 | 像素布局 / 关键静态状态 | 请求 → 操作 → 提交 → 响应 → UI 变化的**逻辑闭环** |
| Mock | canned（静态样本） | 注册表：默认 canned/noop，spec 可注入场景响应 |
| 断言 | 截图 + 文字/计数 | 截图（可选）+ **API 调用契约** + DOM 变化 |
| 文件 | `*.story.ts` / `*.ct.spec.ts` | `*.page.ts` / `*.page.spec.ts` |

---

## 五、文件级改动清单

| 动作 | 文件 | 内容 |
| --- | --- | --- |
| 新增 | `ct/mocks/mock-registry.ts` | `window.__mockApi` 单例：`set` / `setMany` / `clear` / `getCalls` / `match` |
| 改 | `ct/mocks/request.ts` | 注册表感知：默认 noop 回退（story 模式零影响），命中则返回场景响应并 `record` 调用 |
| 改 | `ct/mocks/lov-api.ts` | 注册表感知：默认 canned 回退，按 lovCode 命中场景响应并 `record` lov 调用 |
| 新增 | `ct/pages.ts` | 显式静态导入所有 `*.page.ts`（沿用"显式导入、不靠 glob"的纪律） |
| 改 | `ct/index.html` + `ct/main.ts` | 支持 `?page=<id>` 深链；暴露 `window.mountPage`（复用 `mountStory` 实现） |
| 新增（示例） | `core/components/LovSelectTable/LovSelectTablePage.vue` | 最小页面外壳：持有选中值 + 保存按钮 + 成功/失败提示 |
| 新增（示例） | `core/components/LovSelectTable/LovSelectTable.page.ts` | 页面声明 `{ id, component, props }` |
| 新增（示例） | `core/components/LovSelectTable/LovSelectTable.page.spec.ts` | Playwright 流程测试：注入 mock + 驱动 + 断言 API 契约 + DOM |
| 改 | `playwright-ct.config.ts` | `testMatch` 增加 `**/*.page.spec.ts`（同一 server、同一配置） |
| 改 | `package.json` | 可选新增 `test:ct:page`（grep 仅跑页面模式）或复用 `test:ct` |
| 改 | 技能 `vue-component-visual-loop` | SKILL.md §四新增"页面集成模式"小节；§九表增加 `references/ct-page-mode.md`；新增 `references/ct-page-mode.md` |
| 改 | `docs/guides/component-visual-dev.md` + `docs/standards/testing-standard.md` | 回链本方案（规范/流程层，细节落技能） |

---

## 六、Mock 注册表 API 设计

`ct/mocks/mock-registry.ts` 暴露一个浏览器侧单例 `window.__mockApi`：

```ts
export interface MockCall {
  method: string;          // 'GET' | 'POST' | 'PUT' | 'DELETE'
  url: string;             // 组件实际传入的 url（如 '/api/role/save'、'lov://List.Xxx'）
  params?: unknown;        // GET 的查询参数
  body?: unknown;          // POST/PUT 的请求体
  timestamp: number;
}
export interface MockHandler { status?: number; data?: unknown; delay?: number }

class MockApi {
  set(matcher: string | RegExp, h: MockHandler): void;   // 精确串 'POST /api/role/save' 或正则 /api\/role/
  setMany(map: Record<string, MockHandler>): void;
  clear(): void;                                            // 重置 handlers + calls
  record(c: MockCall): void;                               // 桩内部调用
  getCalls(): MockCall[];                                  // spec 读取断言
  match(method: string, url: string): MockHandler | undefined;
}
```

**匹配规则**：先精确（含 `'METHOD url'` 或仅 `url`），再正则；spec 优先用"具体 method + url"避免串味。

**默认回退（向后兼容）**：
- `request` 桩：未命中 → 返回 `{ data: null, code: 0, msg: '' }`（即现有 noop 行为，story 模式不受影响）。
- `lov-api` 桩：未命中 → 返回现有 canned（SAMPLE / PAGED / ENUM/LIST meta），story 模式基线不因之漂移。

---

## 七、示例：LovSelectTable「角色选择并保存」页面

演示 G1~G3：单页内嵌 LovSelectTable，挂载即渲染，用户选 2 行 → 确定 → 保存按钮 `request.post('/api/role/save', { ids })` → mock 返回成功 → 页面显示"保存成功"，且 spec 断言确实以 `{ ids: [...] }` 调了该接口。

**`LovSelectTablePage.vue`（最小外壳，节选）**

```vue
<script setup lang="ts">
import { ref } from 'vue';
import LovSelectTable from './index.vue';
import request from '@newlifex/cube-vue/core/utils/request'; // 别名导入（纪律：禁用相对路径）

const props = defineProps<{ lovCode?: string; multiple?: boolean }>();
const selected = ref<number[]>([]);
const message = ref<{ type: 'success' | 'error'; text: string } | null>(null);

async function onSave() {
  message.value = null;
  const res: any = await request.post('/api/role/save', { ids: selected.value });
  message.value = res?.code === 0
    ? { type: 'success', text: '保存成功' }
    : { type: 'error', text: '保存失败' };
}
</script>

<template>
  <div class="role-save-page">
    <LovSelectTable
      v-model="selected"
      :lov-code="props.lovCode ?? 'List.CubeDemo.Role'"
      :multiple="props.multiple ?? true"
      dialog-visible
    />
    <el-button type="primary" @click="onSave">保存</el-button>
    <span v-if="message" :class="['result', message.type]">{{ message.text }}</span>
  </div>
</template>
```

**`LovSelectTable.page.ts`**

```ts
import LovSelectTablePage from './LovSelectTablePage.vue';
export const pages = [
  { id: 'LovSelectTable/SaveFlow', component: LovSelectTablePage, props: { multiple: true } },
];
```

**`LovSelectTable.page.spec.ts`（节选）**

```ts
test('选 2 行并提交，以正确 payload 调 /api/role/save', async ({ page }) => {
  await page.goto(`${GALLERY}?page=LovSelectTable/SaveFlow`);
  await page.waitForSelector('.el-table__row');

  // 选第 1、2 行
  const rows = page.locator('.el-table__row');
  await rows.nth(0).locator('.el-checkbox').click();
  await rows.nth(1).locator('.el-checkbox').click();

  // 提交前注入成功响应（只覆盖这一个接口，其余用默认 mock）
  await page.evaluate(() => {
    window.__mockApi.set('POST /api/role/save', { data: { success: true } });
  });

  await page.click('text=保存');
  await expect(page.locator('.result.success')).toHaveText('保存成功');

  // 断言 API 契约：确实以正确 method/url/body 调了保存接口
  const calls = await page.evaluate(() => window.__mockApi.getCalls());
  const saveCall = calls.find((c) => c.method === 'POST' && c.url === '/api/role/save');
  expect(saveCall).toBeTruthy();
  expect(saveCall!.body).toEqual({ ids: [1, 2] });
});
```

> 说明：此处 `selected` 初值默认空，示例中 LovSelectTable 默认回显为空；具体行 id 取决于 `lov-api` 默认 canned（SAMPLE：1/2/3…），断言以实际 canned 为准。

---

## 八、命令

```bash
# 手动预览（HMR，用默认 mock 渲染 ?page= 页面）
pnpm ct:server
# 浏览器打开：http://127.0.0.1:5190/?page=LovSelectTable/SaveFlow

# 回归（同一配置已包含 *.page.spec.ts）
pnpm test:ct                 # 同时跑 story 模式 + 页面集成模式
pnpm test:ct:page            # （可选）仅跑页面集成模式（grep）

# 更新基线（页面模式也可锁视觉基线）
pnpm test:ct:update
```

---

## 九、与现有闭环的衔接

仍是"改代码 → 逻辑回归 → 视觉预览 → 锁基线"的一环，补的是中间缺失的**「逻辑 + 页面综合」层**：

```
Vitest（纯函数/emit/边界）
   │
   ▼
页面集成模式（请求→操作→提交→响应→UI 变化，断言 API 契约）   ← 本方案新增
   │
   ▼
CT 截图（像素级视觉回归）
```

- 组件 README 模板（`references/component-readme-template.md`）：Part A「数据契约」照常填接口/Mock 约定；Part B 新增「页面集成测试 ↔ 功能」对应表。
- 类人点击验证（§八 of SKILL）可复用：页面模式同样走 `?page=` 深链 + `ct/mocks/` 数据，环境一致。

---

## 十、风险与缓解

| 风险 | 缓解 |
| --- | --- |
| 注册表跨测试串味 | 每个 `*.page.spec.ts` 用例用 `page.goto` 全量加载 → 模块重跑 → 注册表重建；用例内多次导航需显式 `__mockApi.clear()` |
| 默认 mock 与场景 mock 冲突 | `match` 精确（`'METHOD url'`）优先；spec 用具体 url+method，不用宽泛正则 |
| 影响现有 story 基线 | `request`/`lov-api` 桩**未命中时回退到现有 noop/canned**，story 模式行为零变化（需回归一次 `test:ct` 验证） |
| 手动预览时页面挂载即调接口但无默认 mock | 默认 noop 返回空数据；页面需对空数据有兜底渲染（列入组件边界要求） |

---

## 十一、实施步骤（待批准后）

1. 新增 `ct/mocks/mock-registry.ts`；升级 `ct/mocks/request.ts` 与 `ct/mocks/lov-api.ts` 为注册表感知（保持默认回退）。
2. 新增 `ct/pages.ts`；`ct/main.ts` 增加 `?page=` 深链与 `window.mountPage`（复用 `mountStory` 实现）。
3. 以 LovSelectTable 落地示例：`LovSelectTablePage.vue` + `LovSelectTable.page.ts` + `LovSelectTable.page.spec.ts`。
4. `playwright-ct.config.ts` 的 `testMatch` 增加 `**/*.page.spec.ts`；按需补 `test:ct:page` 脚本。
5. 技能 `vue-component-visual-loop`：SKILL.md §四新增小节 + §九表增加 `references/ct-page-mode.md`；新增该 reference。同步回链 `docs/guides/component-visual-dev.md` 与 `docs/standards/testing-standard.md`。
6. 跑通验证：起 `ct:server`（`CT_FRESH=1` 规避旧端口），运行页面模式 spec，截图核对，并跑一次 `test:ct` 确认 story 基线未漂移。

---

## 十二、待确认项（Open Questions）

1. **命名**：`*.page.ts` / `*.page.spec.ts` / `?page=` 深链 / "页面集成模式" 是否采用？（备选：`*.flow.*` / `?flow=` / "流程模式"）
2. **默认 mock 自动注册**：是否要为"页面挂载即请求"按 lovCode / url 预置默认响应，减少每个 spec 的样板？
3. **CI 纳入**：页面集成模式是否与 story 模式同卡点接入 CI（合并前阻断）？
4. **首期范围**：先做 LovSelectTable 一个示例，还是同时覆盖 LovSelect？
