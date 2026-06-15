using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace SYSi.BugTracker;

public partial class CrashWindow : Window
{
    private string? _logPath;
    private string _rawLog = string.Empty;

    private const int WM_NCHITTEST = 0x0084;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_NCLBUTTONUP = 0x00A2;
    private const int HTMAXBUTTON = 9;
    private const int HTCLIENT = 1;
    private bool _hoveringMaxButton;

    // ── Palette ──────────────────────────────────────────────────────────────

    private static readonly SolidColorBrush BrushDefault = Brush("#CBD5E1");
    private static readonly SolidColorBrush BrushHeader = Brush("#F8FAFC");
    private static readonly SolidColorBrush BrushSeparator = Brush("#1E3A5F");
    private static readonly SolidColorBrush BrushLabel = Brush("#60A5FA");
    private static readonly SolidColorBrush BrushError = Brush("#F87171");
    private static readonly SolidColorBrush BrushValue = Brush("#34D399");
    private static readonly SolidColorBrush BrushStackAt = Brush("#94A3B8");
    private static readonly SolidColorBrush BrushStackPath = Brush("#475569");
    private static readonly SolidColorBrush BrushMeta = Brush("#A78BFA");
    private static readonly SolidColorBrush BrushInner = Brush("#FB923C");
    private static readonly SolidColorBrush HoverBrush = new(Color.FromRgb(0x1E, 0x29, 0x3B));
    private static readonly SolidColorBrush DefaultWinCtrlButtonForegroundBrush = Brush("#94A3B8");
    private static readonly SolidColorBrush HoverWinCtrlButtonForegroundBrush = Brush("#F1F5F9");
    private static readonly SolidColorBrush PressedBrush = Brush("#0F172A");

    public CrashWindow(string? logPath, string appName)
    {
        InitializeComponent();

        SourceInitialized += OnSourceInitialized;

        // Wire up SystemCommands — không cần code-behind cho window controls
        CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand,
            (_, _) => SystemCommands.MinimizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand,
            (_, _) => SystemCommands.MaximizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand,
            (_, _) => SystemCommands.RestoreWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand,
            (_, _) => SystemCommands.CloseWindow(this)));

        Title = $"{appName} — Bug Tracker";
        TitleText.Text = $"{appName} has crashed";

        _logPath = logPath;
        LoadLog();

        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        };
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var source = (HwndSource)PresentationSource.FromVisual(this)!;
        source.AddHook(WndProc);
    }

    private IntPtr WndProc(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (msg == WM_NCHITTEST)
        {
            Point screenPoint = GetPointFromLParam(lParam);

            bool hover = IsOverMaximizeButton(screenPoint);

            if (hover != _hoveringMaxButton)
            {
                _hoveringMaxButton = hover;

                Dispatcher.BeginInvoke(() =>
                {
                    MaximizeButton.Background =
                        hover ? HoverBrush : Brushes.Transparent;

                    Dispatcher.BeginInvoke(() =>
                    {
                        MaximizeButton.Background =
                            hover ? HoverBrush : Brushes.Transparent;

                        MaximizeButton.Foreground =
                            hover ? HoverWinCtrlButtonForegroundBrush : DefaultWinCtrlButtonForegroundBrush;
                    });
                });
            }

            if (hover)
            {
                handled = true;
                return (IntPtr)HTMAXBUTTON;
            }
        }

        return IntPtr.Zero;
    }

    private static Point GetPointFromLParam(IntPtr lParam)
    {
        long value = lParam.ToInt64();

        int x = unchecked((short)(value & 0xFFFF));
        int y = unchecked((short)((value >> 16) & 0xFFFF));

        return new Point(x, y);
    }

    private bool IsOverMaximizeButton(Point screenPoint)
    {
        if (MaximizeButton == null)
        {
            return false;
        }

        Point topLeft =
            MaximizeButton.PointToScreen(new Point(0, 0));

        Rect rect = new(
            topLeft.X,
            topLeft.Y,
            MaximizeButton.ActualWidth,
            MaximizeButton.ActualHeight);

        return rect.Contains(screenPoint);
    }

    // ── Load & render ────────────────────────────────────────────────────────

    private void LoadLog()
    {
        if (_logPath == null || !File.Exists(_logPath))
        {
            ShowPlainError("Log file not found.\nPlease check the path passed as argument.");
            LogPathText.Text = _logPath ?? "(no path provided)";
            OpenFolderBtn.IsEnabled = false;
            DeleteBtn.IsEnabled = false;
            return;
        }

        try
        {
            _rawLog = File.ReadAllText(_logPath);
        }
        catch (Exception ex)
        {
            ShowPlainError($"Could not read log file:\n{ex.Message}");
            return;
        }

        LogPathText.Text = _logPath;
        RenderLog(_rawLog);
        BuildMetaBar(_rawLog);
    }

    private void ShowPlainError(string message)
    {
        LogBlock.Inlines.Clear();
        LogBlock.Inlines.Add(new Run(message) { Foreground = BrushError });
    }

    // ── Syntax-highlighted render ─────────────────────────────────────────────

    private void RenderLog(string log)
    {
        LogBlock.Inlines.Clear();

        foreach (string rawLine in log.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            RenderLine(line);
            LogBlock.Inlines.Add(new Run("\n"));
        }
    }

    private void RenderLine(string line)
    {
        // ── Separator
        if (line.StartsWith('─') || (line.StartsWith('-') && line.Length > 20 && line.Trim('-').Length == 0))
        {
            LogBlock.Inlines.Add(new Run(line) { Foreground = BrushSeparator });
            return;
        }

        // ── Top header
        if (line.StartsWith("SYSi") || line.StartsWith("App Crash") || line.StartsWith("Crash Report"))
        {
            LogBlock.Inlines.Add(new Run(line) { Foreground = BrushHeader, FontWeight = FontWeights.SemiBold });
            return;
        }

        // ── Meta fields
        if (TrySplitLabel(line, out string label, out string value) && IsMetaLabel(label))
        {
            LogBlock.Inlines.Add(new Run(label + " : ") { Foreground = BrushLabel });
            LogBlock.Inlines.Add(new Run(value) { Foreground = BrushValue });
            return;
        }

        // ── Exception
        if (line.TrimStart().StartsWith("Exception :") || line.TrimStart().StartsWith("Exception:"))
        {
            string indent = GetIndent(line);
            int colon = line.IndexOf(':');
            string rest = line[(colon + 1)..].Trim();
            LogBlock.Inlines.Add(new Run(indent + "Exception : ") { Foreground = BrushLabel });
            LogBlock.Inlines.Add(new Run(rest) { Foreground = BrushError, FontWeight = FontWeights.SemiBold });
            return;
        }

        // ── Message
        if (line.TrimStart().StartsWith("Message :") || line.TrimStart().StartsWith("Message:"))
        {
            string indent = GetIndent(line);
            int colon = line.IndexOf(':');
            string rest = line[(colon + 1)..].Trim();
            LogBlock.Inlines.Add(new Run(indent + "Message   : ") { Foreground = BrushLabel });
            LogBlock.Inlines.Add(new Run(rest) { Foreground = BrushDefault });
            return;
        }

        // ── StackTrace label
        if (line.TrimStart().StartsWith("StackTrace"))
        {
            string indent = GetIndent(line);
            LogBlock.Inlines.Add(new Run(indent + "StackTrace:") { Foreground = BrushLabel });
            return;
        }

        // ── Stack frame
        if (line.TrimStart().StartsWith("at "))
        {
            RenderStackFrame(line);
            return;
        }

        // ── InnerException marker
        if (line.TrimStart().StartsWith("─ InnerException:") || line.TrimStart().StartsWith("- InnerException:"))
        {
            string indent = GetIndent(line);
            LogBlock.Inlines.Add(new Run(indent + "─ InnerException:") { Foreground = BrushInner, FontWeight = FontWeights.SemiBold });
            return;
        }

        // ── Timestamp tag [...]
        if (line.StartsWith('[') && line.Contains(']'))
        {
            int end = line.IndexOf(']');
            string tag = line[..(end + 1)];
            string rest = line[(end + 1)..];
            LogBlock.Inlines.Add(new Run(tag) { Foreground = BrushMeta });
            LogBlock.Inlines.Add(new Run(rest) { Foreground = BrushDefault });
            return;
        }

        // ── Default
        LogBlock.Inlines.Add(new Run(line) { Foreground = BrushDefault });
    }

    private void RenderStackFrame(string line)
    {
        string indent = GetIndent(line);
        string trimmed = line.TrimStart();

        int inIdx = trimmed.LastIndexOf(" in ", StringComparison.Ordinal);
        if (inIdx >= 0)
        {
            string method = trimmed[..inIdx];
            string filePart = trimmed[(inIdx + 4)..];

            LogBlock.Inlines.Add(new Run(indent) { Foreground = BrushStackAt });
            LogBlock.Inlines.Add(new Run(method) { Foreground = BrushStackAt });
            LogBlock.Inlines.Add(new Run(" in ") { Foreground = BrushSeparator });
            LogBlock.Inlines.Add(new Run(filePart) { Foreground = BrushStackPath });
        }
        else
        {
            LogBlock.Inlines.Add(new Run(line) { Foreground = BrushStackAt });
        }
    }

    // ── Meta bar (pills) ──────────────────────────────────────────────────────

    private void BuildMetaBar(string log)
    {
        var fields = new List<(string Label, string Value)>();

        foreach (string rawLine in log.Split('\n'))
        {
            string line = rawLine.Trim();
            if (TrySplitLabel(line, out string label, out string value) && IsMetaLabel(label))
            {
                fields.Add((label.Trim(), value.Trim()));
            }
        }

        if (_logPath != null && File.Exists(_logPath))
        {
            var created = File.GetCreationTime(_logPath);
            fields.Insert(0, ("Time", created.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        foreach (var (label, value) in fields)
        {
            var pill = new Border
            {
                Background        = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                CornerRadius      = new CornerRadius(5),
                Padding           = new Thickness(10, 4, 10, 4),
                Margin            = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock
            {
                Text       = label + ": ",
                Foreground = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA)),
                FontSize   = 11,
            });
            sp.Children.Add(new TextBlock
            {
                Text       = value,
                Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
                FontSize   = 11,
            });

            pill.Child = sp;
            MetaPanel.Children.Add(pill);
        }
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_rawLog))
        {
            Clipboard.SetText(_rawLog);
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_logPath != null && File.Exists(_logPath))
        {
            Process.Start("explorer.exe", $"/select,\"{_logPath}\"");
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_logPath == null || !File.Exists(_logPath))
        {
            return;
        }

        var result = MessageBox.Show(
            "Delete this log file?\n" + _logPath,
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            File.Delete(_logPath);
            LogBlock.Inlines.Clear();
            LogBlock.Inlines.Add(new Run("Log file deleted.") { Foreground = BrushValue });
            LogPathText.Text        = "(deleted)";
            DeleteBtn.IsEnabled     = false;
            OpenFolderBtn.IsEnabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not delete file:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool TrySplitLabel(string line, out string label, out string value)
    {
        int colon = line.IndexOf(':');
        if (colon <= 0 || colon >= line.Length - 1)
        {
            label = value = string.Empty;
            return false;
        }
        label = line[..colon];
        value = line[(colon + 1)..].Trim();
        return true;
    }

    private static bool IsMetaLabel(string label)
    {
        string t = label.Trim();
        return t is "Source" or "OS" or "Runtime" or "Version"
                  or "Time" or "App" or "Machine" or "User";
    }

    private static string GetIndent(string line)
    {
        int i = 0;
        while (i < line.Length && line[i] == ' ')
        {
            i++;
        }

        return line[..i];
    }

    private static SolidColorBrush Brush(string hex)
    {
        hex = hex.TrimStart('#');
        byte r = Convert.ToByte(hex[0..2], 16);
        byte g = Convert.ToByte(hex[2..4], 16);
        byte b = Convert.ToByte(hex[4..6], 16);
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}