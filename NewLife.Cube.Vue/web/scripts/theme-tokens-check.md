# 主题 Token 检查（theme-tokens-check）

一组挂在 `cube-vue` 前端（`@newlifex/cube-vue`）构建/安装前的代码规范校验脚本，
用来防止 `core` 目录里出现**乱写的 CSS 自定义属性**和**硬编码颜色**，保证整套前端主题可换肤、可统一维护。

> 适用目录：`NewLife.Cube.Vue/web`
> 运行环境：Node ≥ 24（`scripts/` 使用 ESM + 正则后顾特性）

---

## 一、用途：它查什么

脚本扫描 `core` 下所有 `.vue` / `.ts` / `.scss` / `.css` 文件，提取每一个 CSS 自定义属性（用法 `var(--x)` 与定义 `--x:`），按 4 条规则检查：

| # | 规则 | 命中即报错 | 说明 |
|---|------|-----------|------|
| 1 | `--el-` 开头的 token 必须真实存在 | `未知的 Element token` | Element Plus 的 CSS 变量（如 `--el-color-primary`）。拼写错、或引用了不存在的档位（如 `--el-border-radius-large`）会被抓出。 |
| 2 | 非 `--el-` 的自定义 token 必须在白名单 | `未授权的自定义 token` | 项目设计系统用的自定义 token，必须登记在 `custom-tokens-allow.json`（命名空间或独立 token），否则视为"未审批的随意 token"——**这样才能及时发现新增的脏写法**。 |
| 3 | 硬编码 16 进制颜色 | `硬编码颜色` | `#rgb` / `#rrggbb` / `#rrggbbaa`（3/4/6/8 位）。 |
| 4 | 硬编码 `rgb()` / `rgba()` | `硬编码颜色` | 直接写死的颜色值。 |
| 4b | 硬编码 **CSS 命名颜色**（如 `red` / `blue`） | `硬编码颜色` | 同样属于硬编码颜色，必须走主题变量；`var(--x, red)` 兜底里的命名色也会被捕获。 |

**合法来源会被放行**（不误报）：
- `var(--el-*)`、已登记的自定义 token（规则 1/2 的合规写法）
- `transparent`、`currentColor`
- `<template>` 标签（Vue 模板防误伤）
- HTML 数字实体 `&#128269;`（emoji 等）、SCSS 插值 `#{ $x }`
- 注释（`//` 与 `/* */`）以及 `<script>` 块内的字符串/类型联合（硬编码颜色只在 `.css/.scss` 全文和 `.vue` 的 `<style>` 块内检查）

---

## 二、文件清单

| 文件 | 作用 |
|------|------|
| `scripts/check-theme-tokens.mjs` | **核心检查器**。导入下面两个数据文件做校验，可传一个可选路径参数（目录或文件）只检查指定范围。 |
| `scripts/gen-element-tokens.mjs` | 从已安装的 `node_modules/element-plus`（`theme-chalk` + `dist`）提取全部合法 `--el-*` token，生成 `element-tokens.json`。 |
| `scripts/element-tokens.json` | **生成物**（≈568 个）。Element token 列表，**通过相对路径导入，不硬编码在脚本里**。建议提交进仓库，保证 CI 开箱即用。 |
| `scripts/custom-tokens-allow.json` | 项目已批准的设计系统自定义 token 白名单（`--cube-layout-`、`--navbar-` 两个命名空间 + 几个独立 token）。 |
| `scripts/theme-check.mjs` | **入口脚本**：依次执行「生成 token → 执行检查」，透传退出码。供 `postinstall` / CI 用。 |
| `scripts/fixtures/theme-tokens-anti-patterns.vue` | **反模式测试文件**：故意写入各类违规写法 + 控制组合法写法，用于回归验证检查器灵敏度（见第四节）。 |

---

## 三、用法

```bash
# 1. 常规主题检查（扫 core，已接入 `npm run check`）
npm run check:theme
#   等价于：node scripts/check-theme-tokens.mjs

# 2. 升级 element-plus 后，重新生成 token 列表（务必在升级后跑一次）
npm run check:theme:gen
#   等价于：node scripts/gen-element-tokens.mjs

# 3. 安装后自动触发（见 package.json 的 postinstall）
#   "@newlifex/cube-vue 安装完成 → 自动 生成token + 跑一次 check"
npm run check:theme:refresh
#   等价于：node scripts/theme-check.mjs
#   说明：这是 postinstall 调用的同一条命令，先 gen 再 check，一步到位、命令不冗长。

# 4. 回归验证 / 演示（跑反模式测试文件，预期捕获全部违规）
npm run check:theme:demo
#   等价于：node scripts/check-theme-tokens.mjs scripts/fixtures/theme-tokens-anti-patterns.vue

# 5. 只检查指定路径（目录或单个文件均可）
node scripts/check-theme-tokens.mjs path/to/some.vue
```

### 与 CI 的关系

`package.json` 里：
```json
"check": "pnpm run type-check && pnpm run check:theme && pnpm run lint:eslint",
"postinstall": "node scripts/theme-check.mjs"
```

- 本地/CI 跑 `npm run check` 会包含主题检查。
- **注意副作用**：`postinstall` 会跑「gen + check」，若 `core` 里存在违规（规则 1–4 命中），`pnpm install` 将以退出码 1 失败。请先保证 `check:theme` 为绿色再安装；或把 `postinstall` 改为只 `check:theme:gen` 以避免安装中断。

### 退出码

- 全部通过：`0`，并打印 `Theme token check: OK`。
- 有违规：`1`，把所有 `相对路径:行号: 原因 -> 那行代码` 打印到 stderr。

---

## 四、验证方式（反模式测试文件）

`scripts/fixtures/theme-tokens-anti-patterns.vue` 是**专用的回归测试样本**：里面故意写入各类反模式，同时保留一批"控制组合法写法"用来确认无误报。

### 运行

```bash
npm run check:theme:demo
# 预期：退出码 1，报告 21 处问题（4 未知 Element + 5 未授权自定义 + 12 硬编码），且控制组 0 误报。
```

### 覆盖的反模式（均会被捕获）

| 类别 | 写法示例 | 报告 |
|------|----------|------|
| A. 不存在的 `--el-` token | `var(--el-border-radius-large, 16px)`、`var(--el-font-family-mono)`、`var(--el-border-radius-sm)`、`var(--el-color-primary-invalid)` | 未知的 Element token ×4 |
| B. 未授权的自定义 token | `var(--my-random-token)`、`var(--random-color)`、`var(--bad-token)` | 未授权的自定义 token ×3 |
| C. 硬编码 16 进制 | `#ff0000` / `#fff` / `#1a2b3c4d`（8 位）/ `#abc` | 硬编码颜色 ×4 |
| D. 硬编码 rgb/rgba | `rgb(255,0,0)` / `rgba(0,0,0,.5)` | 硬编码颜色 ×2 |
| E. **硬编码命名颜色** | `color: red;` / `border: 1px solid blue;` / `background: green;` | 硬编码颜色 ×3 |
| F. `var()` 兜底色值（不管是否兜底都捕获） | `var(--maybe-token, #ffffff)`、`var(--maybe-token-2, red)`、`var(--el-color-primary, #fff)` | 硬编码颜色 ×3 |

> 计数说明：F 类的 `fallback-hex`、`fallback-named` 两行里还各含一个未登记自定义 token（`--maybe-token`、`--maybe-token-2`），
> 因此"未授权的自定义 token"实际共 **5** 处（B 的 3 + F 的 2）；"硬编码颜色"共 **12** 处（C4 + D2 + E3 + F3）。
> 合计 4 + 5 + 12 = **21** 处。

### 命名颜色反模式（重点）

命名颜色（如 `red`、`blue`、`green`）**属于硬编码颜色，必须报错**，不能写死在样式里：

```css
/* ❌ 反模式 E：命名颜色，检查器报 "硬编码颜色" */
.hard-named-1 { color: red; }
.hard-named-2 { border: 1px solid blue; }
.hard-named-3 { background: green; }

/* ❌ 兜底里的命名色同样会被捕获（F 类） */
.fallback-named { color: var(--maybe-token-2, red); }

/* ✅ 正确写法：改用 Element 或自定义 token */
.ok-1 { color: var(--el-color-danger); }      /* Element 语义色 */
.ok-2 { color: var(--cube-color-brand); }     /* 自定义 token，需在白名单登记 */
```

检查器内置约 148 个标准 CSS 命名颜色，并通过前后顾正则 `(?<![\w-])(red|blue|...)(?![\w-])` 避免误伤
`white-space`、`card-red`、`--el-color-red` 这类标识符；同时只在 `<style>`/样式文件内检查，
避免 `<script>` 里的注释、`'blue'` 字符串字面量、`variant?: 'green'` 类型联合被误判。

### 控制组（确认无误报）

以下写法在该 fixture 中**不应**被报告，用来证明检查器不会误伤合法来源：
- `var(--el-color-primary)`、`var(--el-border-radius-small)`（真实存在的 Element token）
- `--cube-layout-*`、`--navbar-*` 命名空间 token，以及 `--radius-md` / `--shadow-lg` / `--ease` / `--layout-nav-height` 独立 token（已在白名单）
- `color-mix(in srgb, var(--el-color-primary), transparent)`、`transparent`、`currentColor`
- HTML 实体 `&#128269;`、SCSS 插值 `#{ $x }`

---

## 五、如何扩展 / 维护

### 新增一个项目自定义 token
先在 `scripts/custom-tokens-allow.json` 登记（命名空间或独立 token），否则检查会报错——**这正是"及时发现未审批 token"的设计目的**。
```json
{
  "prefixes": ["--cube-layout-", "--navbar-", "--cube-"],
  "tokens": ["--radius-md", "--shadow-lg", "--ease", "--layout-nav-height"]
}
```

### 升级 element-plus
运行 `npm run check:theme:gen` 重新提取 `element-tokens.json`（会同步新增/下架的 `--el-` token），然后提交该文件。

### 增加新的检查目标 / 忽略项
- 检查范围：默认扫 `core`；改 `targets` 或传路径参数。
- 忽略文件：编辑脚本顶部的 `ignore` 正则数组（如主题 CSS 本身、登录页、`defaultConfig/index.ts`、`initApp.ts` 等）。

---

## 六、已知局限（按设计不检查，非脚本缺陷）

- **`hsl()` 颜色**（如 `hsl(0,100%,50%)`）不在检查范围内——刻意保留，避免范围膨胀。如需要可后续补充。
- 注释与 `<script>` 内的颜色已正确忽略；但**行内真实颜色写在 `//` 注释之前的代码**仍会被检查（符合预期）。
- `var(--x, #fff)` 兜底色值**会被当硬编码捕获**（设计如此，确保兜底字面量不漏网）。

---

## 七、一键自查清单

```bash
cd NewLife.Cube.Vue/web
npm run check:theme          # 日常检查（应绿）
npm run check:theme:demo     # 回归验证（应红，全捕获）
npm run check:theme:gen      # 升级 element-plus 后重生成列表
npm run check:theme:refresh  # postinstall 同款：生成+检查一步完成
```
