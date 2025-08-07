using System.Collections;
using NewLife.Web;
namespace NewLife.Cube.Extensions;

/// <summary>ApiListResponse返回扩展</summary>
/// <remarks>扩展方法封装</remarks>
public static partial class ApiListResponseHelper
{
    /// <summary>将ApiResponse转换为ApiListResponse的通用方法</summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    /// <typeparam name="TSource">源数据类型</typeparam>
    /// <param name="data">原始ApiResponse</param>
    /// <param name="pager">分页器</param>
    /// <returns></returns>
    public static ApiListResponse<T> WithPager<T, TSource>(this ApiResponse<TSource> data, Pager pager)
    {
        IList<T> list;

        if (data.Data == null)
        {
            list = new List<T>();
        }
        else if (data.Data is IEnumerable<T> enumerable)
        {
            // 如果源数据是 IEnumerable<T>，直接转换
            list = enumerable.ToList();
        }
        else if (data.Data is T singleItem)
        {
            // 如果源数据是单个 T 对象，包装成列表
            list = new List<T> { singleItem };
        }
        else if (data.Data is IEnumerable nonGenericEnumerable)
        {
            // 如果源数据是非泛型的 IEnumerable，尝试转换
            list = nonGenericEnumerable.Cast<T>().ToList();
        }
        else
        {
            // 其他情况，创建空列表
            list = new List<T>();
        }

        // 尝试从分页器状态中获取统计数据
        T stat = default;
        if (pager?.State is T pagerStat) stat = pagerStat;

        return new ApiListResponse<T>
        {
            Code = data.Code,
            Message = data.Message,
            Data = list,
            Page = pager?.ToModel(),
            Stat = stat,
            TraceId = data.TraceId
        };
    }

    /// <summary>将ApiResponse转换为ApiListResponse，自动推断列表元素类型</summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    /// <param name="data">原始ApiResponse，Data必须是IEnumerable&lt;T&gt;类型</param>
    /// <param name="pager">分页器</param>
    /// <returns></returns>
    public static ApiListResponse<T> WithPager<T>(this ApiResponse<IEnumerable<T>> data, Pager pager)
    {
        return data.WithPager<T, IEnumerable<T>>(pager);
    }

    /// <summary>将ApiResponse转换为ApiListResponse，专门处理List类型</summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    /// <param name="data">原始ApiResponse，Data是List&lt;T&gt;类型</param>
    /// <param name="pager">分页器</param>
    /// <returns></returns>
    public static ApiListResponse<T> WithPager<T>(this ApiResponse<List<T>> data, Pager pager)
    {
        return data.WithPager<T, List<T>>(pager);
    }

    /// <summary>将ApiResponse转换为ApiListResponse，专门处理IList类型</summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    /// <param name="data">原始ApiResponse，Data是IList&lt;T&gt;类型</param>
    /// <param name="pager">分页器</param>
    /// <returns></returns>
    public static ApiListResponse<T> WithPager<T>(this ApiResponse<IList<T>> data, Pager pager)
    {
        return data.WithPager<T, IList<T>>(pager);
    }

    /// <summary>将ApiResponse转换为ApiListResponse，专门处理数组类型</summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    /// <param name="data">原始ApiResponse，Data是T[]类型</param>
    /// <param name="pager">分页器</param>
    /// <returns></returns>
    public static ApiListResponse<T> WithPager<T>(this ApiResponse<T[]> data, Pager pager)
    {
        return data.WithPager<T, T[]>(pager);
    }

    /// <summary>将ApiResponse转换为ApiListResponse，专门处理单个对象</summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="data">原始ApiResponse，Data是T类型</param>
    /// <param name="pager">分页器</param>
    /// <returns></returns>
    public static ApiListResponse<T> WithPager<T>(this ApiResponse<T> data, Pager pager)
    {
        return data.WithPager<T, T>(pager);
    }

    /// <summary>将ApiResponse转换为ApiListResponse，带自定义统计数据</summary>
    /// <typeparam name="T">列表元素类型</typeparam>
    /// <typeparam name="TSource">源数据类型</typeparam>
    /// <param name="data">原始ApiResponse</param>
    /// <param name="pager">分页器</param>
    /// <param name="stat">自定义统计数据</param>
    /// <returns></returns>
    public static ApiListResponse<T> WithPager<T, TSource>(this ApiResponse<TSource> data, Pager pager, T stat)
    {
        IList<T> list;

        if (data.Data == null)
        {
            list = new List<T>();
        }
        else if (data.Data is IEnumerable<T> enumerable)
        {
            // 如果源数据是 IEnumerable<T>，直接转换
            list = enumerable.ToList();
        }
        else if (data.Data is T singleItem)
        {
            // 如果源数据是单个 T 对象，包装成列表
            list = new List<T> { singleItem };
        }
        else if (data.Data is IEnumerable nonGenericEnumerable)
        {
            // 如果源数据是非泛型的 IEnumerable，尝试转换
            list = nonGenericEnumerable.Cast<T>().ToList();
        }
        else
        {
            // 其他情况，创建空列表
            list = new List<T>();
        }

        return new ApiListResponse<T>
        {
            Code = data.Code,
            Message = data.Message,
            Data = list,
            Page = pager?.ToModel(),
            Stat = stat,
            TraceId = data.TraceId
        };
    }
}