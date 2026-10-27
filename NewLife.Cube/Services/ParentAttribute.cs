namespace NewLife.Cube;

/// <summary>级联父级标注：标在枚举成员上，声明"本选项的父级 = 另一个枚举的某个值"。</summary>
/// <remarks>
/// <see cref="Services.LovRegistry"/> 反射枚举选项时，按本特性的简单名（ParentAttribute）识别：
/// 被标记的成员，其值集选项会带出 <c>parent</c>（父枚举值），前端 <c>LovSelect :parent=</c> 据此自动级联过滤，前后端零接线。
/// 若项目不希望引用 NewLife.Cube（如公共层需避免依赖 Web 框架），可在自己的程序集中定义同名特性 ParentAttribute（任意命名空间均可，成员需含 <c>Value</c> 属性），
/// 框架同样按名称识别，效果完全一致。
/// </remarks>
/// <example>
/// <code>
/// [Parent(Section.PROD)] Prod_LineDown = 1,   // 该工单类型属于「生产」板块
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ParentAttribute : Attribute
{
    /// <summary>父级枚举值（装箱）。框架读取本属性取父值。</summary>
    public Object Value { get; }

    /// <summary>父级枚举类型（由 <see cref="Value"/> 的运行时类型推得，供多父维度时区分）。</summary>
    public Type ParentType => Value.GetType();

    /// <summary>以父枚举值实例化特性</summary>
    /// <param name="value">父枚举的某个成员值，如 <c>Section.PROD</c>。</param>
    public ParentAttribute(Object value) => Value = value ?? throw new ArgumentNullException(nameof(value));
}
