using Xunit;

// 数据库测试类通过 DAL.AddConnStr("Cube", ...) 注入独立 SQLite 内存库，
// 并行执行会互相覆盖同一连接名导致数据错乱（OSC-0016 曾复现 ViewProfile 用例失败），
// 故程序集级禁用并行，测试串行执行。
[assembly: CollectionBehavior(DisableTestParallelization = true)]
