
using System.Collections.Concurrent;
using System.Text;
using YTS.Logic;
using YTS.Logic.Models;

namespace YTS.TaskTimer.UITools
{
    /// <summary>
    /// 接口: UI日志展示
    /// </summary>
    public interface IUILogView : IDisposable
    {
        /// <summary>
        /// 获取展示控件
        /// </summary>
        Control GetLogControl();

        /// <summary>
        /// 写入日志内容
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="type">类型 (高亮展示)</param>
        void WriteLog(string message, EUILogViewType type = EUILogViewType.INFO);
    }
    /// <summary>
    /// 枚举: UI日志类型
    /// </summary>
    public enum EUILogViewType
    {
        INFO = 1,
        DEBUG = 2,
        ERROR = 3,
        WARN = 4,
    }

    public class LogListView : IUILogView
    {
        private sealed record LogEntry(DateTime Time, EUILogViewType Level, string Message);
        private readonly ListView _logList;
        private readonly List<LogEntry> _logs = [];
        private readonly ConcurrentQueue<LogEntry> _pending = new();
        private System.Windows.Forms.Timer? _flushTimer;
        private readonly int[] _fixedColWidths = [150, 60]; // 时间 / 级别
        private const int LastColMinWidth = 120;                  // 消息列最小宽度
        private readonly ContextMenuStrip _logMenu;
        private const string time_format = "yyyy-hh-dd HH:mm:ss";

        private readonly IAppConfigManage configManage;
        public LogListView(IAppConfigManage configManage)
        {
            this.configManage = configManage;
            _logList = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                Font = new Font("Consolas", 9f),
                VirtualMode = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                ShowItemToolTips = true,
            };
            _logList.Columns.Add("时间", _fixedColWidths[0]);
            _logList.Columns.Add("级别", _fixedColWidths[1]);
            _logList.Columns.Add("消息", 600);
            _logList.RetrieveVirtualItem += OnRetrieveVirtualItem;
            _logList.Resize += (_, _) =>
            {
                ResizeLogColumns();
                // ★ 最大化/最小化时，虚拟项缓存失效，需要强制重绘
                _logList.Invalidate(true);   // true = 同时重绘子控件
            };
            _logList.KeyDown += (_, e) =>
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    e.Handled = true;      // ★ 阻止 ListView 默认行为（避免系统“叮”一声）
                    e.SuppressKeyPress = true;  // ★ 阻止 Ctrl+C 被当普通字符处理
                    CopySelectedRows();
                }
            };

            _flushTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _flushTimer.Tick += (_, _) => FlushLog();
            _flushTimer.Start();

            _logMenu = new ContextMenuStrip();

            var copyRow = new ToolStripMenuItem("复制整行", null, (_, _) => CopySelectedRows())
            {
                ShortcutKeyDisplayString = "Ctrl+C"
            };

            var copyMsg = new ToolStripMenuItem("仅复制消息", null, (_, _) => CopySelectedRows(messageOnly: true));

            var copyAll = new ToolStripMenuItem("复制全部日志", null, (_, _) => CopyAllRows());

            var clearLog = new ToolStripMenuItem("清空日志", null, (_, _) => ClearLogs());

            _logMenu.Items.Add(copyRow);
            _logMenu.Items.Add(copyMsg);
            _logMenu.Items.Add(copyAll);
            _logMenu.Items.Add(new ToolStripSeparator());
            _logMenu.Items.Add(clearLog);

            // 右键时动态启用/禁用菜单项
            _logMenu.Opening += (_, _) =>
            {
                bool hasSelection = _logList.SelectedIndices.Count > 0;
                copyRow.Enabled = hasSelection;
                copyMsg.Enabled = hasSelection;
                copyAll.Enabled = _logs.Count > 0;
                clearLog.Enabled = _logs.Count > 0;
            };

            _logList.ContextMenuStrip = _logMenu;
        }

        private static string ToMessage(LogEntry? log)
        {
            if (log == null) return "<空日志>";
            return $"{log.Time.ToString(time_format)}\t{TypeToStr(log.Level)}\t{log.Message}";
        }

        public void Dispose()
        {
            if (_flushTimer != null)
            {
                _flushTimer.Stop();
                _flushTimer.Dispose();
                _flushTimer = null;
            }
            _logMenu?.Dispose();
            _logList?.Dispose();
        }

        public Control GetLogControl() => _logList;

        public void WriteLog(string message, EUILogViewType type = EUILogViewType.INFO)
        {
            _pending.Enqueue(new LogEntry(DateTime.Now, type, message));
        }

        private void FlushLog()
        {
            if (_pending.IsEmpty) return;

            int added = 0;
            while (_pending.TryDequeue(out var entry))
            {
                _logs.Add(entry);
                added++;
            }
            if (added == 0) return;

            AppConfig config = configManage.Read();
            // 记录限行前是否发生过 RemoveRange（会改变索引基准，需要后续强制刷新）
            bool trimmed = false;
            if (_logs.Count > config.LogConfig.LogMaxLineCount)
            {
                int removeCount = _logs.Count - config.LogConfig.LogMaxLineCount;
                _logs.RemoveRange(0, removeCount);
                trimmed = true;
            }

            _logList.BeginUpdate();
            try
            {
                _logList.VirtualListSize = _logs.Count;
            }
            finally
            {
                _logList.EndUpdate();
            }

            // ★ 限行后必须 Refresh：RemoveRange 改变了所有索引，旧滚动位置失效
            // 普通追加时用 Invalidate 足够；限行后 Invalidate 可能触发"幽灵行"
            if (trimmed)
                _logList.Refresh();
            else
                _logList.Invalidate();

            ResizeLogColumns();

            if (_logs.Count > 0)
            {
                // ★ 延迟到布局完成后再 EnsureVisible
                // 直接在 FlushLog 里调用，此时 ListView 内部布局可能还没算完
                // 滚动位置会算错 → 显示在"底部往上 1/3"
                int lastIndex = _logs.Count - 1;
                _logList.BeginInvoke(() =>
                {
                    try
                    {
                        if (_logList.IsDisposed) return;
                        if (lastIndex >= _logList.VirtualListSize) return;
                        _logList.EnsureVisible(lastIndex);
                    }
                    catch { }
                });
            }
        }

        private void OnRetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex < 0 || e.ItemIndex >= _logs.Count) return;
            var log = _logs[e.ItemIndex];

            e.Item = new ListViewItem(log.Time.ToString(time_format));
            e.Item.SubItems.Add(TypeToStr(log.Level));
            e.Item.SubItems.Add(log.Message);
            e.Item.ForeColor = TypeToColor(log.Level);
        }

        private static string TypeToStr(EUILogViewType type)
        {
            return type switch
            {
                EUILogViewType.ERROR => "错误",
                EUILogViewType.DEBUG => "调试",
                EUILogViewType.WARN => "警告",
                _ => "消息",
            };
        }
        private static Color TypeToColor(EUILogViewType type)
        {
            return type switch
            {
                EUILogViewType.ERROR => Color.Red,
                EUILogViewType.DEBUG => Color.Gray,
                EUILogViewType.WARN => Color.DarkOrange,
                _ => Color.Black,
            };
        }

        private void ResizeLogColumns()
        {
            if (_logList.Columns.Count < 3) return;

            // ClientSize 是客户区，不含垂直滚动条和边框，直接用即可
            int available = _logList.ClientSize.Width;

            // 前三列恢复固定宽度，并累加
            int fixedSum = 0;
            for (int i = 0; i < _fixedColWidths.Length; i++)
            {
                _logList.Columns[i].Width = _fixedColWidths[i];
                fixedSum += _fixedColWidths[i];
            }

            // 最后一列吃剩余；预留 2px，避免因取整/子项边距出现横向滚动条
            int lastWidth = Math.Max(LastColMinWidth, available - fixedSum - 2);
            _logList.Columns[2].Width = lastWidth;
        }

        private void CopySelectedRows(bool messageOnly = false)
        {
            // ★ VirtualMode 下只信 SelectedIndices
            if (_logList.SelectedIndices.Count == 0)
            {
                MessageBox.Show("请先选中要复制的日志行。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sb = new StringBuilder();
            // 按用户看到的选择顺序（索引升序）输出，避免选择顺序混乱
            var indices = _logList.SelectedIndices.Cast<int>().OrderBy(i => i);

            foreach (int idx in indices)
            {
                if (idx < 0 || idx >= _logs.Count) continue;   // 越界兜底
                var log = _logs[idx];

                if (messageOnly)
                {
                    sb.AppendLine(log.Message);
                }
                else
                {
                    // 制表符分隔，粘到 Excel/记事本里对齐好看
                    sb.AppendLine(ToMessage(log));
                }
            }

            try
            {
                Clipboard.SetText(sb.ToString());
            }
            catch (Exception ex)
            {
                // 剪贴板被其他进程占用时 SetText 会抛 ExternalException，必须兜底
                MessageBox.Show($"复制失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CopyAllRows()
        {
            if (_logs.Count == 0) return;

            var sb = new StringBuilder(_logs.Count * 64);
            foreach (var log in _logs)
                sb.AppendLine(ToMessage(log));

            try { Clipboard.SetText(sb.ToString()); }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ClearLogs()
        {
            _logs.Clear();
            _pending.Clear();

            _logList.BeginUpdate();
            try
            {
                // ★ 核心修复：临时切换 VirtualMode 来彻底清空内部状态
                // 直接 _logList.VirtualListSize = 0 只改"总数"，滚动位置残留在旧值
                // 切换模式会重建 ListView 的虚拟列表内部结构，滚动位置真正归零
                _logList.VirtualMode = false;
                _logList.Items.Clear();      // 真正清空 Items
                _logList.VirtualMode = true;
                // 此时 ListView 内部 TopItem = null, ScrollPosition = 0
            }
            finally
            {
                _logList.EndUpdate();
            }

            // ★ 用 Refresh() 而不是 Invalidate()：同步强制重绘
            _logList.Refresh();
            ResizeLogColumns();
        }
    }
}
