using YTS.Logic.Editors;

namespace YTS.TaskTimer.UITools.Editors;

/// <summary>字段编辑器实例：负责创建(T类型)控件 + 读写值</summary>
public interface IFieldEditor : IFieldEditor<Control>
{
}

/// <summary>字段编辑器工厂：判定能否处理 + 创建编辑器</summary>
public interface IFieldEditorFactory : IFieldEditorFactory<IFieldEditor, Control>
{
}

/// <summary>
/// 接口: 编辑器注册表
/// </summary>
public interface IEditorRegistry : IEditorRegistry<IFieldEditorFactory, IFieldEditor, Control>
{
}
