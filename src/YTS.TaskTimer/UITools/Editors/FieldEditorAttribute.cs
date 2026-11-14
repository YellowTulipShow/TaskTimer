namespace YTS.TaskTimer.UITools.Editors;

/// <summary>所有字段编辑器 Attribute 的基类，可继承扩展</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public abstract class FieldEditorAttribute : Attribute { }

/// <summary>字段展示名称 + 描述（未配置时用字段名）</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class DisplayAttribute : FieldEditorAttribute
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>值的 ToString 格式化字符串（如 DateTime 用 "yyyy-MM-dd HH:mm"）</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class FormatAttribute : FieldEditorAttribute
{
    public string Format { get; }
    public FormatAttribute(string format) => Format = format;
}

/// <summary>字符串字段以多行文本框展示</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class MultilineAttribute : FieldEditorAttribute
{
    public int Lines { get; set; } = 3;
    public MultilineAttribute() { }
    public MultilineAttribute(int lines) => Lines = lines;
}

/// <summary>字符串字段以下拉框 / 多选列表展示</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class OptionsAttribute : FieldEditorAttribute
{
    public string[] Options { get; }
    public bool MultiSelect { get; set; }
    public string Separator { get; set; } = ",";

    public OptionsAttribute(params string[] options) => Options = options;
}
