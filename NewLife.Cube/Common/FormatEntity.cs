using NewLife.Reflection;
using XCode.Configuration;

namespace NewLife.Cube.Common;

/// <summary>
/// 实体格式化助手
/// </summary>
public static class FormatEntity
{
    /// <summary>
    /// 根据excel内容赋值实体内容
    /// </summary>
    /// <param name="fieldsItem"></param>
    /// <param name="item"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static void FromExcelToEntity(this FieldItem fieldsItem, IDictionary<String, Object> item, Object entity)
    {
        //解析时间
        if (fieldsItem.Type == typeof(DateTime))
        {
            var fieldsValue = item[fieldsItem.Name].ToString();
            var fieldsValueTime = DateTime.Now;
            if (!fieldsValue.IsNullOrWhiteSpace())
            {
                try
                {
                    fieldsValueTime = fieldsValue.Contains("/") ? DateTime.Parse(fieldsValue) : DateTime.FromOADate(Double.Parse(fieldsValue));
                }
                catch { }
            }

            entity.SetValue(fieldsItem.Name, fieldsValueTime);
        }
        //解析Boolean
        else if (fieldsItem.Type == typeof(Boolean))
        {
            entity.SetValue(fieldsItem.Name, item[fieldsItem.Name].ToBoolean(false));
        }
        //解析int
        else if (fieldsItem.Type == typeof(Int32) || fieldsItem.Type == typeof(Int64))
        {
            entity.SetValue(fieldsItem.Name, item[fieldsItem.Name].ToInt(0));
        }
        //解析double
        else if (fieldsItem.Type == typeof(Double))
        {
            entity.SetValue(fieldsItem.Name, item[fieldsItem.Name].ToDouble(0));
        }
        //解析double
        else if (fieldsItem.Type == typeof(Decimal))
        {
            entity.SetValue(fieldsItem.Name, item[fieldsItem.Name].ToDecimal(0));
        }
        //解析String
        else if (fieldsItem.Type == typeof(String))
        {
            entity.SetValue(fieldsItem.Name, item[fieldsItem.Name]);
        }
    }
}
