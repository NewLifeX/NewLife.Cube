using System.Text.Json.Serialization;

namespace NewLife.Cube.ViewModels;

/// <summary>获取扩展字段委托</summary>
/// <param name="entity"></param>
/// <returns></returns>
public delegate Object GetExpandDelegate(Object entity);

/// <summary>表单字段</summary>
public class FormField : DataField
{
    /// <summary>表单分组视图。MVC 为分部视图名，API 为前端渲染器名</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String GroupView { get; set; }

    /// <summary>表单项视图。MVC 为分部视图名，API 为前端渲染器名</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public String ItemView { get; set; }

    /// <summary>获取扩展字段委托。当前字段所表示的对象，各属性作为表单字段展开</summary>
    [Obsolete("=>Expand")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public GetExpandDelegate GetExpand
    {
        get => Expand?.Decode == null ? null : e => Expand.Decode?.Invoke(e);
        set
        {
            if (value == null)
            {
                if (Expand != null)
                    Expand.Decode = null;
            }
            else
            {
                var exp = Expand ??= new ExpandField();
                exp.Decode = e => value(e);
            }
        }
    }

    /// <summary>保留扩展字段。默认false，字段被扩展以后，表单上就不再出现原字段</summary>
    [Obsolete("=>Expand")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Boolean RetainExpand
    {
        get => Expand?.Retain ?? false;
        set
        {
            if (Expand != null)
                Expand.Retain = value;
        }
    }

    /// <summary>扩展字段</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ExpandField Expand { get; set; }
}