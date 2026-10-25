using NewLife.Common;
using NewLife.Log;
using NewLife.Remoting;
using XCode;

namespace NewLife.Cube;

/// <summary>实体控制器基类。失败响应与字段级错误提取</summary>
/// <remarks>
/// 该分部类由 MVC 与 API 两个项目共同编译，保证 <c>EntityController2</c> 中新增的批量操作
/// 在两个版本里都能构造统一响应（此前仅 MVC 版定义了这些辅助方法，API 版编译失败）。
/// </remarks>
public partial class EntityController<TEntity, TModel>
{
    #region 失败响应
    /// <summary>根据字段名查找实体元数据中的 DisplayName（中文显示名）</summary>
    /// <param name="fieldName">字段名</param>
    /// <returns>DisplayName，找不到时返回原字段名</returns>
    private static String GetFieldDisplayName(String fieldName)
    {
        if (fieldName.IsNullOrEmpty()) return fieldName;
        var fi = Factory.AllFields.FirstOrDefault(e => e.Name.EqualIgnoreCase(fieldName));
        return fi?.DisplayName ?? fieldName;
    }

    /// <summary>从异常链中提取字段级验证错误，自动查找 DisplayName 增强消息可读性</summary>
    /// <param name="ex">异常对象</param>
    /// <returns>字段错误列表，无字段信息时返回null</returns>
    private static List<FieldError> BuildFieldErrors(Exception ex)
    {
        if (ex == null) return null;

        var list = new List<FieldError>();
        var seen = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        // 遍历异常链，收集所有带字段名的参数异常
        var current = ex;
        while (current != null)
        {
            if (current is ArgumentException ae && !ae.ParamName.IsNullOrEmpty())
            {
                // 去重：同一个字段只保留第一条错误
                if (seen.Add(ae.ParamName))
                {
                    // 查找 DisplayName，如果消息中未包含中文名则补充
                    var displayName = GetFieldDisplayName(ae.ParamName);
                    var msg = ae.Message;
                    if (displayName != ae.ParamName && !msg.Contains(displayName))
                        msg = $"{displayName}{msg}";

                    list.Add(new FieldError { Field = ae.ParamName, Message = msg });
                }
            }
            else if (current is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions)
                {
                    var innerErrors = BuildFieldErrors(inner);
                    if (innerErrors != null) list.AddRange(innerErrors);
                }
            }
            current = current.InnerException;
        }
        return list.Count > 0 ? list : null;
    }

    /// <summary>构建失败响应，自动提取字段级错误</summary>
    /// <param name="ex">异常对象</param>
    /// <param name="action">操作名称（添加/保存/删除）</param>
    /// <param name="entity">实体对象</param>
    /// <returns></returns>
    private ApiResponse<TEntity> BuildFailResponse(Exception ex, String action, TEntity entity)
    {
        DefaultSpan.Current?.SetError(ex);

        var trueEx = ex.GetTrue();
        var detail = trueEx.Message;
        WriteLog(action, false, detail);

        var fieldErrors = BuildFieldErrors(ex);

        // ApiException 保留自身码与消息；其余异常统一 500，XException 视为已知业务异常、消息保留，未知异常仅开发模式暴露细节
        var isBiz = trueEx is ApiException or XException;
        var tip = fieldErrors != null && fieldErrors.Count > 0 ? fieldErrors[0].Message : detail;
        var err = isBiz || SysConfig.Develop ? $"{L.T("{0} failed!", action)} {tip}" : L.T("{0} failed!", action);

        var code = trueEx is ApiException ae ? ae.Code : ApiCode.InternalServerError.ToInt();
        return new ApiResponse<TEntity>
        {
            Code = code,
            Message = err,
            Data = entity,
            FieldErrors = fieldErrors
        };
    }
    #endregion
}
