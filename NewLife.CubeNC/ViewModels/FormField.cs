namespace NewLife.Cube.ViewModels;

/// <summary>获取扩展字段委托</summary>
/// <param name="entity"></param>
/// <returns></returns>
public delegate Object GetExpandDelegate(Object entity);

/// <summary>表单字段</summary>
public class FormField : DataField
{
#if MVC
    /// <summary>MVC特有，表单字段的分部视图名称，不要.cshtml后缀。对标_Form_Group，允许针对字段定义视图</summary>
    public String GroupView { get; set; }

    /// <summary>获取扩展字段委托。当前字段所表示的对象，各属性作为表单字段展开</summary>
    public GetExpandDelegate GetExpand { get; set; }

    /// <summary>保留扩展字段。默认false，字段被扩展以后，表单上就不再出现原字段</summary>
    public Boolean RetainExpand { get; set; }
#endif

    #region 方法
    ///// <summary>克隆</summary>
    ///// <returns></returns>
    //public override DataField Clone()
    //{
    //    var df = base.Clone();
    //    if (df is FormField ff)
    //    {
    //        ff.GroupView = GroupView;
    //    }

    //    return df;
    //}
    #endregion
}