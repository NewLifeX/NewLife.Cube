namespace NewLife.Cube;

/// <summary>标记枚举在自动注册为值集（LovDefinition）时，使用成员名（字符串）作为选项值，而非默认的数字值。</summary>
/// <remarks>
/// 适用于字段以枚举名（字符串）存储的场景，例如配置键、状态码键等。
/// <see cref="Services.LovAutoRegisterService"/> 在同步枚举值时，按本特性的简单名（LovStringValueAttribute）反射识别：
/// 被标记的枚举，其值集选项 Value 取成员名（如 "PersistProperties"），可与数据库中以字符串保存的枚举名直接对应，下拉选择即可正确回写。
/// 若项目不希望引用 NewLife.Cube（如公共层需避免依赖 Web 框架），可在自己的程序集中定义同名特性 LovStringValueAttribute（任意命名空间均可），
/// 框架同样按名称识别，效果完全一致。
/// </remarks>
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public sealed class LovStringValueAttribute : Attribute
{
}
