# MCP Playwright：用 AI 工具直接驱动浏览器做类人验证

`@playwright/mcp` 把 Playwright 的能力以 **MCP 工具** 形式暴露给 AI 智能体：智能体可以像人一样"打开网页 → 点击 → 输入 → 截图 → 读截图判断"，而不是只能跑脚本。它弥补了纯脚本断言和 Playwright CT（只做像素比对）的不足——**让智能体自己观察页面、读截图、判断功能是否真的可用**。

## 一、配置 MCP Playwright（按所用环境）

在你的 MCP 客户端里注册一个 stdio 类型的 `playwright` server 即可，命令为：

```json
{
  "playwright": {
    "type": "stdio",
    "command": "npx",
    "args": ["-y", "@playwright/mcp"]
  }
}
```

> 具体注册位置与启用方式取决于你使用的环境/客户端（全局 MCP 配置或项目级配置均可）。首次运行 `npx -y @playwright/mcp` 会下载包并拉起浏览器。
> 浏览器因沙箱策略起不来时，可在启动环境追加 `PLAYWRIGHT_CHROMIUM_ARGS=--no-sandbox`（或在 server 的 `env` 里追加该变量）。

## 二、前置：先起 gallery（ct:server）

MCP Playwright 只负责"开浏览器 + 操控"，gallery 仍由 Vite 提供：

```bash
cd <web-root>
pnpm ct:server          # 端口 5190，HMR
```

然后让智能体把浏览器导航到：

- 单故事深链（推荐，一步挂载）：`http://127.0.0.1:5190/index.html?story=<ComponentName>/<Variant>`
- 侧栏浏览：`http://127.0.0.1:5190/index.html`

## 三、智能体操作清单（类人验证）

智能体借助 MCP Playwright 工具，按组件 README「验证流程」表逐项走：

| 步骤 | MCP 工具（含义） | 对应验证 |
|---|---|---|
| 打开页面 | `navigate` / `open_page` 到 `?story=<id>` | 单故事挂载 |
| 等元素稳定 | `wait_for_selector`（弹窗 / 表格行） | 避免动画抖动误判 |
| 点击 | `click`（复选框 / 翻页按钮 / 下拉） | 勾选 / 翻页 / 展开 |
| 读文字 | `get_text`（计数 / 标签节点） | 验证统计数、回显文本正确 |
| 截图 | `screenshot` 存盘 | 事后读图核对视觉 |
| 读截图 | 用图片读取工具打开截图 png | **核对像素层视觉（布局/颜色/字体）** |

**核对要点（通用）**：

- 打开弹窗后计数节点显示"已选 N 项"，N 应等于跨页去重后的真实已选数量（不被当前页勾选裁小）；
- 翻页后统计与已选行保持、翻回保持（跨页 `reserve-selection`）；
- ENUM / 字典类字段应显示**描述文本**而非原始数字值；LIST 回显只读输入框应有文本；
- 一切以"截图 + 文字断言"双重确认，不只信 DOM。

## 四、与脚本验证的关系

- **脚本方式**：可编写一个按 README「验证流程」表逐项 `click` / 翻页 / `toHaveText` / `toHaveCount` 断言 + 截图的 Playwright 脚本，确定性高、可 CI；
- **MCP 智能体方式**：更灵活，能读截图、能做"人脑级"判断，需要 MCP 已注册并启用；
- 两者都复用同一套 `?story=` 深链 + `ct/mocks/` 数据，环境一致。

> **截图输出路径固定约定**：无论脚本方式还是 MCP 方式，截图一律写入 **`ct/verify-shots/`**（与 `ct/` 同目录），不要散落到桌面 / 临时目录等其他位置；该目录通常被 `.gitignore` 忽略，属瞬态产物。
