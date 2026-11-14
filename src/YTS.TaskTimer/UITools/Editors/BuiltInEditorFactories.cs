using System.Globalization;

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
    private static readonly HashSet<Type> Types = new()
    {
        typeof(int), typeof(long), typeof(short), typeof(byte),
        typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte),
        typeof(float), typeof(double), typeof(decimal),
        typeof(int?), typeof(long?), typeof(short?), typeof(byte?),
        typeof(uint?), typeof(ulong?), typeof(ushort?), typeof(sbyte?),
        typeof(float?), typeof(double?), typeof(decimal?)
    };

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

// ── 编辑器实现 ──────────────────────────────────────

internal sealed class TextEditor : IFieldEditor
{
    private readonly TextBox _box;
    public Control Control => _box;

    public TextEditor(bool multiline = false, int lines = 3)
    {
        _box = new TextBox { Dock = DockStyle.Fill };
        if (multiline)
        {
            _box.Multiline = true;
            _box.ScrollBars = ScrollBars.Vertical;
            _box.AcceptsReturn = true;
            _box.MinimumSize = new Size(0, lines * 20 + 10);
        }
    }

    public void SetValue(object? value) => _box.Text = value?.ToString() ?? "";
    public object? GetValue() => _box.Text;
}

internal sealed class BooleanEditor : IFieldEditor
{
    private readonly CheckBox _box;
    public Control Control => _box;

    public BooleanEditor() => _box = new CheckBox { Dock = DockStyle.Fill, Text = "" };

    public void SetValue(object? value) => _box.Checked = value is true;
    public object? GetValue() => _box.Checked;
}

internal sealed class NumericEditor : IFieldEditor
{
    private readonly NumericUpDown _num;
    public Control Control => _num;

    public NumericEditor(bool floating)
    {
        _num = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            DecimalPlaces = floating ? 4 : 0,
            Minimum = decimal.MinValue,
            Maximum = decimal.MaxValue,
            ThousandsSeparator = false
        };
    }

    public void SetValue(object? value)
    {
        try { _num.Value = value == null ? 0 : Convert.ToDecimal(value, CultureInfo.InvariantCulture); }
        catch { _num.Value = 0; }
    }

    public object? GetValue() => _num.Value;
}

internal sealed class DateTimeEditor : IFieldEditor
{
    private readonly DateTimePicker _picker;
    public Control Control => _picker;

    public DateTimeEditor(string? format)
    {
        _picker = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = string.IsNullOrWhiteSpace(format) ? "yyyy-MM-dd HH:mm:ss" : format,
            ShowUpDown = true
        };
    }

    public void SetValue(object? value)
    {
        if (value is DateTime dt && dt >= _picker.MinDate && dt <= _picker.MaxDate)
            _picker.Value = dt;
        else
            _picker.Value = DateTime.Now;
    }

    public object? GetValue() => _picker.Value;
}

internal sealed class EnumEditor : IFieldEditor
{
    private readonly ComboBox _combo;
    public Control Control => _combo;

    public EnumEditor(Type enumType)
    {
        _combo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var v in Enum.GetValues(enumType)) _combo.Items.Add(v);
    }

    public void SetValue(object? value)
    {
        if (value == null) { _combo.SelectedIndex = -1; return; }
        int idx = _combo.Items.IndexOf(value);
        _combo.SelectedIndex = idx >= 0 ? idx : (_combo.Items.Count > 0 ? 0 : -1);
    }

    public object? GetValue() => _combo.SelectedItem;
}

internal sealed class SingleSelectEditor : IFieldEditor
{
    private readonly ComboBox _combo;
    public Control Control => _combo;

    public SingleSelectEditor(string[] options)
    {
        _combo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _combo.Items.AddRange(options);
    }

    public void SetValue(object? value)
    {
        var s = value?.ToString();
        if (string.IsNullOrEmpty(s)) { _combo.SelectedIndex = -1; return; }
        int idx = _combo.Items.IndexOf(s);
        _combo.SelectedIndex = idx >= 0 ? idx : (_combo.Items.Count > 0 ? 0 : -1);
    }

    public object? GetValue() => _combo.SelectedItem?.ToString() ?? "";
}

internal sealed class MultiSelectEditor : IFieldEditor
{
    private readonly CheckedListBox _clb;
    private readonly string _separator;
    public Control Control => _clb;

    public MultiSelectEditor(string[] options, string separator)
    {
        _separator = separator;
        _clb = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            IntegralHeight = false
        };
        _clb.Items.AddRange(options);
    }

    public void SetValue(object? value)
    {
        var selected = new HashSet<string>(
            (value?.ToString() ?? "").Split(new[] { _separator }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(x => x.Trim()),
            StringComparer.Ordinal);

        for (int i = 0; i < _clb.Items.Count; i++)
            _clb.SetItemChecked(i, selected.Contains(_clb.Items[i]?.ToString() ?? ""));
    }

    public object? GetValue()
    {
        var parts = _clb.CheckedItems.Cast<object>().Select(x => x.ToString() ?? "");
        return string.Join(_separator, parts);
    }
}
