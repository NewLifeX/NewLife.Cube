using System;

namespace NewLife.Cube;

/// <summary>列表型值集自动注册特性。
/// 标注在控制器 Action 方法上，Lov 初始化枚举时会一并扫描并自动注册一个 LIST 类型的列表型值集（Source=AUTO）。
/// 典型用途：把某个返回列表 JSON 的接口（如角色列表）直接声明为可下拉选择的列表值集。
/// 注意：RequestUrl 指向该接口自身（同应用接口），一般配合 ProxyRequest=false 由前端直连请求，避免后端无意义代理。</summary>
/// <remarks>
/// 列/搜索字段以字符串数组声明，元素格式：
/// 表格列  "Field:Title:Width:Align"（Width/Align 可省略）
/// 搜索字段 "Field:Title:ComponentType:ParamType:Required"（Required 为 true/false，可省略）
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class LovListAttribute : Attribute
{
    /// <summary>值集编码。须以 List. 开头，如 List.CubeDemo.Role</summary>
    public String LovCode { get; set; } = "";

    /// <summary>显示名称</summary>
    public String Name { get; set; } = "";

    /// <summary>请求地址。数据接口地址（前端直连或后端代理转发）</summary>
    public String RequestUrl { get; set; } = "";

    /// <summary>请求方式。GET/POST，默认 GET</summary>
    public String Method { get; set; } = "GET";

    /// <summary>是否分页，默认 true</summary>
    public Boolean Pageable { get; set; } = true;

    /// <summary>值字段名，默认 id</summary>
    public String ValueField { get; set; } = "id";

    /// <summary>标签字段名，默认 name</summary>
    public String LabelField { get; set; } = "name";

    /// <summary>数据路径。从响应中提取数据列表的 JSON 路径，可空（取根）</summary>
    public String? DataPath { get; set; }

    /// <summary>总量路径。从响应中提取总数的 JSON 路径，可空</summary>
    public String? TotalPath { get; set; }

    /// <summary>页码字段名，默认 pageIndex</summary>
    public String PageNumField { get; set; } = "pageIndex";

    /// <summary>每页条数字段名，默认 pageSize</summary>
    public String PageSizeField { get; set; } = "pageSize";

    /// <summary>固定参数。每次请求附加的固定参数，JSON 格式</summary>
    public String? FixedParams { get; set; }

    /// <summary>是否代理请求。默认 false（前端直连 RequestUrl）；true 时由后端 /Admin/Lov/ListData 代理转发</summary>
    public Boolean ProxyRequest { get; set; } = false;

    /// <summary>表格列声明。元素格式 "Field:Title:Width:Align"</summary>
    public String[]? Columns { get; set; }

    /// <summary>搜索字段声明。元素格式 "Field:Title:ComponentType:ParamType:Required"</summary>
    public String[]? SearchFields { get; set; }
}
