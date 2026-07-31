# OSC-0002 Tasks

- [x] 编辑 `NewLife.Cube/Entity/Cube.xml`：追加 UserProfile、EntityViewProfile、EntityComment（含唯一/查询索引）
- [x] 按 **xcode** 指令/Agent 生成实体与 Model；核对与 xml 一致、无大段手写实体骨架
- [x] 实现 `/Cube/UserProfile` GET/PUT（当前用户 upsert）
- [x] 实现 `/Cube/EntityViewProfile` GET/PUT/DELETE（typePath）
- [x] 实现 `/Cube/EntityComment` GET/POST/DELETE（含同表回复字段 ParentId/RootId/ReplyUser*）
- [x] **补测：** 新增 XUnit（鉴权语义、Profile 读写与用户绑定、ViewProfile、Comment category+linkId **与回复**）
- [x] **跑测：** `dotnet test --filter ProfileCommentEntityTests` — 4 passed
- [x] **构建：** `dotnet build` NewLife.Cube / NewLife.CubeNC net10.0 无错误
- [x] 更新 `Doc/Api/核心接口架构.md` 高级接口表
- [x] 更新 `Doc/功能清单.md`（SPA-17/18/19 + 测试列）
- [x] CubeNC 同步 API；csproj 链接新实体与 Model
