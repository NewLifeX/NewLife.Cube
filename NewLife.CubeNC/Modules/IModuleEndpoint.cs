namespace NewLife.Cube.Modules;

/// <summary>模块端点。需要注册路由或终结点映射的模块额外实现该接口</summary>
/// <remarks>映射发生在路由注册内部、SPA 回退之前，模块端点优先于前端 History 路由回退。</remarks>
public interface IModuleEndpoint
{
    /// <summary>映射模块端点</summary>
    /// <param name="endpoints">端点路由</param>
    void Map(IEndpointRouteBuilder endpoints);
}
