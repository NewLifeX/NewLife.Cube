using NewLife.Cube.Entity;
using NewLife.Log;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Jobs;

/// <summary>Sql作业参数</summary>
public class SqlJobArgument
{
    /// <summary>连接名</summary>
    public String ConnName { get; set; }

    /// <summary>Sql文本</summary>
    public String Sql { get; set; }
}

/// <summary>SQL服务</summary>
[CronJob("RunSql", "15 * * * * ? *")]
public class SqlService : CubeJobBase<SqlJobArgument>
{
    private readonly ITracer _tracer;

    /// <summary>实例化SQL服务，用于定期执行指定SQL语句</summary>
    /// <param name="tracer"></param>
    public SqlService(ITracer tracer) => _tracer = tracer;

    /// <summary>执行作业</summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected override Task<String> OnExecute(SqlJobArgument argument)
    {
        using var span = _tracer?.NewSpan("RunSql", argument);

        var connName = argument.ConnName;
        var sql = argument.Sql;

        var rs = DAL.Create(connName).Execute(sql);
        return Task.FromResult("返回：" + rs);
    }
}
