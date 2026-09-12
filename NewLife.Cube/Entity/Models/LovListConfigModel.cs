namespace NewLife.Cube.Entity;

/// <summary>值集列表配置。列表型值集的数据源配置（1:1），无真实表，数据以 JSON 存 Parameter（LovStore）</summary>
public class LovListConfigModel
{
    /// <summary>编号</summary>
    public Int32 Id { get; set; }

    /// <summary>值集定义（按 LovCode 关联，不再落真实表）</summary>
    public Int32 LovDefId { get; set; }

    /// <summary>请求地址。数据接口地址，仅后端可见</summary>
    public String RequestUrl { get; set; }

    /// <summary>请求方式。GET/POST</summary>
    public String Method { get; set; }

    /// <summary>是否分页</summary>
    public Boolean Pageable { get; set; }

    /// <summary>页码字段名。分页时页码参数名</summary>
    public String PageNumField { get; set; }

    /// <summary>页量字段名。分页时每页条数参数名</summary>
    public String PageSizeField { get; set; }

    /// <summary>数据路径。从响应中提取数据列表的JSON路径</summary>
    public String DataPath { get; set; }

    /// <summary>总量路径。从响应中提取总数的JSON路径</summary>
    public String TotalPath { get; set; }

    /// <summary>固定参数。每次请求附加的固定参数，JSON格式</summary>
    public String FixedParams { get; set; }

    /// <summary>是否代理请求。true=后端代理转发；false=前端直连 RequestUrl</summary>
    public Boolean ProxyRequest { get; set; }

    /// <summary>创建用户</summary>
    public Int32 CreateUserID { get; set; }

    /// <summary>创建地址</summary>
    public String CreateIP { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreateTime { get; set; }

    /// <summary>更新用户</summary>
    public Int32 UpdateUserID { get; set; }

    /// <summary>更新地址</summary>
    public String UpdateIP { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>备注</summary>
    public String Remark { get; set; }
}
