using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Configuration;
using NewLife.Reflection;

namespace NewLife.Cube;

/// <summary>设置控制器</summary>
public class ConfigController<TConfig> : ObjectController<TConfig> where TConfig : Config<TConfig>, new()
{
    /// <summary>要展现和修改的对象</summary>
    protected override TConfig Value
    {
        get
        {
            return Config<TConfig>.Current;
        }
        set
        {
            if (value != null)
            {
                var cfg = Config<TConfig>.Current;
                //value.ConfigFile = cfg.ConfigFile;
                //value.Save();
                cfg.Copy(value);
                cfg.Save();
            }
            //Config<TConfig>.Current = value;
        }
    }

    ///// <summary>已重载</summary>
    ///// <param name="filterContext"></param>
    //public override void OnActionExecuting(ActionExecutingContext filterContext)
    //{
    //    //var fi = XmlConfig<TConfig>._.ConfigFile;
    //    //if (fi.IsNullOrEmpty() || !fi.AsFile().Exists) throw new Exception("无法找到配置文件 {0}".F(fi));

    //    var bs = this.Bootstrap();
    //    bs.MaxColumn = 1;
    //    bs.LabelWidth = 3;

    //    base.OnActionExecuting(filterContext);
    //}
}