namespace YTS.Logic.Editors;

/// <summary>字段编辑器实例：负责创建(T类型)控件 + 读写值</summary>
public interface IFieldEditor<T>
{
    /// <summary>
    /// 字段编辑器实例
    /// </summary>
    T Control { get; }
    /// <summary>
    /// 设置值
    /// </summary>
    /// <param name="value">目标值</param>
    void SetValue(object? value);
    /// <summary>
    /// 获取值
    /// </summary>
    /// <returns>代表值结果</returns>
    object? GetValue();
}

/// <summary>字段编辑器工厂：判定能否处理 + 创建编辑器</summary>
public interface IFieldEditorFactory<TEditor, TControl> where TEditor : IFieldEditor<TControl>
{
    /// <summary>
    /// 判断传入的字段描述是否支持
    /// </summary>
    /// <param name="descriptor">字段描述内容</param>
    /// <returns>是否支持</returns>
    bool CanHandle(FieldDescriptor descriptor);
    /// <summary>
    /// 支持则创建指定的字段编辑器
    /// </summary>
    /// <param name="descriptor"></param>
    /// <returns>创建的字段编辑器</returns>
    TEditor Create(FieldDescriptor descriptor);
}

/// <summary>
/// 接口: 编辑器注册表
/// </summary>
public interface IEditorRegistry<TFactory, TEditor, TControl>
    where TFactory : IFieldEditorFactory<TEditor, TControl>
    where TEditor : IFieldEditor<TControl>
{
    /// <summary>
    /// 注册字段编辑器的创建工厂
    /// </summary>
    /// <param name="factory">编辑器的创建工厂</param>
    void Register(TFactory factory);
    /// <summary>
    /// 依据字段描述结果获取字段编辑器实例
    /// </summary>
    /// <param name="descriptor">字段描述内容</param>
    /// <returns>字段编辑器实例</returns>
    TEditor Create(FieldDescriptor descriptor);
}
