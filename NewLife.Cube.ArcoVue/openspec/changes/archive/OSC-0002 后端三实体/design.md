# OSC-0002 Design

## 技术方案

### 1. 建模（Cube.xml）

在 `<Tables>` 末尾追加三表，沿用文件级 `Option`（`Namespace=NewLife.Cube.Entity`、`ConnName=Cube`、`ModelClass={name}Model`、中文文件名等）。

| Table | 关键列 | 索引 |
|-------|--------|------|
| **UserProfile** | Id (Identity PK)；UserId；LayoutJson / ThemeJson / WorkspaceJson（Text/String 长文本）；Version；Enable；Create*/Update* | **Unique(UserId)** |
| **EntityViewProfile** | Id；UserId；TypePath；View；ColumnsJson；GanttJson；CardJson；FiltersJson；Version；Create*/Update* | **Unique(UserId, TypePath)** |
| **EntityComment** | Id；Category；LinkId；**ParentId / RootId / ReplyUserId / ReplyUser**（同表回复，不另建表）；Content；CreateUser/CreateUserID/CreateIP/CreateTime；Update* | Index**(Category, LinkId)**；ParentId；RootId；CreateUserID |

JSON 列存嵌套配置；API 层反序列化为迁移方案 §5.2 TypeScript 形状（`layout` / `theme` / `workspace` / `columns` 等）。列名与长度实施时按 XCode 惯例微调，以 xml 为准。

生成顺序（tasks 首段强制）：

1. 编辑 Cube.xml  
2. 运行 **xcode** 生成（instructions/Agent）  
3. 核对生成实体/Model  
4. 再写 API 与测试  

### 2. API（前端契约）

路由对齐迁移方案 §5.3（`[controller]/[action]` → `/Cube/{action}`）：

```
GET    /Cube/UserProfile
PUT    /Cube/UserProfile

GET    /Cube/EntityViewProfile?typePath=
PUT    /Cube/EntityViewProfile
DELETE /Cube/EntityViewProfile?typePath=

GET    /Cube/EntityComment?category=&linkId=&parentId=
POST   /Cube/EntityComment               # body.parentId>0 为回复
DELETE /Cube/EntityComment?id=
```

**语义约束：**

- 一律需登录（沿用 `CubeController` Token/登录校验）；未登录 → 401 JSON。
- UserProfile / EntityViewProfile：**仅操作当前用户**行；PUT 为 upsert；忽略或覆盖 body 中他人 UserId。
- EntityViewProfile：`typePath` 必填；DELETE 删除当前用户该 typePath 配置（前端回落默认视图）。
- EntityComment：GET 按 category+linkId（可选 parentId：-1 全部 / 0 顶层 / >0 直接回复）；POST 绑定当前用户为作者，`ParentId` 指向父评论时写入 RootId/ReplyUser*；DELETE 仅本人或管理员（不级联删子回复）。
- 响应体与现有 Cube JSON 封装风格一致（参考 `Auth` / `Cube` 其它 action）。

**可选：** Area 下 `EntityController` 管理页——**本 OSC 非必须**；若加，不得替代上述个人 API，且勿在菜单默认暴露敏感全表浏览（或仅超管）。默认优先只做个人 API。

### 3. 测试设计

| 用例组 | 断言要点 | 落点 |
|--------|----------|------|
| 鉴权 | 无 Token 访问 Profile/Comment → 401 | XUnitTest |
| UserProfile | GET 空则默认或空对象；PUT 后 GET 一致；Unique(UserId) | 同上 |
| EntityViewProfile | 不同 typePath 隔离；DELETE 后 GET 无数据/空 | 同上 |
| EntityComment | 同 category+linkId 列表与同表回复；删他人失败（非管理员） | 同上 |
| 构建 | NewLife.Cube + XUnitTest 编译成功 | `dotnet build` |

测试可参考现有 `AuthControllerTests` / API 测试宿主模式；实体层可用内存/测试库按项目惯例。

## 规格与界面

- ui/：无（纯后端）

## 核心文档影响（必填）

| 文档路径 | 影响类型 | 说明 |
|----------|----------|------|
| Doc/Api/核心接口架构.md | 修改 | §2.2 高级接口增加 UserProfile / EntityViewProfile / EntityComment |
| Doc/功能清单.md | 修改 | 新增或标注三实体相关编码；测试列随 XUnit 回写 |
| Doc/Api/ArcoVue企业中后台迁移方案.md | 核对 | 路径/字段若实施微调则回写 §5.2.1 |
| NewLife.Cube.ArcoVue/web/** | 无 | 本变更不含前端 |
| Doc/Api/内置前端皮肤.md | 可选 | 一句说明后端 Profile/Comment 已就绪 |

## 风险与注意

- `[Cc]onfig/` gitignore：实体输出勿误入被忽略目录。
- JSON 列长度需够用（列布局等）；过短会导致保存失败。
- 与前端并行：契约以本 design + 核心接口架构为准；破坏性改名须同步 OSC-0004+。
