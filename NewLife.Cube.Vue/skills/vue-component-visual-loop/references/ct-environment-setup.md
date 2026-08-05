# CT 环境搭建（从零复刻）

> 本文描述如何在一个新项目里搭建与本技能一致的 **组件测试（CT）环境**：自建 Gallery 模式（轻量 Storybook 替代）+ Playwright CT 像素级截图 + Vitest 逻辑回归，三者共用同一份 story 与同一份 Vite 配置，做到"所见即所测"。
>
> **适用**：新仓库想要同样的"改代码→看效果→锁基线"闭环；或已有 E2E/单测、但缺组件级视觉回归能力时补齐。
>
> **相关**：环境**怎么用**见 `ct-gallery.md`；Vitest 怎么写见 `vitest-strategy.md`；类人验证怎么驱动见 `mcp-playwright.md`。本文只讲**怎么把这套设施搭起来**。

---

## 一、你要搭出什么

| 能力 | 由什么提供 | 解决什么 |
| --- | --- | --- |
| 手动浏览器预览（HMR） | `ct/index.html` + `ct/main.ts`（独立 Vite） | 开发中秒看渲染，不等 SPA 启动 |
| 像素级视觉回归 | `*.ct.spec.ts` + Playwright（`playwright-ct.config.ts`） | 防止以后被改坏 |
| 逻辑回归 | Vitest（`vitest.config.unit.ts`） | 纯函数 / emit / props 不退化 |
| 后端解耦 | `ct/mocks/` 别名桩 | 组件无需真实后端即可渲染 |

三者通过 `ct/stories.ts` 聚合的 `*.story.ts` 串联：故事既给 Gallery 预览，也给 CT 截图。

---

## 二、目录与文件清单

在 `<web-root>/`（前端根）下创建：

```
<web-root>/
├── ct/                                  # CT 专用设施（与 e2e 的 playwright.config.ts 解耦）
│   ├── index.html                     # Gallery 宿主壳（挂 #gallery-nav + #app）
│   ├── main.ts                          # 渲染侧栏 / ?story= 深链，暴露 window.mountStory 等契约
│   ├── stories.ts                       # 显式静态导入所有 *.story.ts，聚合注册表
│   ├── vite.config.ts                   # CT 专用 Vite（独立端口 + 别名桩 + 虚拟模块桩）
│   └── mocks/                           # 统一后端 mock
│       ├── request.ts                   # 桩掉 @newlifex/cube-vue/core/utils/request
│       ├── lov-api.ts                   # 桩掉 lov-api（返回样本数据 + 确定性 meta）
│       └── configure.ts                 # 桩掉 configure.getConfig
├── core/components/<Name>/              # 业务组件（与测试同目录）
│   ├── <Name>.vue
│   ├── <Name>.story.ts                  # Story 定义（props 变体）
│   ├── <Name>.spec.ts                   # Vitest 测试
│   └── <Name>.ct.spec.ts                # Playwright CT 测试
├── playwright-ct.config.ts              # CT 专用 Playwright 配置
├── vitest.config.unit.ts                # 最小 Vitest 配置
└── package.json                         # ct:* / test:unit 脚本
```

> 包名 `@newlifex/cube-vue` 为本项目别名，指向 `web/core`，新项目替换为自己的源码别名即可。

---

## 三、关键配置文件（逐文件）

### 3.1 `ct/vite.config.ts` —— CT 专用 Vite

要点：独立 `root` 为 `ct/`，端口固定（如 5190，`strictPort` 避免复用陈旧 server），**别名桩优先于父级别名**。

```ts
import { fileURLToPath, URL } from 'node:url';
import { defineConfig, type Plugin } from 'vite';
import vue from '@vitejs/plugin-vue';
import vueJsx from '@vitejs/plugin-vue-jsx';

// 桩掉框架虚拟模块（由项目插件在运行时生成，CT 环境直接给空实现）
function mockVirtual(): Plugin {
  return {
    name: 'mock-virtual',
    enforce: 'pre',
    resolveId(id) { return id.startsWith('virtual:@your-scope/') ? '\0' + id : null; },
    load(id) { return id.startsWith('\0virtual:@your-scope/') ? 'export default {}' : null; },
  };
}

export default defineConfig({
  root: fileURLToPath(new URL('.', import.meta.url)),
  plugins: [mockVirtual(), vue(), vueJsx()],
  resolve: {
    alias: [
      // ⚠️ 具体路径放前面，优先于父级别名（别名优先纪律，见 §五）
      { find: '@your-scope/core/utils/request', replacement: fileURLToPath(new URL('./mocks/request.ts', import.meta.url)) },
      { find: '@your-scope/core/utils/lov-api', replacement: fileURLToPath(new URL('./mocks/lov-api.ts', import.meta.url)) },
      // 项目源码别名（与 web/vite.config.ts 一致）
      { find: '@your-scope', replacement: fileURLToPath(new URL('../', import.meta.url)) },
    ],
  },
  server: { host: '127.0.0.1', port: 5190, strictPort: true },
});
```

### 3.2 `ct/index.html` + `ct/main.ts` —— Gallery 宿主

- `index.html`：只挂 `<div id="gallery-nav">` 与 `<div id="app">`，引入 `./main.ts`；侧栏样式内联。
- `main.ts`：核心契约 + 路由：
  - 暴露 `window.mountStory(id, props?)` / `setStoryProps(patch)` / `unmountStory()`，供 Playwright 控制挂载。
  - `unmountCurrent()` 清理 `ElOverlay`/`ElDialog__wrapper` 等 Teleport 残留，避免污染下一个 story 截图。
  - **路由**：URL 带 `?story=<id>` → 一步挂载该故事（CT 规范入口，不显示侧栏）；无参数 → 渲染分组侧栏（人类浏览），点击即预览、支持 HMR。两者互斥，CT 永不触发侧栏。
  - 侧栏按 story id 的 `/` 前缀分组（如 `LovSelectTable/…`），故事数 > 25 时分组默认收起。

### 3.3 `ct/stories.ts` —— 显式静态导入

> **不要用 `import.meta.glob` 自动收集**：在 HMR 活跃的 CT Vite 下，glob 会给 story 模块附加 `?t=` 查询串，导致模块图不稳定、偶发漏注册（mountStory 报"未知 story"）。显式导入确定性最高；新增组件只需在此追加一行。

```ts
import { stories as a } from '../core/components/LovSelect/LovSelect.story';
import { stories as b } from '../core/components/LovSelectTable/LovSelectTable.story';

export const stories = [...a, ...b];
```

### 3.4 `ct/mocks/*.ts` —— 统一后端 mock

- `request.ts`：导出 noop 的 `get/post/put/del`（默认 `async () => ({ data: null, code: 0, msg: '' })`），避免拉入 axios 等重依赖链。
- `lov-api.ts`：返回**确定性样本数据**与**确定性 meta**（按 code 前缀返回 LIST 或 ENUM），使组件在关闭态也能渲染真实分支（如 ENUM 选项、已选 label），无需后端。
- `configure.ts`：`getConfig()` 返回最小结构。

> 若组件内部依赖其它后端模块，同样在 `ct/vite.config.ts` 加别名桩指向 `ct/mocks/`。

### 3.5 `playwright-ct.config.ts` —— CT 专用 Playwright

```ts
import { defineConfig, devices } from '@playwright/test';

const fresh = process.env.CT_FRESH === '1';
const port = fresh ? 5193 : 5190;   // CT_FRESH 强制换新端口，绕过陈旧 vite

export default defineConfig({
  testDir: './core/components',
  testMatch: '**/*.ct.spec.ts',
  use: { baseURL: `http://127.0.0.1:${port}`, trace: 'on-first-retry' },
  projects: [{
    name: 'chromium-ct',
    use: {
      ...devices['Desktop Chrome'],
      channel: 'chrome',                                   // 复用本机 Chrome；CI 改 chromium + 装浏览器
      ...(process.env.CT_NO_SANDBOX
        ? { launchOptions: { args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'] } }
        : {}),
    },
  }],
  webServer: {
    command: `node node_modules/vite/bin/vite.js --config ct/vite.config.ts --port ${port}`,
    url: `http://127.0.0.1:${port}/index.html`,
    reuseExistingServer: fresh ? false : !process.env.CI,
    timeout: 180_000,
  },
});
```

### 3.6 `vitest.config.unit.ts` —— 最小 Vitest

仅 `@vitejs/plugin-vue` + `jsdom` + 虚拟模块桩，足以驱动单元/组件测试且不拉起重型框架插件（项目根 `vite.config.ts` 的插件会在 CI/沙箱卡死）。

```ts
import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vitest/config';
import vue from '@vitejs/plugin-vue';

function stubVirtual() {
  const id = 'virtual:@your-scope/config';
  return {
    name: 'stub-virtual',
    resolveId: (s: string) => (s === id ? s : null),
    load: (l: string) => (l === id ? 'export const configData = {}; export const currentEnv = "development";' : null),
  };
}

export default defineConfig({
  plugins: [stubVirtual(), vue()],
  resolve: {
    extensions: ['.mjs', '.js', '.mts', '.ts', '.jsx', '.tsx', '.json', '.vue'],
    alias: { '@your-scope': fileURLToPath(new URL('./', import.meta.url)) },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    include: ['core/__tests__/**/*.{spec,test}.ts', 'core/**/*.{spec,test}.ts'],
    exclude: ['**/node_modules/**', '**/dist/**', 'e2e/**', '**/*.ct.spec.ts', '**/.{idea,git,cache}/**'],
    testTimeout: 20000,
    hookTimeout: 20000,
  },
});
```

> 关键：`exclude` 必须排除 `**/*.ct.spec.ts`，否则 Vitest 会误加载 Playwright 的 `test`，与 Playwright runner 串扰。

---

## 四、package.json 脚本

```json
{
  "ct:server": "vite --config ct/vite.config.ts --port 5190",
  "test:ct": "playwright test --config playwright-ct.config.ts",
  "test:ct:update": "playwright test --config playwright-ct.config.ts --update-snapshots",
  "test:ct:dev": "playwright test --config playwright-ct.config.ts --project=chromium-ct --headed --update-snapshots",
  "test:unit": "vitest run --config vitest.config.unit.ts"
}
```

---

## 五、关键设计纪律（务必遵守，否则踩坑）

| 纪律 | 原因 | 违反后果 |
| --- | --- | --- |
| **别名优先**：具体路径别名放数组前面，优先于父级别名 | Vite 别名是前缀匹配，父级会兜底覆盖具体桩 | 反向则 mock 失效 |
| **组件内部用项目别名导入被 mock 的模块，禁止相对路径** | 相对路径绕过 Vite 别名桩，在 Gallery/CT 打到真实后端 | 组件空白 / 显示原始值（如 ENUM 显示数字而非中文） |
| **`stories.ts` 用显式静态导入，不用 `import.meta.glob`** | glob 在 HMR 下附加 `?t=`，模块图不稳、偶发漏注册 | CT 报"未知 story"的 flaky |
| **CT 永远带 `?story=` 深链** | 无参数会渲染侧栏 | 侧栏污染截图基线 |
| **`strictPort: true`** | 防止复用陈旧 vite 进程（端口被占时旧码生效，改不动） | 改了源码 CT 不更新 |

---

## 六、.gitignore

```gitignore
/ct/verify-shots/          # 类人点击验证截图（瞬态产物，固定输出到 ct/verify-shots/）
/test-results/             # Playwright 失败产物
/playwright-report/        # Playwright 报告
/.playwright-mcp/          # MCP 浏览器会话态
```

---

## 七、浏览器与运行注意事项

- **本地 Windows**：`playwright-ct.config.ts` 用 `channel: 'chrome'`，需本机已装 Google Chrome，`test:ct*` 才能渲染。
- **沙箱 / CI / 无系统 Chrome**：用 Playwright 自带 Chromium——把 `channel` 改为 `chromium` 并 `playwright install chromium`；若 Chrome 自身 sandbox 被拒，设 `CT_NO_SANDBOX=1` 加 `--no-sandbox` 启动参数。
- **陈旧 server 占用端口**：`CT_FRESH=1` 强制换新端口（5193）+ 不复用既有 server，绕开本地可能占用 5190 的旧 vite。

---

## 八、新增一个组件的接入步骤

1. 写 `core/components/<Name>/<Name>.story.ts`，导出 `stories: [{ id: '<Name>/<Variant>', component, props }]`。
2. 在 `ct/stories.ts` **显式追加一行** `import { stories as x } from '../core/components/<Name>/<Name>.story';` 并并入数组。
3. 写 `<Name>.spec.ts`（Vitest）+ `<Name>.ct.spec.ts`（Playwright，用 `?story=` 一步挂载 + 等稳定后截图，见 `ct-gallery.md`）。
4. `pnpm ct:server` 预览 / `pnpm test:ct:dev` 确认 / `pnpm test:ct:update` 锁基线。

---

## 九、与技能其它 references 的关系

- **怎么用这套环境**（gallery 架构、开发循环命令、`?story=` 深链、Story / CT 编写、为何不用 Puppeteer）→ `ct-gallery.md`
- **Vitest 怎么写**（Mock 策略、mount 步骤、别名优先纪律）→ `vitest-strategy.md`
- **类人点击验证怎么驱动浏览器 + 读截图** → `mcp-playwright.md`
- **组件 README 模板（开发前必填 + 闭环沉淀）** → `component-readme-template.md`
