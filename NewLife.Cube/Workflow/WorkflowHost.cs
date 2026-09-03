using NewLife.Log;
using XCode;

namespace NewLife.Cube.Workflow;

/// <summary>工作流宿主。把审批写锁拦截器挂到全局实体拦截器（WebAPI 核心库随 AddCube 生效）</summary>
/// <remarks>
/// 编译进 NewLife.Cube（WebAPI 版）：引用 NewLife.Cube 的宿主调用 AddCube 即具备工作流能力；
/// NewLife.CubeNC/CubeDemoNC 不 Link 本目录，MVC 版无此类型、无副作用。
/// </remarks>
public static class WorkflowHost
{
    private static Int32 _inited;

    /// <summary>注册全局写锁拦截器（幂等）。测试可直接调 Register 或自行 Add</summary>
    public static void Register()
    {
        if (Interlocked.Exchange(ref _inited, 1) != 0) return;

        EntityInterceptors.Global.Add(new WorkflowWriteInterceptor());
        XTrace.WriteLine("启用工作流写锁拦截器[WorkflowWriteInterceptor]");
    }

    /// <summary>测试重置</summary>
    public static void Reset() => _inited = 0;
}
