using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Win32;
using HomeworkGate.Shared;

using WpfButton = System.Windows.Controls.Button;
using WpfMessageBox = System.Windows.MessageBox;
using WpfApplication = System.Windows.Application;

namespace HomeworkGate.App;

public partial class MainWindow : Window
{
    private readonly StateManager _state;
    private const string GuardianServiceName = "HomeworkGateGuardian";
    private string? _updateUrl;

    public MainWindow(StateManager state)
    {
        _state = state;
        InitializeComponent();
        _state.StateChanged += RefreshUI;
        ApiKeyInput.Text = _state.State.ApiKey;
        RefreshUI();
        NavTasks_Click(this, new RoutedEventArgs());
        UpdateAutostartButton();
        RefreshGuardianStatus();
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        var update = await UpdateChecker.CheckAsync();
        if (update == null) return;

        _updateUrl = update.DownloadUrl;
        Dispatcher.Invoke(() =>
        {
            UpdateTitle.Text = $"🔔 Доступно обновление v{update.Version}";
            UpdateNotes.Text = string.IsNullOrWhiteSpace(update.ReleaseNotes)
                ? "" : update.ReleaseNotes;
            UpdateBanner.Visibility = System.Windows.Visibility.Visible;
        });
    }

    private void UpdateDownload_Click(object s, RoutedEventArgs e)
    {
        if (_updateUrl != null)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_updateUrl)
                { UseShellExecute = true });
    }

    private void UpdateDismiss_Click(object s, RoutedEventArgs e)
    {
        UpdateBanner.Visibility = System.Windows.Visibility.Collapsed;
    }

    #region Navigation
    private void ShowPage(System.Windows.Controls.Grid page)
    {
        PageTasks.Visibility = Visibility.Collapsed;
        PageApps.Visibility = Visibility.Collapsed;
        PageSettings.Visibility = Visibility.Collapsed;
        page.Visibility = Visibility.Visible;
    }
    private void NavTasks_Click(object s, RoutedEventArgs e) => ShowPage(PageTasks);
    private void NavApps_Click(object s, RoutedEventArgs e) => ShowPage(PageApps);
    private void NavSettings_Click(object s, RoutedEventArgs e)
    {
        ShowPage(PageSettings);
        RefreshGuardianStatus();
    }
    #endregion

    #region UI Refresh
    private void RefreshUI()
    {
        Dispatcher.Invoke(() =>
        {
            RefreshTaskList();
            RefreshAppList();
            RefreshLockStatus();
            RefreshTaskSummary();
        });
    }

    private void RefreshLockStatus()
    {
        var locked = _state.State.IsLocked;
        LockStatusIcon.Text = locked ? "🔒" : "🔓";
        LockStatusText.Text = locked ? "Приложения заблокированы" : "Всё разблокировано";
        LockStatusText.Foreground = locked
            ? (System.Windows.Media.Brush)FindResource("BrAccentRed")
            : (System.Windows.Media.Brush)FindResource("BrAccentGreen");
    }

    private void RefreshTaskList()
    {
        var tasks = _state.State.Tasks
            .OrderBy(t => t.Status)
            .ThenByDescending(t => t.CreatedAt)
            .ToList();
        TaskList.ItemsSource = null;
        TaskList.ItemsSource = tasks;
        EmptyTasksPlaceholder.Visibility = tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshAppList()
    {
        AppList.ItemsSource = null;
        AppList.ItemsSource = _state.State.BlockedApps;
        EmptyAppsPlaceholder.Visibility = _state.State.BlockedApps.Count == 0
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshTaskSummary()
    {
        var total = _state.State.Tasks.Count;
        var done = _state.State.Tasks.Count(t => t.Status == HwStatus.Completed);
        TaskSummary.Text = $"{total} заданий • {done} выполнено";
    }

    private void RefreshGuardianStatus()
    {
        var running = IsGuardianRunning();
        ServiceStatusText.Text = running ? "✓ Работает" : "✗ Остановлен";
        ServiceStatusText.Foreground = running
            ? (System.Windows.Media.Brush)FindResource("BrAccentGreen")
            : (System.Windows.Media.Brush)FindResource("BrAccentRed");

        GuardianDot.Fill = running
            ? (System.Windows.Media.Brush)FindResource("BrAccentGreen")
            : (System.Windows.Media.Brush)FindResource("BrAccentRed");
        GuardianText.Text = running ? "Защита активна" : "Защита не активна";
    }
    #endregion

    #region Task Actions
    private void AddTask_Click(object s, RoutedEventArgs e)
    {
        var dlg = new AddTaskDialog { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Result != null)
            _state.AddTask(dlg.Result);
    }

    private void DeleteTask_Click(object s, RoutedEventArgs e)
    {
        if (s is WpfButton btn && btn.Tag is Guid id)
        {
            var res = WpfMessageBox.Show("Удалить задание?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
                _state.RemoveTask(id);
        }
    }

    private async void SubmitSolution_Click(object s, RoutedEventArgs e)
    {
        if (s is not WpfButton btn || btn.Tag is not Guid id) return;
        var task = _state.State.Tasks.FirstOrDefault(t => t.Id == id);
        if (task == null) return;

        if (task.Status == HwStatus.Completed)
        {
            WpfMessageBox.Show("Это задание уже выполнено!", "Info", MessageBoxButton.OK);
            return;
        }

        var dlg = new SubmitSolutionDialog(task) { Owner = this };
        if (dlg.ShowDialog() != true) return;

        if (string.IsNullOrWhiteSpace(_state.State.ApiKey))
        {
            WpfMessageBox.Show("Укажи Anthropic API ключ в настройках!", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            ShowPage(PageSettings);
            return;
        }

        var progressDlg = new ProgressDialog { Owner = this };
        progressDlg.Show();

        task.Status = HwStatus.InReview;
        task.AttemptCount++;
        _state.UpdateTask(task);

        EvaluationResult? result = null;
        try
        {
            var evaluator = new AiEvaluator(_state.State.ApiKey);
            var progress = new Progress<string>(msg => progressDlg.UpdateMessage(msg));
            result = await evaluator.EvaluateAsync(task, dlg.TextAnswer, dlg.ImagePaths, dlg.FilePaths, progress);

            task.LastFeedback = result.Feedback;

            if (result.IsPassed)
            {
                task.Status = HwStatus.Completed;
                task.CompletedAt = DateTime.Now;
            }
            else
            {
                task.Status = HwStatus.Pending;
            }
        }
        catch (Exception ex)
        {
            task.Status = HwStatus.Pending;
            task.LastFeedback = $"Ошибка: {ex.Message}";
            result = new EvaluationResult(false, ex.Message, 0);
        }
        finally
        {
            _state.UpdateTask(task);
            progressDlg.Close();
        }

        if (result!.IsPassed)
        {
            var allDone = !_state.State.IsLocked;
            WpfMessageBox.Show(
                $"✅ Задание выполнено! (оценка: {result.Score}/100)\n\n{result.Feedback}\n\n" +
                (allDone ? "🎮 Все задания выполнены! Игры разблокированы." : ""),
                "Отлично!", MessageBoxButton.OK, MessageBoxImage.Information);

            ((App)WpfApplication.Current).UpdateTrayIcon();
        }
        else if (result.Score > 0)
        {
            WpfMessageBox.Show($"❌ Решение не зачтено (оценка: {result.Score}/100)\n\n{result.Feedback}",
                "Попробуй ещё раз", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            WpfMessageBox.Show($"Ошибка при проверке:\n{result.Feedback}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

    #region App Blocker Actions
    private void AddApp_Click(object s, RoutedEventArgs e)
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Выбери исполняемый файл",
            Filter = "Приложения (*.exe)|*.exe",
            Multiselect = false
        };
        if (ofd.ShowDialog() != true) return;

        var exePath = ofd.FileName;
        var processName = Path.GetFileNameWithoutExtension(exePath);
        string? displayName = null;
        try { displayName = FileVersionInfo.GetVersionInfo(exePath).ProductName; } catch { }

        _state.AddBlockedApp(new BlockedApp
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? processName : displayName,
            ExecutablePath = exePath,
            ProcessName = processName
        });
    }

    private void RemoveApp_Click(object s, RoutedEventArgs e)
    {
        if (s is WpfButton btn && btn.Tag is Guid id)
            _state.RemoveBlockedApp(id);
    }
    #endregion

    #region Settings
    private void SaveApiKey_Click(object s, RoutedEventArgs e)
    {
        _state.SetApiKey(ApiKeyInput.Text.Trim());
        WpfMessageBox.Show("API ключ сохранён.", "Сохранено", MessageBoxButton.OK);
    }

    private void Hyperlink_Navigate(object s, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void ToggleAutostart_Click(object s, RoutedEventArgs e)
    {
        var key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        if (key == null) return;

        const string name = "HomeworkGate";
        if (key.GetValue(name) == null)
        {
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location
                .Replace(".dll", ".exe");
            key.SetValue(name, $"\"{exePath}\"");
            BtnAutostart.Content = "Отключить";
            WpfMessageBox.Show("Автозапуск включён.", "OK", MessageBoxButton.OK);
        }
        else
        {
            key.DeleteValue(name, false);
            BtnAutostart.Content = "Включить";
            WpfMessageBox.Show("Автозапуск отключён.", "OK", MessageBoxButton.OK);
        }
        key.Close();
    }

    private void UpdateAutostartButton()
    {
        var key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
        if (key?.GetValue("HomeworkGate") != null)
            BtnAutostart.Content = "Отключить";
        key?.Close();
    }
    #endregion

    #region Guardian Service Management
    private static bool IsGuardianRunning()
    {
        try
        {
            using var sc = new ServiceController(GuardianServiceName);
            return sc.Status == ServiceControllerStatus.Running;
        }
        catch { return false; }
    }

    private void InstallGuardian_Click(object s, RoutedEventArgs e)
    {
        try
        {
            var dir = AppContext.BaseDirectory;
            var guardianExe = Path.Combine(dir, "HomeworkGate.Guardian.exe");

            if (!File.Exists(guardianExe))
            {
                WpfMessageBox.Show(
                    $"Не найден файл:\n{guardianExe}\n\nУбедись, что Guardian собран вместе с приложением.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RunSc($"create {GuardianServiceName} binPath= \"{guardianExe}\" start= auto DisplayName= \"HomeworkGate Guardian\"");
            RunSc($"description {GuardianServiceName} \"Блокирует игры пока не выполнены задания (HomeworkGate)\"");
            RunSc($"start {GuardianServiceName}");

            System.Threading.Thread.Sleep(1500);
            RefreshGuardianStatus();
            WpfMessageBox.Show("Guardian Service установлен и запущен.\nТеперь блокировку нельзя обойти через Диспетчер задач.",
                "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"Ошибка установки сервиса:\n{ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UninstallGuardian_Click(object s, RoutedEventArgs e)
    {
        try
        {
            RunSc($"stop {GuardianServiceName}");
            System.Threading.Thread.Sleep(1000);
            RunSc($"delete {GuardianServiceName}");
            System.Threading.Thread.Sleep(500);
            RefreshGuardianStatus();
            WpfMessageBox.Show("Guardian Service остановлен.", "OK", MessageBoxButton.OK);
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void RunSc(string args)
    {
        var psi = new ProcessStartInfo("sc.exe", args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi)!;
        p.WaitForExit(5000);
    }
    #endregion
}
