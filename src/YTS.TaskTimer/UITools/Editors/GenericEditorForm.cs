using YTS.Logic.Editors;

namespace YTS.TaskTimer.UITools.Editors;

public sealed class GenericEditorForm<T> : Form where T : class, new()
{
    public T? Result { get; private set; }

    private readonly T _model;
    private readonly EditorRegistry _registry;
    private readonly List<(FieldDescriptor Descriptor, IFieldEditor Editor)> _bindings = new();
    private readonly ToolTip _toolTip = new();

    public GenericEditorForm(T? initialValue = null, EditorRegistry? registry = null)
    {
        _model = initialValue == null ? new T() : Clone(initialValue);
        _registry = registry ?? EditorRegistry.Default;

        Text = $"编辑 - {typeof(T).Name}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        Size = new Size(640, 520);
        MinimumSize = new Size(480, 320);

        BuildUi();
    }

    private void BuildUi()
    {
        var descriptors = FieldDescriptorBuilder.Build(typeof(T));

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(12)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 0,
            Padding = new Padding(0)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int rowIndex = 0;
        foreach (var desc in descriptors)
        {
            IFieldEditor editor = _registry.Create(desc);
            editor.SetValue(desc.GetValue(_model));

            table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, ComputeRowHeight(desc)));

            var label = new Label
            {
                Text = desc.DisplayName,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 8, 0)
            };
            if (!string.IsNullOrEmpty(desc.Description))
                _toolTip.SetToolTip(label, desc.Description);

            table.Controls.Add(label, 0, rowIndex);
            table.Controls.Add(editor.Control, 1, rowIndex);
            _bindings.Add((desc, editor));
            rowIndex++;
        }

        scrollPanel.Controls.Add(table);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 12, 0)
        };
        var okBtn = new Button { Text = "确定", Size = new Size(88, 30), AutoSize = false };
        var cancelBtn = new Button
        {
            Text = "取消",
            Size = new Size(88, 30),
            AutoSize = false,
            DialogResult = DialogResult.Cancel
        };
        okBtn.Click += (_, _) => OnOk();
        btnPanel.Controls.Add(cancelBtn);
        btnPanel.Controls.Add(okBtn);

        // ★ Fill 控件必须最后 Add（Dock 分配按 Z 序从低到高，先给 Bottom 分空间）
        Controls.Add(btnPanel);
        Controls.Add(scrollPanel);
        AcceptButton = okBtn;
        CancelButton = cancelBtn;
    }

    private static int ComputeRowHeight(FieldDescriptor desc)
    {
        var multiline = desc.GetAttribute<MultilineAttribute>();
        if (multiline != null) return Math.Max(60, multiline.Lines * 20 + 14);

        var options = desc.GetAttribute<OptionsAttribute>();
        if (options != null && options.MultiSelect)
        {
            int rows = Math.Clamp(options.Options.Length, 2, 8);
            return rows * 20 + 12;
        }

        return 32;
    }

    private void OnOk()
    {
        foreach (var (desc, editor) in _bindings)
        {
            try
            {
                desc.SetValue(_model, editor.GetValue());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"字段 '{desc.DisplayName}' 值无效：{ex.Message}",
                    "校验失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        Result = _model;
        DialogResult = DialogResult.OK;
    }

    private static T Clone(T source)
    {
        var dest = new T();
        foreach (var d in FieldDescriptorBuilder.Build(typeof(T)))
            d.SetValue(dest, d.GetValue(source));
        return dest;
    }

    /// <summary>弹出窗口编辑，OK 返回新实例，取消返回 null</summary>
    public static T? Edit(T? initialValue = null, IWin32Window? owner = null, EditorRegistry? registry = null)
    {
        using var form = new GenericEditorForm<T>(initialValue, registry);
        return form.ShowDialog(owner) == DialogResult.OK ? form.Result : null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _toolTip?.Dispose();
        base.Dispose(disposing);
    }
}
