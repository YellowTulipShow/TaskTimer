using System.Globalization;
using YTS.Logic.Editors;

namespace YTS.TaskTimer.UITools.Editors;

// ── 工厂 ────────────────────────────────────────────

public sealed class TextEditorFactory : IFieldEditorFactory
{
    public bool CanHandle(FieldDescriptor d) => d.FieldType == typeof(string);
    public IFieldEditor Create(FieldDescriptor d) => new TextEditor();
}

public sealed class MultilineTextEditorFactory : IFieldEditorFactory
{
    public bool CanHandle(FieldDescriptor d) =>
        d.FieldType == typeof(string) && d.HasAttribute<MultilineAttribute>();

    public IFieldEditor Create(FieldDescriptor d) =>
        new TextEditor(multiline: true, lines: d.GetAttribute<MultilineAttribute>()!.Lines);
}

public sealed class BooleanEditorFactory : IFieldEditorFactory
{
    public bool CanHandle(FieldDescriptor d) =>
        d.FieldType == typeof(bool) || d.FieldType == typeof(bool?);
    public IFieldEditor Create(FieldDescriptor d) => new BooleanEditor();
}

public sealed class NumericEditorFactory : IFieldEditorFactory
{
    private static readonly HashSet<Type> Types =
    [
        typeof(int), typeof(long), typeof(short), typeof(byte),
        typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte),
        typeof(float), typeof(double), typeof(decimal),
        typeof(int?), typeof(long?), typeof(short?), typeof(byte?),
        typeof(uint?), typeof(ulong?), typeof(ushort?), typeof(sbyte?),
        typeof(float?), typeof(double?), typeof(decimal?)
    ];

    public bool CanHandle(FieldDescriptor d) => Types.Contains(d.FieldType);

    public IFieldEditor Create(FieldDescriptor d)
    {
        var u = Nullable.GetUnderlyingType(d.FieldType) ?? d.FieldType;
        bool floating = u == typeof(float) || u == typeof(double) || u == typeof(decimal);
        return new NumericEditor(floating);
    }
}

public sealed class DateTimeEditorFactory : IFieldEditorFactory
{
    public bool CanHandle(FieldDescriptor d) =>
        d.FieldType == typeof(DateTime) || d.FieldType == typeof(DateTime?);

    public IFieldEditor Create(FieldDescriptor d) =>
        new DateTimeEditor(d.GetAttribute<FormatAttribute>()?.Format);
}

public sealed class EnumEditorFactory : IFieldEditorFactory
{
    public bool CanHandle(FieldDescriptor d)
    {
        var t = Nullable.GetUnderlyingType(d.FieldType) ?? d.FieldType;
        return t.IsEnum;
    }

    public IFieldEditor Create(FieldDescriptor d)
    {
        var t = Nullable.GetUnderlyingType(d.FieldType) ?? d.FieldType;
        return new EnumEditor(t);
    }
}

public sealed class OptionsEditorFactory : IFieldEditorFactory
{
    public bool CanHandle(FieldDescriptor d) =>
        d.FieldType == typeof(string) && d.HasAttribute<OptionsAttribute>();

    public IFieldEditor Create(FieldDescriptor d)
    {
        var attr = d.GetAttribute<OptionsAttribute>()!;
        return attr.MultiSelect
            ? new MultiSelectEditor(attr.Options, attr.Separator)
            : new SingleSelectEditor(attr.Options);
    }
}
