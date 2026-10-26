using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using XCode;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Tests.Common;

/// <summary>
/// 测试用连接作用域。XCode 的连接串、DAL 实例与实体会话里的 Dal 都是进程级缓存，
/// 多个测试夹具把 Cube/Log 指向各自临时库后，缓存不会失效，导致后续用例写入错误的库（no such table）。
/// 本工具在切换连接串时同步清理这些缓存，并在作用域结束时还原连接串并再次清理。
/// </summary>
internal static class TestConnections
{
    private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.NonPublic;
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>切换连接串并在作用域内生效，结束时还原</summary>
    /// <param name="name">连接名</param>
    /// <param name="connStr">连接字符串</param>
    /// <returns>用于还原的作用域</returns>
    public static IDisposable Use(String name, String connStr)
    {
        DAL.ConnStrs.TryGetValue(name, out var old);

        // MapTo 映射按 XCode 约定直接写 ConnStrs；真实连接串走 AddConnStr，让连接信息缓存同步更新
        if (connStr.StartsWith("MapTo=", StringComparison.OrdinalIgnoreCase))
            DAL.ConnStrs[name] = connStr;
        else
            DAL.AddConnStr(name, connStr, null, null);

        ResetCaches();

        return new Scope(name, old);
    }

    /// <summary>清理 DAL 实例缓存与实体会话中缓存的 Dal，使新的连接映射立即生效</summary>
    public static void ResetCaches()
    {
        // DAL.Create 的全局实例缓存
        if (typeof(DAL).GetField("_dals", StaticFlags)?.GetValue(null) is IDictionary dals) dals.Clear();

        // 实体会话缓存：EntitySession<T>.Dal => _Dal ??= DAL.Create(ConnName)
        foreach (var factory in EntityFactory.Entities.Values)
        {
            var session = factory.Session;
            if (session == null) continue;

            session.GetType().GetField("_Dal", InstanceFlags)?.SetValue(session, null);
        }
    }

    private sealed class Scope(String name, String old) : IDisposable
    {
        public void Dispose()
        {
            if (old == null)
                DAL.ConnStrs.Remove(name);
            else
                DAL.ConnStrs[name] = old;

            ResetCaches();
        }
    }
}
