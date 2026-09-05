using Xunit;

// XCode 实体通过全局逻辑连接（Membership/Log）访问数据库，SqliteDb 集合与其它集合并行执行时会竞争连接注册表与表结构缓存，
// 导致偶发 no such table（如 Role/NotificationRecord）。禁用测试并行，换取确定性（DB 用例串行初始化连接与建表）。
[assembly: CollectionBehavior(DisableTestParallelization = true)]
