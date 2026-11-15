using YTS.Logic.Editors;

namespace YTS.TaskTimer.UITools.Editors;

public sealed class GenericEditorForm<T> : Form where T : class, new()
{
    public T? Result { get; private set; }

    private readonly T _model;
    private readonly IEditorRegistry _registry;
    private readonly List<(FieldDescriptor Descriptor, IFieldEditor Editor)> _bindings = new();
    private readonly ToolTip _toolTip = new();

    // ── 布局常量 ─────────────────────────────────────
    private const int MainRowHeight = 32;        // Name | Editor 行高（容纳 30 高控件 + 上下 1px Margin）
    private const int DescriptionRowHeight = 20; // Description 行高
    private const int EditorColumnWidth = 260;   // 编辑控件统一宽度
    private const int GroupSpacing = 5;          // 字段组之间的垂直/水平间距
    private const int OuterPadding = 8;          // 内容区四周留白
    private const int ButtonAreaHeight = 48;     // 底部按钮区高度
    private const int MinNameColumnWidth = 40;   // Name 列最小宽度

    public GenericEditorForm(T? initialValue = null, IEditorRegistry? registry = null)
    {
        _model = initialValue == null ? new T() : Clone(initialValue);
        _registry = registry ?? EditorRegistry.Default;

        Text = $"编辑 - {typeof(T).Name}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        MinimumSize = new Size(320, 240);

        BuildUi();
    }

    // ═════════════════════════════════════════════════
    //  布局主流程
    // ═════════════════════════════════════════════════

    private void BuildUi()
    {
        var descriptors = FieldDescriptorBuilder.Build(typeof(T));

        if (descriptors.Count == 0)
        {
            BuildEmptyUi();
            return;
        }

        // 1. 测量 Name 列宽度（取所有 DisplayName 中最宽的）
        int nameColumnWidth = MinNameColumnWidth;
        foreach (var desc in descriptors)
        {
            int w = TextRenderer.MeasureText(desc.DisplayName, Font).Width;
            nameColumnWidth = Math.Max(nameColumnWidth, w);
        }
        nameColumnWidth += 12;  // Label 右边距 6 + 视觉间隙 6

        // 2. 为每个字段创建"组"控件
        var groups = new List<FieldGroupInfo>(descriptors.Count);
        foreach (var desc in descriptors)
        {
            var editor = _registry.Create(desc);
            editor.SetValue(desc.GetValue(_model));
            _bindings.Add((desc, editor));

            if (!string.IsNullOrWhiteSpace(desc.Description))
                _toolTip.SetToolTip(editor.Control, desc.Description);

            bool hasDesc = !string.IsNullOrWhiteSpace(desc.Description);
            int groupHeight = MainRowHeight + (hasDesc ? DescriptionRowHeight : 0);

            var groupControl = BuildFieldGroup(desc, editor, nameColumnWidth, hasDesc);
            groups.Add(new FieldGroupInfo(groupControl, groupHeight));
        }

        // 3. 判断单列 / 双列
        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1200, 800);
        int maxVisibleContentHeight = (int)(screen.Height * 0.75)
                                    - ButtonAreaHeight
                                    - SystemInformation.CaptionHeight
                                    - 20;

        int singleColContentHeight = OuterPadding * 2
                                   + groups.Sum(g => g.Height)
                                   + (groups.Count - 1) * GroupSpacing;

        bool useTwoColumns = singleColContentHeight > maxVisibleContentHeight;

        // 4. 构建滚动面板 + 内容
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(OuterPadding),
            BackColor = SystemColors.Window,
        };

        int contentWidth;
        int contentHeight;
        bool is_add_fiexd_height = false;

        if (!useTwoColumns)
        {
            // ── 单列 ─────────────────────────────────
            var column = BuildColumn(groups, nameColumnWidth);
            scrollPanel.Controls.Add(column);

            contentWidth = OuterPadding * 2 + nameColumnWidth + EditorColumnWidth;
            contentHeight = singleColContentHeight;
        }
        else
        {
            // ── 双列：左半 + 右半 ────────────────────
            int leftCount = (groups.Count + 1) / 2;
            var leftGroups = groups.Take(leftCount).ToList();
            var rightGroups = groups.Skip(leftCount).ToList();

            var leftCol = BuildColumn(leftGroups, nameColumnWidth);
            var rightCol = BuildColumn(rightGroups, nameColumnWidth);

            var twoColTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
            };
            int colWidth = nameColumnWidth + EditorColumnWidth + GroupSpacing;
            twoColTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, colWidth));
            twoColTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, colWidth));
            twoColTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            twoColTable.Controls.Add(leftCol, 0, 0);
            twoColTable.Controls.Add(rightCol, 1, 0);

            scrollPanel.Controls.Add(twoColTable);

            contentWidth = OuterPadding * 2 + colWidth * 2;
            int leftH = leftGroups.Sum(g => g.Height) + Math.Max(0, leftGroups.Count - 1) * GroupSpacing;
            int rightH = rightGroups.Sum(g => g.Height) + Math.Max(0, rightGroups.Count - 1) * GroupSpacing;
            contentHeight = OuterPadding * 2 + Math.Max(leftH, rightH);
            is_add_fiexd_height = true;
        }

        // 5. 按钮区
        var (btnPanel, okBtn, cancelBtn) = BuildButtonPanel();
        okBtn.Click += (_, _) => OnOk();
        // 6. 添加到窗体（Bottom 先加，Fill 后加，保证 Dock 分配正确）
        Controls.Add(btnPanel);
        Controls.Add(scrollPanel);
        AcceptButton = okBtn;
        CancelButton = cancelBtn;

        // 7. 计算并设置窗体尺寸
        int finalWidth = contentWidth + SystemInformation.VerticalScrollBarWidth + 20;
        int finalHeight = Math.Min(contentHeight, maxVisibleContentHeight)
                        + ButtonAreaHeight
                        + SystemInformation.CaptionHeight
                        + 20;
        if (is_add_fiexd_height)
            finalHeight += 150;

        finalWidth = Math.Max(finalWidth, 320);
        finalWidth = Math.Min(finalWidth, screen.Width - 80);
        finalHeight = Math.Max(finalHeight, 200);
        finalHeight = Math.Min(finalHeight, screen.Height - 80);

        Size = new Size(finalWidth, finalHeight);
    }

    // ═════════════════════════════════════════════════
    //  单个字段组：Name | Editor
    //               空 | Description
    // ═════════════════════════════════════════════════

    private static Control BuildFieldGroup(
        FieldDescriptor desc,
        IFieldEditor editor,
        int nameWidth,
        bool hasDesc)
    {
        var group = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = hasDesc ? 2 : 1,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            AutoSize = false,
            Size = new Size(nameWidth + EditorColumnWidth,
                            MainRowHeight + (hasDesc ? DescriptionRowHeight : 0)),
        };
        group.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, nameWidth));
        group.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, EditorColumnWidth));
        group.RowStyles.Add(new RowStyle(SizeType.Absolute, MainRowHeight));
        if (hasDesc)
            group.RowStyles.Add(new RowStyle(SizeType.Absolute, DescriptionRowHeight));

        // 第一行：DisplayName | Editor
        var nameLabel = new Label
        {
            Text = desc.DisplayName,
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill,
            AutoSize = false,
            AutoEllipsis = true,
            Margin = new Padding(0, 0, 6, 0),
        };
        group.Controls.Add(nameLabel, 0, 0);

        editor.Control.Dock = DockStyle.Fill;
        editor.Control.Margin = new Padding(0, 1, 0, 1);   // 让控件实际高度 ≈ 30（32 - 上下各 1）
        group.Controls.Add(editor.Control, 1, 0);

        // 第二行：（左空） | Description
        if (hasDesc)
        {
            var descLabel = new Label
            {
                Text = desc.Description,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoEllipsis = true,
                ForeColor = Color.Gray,
                Margin = new Padding(0),
                // 用比正常小一号的字体，紧凑一点
                // Font = new Font(Font.FontFamily, Math.Max(7f, Font.Size - 1f)),
            };
            group.Controls.Add(descLabel, 1, 1);
        }

        return group;
    }

    // ═════════════════════════════════════════════════
    //  把若干字段组垂直排成一列
    // ═════════════════════════════════════════════════

    private static Control BuildColumn(List<FieldGroupInfo> groups, int nameWidth)
    {
        var column = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = groups.Count,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
        };
        column.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, nameWidth + EditorColumnWidth));

        for (int i = 0; i < groups.Count; i++)
        {
            // 行高 = 组内容高 + 组间距（最后一个不加）
            int rowH = groups[i].Height + (i < groups.Count - 1 ? GroupSpacing : 0);
            column.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));

            groups[i].Control.Dock = DockStyle.Top;
            column.Controls.Add(groups[i].Control, 0, i);
        }

        return column;
    }

    // ═════════════════════════════════════════════════
    //  按钮区
    // ═════════════════════════════════════════════════

    private static (FlowLayoutPanel Panel, Button Ok, Button Cancel) BuildButtonPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = ButtonAreaHeight,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 12, 0),
        };

        var okBtn = new Button
        {
            Text = "确定",
            Size = new Size(88, 30),
            AutoSize = false,
        };
        var cancelBtn = new Button
        {
            Text = "取消",
            Size = new Size(88, 30),
            AutoSize = false,
            DialogResult = DialogResult.Cancel,
        };
        panel.Controls.Add(cancelBtn);
        panel.Controls.Add(okBtn);
        return (panel, okBtn, cancelBtn);
    }

    // ═════════════════════════════════════════════════
    //  空字段兜底
    // ═════════════════════════════════════════════════

    private void BuildEmptyUi()
    {
        var (btnPanel, okBtn, cancelBtn) = BuildButtonPanel();
        okBtn.Click += (_, _) => OnOk();     // ★ 同样需要
        Controls.Add(btnPanel);
        AcceptButton = okBtn;
        CancelButton = cancelBtn;
        Size = new Size(320, ButtonAreaHeight + SystemInformation.CaptionHeight + 20);
    }

    // ═════════════════════════════════════════════════
    //  OK / 克隆 / 静态入口
    // ═════════════════════════════════════════════════

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

    public static T? Edit(T? initialValue = null, IWin32Window? owner = null, IEditorRegistry? registry = null)
    {
        using var form = new GenericEditorForm<T>(initialValue, registry);
        return form.ShowDialog(owner) == DialogResult.OK ? form.Result : null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _toolTip?.Dispose();
        base.Dispose(disposing);
    }

    // ═════════════════════════════════════════════════
    //  内部数据类型
    // ═════════════════════════════════════════════════

    private sealed record FieldGroupInfo(Control Control, int Height);
}
