namespace NewLife.Cube.Common;

/// <summary>
/// Excel扩展
/// </summary>
public static class ExcelExtension
{
    /// <summary>
    /// 1、2、3 转换A、B、C
    /// <param name="index"></param>
    /// <returns></returns>
    public static String IndexToName(Int32 index)
    {
        if (index < 0)
        {
            throw new Exception("参数非法！");
        }
        var chars = new List<String>();
        do
        {
            if (chars.Count > 0) index--;
            var tempchar = "";
            if (chars.Count == 0)
            {
                tempchar = ((Char)(index % 26 + 'A' - 1)).ToString();
            }
            else
            {
                tempchar = ((Char)(index % 26 + 'A')).ToString();
            }
            chars.Insert(0, tempchar);
            index = (index - index % 26) / 26;
        } while (index > 0);
        return String.Join(String.Empty, chars.ToArray());
    }
}