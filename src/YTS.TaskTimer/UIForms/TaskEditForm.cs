

using YTS.Logic.Models;

namespace YTS.TaskTimer.UIForms
{

    public sealed class TaskEditForm : Form
    {
        private readonly ScheduleConfig _config;
        public ScheduleConfig Result => _config;

        private readonly TextBox _nameBox;
        private readonly TextBox _pathBox;
        private readonly TextBox _argsBox;
        private readonly TextBox _wdBox;
        private readonly TextBox _cronBox;
        private readonly CheckBox _enabledBox;
        private readonly Label _previewLabel;

        public TaskEditForm(ScheduleConfig config)
        {
            _config = config;

            Text = "编辑任务";
            Size = new Size(640, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 7,
                Padding = new Padding(12)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
            for (int i = 0; i < 6; i++) table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            table.Controls.Add(MakeLabel("名称"), 0, 0);
            _nameBox = new TextBox { Dock = DockStyle.Fill, Text = config.Name };
            table.Controls.Add(_nameBox, 1, 0);
            table.SetColumnSpan(_nameBox, 2);

            table.Controls.Add(MakeLabel("脚本路径"), 0, 1);
            _pathBox = new TextBox { Dock = DockStyle.Fill, Text = config.FilePath };
            table.Controls.Add(_pathBox, 1, 1);
            var browseBtn = new Button { Text = "浏览...", Dock = DockStyle.Fill };
            browseBtn.Click += (_, _) => BrowseFile();
            table.Controls.Add(browseBtn, 2, 1);

            table.Controls.Add(MakeLabel("参数"), 0, 2);
            _argsBox = new TextBox { Dock = DockStyle.Fill, Text = config.Arguments };
            table.Controls.Add(_argsBox, 1, 2);
            table.SetColumnSpan(_argsBox, 2);

            table.Controls.Add(MakeLabel("工作目录"), 0, 3);
            _wdBox = new TextBox { Dock = DockStyle.Fill, Text = config.WorkingDirectory };
            table.Controls.Add(_wdBox, 1, 3);
            table.SetColumnSpan(_wdBox, 2);

            table.Controls.Add(MakeLabel("Cron 表达式"), 0, 4);
            _cronBox = new TextBox { Dock = DockStyle.Fill, Text = config.Cron };
            _cronBox.TextChanged += (_, _) => UpdatePreview();
            table.Controls.Add(_cronBox, 1, 4);
            table.SetColumnSpan(_cronBox, 2);

            table.Controls.Add(MakeLabel("预览"), 0, 5);
            _previewLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DarkSlateBlue
            };
            table.Controls.Add(_previewLabel, 1, 5);
            table.SetColumnSpan(_previewLabel, 2);

            var quickPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            quickPanel.Controls.Add(MakeQuickBtn("每天9点", "0 9 * * *"));
            quickPanel.Controls.Add(MakeQuickBtn("每天18点", "0 18 * * *"));
            quickPanel.Controls.Add(MakeQuickBtn("工作日9点", "0 9 * * 1-5"));
            quickPanel.Controls.Add(MakeQuickBtn("每周一9点", "0 9 * * 1"));
            quickPanel.Controls.Add(MakeQuickBtn("每月1日2点", "0 2 1 * *"));
            _enabledBox = new CheckBox
            {
                Text = "启用",
                Checked = config.Enabled,
                AutoSize = true,
                Margin = new Padding(12, 10, 0, 0)
            };
            quickPanel.Controls.Add(_enabledBox);
            table.Controls.Add(quickPanel, 0, 6);
            table.SetColumnSpan(quickPanel, 3);

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 12, 0)
            };
            var okBtn = new Button
            {
                Text = "确定",
                Size = new Size(88, 30),
                BackColor = Color.Green,
                ForeColor = Color.White,
                Padding = new Padding(0),
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance =
                {
                    BorderSize = 0,
                },
            };
            var cancelBtn = new Button
            {
                Text = "取消",
                Size = new Size(88, 30),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.Red,
                ForeColor = Color.White,
                Padding = new Padding(0),
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance =
                {
                    BorderSize = 0,
                },
            };
            okBtn.Click += (_, _) => SaveAndClose();
            btnPanel.Controls.Add(cancelBtn);
            btnPanel.Controls.Add(okBtn);

            Controls.Add(table);
            Controls.Add(btnPanel);
            AcceptButton = okBtn;
            CancelButton = cancelBtn;

            UpdatePreview();
        }

        private static Label MakeLabel(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 8, 0)
        };

        private Button MakeQuickBtn(string text, string cron)
        {
            var b = new Button { Text = text, AutoSize = true, Height = 26, Margin = new Padding(0, 8, 4, 0) };
            b.Click += (_, _) => _cronBox.Text = cron;
            return b;
        }

        private void BrowseFile()
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "可执行文件|*.exe;*.bat;*.cmd;*.ps1;*.py|所有文件|*.*",
                CheckFileExists = true
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _pathBox.Text = dlg.FileName;
                if (string.IsNullOrWhiteSpace(_wdBox.Text))
                    _wdBox.Text = Path.GetDirectoryName(dlg.FileName) ?? "";
            }
        }

        private void UpdatePreview()
        {
            try
            {
                var expr = Cronos.CronExpression.Parse(_cronBox.Text, Cronos.CronFormat.Standard);
                var next = expr.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Local);
                _previewLabel.Text = next is null
                    ? "无法计算"
                    : $"下次触发：{next.Value:yyyy-MM-dd HH:mm:ss}";
                _previewLabel.ForeColor = Color.DarkSlateBlue;
            }
            catch (Exception ex)
            {
                _previewLabel.Text = $"表达式无效：{ex.Message}";
                _previewLabel.ForeColor = Color.Red;
            }
        }

        private void SaveAndClose()
        {
            if (string.IsNullOrWhiteSpace(_nameBox.Text))
            { MessageBox.Show(this, "请输入任务名称"); return; }

            if (string.IsNullOrWhiteSpace(_pathBox.Text) || !File.Exists(_pathBox.Text))
            { MessageBox.Show(this, "脚本/程序文件不存在"); return; }

            try { Cronos.CronExpression.Parse(_cronBox.Text, Cronos.CronFormat.Standard); }
            catch { MessageBox.Show(this, "Cron 表达式无效"); return; }

            _config.Name = _nameBox.Text.Trim();
            _config.FilePath = _pathBox.Text.Trim();
            _config.Arguments = _argsBox.Text.Trim();
            _config.WorkingDirectory = _wdBox.Text.Trim();
            _config.Cron = _cronBox.Text.Trim();
            _config.Enabled = _enabledBox.Checked;

            DialogResult = DialogResult.OK;
        }
    }

}
