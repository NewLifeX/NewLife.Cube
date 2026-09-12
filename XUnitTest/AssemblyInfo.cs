using Xunit;

// 程序集级禁用并行，测试串行执行，原因有两条：
// 1. 数据库测试类通过 DAL.AddConnStr("Cube", ...) 注入独立 SQLite 内存库，并行执行会互相覆盖同一连接名导致数据错乱（OSC-0016 曾复现 ViewProfile 用例失败）；
// 2. XCode 实体通过全局逻辑连接（Membership/Log）访问数据库，SqliteDb 集合与其它集合并行执行时会竞争连接注册表与表结构缓存，导致偶发 no such table（如 Role/NotificationRecord）。
[assembly: CollectionBehavior(DisableTestParallelization = true)]
