using System.IO;
using System.Windows;
using Microsoft.Win32;
using HomeworkGate.Shared;

namespace HomeworkGate.App;

public partial class SubmitSolutionDialog : Window
{
    public string TextAnswer { get; private set; } = "";
    public List<string> ImagePaths { get; private set; } = new();
    public List<string> FilePaths { get; private set; } = new();

    public SubmitSolutionDialog(HomeworkTask task)
    {
        InitializeComponent();
        TaskTitleLabel.Text = task.Title;
        TaskDescLabel.Text = task.Description;
    }

    private void AddImage_Click(object s, RoutedEventArgs e)
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Выбери изображение",
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp",
            Multiselect = true
        };
        if (ofd.ShowDialog() != true) return;
        foreach (var f in ofd.FileNames)
        {
            ImagePaths.Add(f);
            ImageList.Items.Add(Path.GetFileName(f));
        }
    }

    private void AddFile_Click(object s, RoutedEventArgs e)
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Выбери файл с решением",
            Filter = "Текстовые файлы|*.txt;*.md;*.py;*.cs;*.js;*.ts;*.java;*.cpp;*.c;*.html;*.pdf|Все файлы|*.*",
            Multiselect = true
        };
        if (ofd.ShowDialog() != true) return;
        foreach (var f in ofd.FileNames)
        {
            FilePaths.Add(f);
            FileList.Items.Add(Path.GetFileName(f));
        }
    }

    private void Submit_Click(object s, RoutedEventArgs e)
    {
        TextAnswer = AnswerInput.Text.Trim();
        if (string.IsNullOrEmpty(TextAnswer) && !ImagePaths.Any() && !FilePaths.Any())
        {
            System.Windows.MessageBox.Show("Добавь хотя бы текстовое решение или прикрепи файл.",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object s, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
