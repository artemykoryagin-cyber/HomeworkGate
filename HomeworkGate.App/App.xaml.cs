using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using HomeworkGate.Shared;

namespace HomeworkGate.App;

public partial class App : System.Windows.Application
{
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private StateManager _stateManager = null!;
    private BlockerService _blockerService = null!;
    private DispatcherTimer _blockTimer = null!;
    private System.Drawing.Icon? _currentIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _stateManager = new StateManager();
        _blockerService = new BlockerService(_stateManager);

        SetupTrayIcon();
        StartBlockerTimer();

        if (!_stateManager.State.Tasks.Any() || e.Args.Contains("--show"))
            ShowMainWindow();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Visible = true,
            Text = "HomeworkGate"
        };

        UpdateTrayIcon();

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Открыть", null, (_, _) => ShowMainWindow());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => ExitApp());
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private System.Drawing.Icon CreateTrayIcon(bool isLocked)
    {
        var bmp = new System.Drawing.Bitmap(16, 16);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.Transparent);

        using var bodyBrush = new System.Drawing.SolidBrush(
            isLocked ? System.Drawing.Color.FromArgb(220, 60, 60)
                     : System.Drawing.Color.FromArgb(60, 180, 100));
        g.FillRectangle(bodyBrush, 2, 7, 12, 8);

        using var pen = new System.Drawing.Pen(
            isLocked ? System.Drawing.Color.FromArgb(200, 40, 40)
                     : System.Drawing.Color.FromArgb(40, 150, 80), 2);
        if (isLocked)
            g.DrawArc(pen, 3, 1, 10, 10, 180, 180);
        else
            g.DrawLines(pen, new System.Drawing.Point[] { new(3, 7), new(3, 3), new(13, 3) });

        var hIcon = bmp.GetHicon();
        var icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }

    private void StartBlockerTimer()
    {
        _blockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _blockTimer.Tick += (_, _) =>
        {
            _blockerService.EnforceBlock();
            UpdateTrayIcon();
        };
        _blockTimer.Start();
    }

    public void UpdateTrayIcon()
    {
        if (_trayIcon == null) return;
        var locked = _stateManager.State.IsLocked;

        var newIcon = CreateTrayIcon(locked);
        _trayIcon.Icon = newIcon;

        _currentIcon?.Dispose();
        _currentIcon = newIcon;

        _trayIcon.Text = locked
            ? "HomeworkGate — Приложения заблокированы"
            : "HomeworkGate — Всё разблокировано ✓";
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null || !_mainWindow.IsLoaded)
        {
            _mainWindow = new MainWindow(_stateManager);
            _mainWindow.Closed += (_, _) => _mainWindow = null;
        }
        _mainWindow.Show();
        _mainWindow.Activate();
        _mainWindow.WindowState = System.Windows.WindowState.Normal;
    }

    private void ExitApp()
    {
        _blockTimer.Stop();
        _trayIcon?.Dispose();
        _currentIcon?.Dispose();
        Shutdown();
    }
}
