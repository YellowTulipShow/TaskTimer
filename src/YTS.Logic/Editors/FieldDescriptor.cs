using System.Reflection;

namespace YTS.Logic.Editors;

/// <summary>
/// 字段元数据：反射 + Attribute 解析后的统一描述
/// </summary>
public sealed class FieldDescriptor
{
    /// <summary>
    /// 反射属性对象
    /// </summary>
    public MemberInfo Member { get; }
    /// <summary>
    /// 基础字段名称 (代码内名称)
    /// </summary>
    public string Name => Member.Name;
    /// <summary>
    /// 字段代表使用的类型
    /// </summary>
    public Type FieldType { get; }

    /// <summary>
    /// 展示名称
    /// </summary>
    public string DisplayName { get; }
    /// <summary>
    /// 字段功能描述
    /// </summary>
    public string? Description { get; }
    /// <summary>
    /// 附带编辑属性
    /// </summary>
    public IReadOnlyList<FieldEditorAttribute> Attributes { get; }

    internal FieldDescriptor(MemberInfo member, Type fieldType, IReadOnlyList<FieldEditorAttribute> attrs)
    {
        Member = member;
        FieldType = fieldType;
        Attributes = attrs;

        var display = attrs.OfType<DisplayAttribute>().FirstOrDefault();
        DisplayName = string.IsNullOrWhiteSpace(display?.Name) ? member.Name : display!.Name!;
        Description = display?.Description;
    }

    /// <summary>
    /// 获取指定的属性
    /// </summary>
    public T? GetAttribute<T>() where T : FieldEditorAttribute => Attributes.OfType<T>().FirstOrDefault();
    /// <summary>
    /// 是否有指定的属性
    /// </summary>
    public bool HasAttribute<T>() where T : FieldEditorAttribute => Attributes.OfType<T>().Any();

    /// <summary>
    /// 从模型实例对象中获取该字段的值
    /// </summary>
    /// <param name="instance">实例对象</param>
    /// <returns>获取值</returns>
    /// <exception cref="NotSupportedException">不能支持的成员类型</exception>
    public object? GetValue(object instance) => Member switch
    {
        PropertyInfo p => p.GetValue(instance),
        FieldInfo f => f.GetValue(instance),
        _ => throw new NotSupportedException($"不支持的成员类型: {Member.MemberType}")
    };

    /// <summary>
    /// 将该字段的指定的值赋值到模型实例中
    /// </summary>
    /// <param name="instance">实例对象</param>
    /// <param name="value">目标值</param>
    public void SetValue(object instance, object? value)
    {
        var v = Coerce(value, FieldType);
        switch (Member)
        {
            case PropertyInfo p:
                if (p.CanWrite) p.SetValue(instance, v);
                break;
            case FieldInfo f:
                if (!f.IsInitOnly) f.SetValue(instance, v);
                break;
        }
    }

    /// <summary>
    /// 值转换, 未知值转为字段使用的类型值
    /// </summary>
    private static object? Coerce(object? value, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        bool isNullable = Nullable.GetUnderlyingType(targetType) != null;

        if (value == null)
            return isNullable ? null : (underlying.IsValueType ? Activator.CreateInstance(underlying) : null);

        if (underlying.IsInstanceOfType(value)) return value;

        if (underlying.IsEnum)
            return value is string s
                ? Enum.Parse(underlying, s, true)
                : Enum.ToObject(underlying, value);

        return Convert.ChangeType(value, underlying);
    }
}
