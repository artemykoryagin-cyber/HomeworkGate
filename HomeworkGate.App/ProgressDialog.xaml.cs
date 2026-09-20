using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using HomeworkGate.Shared;

using WpfColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;

namespace HomeworkGate.App;

public partial class ProgressDialog : Window
{
    public ProgressDialog() { InitializeComponent(); }

    public void UpdateMessage(string msg) =>
        Dispatcher.Invoke(() => StatusText.Text = msg);
}

// ─── Converters ───────────────────────────────────────────────────────────────

public class HwStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is HwStatus status
            ? status switch
            {
                HwStatus.Completed => new SolidColorBrush(WpfColor.FromRgb(57, 217, 138)),
                HwStatus.InReview  => new SolidColorBrush(WpfColor.FromRgb(77, 159, 255)),
                HwStatus.Failed    => new SolidColorBrush(WpfColor.FromRgb(255, 77, 106)),
                _                  => new SolidColorBrush(WpfColor.FromRgb(255, 209, 102))
            }
            : WpfBrushes.Gray;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class HwStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is HwStatus status
            ? status switch
            {
                HwStatus.Completed => "✓ Выполнено",
                HwStatus.InReview  => "⟳ На проверке",
                HwStatus.Failed    => "✗ Не зачтено",
                _                  => "◎ Ожидает"
            }
            : "Неизвестно";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s) return string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible;
        if (value is bool b)   return b ? Visibility.Visible : Visibility.Collapsed;
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
