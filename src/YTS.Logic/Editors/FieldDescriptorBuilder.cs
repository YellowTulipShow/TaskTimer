using System.Reflection;

namespace YTS.Logic.Editors;

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
