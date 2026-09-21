using System.Windows;

namespace AudioAnchor.App;

public partial class InfoDialog : Window
{
    public InfoDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }
    private void OkClicked(object sender, RoutedEventArgs e) => Close();
}
