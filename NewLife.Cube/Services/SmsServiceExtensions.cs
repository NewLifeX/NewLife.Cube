namespace NewLife.Cube.Services;

/// <summary>短信服务扩展</summary>
public static class SmsServiceExtensions
{
    /// <summary>添加自定义短信验证码服务</summary>
    /// <remarks>
    /// 注册自定义 ISmsVerifyCode 实现，替换默认阿里云实现。
    /// <code>
    /// // 使用自定义短信服务商
    /// services.AddSmsVerifyCode&lt;TencentSmsVerifyCode&gt;();
    /// </code>
    /// </remarks>
    /// <typeparam name="TImplementation">自定义实现类型</typeparam>
    /// <param name="services">服务容器</param>
    /// <returns></returns>
    public static IServiceCollection AddSmsVerifyCode<TImplementation>(this IServiceCollection services)
        where TImplementation : class, ISmsVerifyCode
    {
        services.AddSingleton<ISmsVerifyCode, TImplementation>();
        return services;
    }

    /// <summary>添加自定义短信验证码服务（工厂方式）</summary>
    /// <remarks>
    /// 使用工厂方法注册自定义 ISmsVerifyCode 实现。
    /// <code>
    /// // 使用工厂方法配置
    /// services.AddSmsVerifyCode(sp => new TencentSmsVerifyCode
    /// {
    ///     SecretId = "YOUR_SECRET_ID",
    ///     SecretKey = "YOUR_SECRET_KEY"
    /// });
    /// </code>
    /// </remarks>
    /// <param name="services">服务容器</param>
    /// <param name="factory">工厂方法</param>
    /// <returns></returns>
    public static IServiceCollection AddSmsVerifyCode(this IServiceCollection services, Func<IServiceProvider, ISmsVerifyCode> factory)
    {
        services.AddSingleton(factory);
        return services;
    }
}
