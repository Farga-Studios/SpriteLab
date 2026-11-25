using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace SpriteLab.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        VersionText.Text = $"SpriteLab {GetVersion()} · SL0";
    }

    private static string GetVersion() =>
        typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    private void OnExit(object? sender, RoutedEventArgs e) => Close();

    private async void OnAbout(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Acerca de SpriteLab",
            Width = 420,
            Height = 220,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var close = new Button { Content = "Cerrar", HorizontalAlignment = HorizontalAlignment.Right };
        close.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 14,
            Children =
            {
                new TextBlock { Text = $"SpriteLab {GetVersion()}", FontSize = 22 },
                new TextBlock { Text = "Herramienta independiente para sprites 2D.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new TextBlock { Text = "SL0 · Interfaz inicial. Edición e importación pendientes.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                close
            }
        };
        await dialog.ShowDialog(this);
    }
}
