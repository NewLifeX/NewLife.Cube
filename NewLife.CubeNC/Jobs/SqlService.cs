using System.ComponentModel;
using NewLife.Cube.Entity;
using NewLife.Log;
using XCode.DataAccessLayer;

namespace NewLife.Cube.Jobs;

/// <summary>Sql作业参数</summary>
public class SqlJobArgument
{
    /// <summary>连接名</summary>
    [DisplayName("连接名")]
    public String ConnName { get; set; }

    /// <summary>Sql文本</summary>
    [DisplayName("Sql文本")]
    public String Sql { get; set; }
}

/// <summary>SQL服务</summary>
[DisplayName("执行Sql")]
[Description("在指定数据库连接上，执行指定Sql语句")]
[CronJob("RunSql", "15 * * * * ? *", Enable = false)]
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
        var connName = argument.ConnName;
        var sql = argument.Sql;
        if (connName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(argument.ConnName));
        if (sql.IsNullOrEmpty()) throw new ArgumentNullException(nameof(argument.Sql));

        using var span = _tracer?.NewSpan("RunSql", argument);

        var rs = DAL.Create(connName).Execute(sql);
        return Task.FromResult("返回：" + rs);
    }
}
