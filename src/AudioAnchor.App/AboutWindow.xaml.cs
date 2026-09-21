using System.Diagnostics;
using System.Windows;

namespace AudioAnchor.App;

public partial class AboutWindow : Window
{
    public AboutWindow(Version version)
    {
        InitializeComponent();
        VersionText.Text = $"Version {version}";
    }
    private void RepositoryLinkClicked(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://github.com/rm968211/audio-anchor") { UseShellExecute = true })?.Dispose(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "AudioAnchor", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private void CloseClicked(object sender, RoutedEventArgs e) => Close();
}
