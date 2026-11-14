using System.Reflection;

namespace YTS.Logic.Editors;

/// <summary>字段元数据：反射 + Attribute 解析后的统一描述</summary>
public sealed class FieldDescriptor
{
    public MemberInfo Member { get; }
    public string Name => Member.Name;
    public Type FieldType { get; }
    public string DisplayName { get; }
    public string? Description { get; }
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

    public T? GetAttribute<T>() where T : FieldEditorAttribute => Attributes.OfType<T>().FirstOrDefault();
    public bool HasAttribute<T>() where T : FieldEditorAttribute => Attributes.OfType<T>().Any();

    public object? GetValue(object instance) => Member switch
    {
        PropertyInfo p => p.GetValue(instance),
        FieldInfo f => f.GetValue(instance),
        _ => throw new NotSupportedException($"不支持的成员类型: {Member.MemberType}")
    };

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

public static class FieldDescriptorBuilder
{
    /// <summary>反射构建类型的所有可编辑字段描述（属性 + 字段）</summary>
    public static IReadOnlyList<FieldDescriptor> Build(Type type)
    {
        var result = new List<FieldDescriptor>();
        var flags = BindingFlags.Public | BindingFlags.Instance;

        foreach (var prop in type.GetProperties(flags))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
            result.Add(Create(prop, prop.PropertyType));
        }
        foreach (var field in type.GetFields(flags))
        {
            if (field.IsInitOnly || field.IsLiteral) continue;
            result.Add(Create(field, field.FieldType));
        }
        return result;
    }

    private static FieldDescriptor Create(MemberInfo member, Type type)
    {
        var attrs = member.GetCustomAttributes<FieldEditorAttribute>(true).ToList();
        return new FieldDescriptor(member, type, attrs);
    }
}
