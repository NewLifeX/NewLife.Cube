using NewLife.Cube.Entity;

namespace NewLife.Cube.Services;

/// <summary>列表型值集数据代理接口。
/// 把 LovController.ListData 的「外部数据源转发」逻辑抽象为接口，默认由 <see cref="DefaultLovListDataProxy"/> 以 HTTP 客户端实现。
/// 使用者可通过依赖注入注册自己的实现来覆盖默认行为（见 <see cref="LovServiceExtensions.AddCubeLov"/> 的 TryAddSingleton 说明）。</summary>
public interface ILovListDataProxy
{
    /// <summary>根据列表配置与查询请求，向外部数据源发起请求并解析出数据与总数</summary>
    /// <param name="config">列表型值集的数据源配置（请求地址、方式、分页、路径等）</param>
    /// <param name="request">前端下发的查询请求（lovCode、搜索参数、分页）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>代理查询结果：数据列表（已反序列化的对象数组）与总数</returns>
    Task<LovListDataProxyResult> FetchAsync(LovListConfig config, LovListDataRequest request, CancellationToken cancellationToken = default);
}

/// <summary>列表型值集代理查询结果</summary>
public class LovListDataProxyResult
{
    /// <summary>数据列表（已反序列化的对象数组）</summary>
    public Object? Data { get; set; }

    /// <summary>总数（分页场景有效）</summary>
    public Int32 Total { get; set; }
}

/// <summary>列表数据查询请求（代理转发时由前端经 /Admin/Lov/ListData 下发）</summary>
public class LovListDataRequest
{
    /// <summary>值集编码</summary>
    public String LovCode { get; set; } = null!;

    /// <summary>搜索参数</summary>
    public Dictionary<String, Object>? Params { get; set; }

    /// <summary>页码</summary>
    public Int32 PageNum { get; set; } = 1;

    /// <summary>每页条数</summary>
    public Int32 PageSize { get; set; } = 20;
}

/// <summary>批量翻译请求</summary>
public class LovBatchLabelRequest
{
    /// <summary>值集编码</summary>
    public String LovCode { get; set; } = null!;

    /// <summary>需要翻译的原始值列表</summary>
    public String[]? Values { get; set; }
}
