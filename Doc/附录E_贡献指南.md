# 附录E 贡献指南

> 本附录介绍如何参与魔方开源项目的贡献。

---

## E.1 代码规范

### 基础规范

| 项目 | 规范 |
|------|------|
| 语言版本 | `<LangVersion>latest</LangVersion>` |
| 命名空间 | file-scoped namespace |
| 类型名 | 使用 .NET 正式名（`String`/`Int32`/`Boolean`） |
| 缩进 | 4 空格 |
| 编码 | UTF-8（无 BOM） |

### 命名规范

| 成员类型 | 命名规则 | 示例 |
|---------|---------|------|
| 类型/公共成员 | PascalCase | `UserService`、`GetName()` |
| 参数/局部变量 | camelCase | `userName`、`count` |
| 私有字段 | `_camelCase` | `_cache`、`_instance` |
| 常量 | PascalCase | `MaxRetryCount` |
| 接口 | `I` 前缀 | `IUserService` |

### 代码风格

```csharp
// ? 单行 if
if (value == null) return;
if (key == null) throw new ArgumentNullException(nameof(key));

// ? 多行需要花括号
if (count > 0)
{
    DoSomething();
    DoOther();
}

// ? 循环必须花括号
foreach (var item in list)
{
    Process(item);
}
```

### 文档注释

```csharp
/// <summary>获取用户信息</summary>
/// <param name="id">用户编号</param>
/// <returns>用户对象，不存在时返回 null</returns>
public User GetUser(Int32 id)
{
    // ...
}
```

---

## E.2 提交 PR 流程

### 1. Fork 仓库

在 GitHub/Gitee 上 Fork 魔方仓库到自己的账号下。

### 2. Clone 到本地

```bash
git clone https://github.com/你的用户名/NewLife.Cube.git
cd NewLife.Cube
```

### 3. 创建分支

```bash
# 从 master 创建功能分支
git checkout -b feature/your-feature-name

# 或修复分支
git checkout -b fix/issue-description
```

### 4. 进行修改

- 遵循代码规范
- 添加必要的测试
- 更新相关文档

### 5. 提交代码

```bash
# 添加修改
git add .

# 提交（使用规范的提交信息）
git commit -m "feat: 添加新功能描述"
```

### 提交信息规范

```
<type>: <subject>

<body>

<footer>
```

| 类型 | 说明 |
|------|------|
| `feat` | 新功能 |
| `fix` | Bug 修复 |
| `docs` | 文档更新 |
| `style` | 代码格式（不影响功能） |
| `refactor` | 重构（不是新功能也不是修复） |
| `perf` | 性能优化 |
| `test` | 测试相关 |
| `chore` | 构建/工具相关 |

示例：

```
feat: 添加多租户数据过滤功能

- 新增 TenantFilter 中间件
- 实体自动添加租户条件
- 支持配置租户字段名

Closes #123
```

### 6. 推送分支

```bash
git push origin feature/your-feature-name
```

### 7. 创建 Pull Request

在 GitHub/Gitee 上创建 PR，填写：

- **标题**：简洁描述改动
- **描述**：详细说明改动内容、原因、影响
- **关联 Issue**：如果有相关 Issue

### 8. 等待 Review

- 响应 Review 意见
- 必要时进行修改
- 合并后删除分支

---

## E.3 Issue 模板

### Bug 报告

```markdown
### 问题描述
简洁描述遇到的问题

### 复现步骤
1. 执行操作 A
2. 点击按钮 B
3. 查看结果 C

### 期望行为
描述期望的正确行为

### 实际行为
描述实际发生的错误行为

### 环境信息
- 操作系统：Windows 11 / Ubuntu 22.04 / macOS 14
- .NET 版本：.NET 8.0
- 魔方版本：6.2.0
- 数据库：MySQL 8.0 / SQLite

### 错误日志
```
粘贴相关错误日志
```

### 截图
如果适用，添加截图帮助解释问题
```

### 功能请求

```markdown
### 功能描述
简洁描述希望添加的功能

### 使用场景
描述这个功能的使用场景和价值

### 建议实现方式
如果有想法，描述可能的实现方式

### 替代方案
描述已经考虑过的替代方案

### 附加信息
任何其他相关信息
```

---

## E.4 开发环境搭建

### 必要工具

| 工具 | 版本 | 说明 |
|------|------|------|
| Visual Studio | 2022+ | 推荐，完整支持 |
| VS Code | 最新 | 轻量替代 |
| Rider | 最新 | JetBrains IDE |
| .NET SDK | 8.0+ | 必需 |
| Git | 最新 | 版本控制 |

### 克隆代码

```bash
# 克隆主仓库
git clone https://github.com/NewLifeX/NewLife.Cube.git

# 进入目录
cd NewLife.Cube

# 还原依赖
dotnet restore

# 构建
dotnet build
```

### 运行测试

```bash
# 运行所有测试
dotnet test

# 运行特定项目测试
dotnet test XUnitTest
```

### 本地运行

```bash
# 运行示例项目
cd CubeDemo
dotnet run
```

---

## E.5 项目结构

```
NewLife.Cube/
├── NewLife.Cube/              # WebAPI 版核心库
│   ├── Common/                # 通用组件
│   ├── Controllers/           # 控制器基类
│   ├── Entity/                # 实体类
│   ├── Extensions/            # 扩展方法
│   ├── Filters/               # 过滤器
│   ├── Services/              # 服务类
│   └── NewLife.Cube.csproj
│
├── NewLife.CubeNC/            # MVC 版核心库
│   ├── Areas/                 # 区域
│   ├── Controllers/           # 控制器
│   ├── Views/                 # 视图
│   └── NewLife.CubeNC.csproj
│
├── NewLife.Cube.AdminLTE/     # AdminLTE 皮肤
├── NewLife.Cube.Metronic/     # Metronic 皮肤
├── NewLife.Cube.LayuiAdmin/   # LayuiAdmin 皮肤
├── NewLife.Cube.Tabler/       # Tabler 皮肤
│
├── CubeDemo/                  # WebAPI 版示例
├── CubeSSO/                   # MVC 版示例（SSO 服务端）
│
├── XUnitTest/                 # 单元测试
│
├── Doc/                       # 文档目录
│
└── NewLife.Cube.sln           # 解决方案文件
```

---

## E.6 贡献类型

### 代码贡献

- 修复 Bug
- 添加新功能
- 优化性能
- 重构代码

### 文档贡献

- 完善现有文档
- 添加使用示例
- 翻译文档
- 撰写教程

### 测试贡献

- 添加单元测试
- 添加集成测试
- 报告测试问题

### 社区贡献

- 回答问题
- 分享使用经验
- 推广项目

---

## E.7 行为准则

### 我们的承诺

- 创造友好、包容的社区环境
- 尊重所有贡献者
- 建设性地处理不同意见

### 我们的标准

? **鼓励**：
- 使用友好和包容的语言
- 尊重不同观点和经验
- 优雅地接受建设性批评
- 关注对社区最有利的事情

? **不允许**：
- 使用带有性别、种族歧视的语言
- 发表攻击性或侮辱性评论
- 骚扰行为
- 未经同意发布他人隐私信息

---

## E.8 获取帮助

### 沟通渠道

| 渠道 | 用途 |
|------|------|
| GitHub Issues | Bug 报告、功能请求 |
| GitHub Discussions | 讨论、问答 |
| QQ 群 | 即时交流 |
| 邮件列表 | 正式沟通 |

### 相关链接

- **GitHub**：https://github.com/NewLifeX/NewLife.Cube
- **Gitee**：https://gitee.com/NewLifeX/NewLife.Cube
- **官网**：https://newlifex.com
- **文档**：https://newlifex.com/cube

---

## E.9 致谢

感谢所有为魔方项目做出贡献的开发者！

无论是代码、文档、测试还是反馈，每一份贡献都让魔方变得更好。

### 核心贡献者

- **Stone（大石头）** - 项目创始人
- 以及所有提交过 PR 的贡献者

### 特别感谢

- 使用魔方并提供反馈的所有用户
- 在社区帮助回答问题的热心开发者
- 撰写教程和分享经验的作者

---

## 本附录小结

本附录介绍了参与魔方开源贡献的完整指南：

1. 代码规范要求
2. PR 提交流程
3. Issue 模板
4. 开发环境搭建
5. 项目结构说明
6. 贡献类型
7. 行为准则
8. 获取帮助渠道

欢迎所有人参与魔方的建设，让我们一起打造更好的 .NET 快速开发平台！

---

**魔方手册完结**

感谢阅读《魔方 NewLife.Cube 手册》，希望本手册能帮助您快速上手魔方，构建出优秀的应用系统。

如有问题或建议，欢迎通过 GitHub Issues 或社区渠道反馈。
