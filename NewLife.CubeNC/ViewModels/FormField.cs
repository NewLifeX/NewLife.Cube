namespace NewLife.Cube.ViewModels;

/// <summary>表单字段</summary>
public class FormField : DataField
{
#if MVC
    /// <summary>MVC特有，表单字段的分部视图名称。对标_Form_Group，允许针对字段定义视图</summary>
    public String GroupView { get; set; }
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