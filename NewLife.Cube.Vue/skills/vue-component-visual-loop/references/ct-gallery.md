# Playwright CT 截图验证（Gallery 模式）

> 配套 `vue-component-visual-loop` SKILL.md §四。此处放细节，SKILL 只留索引。

## 一、架构：自建 Gallery 模式（轻量 Storybook 替代）

不使用 `@playwright/experimental-ct-vue` 官方包，而是自建一条轻链路：

```
*.story.ts → ct/stories.ts（显式静态导入）→ ct/index.html → Playwright *.ct.spec.ts → 截图对比
```

| 文件                          | 职责                                                         |
| ----------------------------- | ------------------------------------------------------------ |
| `*.story.ts`                  | 声明组件 + props 变体（"在什么状态下长什么样"）              |
| `ct/stories.ts`               | **显式静态导入**所有 `*.story.ts`（不靠 `import.meta.glob`，避免 HMR 下漏注册） |
| `ct/index.html` + `ct/main.ts` | 渲染 gallery，暴露 `window.mountStory()` / `setStoryProps()`；支持 `?story=` 深链与无参数侧栏 |
| `ct/vite.config.ts`           | 独立 Vite 配置（端口 5190），mock 后端 API                   |
| `ct/mocks/`                   | 统一网络层 mock                                              |
| `*.ct.spec.ts`                | Playwright 截图测试                                          |

## 二、开发循环命令（浏览器手动预览，HMR，分钟级）

```bash
# 方式 A：手动浏览器预览（最快，hot reload）—— 视觉快速迭代主战场
pnpm ct:server                    # 启动 gallery（端口 5190，Vite dev server）
# 浏览器打开（二选一）：
#   1) 无参数 → 故事侧栏，点击即预览（人类浏览默认入口）
#      根目录 / 默认即渲染 index.html（gallery 宿主），直接开 http://127.0.0.1:5190/ 即可，无需 /index.html
#      http://127.0.0.1:5190/index.html
#   2) ?story=<id> 深链 → 直接挂载单个故事、不显示侧栏（CT 也走这条）
#      http://127.0.0.1:5190/index.html?story=<ComponentName>/<Variant>
# 改 .vue 后 Vite HMR 自动刷新，无需重启 server

# 方式 B：有头截图存档（不对比，永不失败）
pnpm test:ct:dev                  # 自动启动 gallery → 截图 → 存盘

# 方式 C：无头回归对比（需要基线已确立）
pnpm test:ct                      # 截图 vs 基线，像素级 diff

# 方式 D：更新基线
pnpm test:ct:update               # 确认效果后，覆盖旧基线
```

> `?story=<id>` 是 **URL 查询参数，不是 server CLI 参数**：只能出现在浏览器打开的 URL 里；`ct:server` 只负责起开发服务器，不懂"故事"。
> `?story=` 与「无参数」互斥：无参数 → 渲染故事侧栏；带 `?story=` → 只渲染那一个组件。两者都不会白屏——CT 永远带 `?story=`，侧栏不会污染 CT 基线。

## 三、编写 Story

```ts
// core/components/<ComponentName>/<ComponentName>.story.ts
import Comp from './index.vue';

const base = { /* 该组件典型 props 组合 */ };

export const stories = [
  { id: '<ComponentName>/Open',     component: Comp, props: { ...base } },
  { id: '<ComponentName>/Echo',     component: Comp, props: { ...base, modelValue: /* 已选值 */ } },
];
```

Story 定义"组件在什么 props 组合下应该长什么样"，`ct/stories.ts` 显式静态导入后自动收集。

> 约定：每个 story 加 `// 测试状态：` 注释，说明该变体被哪个 `*.ct.spec.ts` 覆盖、断言什么，便于功能↔Story↔测试一一对应。

## 四、编写 CT 截图测试（?story= 深链一步挂载）

```ts
import { test, expect } from '@playwright/test';

const GALLERY = process.env.CT_BASE_URL || 'http://127.0.0.1:5190/index.html';

async function openStory(page: import('@playwright/test').Page, id: string) {
  // ?story=<id> 深链一步挂载：main.ts 加载时即 mountStory(id)，无需再 page.evaluate(mountStory)
  await page.goto(`${GALLERY}?story=${encodeURIComponent(id)}`);
  await page.waitForSelector('<目标选择器>');
  // 等挂载 + 开场动画收尾，再断言/截图，避免像素抖动导致基线 diff 不稳定
  await page.waitForFunction(() => {
    const d = document.querySelector('<弹窗/容器选择器>');
    return !!d && getComputedStyle(d).visibility === 'visible';
  });
  await page.waitForTimeout(400);
}

test('打开弹窗', async ({ page }) => {
  await openStory(page, '<ComponentName>/Open');
  await expect(page.locator('<弹窗选择器>')).toHaveScreenshot();
});
```

**关键纪律（从"视觉快速迭代闭环"借来的最佳实践）**：

- **挂载即驱动**：优先用 `?story=` 深链让 `main.ts` 在加载时完成挂载，不要再用 `page.evaluate(window.mountStory)` 手动驱动——少一步、少一处时序坑。
- **断言/截图前必须等稳定**：`waitForSelector` → `waitForFunction`（等 transition 收尾，`visibility: visible`）→ `waitForTimeout(400)` 兜底。UI 库弹窗多有 transition，不等就截会像素抖动、基线频繁失败。
- **选择器型组件不自动开弹窗**：`?story=` 仅对表格型前缀自动开弹窗；选择器型故事需测试显式点击 `.el-select` 展开 / 点 append 按钮开弹窗，以截图中间态。

## 五、新增组件 CT 三步

1. 写 `*.story.ts`：声明 props 变体；
2. 写 `*.ct.spec.ts`：用 `?story=` 深链 `openStory` + 截图断言；
3. 运行 `pnpm test:ct:dev` 看效果 → 满意后 `pnpm test:ct:update` 确立基线。

## 六、为何不用 Puppeteer 兜底方案（已废弃）

早期存在「独立 HTML + vendored UMD 库 + 代理 + Puppeteer 截图 + 真实后端」的兜底渲染方案，已被 Gallery 模式取代：

- Gallery 通过 Vite **直接渲染真实 `.vue` 组件**（含 `<script setup>`、`scoped` 样式、真实 UI 库），无需手搓演示页；
- 网络层用 `ct/mocks/` 统一 mock，**不需要真实后端**；
- 预览与测试**共用同一套 story / 同一份 Vite 配置**，不存在"演示页和真实组件两套实现"的漂移；
- 手动预览走 `pnpm ct:server` + `?story=` / 无参数侧栏，自带 HMR，比 Puppeteer 手搓快得多。

> 结论：不需要 Puppeteer 兜底装置。若某天 SPA 完全起不来，正确做法是修构建/补插件，而不是退回手搓演示页。
