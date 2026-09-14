# 系统模型摘要与维护说明（newlife-cube.summary）

## 结果摘要

魔方是一个**双形态单仓库框架**：一套共享源码编译出两个核心包
（WebAPI 版 `NewLife.Cube` 与 MVC 版 `NewLife.Cube.Core`），
外围挂 16 个主题/皮肤包、3 个宿主应用（两个演示站 + 独立 SSO 服务端），
通过 NuGet + npm 双渠道发布。

最关键的三个架构事实：

1. **共享源码编译（双向 Compile Include）是双形态的根基，也是最大耦合源**——
   任何共享文件变更同时影响两个包，变更影响评估必须同时检查两条编译链。
2. **认证体系自成闭环**：密码/短信/邮箱登录 + 14 个第三方 OAuth + 自身可作
   OAuth2 服务端，令牌统一走 JWT（jti 绑定 UserToken 表，支持精确吊销）。
3. **框架即应用**：核心库自带完整后台区域（Areas/Admin + Areas/Cube），
   集成方"零代码"得到可用后台，定制靠实体控制器继承与主题包替换。

## 下一步动作建议

- 做变更影响分析时，以 [`../dependencies/module-dependencies.dot`](../dependencies/module-dependencies.dot)
  为起点，重点关注共享源码文件。
- 认证/多租户相关改动先读 [`../flows/auth-login-flow.md`](../flows/auth-login-flow.md)。
- React 主题已独立仓库：清理 `魔方3.sln` 与 `CubeDemo.csproj` 中的陈旧引用（技术债）。

## 维护说明

- 本模型是各视图的单一事实来源；仓库结构变化时先更新 `newlife-cube.dsl`，
  再同步对应 evidence 行。
- 验证命令参考：`dotnet sln 魔方.sln list`、`git grep "<Compile Include"`。
- 重新生成日期：2026-09-01；技能：`system-modeler` + `c4model`。
