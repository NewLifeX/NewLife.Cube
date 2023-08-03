namespace NewLife.Cube.ViewModels
{
    /// <summary>Url打开方式</summary>
    public enum TargetEnum
    {
        /// <summary>浏览器新标签页</summary>
        _blank,

        /// <summary>当前页面</summary>
        _self,

        /// <summary></summary>
        _parent,

        /// <summary></summary>
        _top
    }

    /// <summary>数据请求方法</summary>
    public enum DataAction
    {
        /// <summary>以请求url方式直接打开</summary>
        url,

        /// <summary>以ajax方式请求url</summary>
        action,
    }
}
