using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube;

/// <summary>只读实体控制器基类</summary>
/// <typeparam name="TEntity"></typeparam>
public partial class ReadOnlyEntityController<TEntity> : ControllerBaseX where TEntity : Entity<TEntity>, new()
{
    #region 构造
    /// <summary>动作执行前</summary>
    /// <param name="filterContext"></param>
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext filterContext)
    {
        // 多选框强制使用Form提交数据，未选中时不会提交数据，但也要强行覆盖Url参数
        if (Request.HasFormContentType)
        {
            if (filterContext.ActionArguments.TryGetValue("p", out var aa) && aa is Pager p)
            {
                foreach (var item in OnGetFields(ViewKinds.Search, null))
                {
                    if (item is SearchField sf && sf.Multiple)
                    {
                        p[sf.Name] = Request.Form.TryGetValue(sf.Name, out var vs) ? (String)vs : null;
                        //// 以下写法，Form没有数据时，也会返回空字符串，而不是null
                        //p[sf.Name] = Request.Form[sf.Name];
                    }
                }
            }
        }

        base.OnActionExecuting(filterContext);
    }
    #endregion

    #region 默认Action
    /// <summary>多行数据列表</summary>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("{type}管理")]
    [HttpGet("/[area]/[controller]")]
    public virtual ApiListResponse<TEntity> Index()
    {
        var p = new Pager(WebHelper.Params)
        {
            // 需要总记录数来分页
            RetrieveTotalCount = true
        };

        var list = SearchData(p);

        return new ApiListResponse<TEntity>
        {
            Data = list.ToList(),
            Page = p.ToModel(),
            Stat = (TEntity)p.State,
            TraceId = DefaultSpan.Current?.TraceId,
        };
    }

    /// <summary>查看单行数据</summary>
    /// <param name="id">主键。可能为空（表示添加），所以用字符串而不是整数</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Detail)]
    [DisplayName("查看{type}")]
    [HttpGet]
    public virtual ApiResponse<TEntity> Detail([Required] String id)
    {
        var entity = FindData(id);
        if (entity == null || (entity as IEntity).IsNullKey) throw new XException("要查看的数据[{0}]不存在！", id);

        // 验证数据权限
        Valid(entity, DataObjectMethodType.Select, false);

        return new ApiResponse<TEntity> { Data = entity };
    }
    #endregion

    #region 列表字段和表单字段
    /// <summary>获取字段信息。支持用户重载并根据上下文定制界面</summary>
    /// <param name="kind">字段类型：1-列表List、2-详情Detail、3-添加AddForm、4-编辑EditForm、5-搜索Search</param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    public virtual List<DataField> GetFields(ViewKinds kind) => OnGetFields(kind, null);
    #endregion
}