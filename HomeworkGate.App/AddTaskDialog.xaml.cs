using System.Windows;
using HomeworkGate.Shared;

namespace HomeworkGate.App;

public partial class AddTaskDialog : Window
{
    public HomeworkTask? Result { get; private set; }

    public AddTaskDialog()
    {
        InitializeComponent();
    }

    private void Create_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleInput.Text))
        {
            System.Windows.MessageBox.Show("Укажи название задания.", "Ошибка", MessageBoxButton.OK);
            return;
        }
        if (string.IsNullOrWhiteSpace(DescInput.Text))
        {
            System.Windows.MessageBox.Show("Укажи условие задания.", "Ошибка", MessageBoxButton.OK);
            return;
        }

        Result = new HomeworkTask
        {
            Title = TitleInput.Text.Trim(),
            Description = DescInput.Text.Trim(),
            IsMandatory = IsMandatoryCheck.IsChecked == true
        };
        DialogResult = true;
    }

    private void Cancel_Click(object s, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
