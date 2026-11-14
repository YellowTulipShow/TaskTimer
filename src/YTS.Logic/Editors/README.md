# 使用案例

```C#
// 1. 定义模型
public enum TaskPriority { Low, Normal, High, Critical }

public class TaskModel
{
    [Display(Name = "任务名称", Description = "任务的展示名称")]
    public string Name { get; set; } = "";

    [Display(Name = "脚本路径")]
    public string ScriptPath { get; set; } = "";

    [Display(Name = "触发时间"), Format("yyyy-MM-dd HH:mm")]
    public DateTime TriggerAt { get; set; } = DateTime.Now;

    [Display(Name = "优先级")]
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    [Display(Name = "标签"), Options("日常", "紧急", "系统"), MultiSelect = true, Separator = "|"]
    public string Tags { get; set; } = "";

    [Display(Name = "类型"), Options("定时", "间隔", "触发")]
    public string TaskType { get; set; } = "定时";

    [Display(Name = "备注"), Multiline(Lines = 5)]
    public string Remarks { get; set; } = "";

    [Display(Name = "启用")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "重试次数")]
    public int RetryCount { get; set; }
}

// 2. 弹出编辑
var result = GenericEditorForm<TaskModel>.Edit(new TaskModel { Name = "示例任务" });
if (result != null)
{
    // 使用编辑后的结果
    Console.WriteLine($"名称: {result.Name}, 触发: {result.TriggerAt:yyyy-MM-dd HH:mm}");
}
```

# 如何扩展

扩展 1：新增字段类型（如 Color）
```C#
// 1. 写编辑器
internal sealed class ColorEditor : IFieldEditor
{
    private readonly TextBox _box = new() { Dock = DockStyle.Fill };
    public Control Control => _box;
    public void SetValue(object? v) => _box.Text = v?.ToString() ?? "";
    public object? GetValue() => _box.Text;  // 存为 "Red" 或 "#FF0000"
}

// 2. 写工厂
public sealed class ColorEditorFactory : IFieldEditorFactory
{
    public bool CanHandle(FieldDescriptor d) => d.FieldType == typeof(Color);
    public IFieldEditor Create(FieldDescriptor d) => new ColorEditor();
}

// 3. 在 Program.cs / 应用启动时注册（不需要改任何现有代码）
EditorRegistry.Default.Register(new ColorEditorFactory());
```

扩展 2：新增 Attribute（如 [Required]）
```C#
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class RequiredAttribute : FieldEditorAttribute { }

// 在 Form 里或某个 factory 里判断 HasAttribute<RequiredAttribute>()
```

扩展 3：覆盖已有字段行为（如让所有 string 走自定义编辑器）
由于 Register 是 Insert(0) 后注册优先，注册一个新工厂就会"抢在" TextEditorFactory 之前：
```C#
EditorRegistry.Default.Register(new MyCustomStringFactory());
// 之后所有 string 字段都会用你的工厂
```
