namespace NewLife.Cube.AI;

/// <summary>标记字段可发送给 AI 分析。未标记的字段默认走黑名单过滤（排除密码/手机号等敏感字段）</summary>
/// <remarks>
/// 用法：在实体类的属性上添加 [AIVisible]。
/// 若实体有任意字段标记了此特性，则仅发送标记字段；否则使用黑名单规则过滤。
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AIVisibleAttribute : Attribute
{
}
