using YTS.Logic.Editors;

namespace YTS.TaskTimer.UITools.Editors;

/// <summary>编辑器注册表：按优先级匹配工厂</summary>
public sealed class EditorRegistry : IEditorRegistry
{
    private readonly List<IFieldEditorFactory> _factories = [];

    /// <summary>注册工厂，后注册的优先级更高</summary>
    public void Register(IFieldEditorFactory factory)
    {
        lock (_factories) { _factories.Insert(0, factory); }
    }

    public IFieldEditor Create(FieldDescriptor descriptor)
    {
        IFieldEditorFactory[] snapshot;
        lock (_factories) { snapshot = _factories.ToArray(); }

        foreach (var f in snapshot)
            if (f.CanHandle(descriptor)) return f.Create(descriptor);

        throw new InvalidOperationException(
            $"找不到字段 '{descriptor.Name}' ({descriptor.FieldType.Name}) 对应的编辑器。" +
            $"请实现 IFieldEditorFactory 并调用 EditorRegistry.Default.Register(...)。");
    }

    public static EditorRegistry Default { get; } = CreateDefault();

    private static EditorRegistry CreateDefault()
    {
        var r = new EditorRegistry();
        r.Register(new OptionsEditorFactory());       // string + OptionsAttribute
        r.Register(new MultilineTextEditorFactory()); // string + MultilineAttribute
        r.Register(new EnumEditorFactory());          // enum
        r.Register(new BooleanEditorFactory());       // bool
        r.Register(new DateTimeEditorFactory());      // DateTime
        r.Register(new NumericEditorFactory());       // 数值
        r.Register(new TextEditorFactory());          // string 兜底
        return r;
    }
}
